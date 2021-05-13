using System;
using System.Security.Cryptography;

namespace waxnet.Internal.Utils
{
    class Hkdf : IDisposable
    {
		public int HashLength { get; }
		private HMAC _hmac;
		private HashAlgorithmName _algorithm;
		private byte[] _info;
		private bool _disposed = false;
		public Hkdf(HashAlgorithmName hashAlgorithm)
		{
			string name = hashAlgorithm.Name;
			if (!(name == "MD5"))
			{
				if (!(name == "SHA1"))
				{
					if (!(name == "SHA256"))
					{
						if (!(name == "SHA384"))
						{
							if (!(name == "SHA512"))
							{
								throw new NotSupportedException(string.Format("The hash algorithm {0} is not supported.", hashAlgorithm));
							}
							HashLength = 64;
						}
						else
						{
							HashLength = 48;
						}
					}
					else
					{
						HashLength = 32;
					}
				}
				else
				{
					HashLength = 20;
				}
			}
			else
			{
				HashLength = 16;
			}
			_algorithm = hashAlgorithm;
		}
		public byte[] Extract(byte[] ikm, byte[] salt = null)
		{
			bool flag = ikm == null;
			if (flag)
			{
				throw new ArgumentNullException("ikm");
			}
			bool flag2 = salt == null;
			if (flag2)
			{
				salt = new byte[HashLength];
			}
			bool disposed = _disposed;
			if (disposed)
			{
				throw new ObjectDisposedException(base.GetType().FullName);
			}
			InitializeHMAC(salt);
			return _hmac.ComputeHash(ikm);
		}
		public byte[] Expand(byte[] prk, int length, byte[] info = null)
		{
			bool flag = prk == null;
			if (flag)
			{
				throw new ArgumentNullException("prk");
			}
			bool flag2 = prk.Length < HashLength;
			if (flag2)
			{
				throw new ArgumentException(string.Format("The length of prk must be equal or greater than {0} octets.", HashLength), "prk");
			}
			bool flag3 = length < 0 || length > 255 * HashLength;
			if (flag3)
			{
				throw new ArgumentOutOfRangeException("length");
			}
			bool disposed = _disposed;
			if (disposed)
			{
				throw new ObjectDisposedException(base.GetType().FullName);
			}
			bool flag4 = length == 0;
			byte[] result;
			if (flag4)
			{
				result = Array.Empty<byte>();
			}
			else
			{
				bool flag5 = info == null;
				if (flag5)
				{
					info = Array.Empty<byte>();
				}
				bool flag6 = _info == null || _info.Length != HashLength + info.Length + 1;
				if (flag6)
				{
					_info = new byte[HashLength + info.Length + 1];
				}
				InitializeHMAC(prk);
				byte[] array = new byte[length];
				int num = 0;
				int num2 = 1;
				int num3 = HashLength;
				Array.Copy(info, 0, _info, HashLength, info.Length);
				for (; ; )
				{
					_info[HashLength + info.Length] = (byte)num2++;
					byte[] sourceArray = _hmac.ComputeHash(_info, num3, _info.Length - num3);
					num3 = 0;
					Array.Copy(sourceArray, 0, array, num, Math.Min(array.Length - num, HashLength));
					bool flag7 = (num += HashLength) >= length;
					if (flag7)
					{
						break;
					}
					Array.Copy(sourceArray, 0, _info, 0, HashLength);
				}
				result = array;
			}
			return result;
		}
		private void InitializeHMAC(byte[] key)
		{
			bool flag = _hmac != null;
			if (flag)
			{
				_hmac.Key = key;
			}
			else
			{
				string name = _algorithm.Name;
				if (!(name == "MD5"))
				{
					if (!(name == "SHA1"))
					{
						if (!(name == "SHA256"))
						{
							if (!(name == "SHA384"))
							{
								if (name == "SHA512")
								{
									_hmac = new HMACSHA512(key);
								}
							}
							else
							{
								_hmac = new HMACSHA384(key);
							}
						}
						else
						{
							_hmac = new HMACSHA256(key);
						}
					}
					else
					{
						_hmac = new HMACSHA1(key);
					}
				}
				else
				{
					_hmac = new HMACMD5(key);
				}
			}
		}
		public void Dispose()
		{
			bool disposed = _disposed;
			if (!disposed)
			{
				bool flag = _hmac != null;
				if (flag)
				{
					_hmac.Dispose();
				}
				bool flag2 = _info != null;
				if (flag2)
				{
					Array.Clear(_info, 0, _info.Length);
					_info = null;
				}
				_disposed = true;
			}
		}
	}
}
