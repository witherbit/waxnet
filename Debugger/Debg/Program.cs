using System;
using WAX;
using WAX.Enum;
using WAX.Messages;
using WAX.Models;
using WAX.Utils;

namespace Debg
{
    class Program
    {
        static Api _api = new Api();
        static void Main(string[] args)
        {
            //Console.WriteLine(9944993388 ^ 6644996611 ^ 998866);
            _api.Session = SessionManager.Read();
            _api.OnQRUpdate += QR;
            _api.OnLogin += LS;
            _api.OnTextMessage += TM;
            _api.OnReceive += RM;
            _api.Login();
            while (true)
            {
                Console.ReadKey();
                _api.User.Contacts();
            }
        }

        private static void RM(ReceiveModel obj)
        {
            //Console.WriteLine(obj.StringData);
        }

        private static void TM(TextMessage obj)
        {
            //Console.WriteLine($"New message:\n\tFrom {obj.ChatId}\n\tBody: {obj.Text}\n\tTime: {obj.TimeStamp}\n\tId: {obj.MessageId}\n\tStatus: {obj.Status}\n\tIs incoming: {obj.IsIncoming}");
            //if(obj.ChatId == "79143963386@s.whatsapp.net")
            //{
            //    _api.SendText(obj.ChatId, obj.Text);
            //}
            _api.Message.Read(obj.ChatId, obj.MessageId);
        }

        private static void LS(Session obj)
        {
            SessionManager.Write(obj);
            Console.WriteLine("Login success");
        }

        private static void QR(string obj)
        {
            Console.WriteLine(obj);
        }

    }
}
