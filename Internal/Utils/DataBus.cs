using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using WAX.Models;

namespace waxnet.Internal.Utils
{
    static class DataBus
    {
        private static Dictionary<string, object> _data = new Dictionary<string, object>();
        /// <summary>
        /// Ожидает результат
        /// </summary>
        /// <typeparam name="T">Возвращаемый тип</typeparam>
        /// <param name="tag">Метка</param>
        /// <param name="passesCount">Кол-во пропусков</param>
        /// <param name="timeout">Таймаут</param>
        /// <returns></returns>
        public static T WaitResult<T>(string tag, int passesCount = 0, int timeout = 5000)
        {
            if (passesCount < 0) throw new Exception("passesCount cannot be less than zero");
            var time = DateTime.Now;
            object result = null;
            for (int i = -1; i < passesCount; i++)
            {
                while ((DateTime.Now - time).TotalMilliseconds <= timeout)
                {
                    if (IsExist(tag))
                    {
                        try
                        {
                            result = _data[tag];
                            _data.Remove(tag);
                        }
                        catch { continue; }
                        break;
                    }
                }
            }
            return (T)result;
        }
        /// <summary>
        /// Отправляет данные
        /// </summary>
        /// <typeparam name="T">Тип оправляемых данных</typeparam>
        /// <param name="tag">Метка</param>
        /// <param name="value">Данные</param>
        /// <param name="timeout">Таймаут на удаление данных</param>
        public async static void Send<T>(string tag, T value, int timeout = 20000)
        {
            await Task.Run(()=>
            {
                if (tag == null) return;
                if (IsExist(tag))
                    _data.Remove(tag);
                _data.Add(tag, value);

                Task.Delay(timeout).Wait();
                if (IsExist(tag)) _data.Remove(tag);
            });
        }

        private static bool IsExist(string tag)
        {
            try
            {
                _data.ContainsKey(tag);
                return true;
            }
            catch
            {
                return false;
            }
        }
    }
}
