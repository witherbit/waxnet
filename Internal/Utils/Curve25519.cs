using System;
using System.Security.Cryptography;

namespace waxnet.Internal.Utils
{
    class Curve25519
    {
		public const int KeySize = 32;
		private const int _P25 = 33554431;
		private const int _P26 = 67108863;
		private static readonly byte[] _order = new byte[]
		{
			237,
			211,
			245,
			92,
			26,
			99,
			18,
			88,
			214,
			156,
			247,
			162,
			222,
			249,
			222,
			20,
			0,
			0,
			0,
			0,
			0,
			0,
			0,
			0,
			0,
			0,
			0,
			0,
			0,
			0,
			0,
			16
		};
		private static readonly byte[] _orderTimes8 = new byte[]
		{
			104,
			159,
			174,
			231,
			210,
			24,
			147,
			192,
			178,
			230,
			188,
			23,
			245,
			206,
			247,
			166,
			0,
			0,
			0,
			0,
			0,
			0,
			0,
			0,
			0,
			0,
			0,
			0,
			0,
			0,
			0,
			128
		};
		public static void ClampPrivateKeyInline(byte[] key)
		{
			if (key == null)
			{
				throw new ArgumentNullException("key");
			}
			if (key.Length != 32)
			{
				throw new ArgumentException(string.Format("key must be 32 bytes long (but was {0} bytes long)", key.Length));
			}
			int num = 31;
			key[num] &= 127;
			int num2 = 31;
			key[num2] |= 64;
			int num3 = 0;
			key[num3] &= 248;
		}
		public static byte[] ClampPrivateKey(byte[] rawKey)
		{
			if (rawKey == null)
			{
				throw new ArgumentNullException("rawKey");
			}
			if (rawKey.Length != 32)
			{
				throw new ArgumentException(string.Format("rawKey must be 32 bytes long (but was {0} bytes long)", rawKey.Length), "rawKey");
			}
			byte[] array = new byte[32];
			Array.Copy(rawKey, array, 32);
			byte[] array2 = array;
			int num = 31;
			array2[num] &= 127;
			byte[] array3 = array;
			int num2 = 31;
			array3[num2] |= 64;
			byte[] array4 = array;
			int num3 = 0;
			array4[num3] &= 248;
			return array;
		}
		public static byte[] CreateRandomPrivateKey()
		{
			byte[] array = new byte[32];
			RandomNumberGenerator.Create().GetBytes(array);
			ClampPrivateKeyInline(array);
			return array;
		}
		public static void KeyGenInline(byte[] publicKey, byte[] signingKey, byte[] privateKey)
		{
			ClampPrivateKeyInline(privateKey);
			Core(publicKey, signingKey, privateKey, null);
		}
		public static byte[] GetPublicKey(byte[] privateKey)
		{
			byte[] array = new byte[32];
			Core(array, null, CreateRandomPrivateKey(), null);
			Core(array, null, privateKey, null);
			return array;
		}
		public static byte[] GetSigningKey(byte[] privateKey)
		{
			byte[] array = new byte[32];
			byte[] publicKey = new byte[32];
			Core(publicKey, array, CreateRandomPrivateKey(), null);
			Core(publicKey, array, privateKey, null);
			return array;
		}
		public static byte[] GetSharedSecret(byte[] privateKey, byte[] peerPublicKey)
		{
			byte[] array = new byte[32];
			Core(array, null, CreateRandomPrivateKey(), peerPublicKey);
			Core(array, null, privateKey, peerPublicKey);
			return array;
		}
		private static void Copy32(byte[] source, byte[] destination)
		{
			Array.Copy(source, 0, destination, 0, 32);
		}
		private static int MultiplyArraySmall(byte[] p, byte[] q, int m, byte[] x, int n, int z)
		{
			int num = 0;
			for (int i = 0; i < n; i++)
			{
				num += (int)(q[i + m] & byte.MaxValue) + z * (int)(x[i] & byte.MaxValue);
				p[i + m] = (byte)num;
				num >>= 8;
			}
			return num;
		}
		private static void MultiplyArray32(byte[] p, byte[] x, byte[] y, int t, int z)
		{
			int num = 0;
			int i;
			for (i = 0; i < t; i++)
			{
				int num2 = z * (int)(y[i] & byte.MaxValue);
				num += MultiplyArraySmall(p, p, i, x, 31, num2) + (int)(p[i + 31] & byte.MaxValue) + num2 * (int)(x[31] & byte.MaxValue);
				p[i + 31] = (byte)num;
				num >>= 8;
			}
			p[i + 31] = (byte)(num + (int)(p[i + 31] & byte.MaxValue));
		}
		private static void DivMod(byte[] q, byte[] r, int n, byte[] d, int t)
		{
			int num = 0;
			int num2 = (int)(d[t - 1] & byte.MaxValue) << 8;
			if (t > 1)
			{
				num2 |= (int)(d[t - 2] & byte.MaxValue);
			}
			while (n-- >= t)
			{
				int num3 = num << 16 | (int)(r[n] & byte.MaxValue) << 8;
				if (n > 0)
				{
					num3 |= (int)(r[n - 1] & byte.MaxValue);
				}
				num3 /= num2;
				num += MultiplyArraySmall(r, r, n - t + 1, d, t, -num3);
				q[n - t + 1] = (byte)(num3 + num & 255);
				MultiplyArraySmall(r, r, n - t + 1, d, t, -num);
				num = (int)(r[n] & byte.MaxValue);
				r[n] = 0;
			}
			r[t - 1] = (byte)num;
		}
		private static int GetNumSize(byte[] num, int maxSize)
		{
			for (int i = maxSize; i >= 0; i++)
			{
				if (num[i] == 0)
				{
					return i + 1;
				}
			}
			return 0;
		}
		private static byte[] Egcd32(byte[] x, byte[] y, byte[] a, byte[] b)
		{
			int num = 32;
			for (int i = 0; i < 32; i++)
			{
				x[i] = (y[i] = 0);
			}
			x[0] = 1;
			int numSize = GetNumSize(a, 32);
			if (numSize == 0)
			{
				return y;
			}
			byte[] array = new byte[32];
			for (; ; )
			{
				int t = num - numSize + 1;
				DivMod(array, b, num, a, numSize);
				num = GetNumSize(b, num);
				if (num == 0)
				{
					break;
				}
				MultiplyArray32(y, x, array, t, -1);
				t = numSize - num + 1;
				DivMod(array, a, numSize, b, num);
				numSize = GetNumSize(a, numSize);
				if (numSize == 0)
				{
					return y;
				}
				MultiplyArray32(x, y, array, t, -1);
			}
			return x;
		}
		private static void Unpack(LongContainer x, byte[] m)
		{
			x.L0 = (long)((int)(m[0] & byte.MaxValue) | (int)(m[1] & byte.MaxValue) << 8 | (int)(m[2] & byte.MaxValue) << 16 | (int)(m[3] & byte.MaxValue & 3) << 24);
			x.L1 = (long)(((int)(m[3] & byte.MaxValue) & -4) >> 2 | (int)(m[4] & byte.MaxValue) << 6 | (int)(m[5] & byte.MaxValue) << 14 | (int)(m[6] & byte.MaxValue & 7) << 22);
			x.L2 = (long)(((int)(m[6] & byte.MaxValue) & -8) >> 3 | (int)(m[7] & byte.MaxValue) << 5 | (int)(m[8] & byte.MaxValue) << 13 | (int)(m[9] & byte.MaxValue & 31) << 21);
			x.L3 = (long)(((int)(m[9] & byte.MaxValue) & -32) >> 5 | (int)(m[10] & byte.MaxValue) << 3 | (int)(m[11] & byte.MaxValue) << 11 | (int)(m[12] & byte.MaxValue & 63) << 19);
			x.L4 = (long)(((int)(m[12] & byte.MaxValue) & -64) >> 6 | (int)(m[13] & byte.MaxValue) << 2 | (int)(m[14] & byte.MaxValue) << 10 | (int)(m[15] & byte.MaxValue) << 18);
			x.L5 = (long)((int)(m[16] & byte.MaxValue) | (int)(m[17] & byte.MaxValue) << 8 | (int)(m[18] & byte.MaxValue) << 16 | (int)(m[19] & byte.MaxValue & 1) << 24);
			x.L6 = (long)(((int)(m[19] & byte.MaxValue) & -2) >> 1 | (int)(m[20] & byte.MaxValue) << 7 | (int)(m[21] & byte.MaxValue) << 15 | (int)(m[22] & byte.MaxValue & 7) << 23);
			x.L7 = (long)(((int)(m[22] & byte.MaxValue) & -8) >> 3 | (int)(m[23] & byte.MaxValue) << 5 | (int)(m[24] & byte.MaxValue) << 13 | (int)(m[25] & byte.MaxValue & 15) << 21);
			x.L8 = (long)(((int)(m[25] & byte.MaxValue) & -16) >> 4 | (int)(m[26] & byte.MaxValue) << 4 | (int)(m[27] & byte.MaxValue) << 12 | (int)(m[28] & byte.MaxValue & 63) << 20);
			x.L9 = (long)(((int)(m[28] & byte.MaxValue) & -64) >> 6 | (int)(m[29] & byte.MaxValue) << 2 | (int)(m[30] & byte.MaxValue) << 10 | (int)(m[31] & byte.MaxValue) << 18);
		}
		private static bool IsOverflow(LongContainer x)
		{
			return (x.L0 > 67108844L && (x.L1 & x.L3 & x.L5 & x.L7 & x.L9) == 33554431L && (x.L2 & x.L4 & x.L6 & x.L8) == 67108863L) || x.L9 > 33554431L;
		}
		private static void Pack(LongContainer x, byte[] m)
		{
			int num = (IsOverflow(x) ? 1 : 0) - ((x.L9 < 0L) ? 1 : 0);
			int num2 = num * -33554432;
			num *= 19;
			long num3 = (long)num + x.L0 + (x.L1 << 26);
			m[0] = (byte)num3;
			m[1] = (byte)(num3 >> 8);
			m[2] = (byte)(num3 >> 16);
			m[3] = (byte)(num3 >> 24);
			num3 = (num3 >> 32) + (x.L2 << 19);
			m[4] = (byte)num3;
			m[5] = (byte)(num3 >> 8);
			m[6] = (byte)(num3 >> 16);
			m[7] = (byte)(num3 >> 24);
			num3 = (num3 >> 32) + (x.L3 << 13);
			m[8] = (byte)num3;
			m[9] = (byte)(num3 >> 8);
			m[10] = (byte)(num3 >> 16);
			m[11] = (byte)(num3 >> 24);
			num3 = (num3 >> 32) + (x.L4 << 6);
			m[12] = (byte)num3;
			m[13] = (byte)(num3 >> 8);
			m[14] = (byte)(num3 >> 16);
			m[15] = (byte)(num3 >> 24);
			num3 = (num3 >> 32) + x.L5 + (x.L6 << 25);
			m[16] = (byte)num3;
			m[17] = (byte)(num3 >> 8);
			m[18] = (byte)(num3 >> 16);
			m[19] = (byte)(num3 >> 24);
			num3 = (num3 >> 32) + (x.L7 << 19);
			m[20] = (byte)num3;
			m[21] = (byte)(num3 >> 8);
			m[22] = (byte)(num3 >> 16);
			m[23] = (byte)(num3 >> 24);
			num3 = (num3 >> 32) + (x.L8 << 12);
			m[24] = (byte)num3;
			m[25] = (byte)(num3 >> 8);
			m[26] = (byte)(num3 >> 16);
			m[27] = (byte)(num3 >> 24);
			num3 = (num3 >> 32) + (x.L9 + (long)num2 << 6);
			m[28] = (byte)num3;
			m[29] = (byte)(num3 >> 8);
			m[30] = (byte)(num3 >> 16);
			m[31] = (byte)(num3 >> 24);
		}
		private static void Copy(LongContainer numOut, LongContainer numIn)
		{
			numOut.L0 = numIn.L0;
			numOut.L1 = numIn.L1;
			numOut.L2 = numIn.L2;
			numOut.L3 = numIn.L3;
			numOut.L4 = numIn.L4;
			numOut.L5 = numIn.L5;
			numOut.L6 = numIn.L6;
			numOut.L7 = numIn.L7;
			numOut.L8 = numIn.L8;
			numOut.L9 = numIn.L9;
		}
		private static void Set(LongContainer numOut, int numIn)
		{
			numOut.L0 = (long)numIn;
			numOut.L1 = 0L;
			numOut.L2 = 0L;
			numOut.L3 = 0L;
			numOut.L4 = 0L;
			numOut.L5 = 0L;
			numOut.L6 = 0L;
			numOut.L7 = 0L;
			numOut.L8 = 0L;
			numOut.L9 = 0L;
		}
		private static void Add(LongContainer xy, LongContainer x, LongContainer y)
		{
			xy.L0 = x.L0 + y.L0;
			xy.L1 = x.L1 + y.L1;
			xy.L2 = x.L2 + y.L2;
			xy.L3 = x.L3 + y.L3;
			xy.L4 = x.L4 + y.L4;
			xy.L5 = x.L5 + y.L5;
			xy.L6 = x.L6 + y.L6;
			xy.L7 = x.L7 + y.L7;
			xy.L8 = x.L8 + y.L8;
			xy.L9 = x.L9 + y.L9;
		}
		private static void Sub(LongContainer xy, LongContainer x, LongContainer y)
		{
			xy.L0 = x.L0 - y.L0;
			xy.L1 = x.L1 - y.L1;
			xy.L2 = x.L2 - y.L2;
			xy.L3 = x.L3 - y.L3;
			xy.L4 = x.L4 - y.L4;
			xy.L5 = x.L5 - y.L5;
			xy.L6 = x.L6 - y.L6;
			xy.L7 = x.L7 - y.L7;
			xy.L8 = x.L8 - y.L8;
			xy.L9 = x.L9 - y.L9;
		}
		private static void MulSmall(LongContainer xy, LongContainer x, long y)
		{
			long num = x.L8 * y;
			xy.L8 = (num & 67108863L);
			num = (num >> 26) + x.L9 * y;
			xy.L9 = (num & 33554431L);
			num = 19L * (num >> 25) + x.L0 * y;
			xy.L0 = (num & 67108863L);
			num = (num >> 26) + x.L1 * y;
			xy.L1 = (num & 33554431L);
			num = (num >> 25) + x.L2 * y;
			xy.L2 = (num & 67108863L);
			num = (num >> 26) + x.L3 * y;
			xy.L3 = (num & 33554431L);
			num = (num >> 25) + x.L4 * y;
			xy.L4 = (num & 67108863L);
			num = (num >> 26) + x.L5 * y;
			xy.L5 = (num & 33554431L);
			num = (num >> 25) + x.L6 * y;
			xy.L6 = (num & 67108863L);
			num = (num >> 26) + x.L7 * y;
			xy.L7 = (num & 33554431L);
			num = (num >> 25) + xy.L8;
			xy.L8 = (num & 67108863L);
			xy.L9 += num >> 26;
		}
		private static void Multiply(LongContainer xy, LongContainer x, LongContainer y)
		{
			long n = x.L0;
			long n2 = x.L1;
			long n3 = x.L2;
			long n4 = x.L3;
			long n5 = x.L4;
			long n6 = x.L5;
			long n7 = x.L6;
			long n8 = x.L7;
			long n9 = x.L8;
			long n10 = x.L9;
			long n11 = y.L0;
			long n12 = y.L1;
			long n13 = y.L2;
			long n14 = y.L3;
			long n15 = y.L4;
			long n16 = y.L5;
			long n17 = y.L6;
			long n18 = y.L7;
			long n19 = y.L8;
			long n20 = y.L9;
			long num = n * n19 + n3 * n17 + n5 * n15 + n7 * n13 + n9 * n11 + 2L * (n2 * n18 + n4 * n16 + n6 * n14 + n8 * n12) + 38L * (n10 * n20);
			xy.L8 = (num & 67108863L);
			num = (num >> 26) + n * n20 + n2 * n19 + n3 * n18 + n4 * n17 + n5 * n16 + n6 * n15 + n7 * n14 + n8 * n13 + n9 * n12 + n10 * n11;
			xy.L9 = (num & 33554431L);
			num = n * n11 + 19L * ((num >> 25) + n3 * n19 + n5 * n17 + n7 * n15 + n9 * n13) + 38L * (n2 * n20 + n4 * n18 + n6 * n16 + n8 * n14 + n10 * n12);
			xy.L0 = (num & 67108863L);
			num = (num >> 26) + n * n12 + n2 * n11 + 19L * (n3 * n20 + n4 * n19 + n5 * n18 + n6 * n17 + n7 * n16 + n8 * n15 + n9 * n14 + n10 * n13);
			xy.L1 = (num & 33554431L);
			num = (num >> 25) + n * n13 + n3 * n11 + 19L * (n5 * n19 + n7 * n17 + n9 * n15) + 2L * (n2 * n12) + 38L * (n4 * n20 + n6 * n18 + n8 * n16 + n10 * n14);
			xy.L2 = (num & 67108863L);
			num = (num >> 26) + n * n14 + n2 * n13 + n3 * n12 + n4 * n11 + 19L * (n5 * n20 + n6 * n19 + n7 * n18 + n8 * n17 + n9 * n16 + n10 * n15);
			xy.L3 = (num & 33554431L);
			num = (num >> 25) + n * n15 + n3 * n13 + n5 * n11 + 19L * (n7 * n19 + n9 * n17) + 2L * (n2 * n14 + n4 * n12) + 38L * (n6 * n20 + n8 * n18 + n10 * n16);
			xy.L4 = (num & 67108863L);
			num = (num >> 26) + n * n16 + n2 * n15 + n3 * n14 + n4 * n13 + n5 * n12 + n6 * n11 + 19L * (n7 * n20 + n8 * n19 + n9 * n18 + n10 * n17);
			xy.L5 = (num & 33554431L);
			num = (num >> 25) + n * n17 + n3 * n15 + n5 * n13 + n7 * n11 + 19L * (n9 * n19) + 2L * (n2 * n16 + n4 * n14 + n6 * n12) + 38L * (n8 * n20 + n10 * n18);
			xy.L6 = (num & 67108863L);
			num = (num >> 26) + n * n18 + n2 * n17 + n3 * n16 + n4 * n15 + n5 * n14 + n6 * n13 + n7 * n12 + n8 * n11 + 19L * (n9 * n20 + n10 * n19);
			xy.L7 = (num & 33554431L);
			num = (num >> 25) + xy.L8;
			xy.L8 = (num & 67108863L);
			xy.L9 += num >> 26;
		}
		private static void Square(LongContainer xsqr, LongContainer x)
		{
			long n = x.L0;
			long n2 = x.L1;
			long n3 = x.L2;
			long n4 = x.L3;
			long n5 = x.L4;
			long n6 = x.L5;
			long n7 = x.L6;
			long n8 = x.L7;
			long n9 = x.L8;
			long n10 = x.L9;
			long num = n5 * n5 + 2L * (n * n9 + n3 * n7) + 38L * (n10 * n10) + 4L * (n2 * n8 + n4 * n6);
			xsqr.L8 = (num & 67108863L);
			num = (num >> 26) + 2L * (n * n10 + n2 * n9 + n3 * n8 + n4 * n7 + n5 * n6);
			xsqr.L9 = (num & 33554431L);
			num = 19L * (num >> 25) + n * n + 38L * (n3 * n9 + n5 * n7 + n6 * n6) + 76L * (n2 * n10 + n4 * n8);
			xsqr.L0 = (num & 67108863L);
			num = (num >> 26) + 2L * (n * n2) + 38L * (n3 * n10 + n4 * n9 + n5 * n8 + n6 * n7);
			xsqr.L1 = (num & 33554431L);
			num = (num >> 25) + 19L * (n7 * n7) + 2L * (n * n3 + n2 * n2) + 38L * (n5 * n9) + 76L * (n4 * n10 + n6 * n8);
			xsqr.L2 = (num & 67108863L);
			num = (num >> 26) + 2L * (n * n4 + n2 * n3) + 38L * (n5 * n10 + n6 * n9 + n7 * n8);
			xsqr.L3 = (num & 33554431L);
			num = (num >> 25) + n3 * n3 + 2L * (n * n5) + 38L * (n7 * n9 + n8 * n8) + 4L * (n2 * n4) + 76L * (n6 * n10);
			xsqr.L4 = (num & 67108863L);
			num = (num >> 26) + 2L * (n * n6 + n2 * n5 + n3 * n4) + 38L * (n7 * n10 + n8 * n9);
			xsqr.L5 = (num & 33554431L);
			num = (num >> 25) + 19L * (n9 * n9) + 2L * (n * n7 + n3 * n5 + n4 * n4) + 4L * (n2 * n6) + 76L * (n8 * n10);
			xsqr.L6 = (num & 67108863L);
			num = (num >> 26) + 2L * (n * n8 + n2 * n7 + n3 * n6 + n4 * n5) + 38L * (n9 * n10);
			xsqr.L7 = (num & 33554431L);
			num = (num >> 25) + xsqr.L8;
			xsqr.L8 = (num & 67108863L);
			xsqr.L9 += num >> 26;
		}
		private static void Reciprocal(LongContainer y, LongContainer x, bool sqrtAssist)
		{
			LongContainer @long = new LongContainer();
			LongContainer long2 = new LongContainer();
			LongContainer long3 = new LongContainer();
			LongContainer long4 = new LongContainer();
			LongContainer long5 = new LongContainer();
			Square(long2, x);
			Square(long3, long2);
			Square(@long, long3);
			Multiply(long3, @long, x);
			Multiply(@long, long3, long2);
			Square(long2, @long);
			Multiply(long4, long2, long3);
			Square(long2, long4);
			Square(long3, long2);
			Square(long2, long3);
			Square(long3, long2);
			Square(long2, long3);
			Multiply(long3, long2, long4);
			Square(long2, long3);
			Square(long4, long2);
			for (int i = 1; i < 5; i++)
			{
				Square(long2, long4);
				Square(long4, long2);
			}
			Multiply(long2, long4, long3);
			Square(long4, long2);
			Square(long5, long4);
			for (int i = 1; i < 10; i++)
			{
				Square(long4, long5);
				Square(long5, long4);
			}
			Multiply(long4, long5, long2);
			for (int i = 0; i < 5; i++)
			{
				Square(long2, long4);
				Square(long4, long2);
			}
			Multiply(long2, long4, long3);
			Square(long3, long2);
			Square(long4, long3);
			for (int i = 1; i < 25; i++)
			{
				Square(long3, long4);
				Square(long4, long3);
			}
			Multiply(long3, long4, long2);
			Square(long4, long3);
			Square(long5, long4);
			for (int i = 1; i < 50; i++)
			{
				Square(long4, long5);
				Square(long5, long4);
			}
			Multiply(long4, long5, long3);
			for (int i = 0; i < 25; i++)
			{
				Square(long5, long4);
				Square(long4, long5);
			}
			Multiply(long3, long4, long2);
			Square(long2, long3);
			Square(long3, long2);
			if (sqrtAssist)
			{
				Multiply(y, x, long3);
				return;
			}
			Square(long2, long3);
			Square(long3, long2);
			Square(long2, long3);
			Multiply(y, long2, @long);
		}
		private static int IsNegative(LongContainer x)
		{
			return (int)(((IsOverflow(x) || x.L9 < 0L) ? 1L : 0L) ^ (x.L0 & 1L));
		}
		private static void MontyPrepare(LongContainer t1, LongContainer t2, LongContainer ax, LongContainer az)
		{
			Add(t1, ax, az);
			Sub(t2, ax, az);
		}
		private static void MontyAdd(LongContainer t1, LongContainer t2, LongContainer t3, LongContainer t4, LongContainer ax, LongContainer az, LongContainer dx)
		{
			Multiply(ax, t2, t3);
			Multiply(az, t1, t4);
			Add(t1, ax, az);
			Sub(t2, ax, az);
			Square(ax, t1);
			Square(t1, t2);
			Multiply(az, t1, dx);
		}
		private static void MontyDouble(LongContainer t1, LongContainer t2, LongContainer t3, LongContainer t4, LongContainer bx, LongContainer bz)
		{
			Square(t1, t3);
			Square(t2, t4);
			Multiply(bx, t1, t2);
			Sub(t2, t1, t2);
			MulSmall(bz, t2, 121665L);
			Add(t1, t1, bz);
			Multiply(bz, t1, t2);
		}
		private static void CurveEquationInline(LongContainer y2, LongContainer x, LongContainer temp)
		{
			Square(temp, x);
			MulSmall(y2, x, 486662L);
			Add(temp, temp, y2);
			temp.L0 += 1L;
			Multiply(y2, temp, x);
		}
		private static void Core(byte[] publicKey, byte[] signingKey, byte[] privateKey, byte[] peerPublicKey)
		{
			if (publicKey == null)
			{
				throw new ArgumentNullException("publicKey");
			}
			if (publicKey.Length != 32)
			{
				throw new ArgumentException(string.Format("publicKey must be 32 bytes long (but was {0} bytes long)", publicKey.Length), "publicKey");
			}
			if (signingKey != null && signingKey.Length != 32)
			{
				throw new ArgumentException(string.Format("signingKey must be null or 32 bytes long (but was {0} bytes long)", signingKey.Length), "signingKey");
			}
			if (privateKey == null)
			{
				throw new ArgumentNullException("privateKey");
			}
			if (privateKey.Length != 32)
			{
				throw new ArgumentException(string.Format("privateKey must be 32 bytes long (but was {0} bytes long)", privateKey.Length), "privateKey");
			}
			if (peerPublicKey != null && peerPublicKey.Length != 32)
			{
				throw new ArgumentException(string.Format("peerPublicKey must be null or 32 bytes long (but was {0} bytes long)", peerPublicKey.Length), "peerPublicKey");
			}
			LongContainer @long = new LongContainer();
			LongContainer long2 = new LongContainer();
			LongContainer long3 = new LongContainer();
			LongContainer long4 = new LongContainer();
			LongContainer long5 = new LongContainer();
			LongContainer[] array = new LongContainer[]
			{
				new LongContainer(),
				new LongContainer()
			};
			LongContainer[] array2 = new LongContainer[]
			{
				new LongContainer(),
				new LongContainer()
			};
			if (peerPublicKey != null)
			{
				Unpack(@long, peerPublicKey);
			}
			else
			{
				Set(@long, 9);
			}
			Set(array[0], 1);
			Set(array2[0], 0);
			Copy(array[1], @long);
			Set(array2[1], 1);
			int num = 32;
			while (num-- != 0)
			{
				if (num == 0)
				{
					num = 0;
				}
				int num2 = 8;
				while (num2-- != 0)
				{
					int num3 = (privateKey[num] & byte.MaxValue) >> num2 & 1;
					int num4 = ~(privateKey[num] & byte.MaxValue) >> num2 & 1;
					LongContainer ax = array[num4];
					LongContainer az = array2[num4];
					LongContainer long6 = array[num3];
					LongContainer long7 = array2[num3];
					MontyPrepare(long2, long3, ax, az);
					MontyPrepare(long4, long5, long6, long7);
					MontyAdd(long2, long3, long4, long5, ax, az, @long);
					MontyDouble(long2, long3, long4, long5, long6, long7);
				}
			}
			Reciprocal(long2, array2[0], false);
			Multiply(@long, array[0], long2);
			Pack(@long, publicKey);
			if (signingKey != null)
			{
				CurveEquationInline(long2, @long, long3);
				Reciprocal(long4, array2[1], false);
				Multiply(long3, array[1], long4);
				Add(long3, long3, @long);
				long3.L0 += 486671L;
				@long.L0 -= 9L;
				Square(long4, @long);
				Multiply(@long, long3, long4);
				Sub(@long, @long, long2);
				@long.L0 -= 39420360L;
				Multiply(long2, @long, _baseR2Y);
				if (IsNegative(long2) != 0)
				{
					Copy32(privateKey, signingKey);
				}
				else
				{
					MultiplyArraySmall(signingKey, _orderTimes8, 0, privateKey, 32, -1);
				}
				byte[] array3 = new byte[32];
				byte[] x = new byte[64];
				byte[] y = new byte[64];
				Copy32(_order, array3);
				Copy32(Egcd32(x, y, signingKey, array3), signingKey);
				if ((signingKey[31] & 128) != 0)
				{
					MultiplyArraySmall(signingKey, signingKey, 0, _order, 32, 1);
				}
			}
		}
		private static readonly LongContainer _baseR2Y = new LongContainer(5744L, 8160848L, 4790893L, 13779497L, 35730846L, 12541209L, 49101323L, 30047407L, 40071253L, 6226132L);
		private sealed class LongContainer
		{
			public LongContainer()
			{
			}
			public LongContainer(long n0, long n1, long n2, long n3, long n4, long n5, long n6, long n7, long n8, long n9)
			{
				L0 = n0;
				L1 = n1;
				L2 = n2;
				L3 = n3;
				L4 = n4;
				L5 = n5;
				L6 = n6;
				L7 = n7;
				L8 = n8;
				L9 = n9;
			}
			public long L0;
			public long L1;
			public long L2;
			public long L3;
			public long L4;
			public long L5;
			public long L6;
			public long L7;
			public long L8;
			public long L9;
		}
	}
}
