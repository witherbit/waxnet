using System;
using WAX;
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
            _api.Session = SessionManager.Read();
            _api.OnQRUpdate += QR;
            _api.OnLoginSuccess += LS;
            _api.OnTextMessage += TM;
            _api.Login();
            Console.ReadKey();
        }

        private static void TM(TextMessage obj)
        {
            Console.WriteLine($"New message:\n\tFrom {obj.ChatId}\n\tBody: {obj.Text}\n\tTime: {obj.TimeStamp}\n\tId: {obj.MessageId}\n\tStatus: {obj.Status}\n\tIs incoming: {obj.IsIncoming}");
            //if(obj.ChatId == "79143963386@s.whatsapp.net")
            //{
            //    _api.SendText(obj.ChatId, obj.Text);
            //}
            _api.Message.Read(obj.ChatId, obj.MessageId);
        }

        private static void LS(Session obj)
        {
            Console.Clear();
            SessionManager.Write(obj);
        }

        private static void QR(string obj)
        {
            Console.WriteLine(obj);
        }

    }
}
