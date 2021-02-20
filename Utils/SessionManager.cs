using Newtonsoft.Json;
using System;
using System.Collections.Generic;
using System.IO;
using System.Text;
using WAX.Models;

namespace WAX.Utils
{
    public class SessionManager
    {
        public static void Write(Session session)
        {
            File.WriteAllText("session.bin", JsonConvert.SerializeObject(session));
        }

        public static Session Read()
        {
            if (File.Exists("session.bin"))
            {
                return JsonConvert.DeserializeObject<Session>(File.ReadAllText("session.bin"));
            }
            return null;
        }
    }
}
