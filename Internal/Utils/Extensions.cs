using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Net;
using System.Net.Http;
using System.Security.Cryptography;
using System.Text;
using System.Text.RegularExpressions;
using System.Threading;
using System.Threading.Tasks;
using System.Web;
using WAX;

namespace waxnet.Internal.Utils
{
    static class Extensions
    {
        public static byte[] GetRandomByte(this int length)
        {
            var random = new Random();
            byte[] bs = new byte[length];
            for (int i = 0; i < length; i++)
            {
                bs[i] = (byte)random.Next(0, 255);
            }
            return bs;
        }
        public static long GetId(this string id)
        {
            return Convert.ToInt64(id.Replace("@s.whatsapp.net", ""));
        }

        public static string GetId(this long id)
        {
            return id.ToString() + "@s.whatsapp.net";
        }

        public static (long OwnerId, long ChatId) GetGroupId(this string id)
        {
            //79145768746-1567482853@g.us
            id = id.Replace("@g.us", "");
            var spt = id.Split(new char[] {'-'}, StringSplitOptions.RemoveEmptyEntries);
            return (Convert.ToInt64(spt[0]), Convert.ToInt64(spt[1]));
        }

        public static string GetGroupId(this long id, long ownerId)
        {
            return ownerId + "-" + id.ToString() + "@g.us";
        }

        public static string ConverFromUnicode(this string str)
        {
            return Regex.Replace(str.Replace(@"\u200e", ""), @"\\u([\da-f]{4})", m => ((char)Convert.ToInt32(m.Groups[1].Value, 16)).ToString());
        }

        public static string RegexGetString(this string str, string pattern, int returnIndex = 1)
        {
            Regex r = new Regex(pattern, RegexOptions.None);
            return r.Match(str).Groups[returnIndex].Value;
        }

        public static string UrlEncode(this string str)
        {
            return HttpUtility.UrlEncode(str);
        }

        public static long GetTimeStampLong(this DateTime dateTime)
        {
            return (dateTime.ToUniversalTime().Ticks - 621355968000000000) / 10000;
        }

        public static long GetTimeStampInt(this DateTime dateTime)
        {
            return GetTimeStampLong(dateTime) / 1000;
        }

        public static DateTime GetDateTime(this string timeStamp)
        {
            if (string.IsNullOrWhiteSpace(timeStamp))
            {
                return DateTime.MinValue;
            }
            var num = long.Parse(timeStamp);
            DateTime dtStart = TimeZoneInfo.ConvertTime(new DateTime(1970, 1, 1), TimeZoneInfo.Local);
            if (num > 9466560000)
            {
                TimeSpan toNow = new TimeSpan(num * 10000);
                return dtStart.Add(toNow);
            }
            else
            {
                TimeSpan toNow = new TimeSpan(num * 1000 * 10000);
                return dtStart.Add(toNow);
            }
        }

        public static byte[] HMACSHA256_Encrypt(this byte[] bs, byte[] key)
        {
            using (HMACSHA256 hmac = new HMACSHA256(key))
            {
                byte[] computedHash = hmac.ComputeHash(bs);
                return computedHash;
            }
        }

        public static byte[] SHA256_Encrypt(this byte[] bs)
        {
            HashAlgorithm iSha = new SHA256CryptoServiceProvider();
            return iSha.ComputeHash(bs);
        }

        public static bool ValueEquals(this byte[] bs, byte[] bs2)
        {
            if (bs.Length != bs.Length)
            {
                return false;
            }
            for (int i = 0; i < bs.Length; i++)
            {
                if (bs[i] != bs2[i])
                {
                    return false;
                }
            }
            return true;
        }

        public static string ToHexString(this byte[] bytes)
        {
            string hexString = string.Empty;
            if (bytes != null)
            {
                StringBuilder strB = new StringBuilder();
                for (int i = 0; i < bytes.Length; i++)
                {
                    strB.Append(bytes[i].ToString("X2"));
                }
                hexString = strB.ToString();
            }
            return hexString;
        }
        public static byte[] AesCbcDecrypt(this byte[] data, byte[] key, byte[] iv)
        {
            var rijndaelCipher = new RijndaelManaged
            {
                Mode = CipherMode.CBC,
                Padding = PaddingMode.PKCS7,
                KeySize = key.Length * 8,
                BlockSize = iv.Length * 8
            };
            rijndaelCipher.Key = key;
            rijndaelCipher.IV = iv;
            var transform = rijndaelCipher.CreateDecryptor();
            var plainText = transform.TransformFinalBlock(data, 0, data.Length);
            return plainText;
        }
        public static byte[] AesCbcEncrypt(this byte[] data, byte[] key, byte[] iv)
        {
            var rijndaelCipher = new RijndaelManaged
            {
                Mode = CipherMode.CBC,
                Padding = PaddingMode.PKCS7,
                KeySize = key.Length * 8,
                BlockSize = iv.Length * 8
            };
            rijndaelCipher.Key = key;
            rijndaelCipher.IV = iv;
            var transform = rijndaelCipher.CreateEncryptor();
            var plainText = transform.TransformFinalBlock(data, 0, data.Length);
            return plainText;
        }
        public static byte[] AesCbcDecrypt(this byte[] data, byte[] key)
        {
            return AesCbcDecrypt(data.Skip(16).ToArray(), key, data.Take(16).ToArray());
        }

        public static bool IsNullOrWhiteSpace(this string str)
        {
            return string.IsNullOrWhiteSpace(str);
        }

        public static async Task<MemoryStream> GetStream(this string url)
        {
            MemoryStream memory = new MemoryStream();
            HttpClientHandler Handler = new HttpClientHandler();
            using (var client = new HttpClient(Handler))
            {
                var message = new HttpRequestMessage(HttpMethod.Get, url)
                {
                    Version = HttpVersion.Version20,
                };
                message.Headers.Add("user-agent", "Mozilla/5.0 (MSIE 10.0; Windows NT 6.1; Trident/5.0)");
                var response = await client.SendAsync(message);
                if (response.IsSuccessStatusCode)
                {
                    await response.Content.CopyToAsync(memory);
                }
            }
            return memory;
        }
        public static async Task<MemoryStream> Post(this string url, byte[] data, Dictionary<string, string> head = null)
        {
            MemoryStream memory = new MemoryStream();
            HttpClientHandler Handler = new HttpClientHandler();
            using (var client = new HttpClient(Handler))
            {
                var message = new HttpRequestMessage(HttpMethod.Post, url)
                {
                    Version = HttpVersion.Version20,
                };
                message.Content = new ByteArrayContent(data);
                message.Headers.Add("user-agent", "Mozilla/5.0 (MSIE 10.0; Windows NT 6.1; Trident/5.0)");
                if (head != null)
                {
                    foreach (var item in head)
                    {
                        message.Headers.Add(item.Key, item.Value);
                    }
                }
                var response = await client.SendAsync(message);
                if (response.IsSuccessStatusCode)
                {
                    await response.Content.CopyToAsync(memory);
                }
            }
            return memory;
        }
        public static async Task<string> PostHtml(this string url, byte[] data, Dictionary<string, string> head = null, Encoding encoding = null)
        {
            var memory = await Post(url, data, head);
            if (encoding == null)
            {
                return Encoding.UTF8.GetString(memory.ToArray());
            }
            else
            {
                return encoding.GetString(memory.ToArray());
            }
        }

        public static string EncryptAES(this string plainText, string ckey, string salt = "defaultSaltKey")
        {
            var bsalt = Encoding.ASCII.GetBytes(salt);
            if (string.IsNullOrEmpty(plainText))
                throw new ArgumentNullException("invalid plain text");
            if (string.IsNullOrEmpty(ckey))
                throw new ArgumentNullException("invalid key");
            string outStr;
            RijndaelManaged aesAlg = null;
            try
            {
                var key = new Rfc2898DeriveBytes(ckey, bsalt);
                aesAlg = new RijndaelManaged();
                aesAlg.Key = key.GetBytes(aesAlg.KeySize / 8);
                ICryptoTransform encryptor = aesAlg.CreateEncryptor(aesAlg.Key, aesAlg.IV);
                using (var msEncrypt = new MemoryStream())
                {
                    msEncrypt.Write(BitConverter.GetBytes(aesAlg.IV.Length), 0, sizeof(int));
                    msEncrypt.Write(aesAlg.IV, 0, aesAlg.IV.Length);
                    using (var csEncrypt = new CryptoStream(msEncrypt, encryptor, CryptoStreamMode.Write))
                    {
                        using (var swEncrypt = new StreamWriter(csEncrypt))
                        {
                            swEncrypt.Write(plainText);
                        }
                    }
                    outStr = Convert.ToBase64String(msEncrypt.ToArray());
                }
            }
            finally
            {
                if (aesAlg != null)
                    aesAlg.Clear();
            }
            return outStr;
        }
        public static string DecryptAES(this string cipherText, string ckey, string salt = "defaultSaltKey")
        {
            var bsalt = Encoding.ASCII.GetBytes(salt);
            if (string.IsNullOrEmpty(cipherText))
                throw new ArgumentNullException("invalid cipher text");
            if (string.IsNullOrEmpty(ckey))
                throw new ArgumentNullException("invalid key");
            RijndaelManaged aesAlg = null;
            string plaintext;
            try
            {
                var key = new Rfc2898DeriveBytes(ckey, bsalt);
                byte[] bytes = Convert.FromBase64String(cipherText);
                using (var msDecrypt = new MemoryStream(bytes))
                {
                    aesAlg = new RijndaelManaged();
                    aesAlg.Key = key.GetBytes(aesAlg.KeySize / 8);
                    aesAlg.IV = ReadByteArray(msDecrypt);
                    ICryptoTransform decryptor = aesAlg.CreateDecryptor(aesAlg.Key, aesAlg.IV);
                    using (var csDecrypt = new CryptoStream(msDecrypt, decryptor, CryptoStreamMode.Read))
                    {
                        using (var srDecrypt = new StreamReader(csDecrypt))
                            plaintext = srDecrypt.ReadToEnd();
                    }
                }
            }
            finally
            {
                if (aesAlg != null)
                    aesAlg.Clear();
            }

            return plaintext;
        }
        private static byte[] ReadByteArray(Stream s)
        {
            var rawLength = new byte[sizeof(int)];
            if (s.Read(rawLength, 0, rawLength.Length) == rawLength.Length)
            {
                var buffer = new byte[BitConverter.ToInt32(rawLength, 0)];
                if (s.Read(buffer, 0, buffer.Length) != buffer.Length)
                {
                    throw new SystemException("Did not read byte array properly");
                }
                return buffer;
            }
            throw new SystemException("Stream did not contain properly formatted byte array");
        }
    }
}
