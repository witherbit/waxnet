using Newtonsoft.Json;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using WAX.Methods;
using WAX.Models;
using WAX.Models.Messages;
using waxnet.Internal.Core;
using waxnet.Internal.Models;
using waxnet.Internal.Utils;

namespace WAX
{
    sealed class Api : IDisposable
    {
        public const string CoreVersion = "2.3.8";
        public event EventHandler<string> OnCodeUpdate;
        public event EventHandler OnLogin;
        public event EventHandler<Exception> OnAccountDropped;
        public event EventHandler<CancellationToken> OnStart;
        public event EventHandler<CancellationToken> OnStop;
        public event EventHandler<CancellationToken> OnDispose;
        public static event EventHandler<Exception> OnException;
        public event EventHandler<CancellationToken> OnConnection;
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
        public long Id
        {
            get => Engine.Session.Id.GetId();
        }
        public UserInfo UserInfo { get; internal set; }
        public DeviceInfo DeviceInfo { get; internal set; }

        public Api()
        {
            Initialize();
        }
        private void Initialize()
        {
            Engine = new Engine();
            _handler = new Handler { _api = this };
            Engine.CallEvent += CallEvent;
            Messages = new Messages { _api = this };
            User = new User { _api = this };
            Profile = new Profile { _api = this };
            Group = new Group { _api = this };
            Chat = new Chat { _api = this };
        }
        public Session GetSession()
        {
            return Engine.Session;
        }
        public void SetSession(Session session)
        {
            Engine.Session = session;
        }
        internal void CallEvent(object sender, CallEventArgs e)
        {
            if (e.Type == CallEventType.Handle) _handler.Controller(e.Content as ReceiveModel);
            else if (e.Type == CallEventType.CodeUpdate) Task.Factory.StartNew(() => OnCodeUpdate?.Invoke(this, e.Content as string));
            else if (e.Type == CallEventType.Login)
            {
                IsAuthorized = true;
                Task.Factory.StartNew(() => OnLogin?.Invoke(this, null));
            }
            else if (e.Type == CallEventType.Exception) CallException(this, e.Content as Exception);
            else if (e.Type == CallEventType.AccountDropped)
            {
                IsAuthorized = false;
                Stop();
                Task.Factory.StartNew(() => OnAccountDropped?.Invoke(this, e.Content as Exception));
            }
            else if(e.Type == CallEventType.Stop)
            {
                IsAuthorized = false;
                Task.Factory.StartNew(() => OnStop?.Invoke(this, CancellationToken));
            }
            else if(e.Type == CallEventType.Start) Task.Factory.StartNew(() => OnStart?.Invoke(this, CancellationToken));
            else if (e.Type == CallEventType.Connection) Task.Factory.StartNew(() => OnConnection?.Invoke(this, CancellationToken));
        }
        public void Start()
        {
            Engine.Start();
        }
        public void ReStart()
        {
            Stop();
            Start();
        }
        public void Stop()
        {
            IsAuthorized = false;
            Engine.Stop();
        }
        public void Dispose()
        {
            IsAuthorized = false;
            Task.Factory.StartNew(() => OnDispose?.Invoke(this, CancellationToken));
            Engine.Dispose();
            GC.SuppressFinalize(this);
            GC.Collect();
        }
    }
}
