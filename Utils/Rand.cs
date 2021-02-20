using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace WAX.Utils
{
    static class Rand
    {
        public static byte[] GetRandomByte(int length)
        {
            var random = new Random();
            byte[] bs = new byte[length];
            for (int i = 0; i < length; i++)
            {
                bs[i] = (byte)random.Next(0, 255);
            }
            return bs;
        }
    }
}
