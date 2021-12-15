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
    }
}
