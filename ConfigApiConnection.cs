using Newtonsoft.Json.Linq;

using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace WAX
{
    static class ConfigApiConnection
    {
        internal static string Version = "[2,2147,16]";
        internal static string ShortUA = "[\"Windows\",\"Chrome\",\"10\"]";
        internal static string IV = "aKs1sBxLFMBHVkUQwS/YEg==";
        public static void LoadCustom(string path)
        {
            try
            {
                var json = JObject.Parse(File.ReadAllText(path));
                Version = json["Version"].ToString();
                ShortUA = json["ShortUA"].ToString();
                IV = json["IV"].ToString();
            }
            catch (Exception ex)
            {
            }
        }
    }
}
