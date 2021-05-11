using System;
using System.Collections.Generic;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using WAX.Models;
using WAX.Models.Messages;
using waxnet.Internal.Core;
using waxnet.Internal.Models;

namespace WAX
{
    public sealed class Api : IDisposable
    {
        public event EventHandler<string> OnCodeUpdate;
        public event EventHandler OnLogin;
        public event EventHandler<ChatMessage> OnChatMessage;
        public event EventHandler<GroupMessage> OnGroupMessage;
        public event EventHandler<Exception> OnAccountDropped;
        public event EventHandler<CancellationToken> OnStart;
        public event EventHandler<CancellationToken> OnStop;
        public event EventHandler<CancellationToken> OnDispose;
        public static event EventHandler<Exception> OnException;
        internal static void CallException(object sender, Exception e)
        {
            Task.Factory.StartNew(() =>
            {
                OnException?.Invoke(sender, e);
            });
        }

        private Handler _handler;
        internal Engine _engine;

        public CancellationToken CancellationToken 
        { 
            get 
            { 
                return _engine.CancellationToken; 
            } 
        }

        public Api()
        {
            _engine = new Engine();
            _handler = new Handler { api = this };
            _engine.SessionManager = new SessionManager();
            _engine.Initialize();
            _engine.CallEvent += CallEvent;
        }
        public Api(SessionManagerParameters parameters)
        {
            _engine = new Engine();
            _handler = new Handler { api = this };
            _engine.SessionManager = new SessionManager(parameters);
            _engine.SessionManager.Read();
            _engine.Initialize();
            _engine.CallEvent += CallEvent;
        }

        internal void CallEvent(object sender, CallEventArgs e)
        {
            if (e.Type == CallEventType.Handle) _handler.Controller(e.Content as ReceiveModel);
            if (e.Type == CallEventType.CodeUpdate) Task.Factory.StartNew(()=>OnCodeUpdate?.Invoke(this, e.Content as string));
            if (e.Type == CallEventType.Login)
            {
                _engine.SessionManager.Save();
                Task.Factory.StartNew(() => OnLogin?.Invoke(this, null));
            }
            if (e.Type == CallEventType.Exception) CallException(this, e.Content as Exception);
            if (e.Type == CallEventType.AccountDropped) Task.Factory.StartNew(() => OnAccountDropped?.Invoke(this, e.Content as Exception));
            if (e.Type == CallEventType.Stop) Task.Factory.StartNew(() => OnStop?.Invoke(this, CancellationToken));
            if (e.Type == CallEventType.Message) Task.Factory.StartNew(() => OnChatMessage?.Invoke(this, e.Content as ChatMessage));
            if (e.Type == CallEventType.GroupMessage) Task.Factory.StartNew(() => OnGroupMessage?.Invoke(this, e.Content as GroupMessage));
        }

        public void Start()
        {
            Task.Factory.StartNew(() => OnStart?.Invoke(this, CancellationToken));
            _engine.Start();
        }
        public void Stop()
        {
            _engine.Stop();
        }
        public void Dispose()
        {
            Task.Factory.StartNew(() => OnDispose?.Invoke(this, CancellationToken));
            _engine.Dispose();
            GC.Collect();
        }
    }
}
