using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using WAX;
using waxnet.Internal.Models;

namespace waxnet.Internal.Core
{
    class ReceiveManager : Dictionary<string, ReceiveModel>
    {
        private async void Add(string tag, int timeout)
        {
            try
            {
                Add(tag, null);
                await Task.Run(() =>
                {
                    Task.Delay(timeout + 500).Wait();
                    Remove(tag);
                });
            }
            catch
            {

            }
            
        }
        public ReceiveModel WaitResult(string tag, int timeout = 5000, short ignoreCount = 0)
        {
            Add(tag, timeout);
            var time = DateTime.Now;
            while ((DateTime.Now - time).TotalMilliseconds <= timeout)
            {
                if (this[tag] != null)
                {
                    if (ignoreCount-- <= 0)
                        return this[tag];
                    this[tag] = null;
                }
            }
            return null;
        }
        public async void Send(ReceiveModel rm)
        {
            await Task.Run(()=>
            {
                try
                {
                    if (rm != null && rm.Tag != null && ContainsKey(rm.Tag))
                        this[rm.Tag] = rm;
                }
                catch (Exception e)
                {
                    Api.CallException(rm, e);
                }
            });
        }
    }
}
