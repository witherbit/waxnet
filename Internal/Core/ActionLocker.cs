using System;
using System.Collections.Generic;
using System.Text;
using WAX;

namespace waxnet.Internal.Core
{
    static class ActionLocker
    {
        public static bool CheckLock(this Api api)
        {
            if (api.IsAuthorized) return true;
            else
            {
                Api.CallException(api, new Exception("The action cannot be performed because the user is not logged in!"));
                return false;
            }
        }

        public static bool CheckLicense(this Api api, bool checkTrial = false)
        {
            var flag = !api.Engine.ServiceKeyManager.Info.Active;
            if (flag) api.Stop();
            if (checkTrial && api.Engine.ServiceKeyManager.Info.Trial) flag = true;
            return flag;
        }
        public static bool CheckLicense(this Engine eng, bool checkTrial = false)
        {
            var flag = !eng.ServiceKeyManager.Info.Active;
            if (flag) eng.Stop();
            if (checkTrial && eng.ServiceKeyManager.Info.Trial) flag = true;
            return flag;
        }
    }
}
