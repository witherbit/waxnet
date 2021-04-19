using Newtonsoft.Json;
using System.IO;
using WAX.Models;

namespace WAX.Utils
{
    public class SessionManager
    {
        public static string FilePath { get; set; } = "session";

        public static string Salt { private get; set; }

        public static string Key { private get; set; }

        public static void Write(Session session)
        {
            File.WriteAllText($"{FilePath}.waxs", JsonConvert.SerializeObject(session).EncryptAES(Key, Salt));
            Key = string.Empty;
            Salt = string.Empty;
        }

        public static Session Read()
        {
            if (File.Exists($"{FilePath}.waxs"))
            {
                var session = JsonConvert.DeserializeObject<Session>(File.ReadAllText($"{FilePath}.waxs").DecryptAES(Key, Salt));
                Key = string.Empty;
                Salt = string.Empty;
                return session;
            }
            return null;
        }
    }
}
