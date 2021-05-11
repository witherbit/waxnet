using Newtonsoft.Json;
using System;
using System.Collections.Generic;
using System.IO;
using System.Text;
using WAX;
using WAX.Models;
using waxnet.Internal.Models;
using waxnet.Internal.Utils;

namespace waxnet.Internal.Core
{
    class SessionManager
    {
        public Session Session { get; set; }

        public SessionManagerParameters _parameters;
        public SessionManager(SessionManagerParameters parameters)
        {
            _parameters = parameters;
        }

        public SessionManager()
        {
        }

        public void Save()
        {
            try
            {
                if (Session != null)
                    File.WriteAllText($"{_parameters.FilePath}.waxs", JsonConvert.SerializeObject(Session).EncryptAES(_parameters.Key, _parameters.Salt));
                else Api.CallException(null, new Exception("The session object was null! Unable to save session."));
            }
            catch (Exception e)
            {
                Api.CallException(null, e);
            }
            
        }

        public void Read()
        {
            try
            {
                Session = JsonConvert.DeserializeObject<Session>(File.ReadAllText($"{_parameters.FilePath}.waxs").DecryptAES(_parameters.Key, _parameters.Salt));
            }
            catch
            {
                Api.CallException(null, new Exception($"The session file does not exist. Check the file path!"));
            }
        }
    }
}
