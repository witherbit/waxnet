using System;
using System.Collections.Generic;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using WAX.Methods;
using WAX.Models;
using WAX.Models.Messages;
using WAX.Models.Parameters;
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
        internal Engine Engine;

        public CancellationToken CancellationToken
        {
            get
            {
                return Engine.CancellationToken;
            }
        }
        public bool IsAuthorized { get; private set; }

        public Messages Messages { get; private set; }
        public User User { get; private set; }
        public Profile Profile { get; private set; }
        public Group Group { get; private set; }
        public Chat Chat { get; private set; }

        public UserInfo UserInfo { get; internal set; }
        public DeviceInfo DeviceInfo { get; internal set; }

        public Api()
        {
            Initialize();
            Engine.SessionManager = new SessionManager();
        }
        public Api(SessionManagerParameters parameters)
        {
            Initialize();
            Engine.SessionManager = new SessionManager(parameters);
            Engine.SessionManager.Read();
        }

        private void Initialize()
        {
            Engine = new Engine();
            _handler = new Handler { _api = this };
            Engine.Initialize();
            Engine.CallEvent += CallEvent;
            Messages = new Messages { _api = this };
            User = new User { _api = this };
            Profile = new Profile { _api = this };
            Group = new Group { _api = this };
            Chat = new Chat { _api = this };
        }

        internal void CallEvent(object sender, CallEventArgs e)
        {
            if (e.Type == CallEventType.Handle) _handler.Controller(e.Content as ReceiveModel);
            if (e.Type == CallEventType.CodeUpdate) Task.Factory.StartNew(() => OnCodeUpdate?.Invoke(this, e.Content as string));
            if (e.Type == CallEventType.Login)
            {
                IsAuthorized = true;
                Engine.SessionManager.Save();
                Task.Factory.StartNew(() => OnLogin?.Invoke(this, null));
            }
            if (e.Type == CallEventType.Exception) CallException(this, e.Content as Exception);
            if (e.Type == CallEventType.AccountDropped)
            {
                IsAuthorized = false;
                Task.Factory.StartNew(() => OnAccountDropped?.Invoke(this, e.Content as Exception));
            }
            if (e.Type == CallEventType.Stop)
            {
                IsAuthorized = false;
                Task.Factory.StartNew(() => OnStop?.Invoke(this, CancellationToken));
            }
            if (e.Type == CallEventType.Message) Task.Factory.StartNew(() => OnChatMessage?.Invoke(this, e.Content as ChatMessage));
            if (e.Type == CallEventType.GroupMessage) Task.Factory.StartNew(() => OnGroupMessage?.Invoke(this, e.Content as GroupMessage));
        }

        public void Start()
        {
            Task.Factory.StartNew(() => OnStart?.Invoke(this, CancellationToken));
            Engine.Start();
        }
        public void Stop()
        {
            Engine.Stop();
        }
        public void Dispose()
        {
            Task.Factory.StartNew(() => OnDispose?.Invoke(this, CancellationToken));
            Engine.Dispose();
            GC.Collect();
        }
    }
}
