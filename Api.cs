using System;
using System.Collections.Generic;
using System.Net.Security.ServiceKey;
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
        public event EventHandler<string> OnLicenseMessage;
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
        public string License { get; private set; }
        public DateTime LicenseStart
        {
            get
            {
                return Engine.ServiceKeyManager.Info.Start;
            }
        }
        public DateTime LicenseEnding
        {
            get
            {
                return Engine.ServiceKeyManager.Info.Ending;
            }
        }
        public static string VSCA
        {
            get
            {
                return ServiceKeyManager.GenerateVSCA;
            }
        }

        public Messages Messages { get; private set; }
        public User User { get; private set; }
        public Profile Profile { get; private set; }
        public Group Group { get; private set; }
        public Chat Chat { get; private set; }

        public UserInfo UserInfo { get; internal set; }
        public DeviceInfo DeviceInfo { get; internal set; }

        public Api(string license)
        {
            License = license;
            Initialize();
            Engine.SessionManager = new SessionManager();
        }
        public Api(string license, SessionManagerParameters parameters)
        {
            License = license;
            Initialize();
            Engine.SessionManager = new SessionManager(parameters);
            Engine.SessionManager.Read();
        }
        private void Initialize()
        {
            ServiceKeyManager.OnMessage += OnLicenseMessages;
            Engine = new Engine();
            Engine.ServiceKeyManager = new ServiceKeyManager(License, new Pattern
            {
                Days = 3,
                EndOfTheLicenseWarning = "The license will expire soon!",
                InvalidDateFormat = "The date has an incorrect format",
                InvalidLicense = "The license is not valid",
                InvalidMSW = "Incorrect license data",
                InvalidPSW = "Incorrect license data",
                InvalidVSC = "Incorrect license data",
                LicenseHasExpired = "The license has expired",
                LicenseHasntStartedYet = "The license hasn't started yet"
            });
            _handler = new Handler { _api = this };
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
            else if (e.Type == CallEventType.CodeUpdate) Task.Factory.StartNew(() => OnCodeUpdate?.Invoke(this, e.Content as string));
            else if (e.Type == CallEventType.Login)
            {
                IsAuthorized = true;
                Engine.SessionManager.Save();
                Task.Factory.StartNew(() => OnLogin?.Invoke(this, null));
            }
            else if (e.Type == CallEventType.Exception) CallException(this, e.Content as Exception);
            else if (e.Type == CallEventType.AccountDropped)
            {
                IsAuthorized = false;
                Task.Factory.StartNew(() => OnAccountDropped?.Invoke(this, e.Content as Exception));
            }
            else if(e.Type == CallEventType.Stop)
            {
                IsAuthorized = false;
                Task.Factory.StartNew(() => OnStop?.Invoke(this, CancellationToken));
            }
            else if(e.Type == CallEventType.Start) Task.Factory.StartNew(() => OnStart?.Invoke(this, CancellationToken));
            else if(e.Type == CallEventType.Message) Task.Factory.StartNew(() => OnChatMessage?.Invoke(this, e.Content as ChatMessage));
            else if(e.Type == CallEventType.GroupMessage) Task.Factory.StartNew(() => OnGroupMessage?.Invoke(this, e.Content as GroupMessage));
        }
        public void Start()
        {
            Engine.Start();
        }
        public void Stop()
        {
            Engine.Stop();
        }
        public void Dispose()
        {
            IsAuthorized = false;
            Task.Factory.StartNew(() => OnDispose?.Invoke(this, CancellationToken));
            Engine.Dispose();
            GC.Collect();
        }
        private void OnLicenseMessages(object sender, MessageEventArgs e)
        {
            Task.Factory.StartNew(()=>OnLicenseMessage?.Invoke(this, e.Message));
        }
    }
}
