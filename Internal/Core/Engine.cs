using Newtonsoft.Json;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Net;
using System.Net.WebSockets;
using System.Security.Cryptography;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using WAX.Models;
using waxnet.Internal.Consts;
using waxnet.Internal.Models;
using waxnet.Internal.Serialization;
using waxnet.Internal.Utils;

namespace waxnet.Internal.Core
{
    class Engine : IDisposable
    {
        public event EventHandler<CallEventArgs> CallEvent;

        public CancellationTokenSource Cts;
        public CancellationToken CancellationToken;

        public string Tag { get { return $"{DateTime.Now.GetTimeStampInt()}.--{Interlocked.Increment(ref _msgCount) - 1}"; } }
        public ReceiveManager ReceiveManager;

        public SessionManager SessionManager;
        internal ClientWebSocket _socket;
        internal int _msgCount;
        private bool _loginSuccess;
        private object _snapReceiveLock = new object();
        private Dictionary<string, Func<ReceiveModel, bool>> _snapReceiveDictionary = new Dictionary<string, Func<ReceiveModel, bool>>();
        private Dictionary<string, int> _snapReceiveRemoveCountDictionary = new Dictionary<string, int>();

        public void Initialize()
        {
            ReceiveManager = new ReceiveManager();
            _socket = new ClientWebSocket();
            _socket.Options.SetRequestHeader("Origin", "https://web.whatsapp.com");
        }

        static Engine()
        {
            ServicePointManager.DefaultConnectionLimit = int.MaxValue;
        }


        public async void Start()
        {
            if(Cts == null)
            {
                Cts = new CancellationTokenSource();
                CancellationToken = Cts.Token;
                _snapReceiveDictionary.Clear();
                if (!await Connect())
                {
                    await Task.Factory.StartNew(()=>CallEvent?.Invoke(this, new CallEventArgs { Content = new Exception("Connect Error!"), Type = CallEventType.Exception }));
                    Stop();
                }
                if (SessionManager.Session == null)
                {
                    SessionManager.Session = new Session();
                }
                if (string.IsNullOrEmpty(SessionManager.Session.ClientToken))
                {
                    Login();
                }
                else
                {
                    ReLogin();
                }
            }
        }
        public void Stop()
        {
            if (Cts != null)
            {
                Cts.Cancel();
                Cts.Dispose();
                Cts = null;
                try
                {
                    _socket.CloseAsync(WebSocketCloseStatus.Empty, null, CancellationToken.None);
                }
                catch { }
            }
        }
        public void Dispose()
        {
            Stop();
            GC.Collect();
        }

        private async Task<bool> Connect()
        {
            if (_socket.State == WebSocketState.Open || _socket.State == WebSocketState.Connecting)
            {
                await _socket.CloseAsync(WebSocketCloseStatus.Empty, "Close", CancellationToken.None);
            }
            await _socket.ConnectAsync(new Uri("wss://web.whatsapp.com/ws"), CancellationToken.None);
            Receive(ReceiveModel.GetReceiveModel());
            this.Send("?,,");
            return true;
        }
        private void Login()
        {
            Task.Factory.StartNew(async () =>
            {
                var clientId = 16.GetRandomByte();
                SessionManager.Session.ClientId = Convert.ToBase64String(clientId);
                var tag = this.SendJson($"[\"admin\",\"init\",[2,2033,7],[\"Windows\",\"Chrome\",\"10\"],\"{SessionManager.Session.ClientId}\",true]");
                string refUrl = null;
                AddSnapReceive(tag, rm =>
                {
                    if (rm.Body.Contains("\"ref\":\""))
                    {
                        refUrl = rm.Body.RegexGetString("\"ref\":\"([^\"]*)\"");
                        return true;
                    }
                    return false;
                });
                var privateKey = Curve25519.CreateRandomPrivateKey();
                var publicKey = Curve25519.GetPublicKey(privateKey);
                AddSnapReceive("s1", rm =>
                {
                    var jsData = JsonConvert.DeserializeObject<dynamic>(rm.Body);
                    SessionManager.Session.ClientToken = jsData[1]["clientToken"];
                    SessionManager.Session.ServerToken = jsData[1]["serverToken"];
                    SessionManager.Session.Id = jsData[1]["wid"];
                    string secret = jsData[1]["secret"];
                    var decodedSecret = Convert.FromBase64String(secret);
                    var pubKey = decodedSecret.Take(32).ToArray();
                    var sharedSecret = Curve25519.GetSharedSecret(privateKey, pubKey);
                    var data = sharedSecret.HMACSHA256_Encrypt(new byte[32]);
                    var sharedSecretExtended = new Hkdf(HashAlgorithmName.SHA256).Expand(data, 80);
                    var checkSecret = new byte[112];
                    Array.Copy(decodedSecret, checkSecret, 32);
                    Array.Copy(decodedSecret, 64, checkSecret, 32, 80);
                    var sign = checkSecret.HMACSHA256_Encrypt(sharedSecretExtended.Skip(32).Take(32).ToArray());
                    if (!sign.ValueEquals(decodedSecret.Skip(32).Take(32).ToArray()))
                    {
                        return true;
                    }
                    var keysEncrypted = new byte[96];
                    Array.Copy(sharedSecretExtended, 64, keysEncrypted, 0, 16);
                    Array.Copy(decodedSecret, 64, keysEncrypted, 16, 80);
                    var keyDecrypted = decodedSecret.Skip(64).ToArray().AesCbcDecrypt(sharedSecretExtended.Take(32).ToArray(), sharedSecretExtended.Skip(64).ToArray());
                    SessionManager.Session.EncKey = keyDecrypted.Take(32).ToArray();
                    SessionManager.Session.MacKey = keyDecrypted.Skip(32).ToArray();
                    _ = Task.Factory.StartNew(() => CallEvent?.Invoke(this, new CallEventArgs { Type = CallEventType.Login }));
                    _loginSuccess = true;
                    return true;
                });
                while (refUrl.IsNullOrWhiteSpace())
                {
                    await Task.Delay(100);
                }
                var loginUrl = $"{refUrl},{Convert.ToBase64String(publicKey)},{SessionManager.Session.ClientId}";
                _ = Task.Factory.StartNew(() => CallEvent?.Invoke(this, new CallEventArgs { Content = loginUrl, Type = CallEventType.CodeUpdate }));

            });

        }
        private void ReLogin()
        {
            AddSnapReceive("s1", LoginResponseHandle);
            this.SendJson($"[\"admin\",\"init\",[2,2033,7],[\"Windows\",\"Chrome\",\"10\"],\"{SessionManager.Session.ClientId}\",true]");
            Task.Delay(5000).ContinueWith(t =>
            {
                this.SendJson($"[\"admin\",\"login\",\"{SessionManager.Session.ClientToken}\",\"{SessionManager.Session.ServerToken}\",\"{SessionManager.Session.ClientId}\",\"takeover\"]");
            });

        }
        private bool LoginResponseHandle(ReceiveModel receive)
        {
            var challenge = receive.Body.RegexGetString("\"challenge\":\"([^\"]*)\"");
            if (challenge.IsNullOrWhiteSpace())
            {
                var jsData = JsonConvert.DeserializeObject<dynamic>(receive.Body);
                SessionManager.Session.ClientToken = jsData[1]["clientToken"];
                SessionManager.Session.ServerToken = jsData[1]["serverToken"];
                SessionManager.Session.Id = jsData[1]["wid"];
                _ = Task.Factory.StartNew(() => CallEvent?.Invoke(this, new CallEventArgs { Type = CallEventType.Login }));
                _loginSuccess = true;
            }
            else
            {
                AddSnapReceive("s2", LoginResponseHandle);
                ResolveChallenge(challenge);
            }
            return true;
        }
        private void ResolveChallenge(string challenge)
        {
            var decoded = Convert.FromBase64String(challenge);
            var loginChallenge = decoded.HMACSHA256_Encrypt(SessionManager.Session.MacKey);
            this.SendJson($"[\"admin\",\"challenge\",\"{Convert.ToBase64String(loginChallenge)}\",\"{SessionManager.Session.ServerToken}\",\"{SessionManager.Session.ClientId}\"]");
        }

        public async Task<Node> GetDecryptNode(ReceiveModel rm)
        {
            if (rm.Nodes != null)
            {
                return rm.Nodes;
            }
            if (rm.MessageType == WebSocketMessageType.Binary && rm.ByteData.Length >= 33)
            {
                while (!_loginSuccess)
                {
                    await Task.Delay(100);
                }
                var tindex = Array.IndexOf(rm.ByteData, (byte)44, 0, rm.ByteData.Length);
                var wd = rm.ByteData.Skip(tindex + 1).ToArray();
                var data = wd.Skip(32).ToArray();
                if (!wd.Take(32).ToArray().ValueEquals(data.HMACSHA256_Encrypt(SessionManager.Session.MacKey)))
                {
                    return null;
                }
                var decryptData = data.AesCbcDecrypt(SessionManager.Session.EncKey);
                var bd = new BinaryDecoder(decryptData);
                var node = bd.ReadNode();
                rm.Nodes = node;
                return rm.Nodes;
            }
            return null;
        }
        public byte[] EncryptBinaryMessage(Node node)
        {
            var b = node.Marshal();
            var iv = Convert.FromBase64String("aKs1sBxLFMBHVkUQwS/YEg==");
            var cipher = b.AesCbcEncrypt(SessionManager.Session.EncKey, iv);
            var cipherIv = iv.Concat(cipher).ToArray();
            var hash = cipherIv.HMACSHA256_Encrypt(SessionManager.Session.MacKey);
            var data = new byte[cipherIv.Length + 32];
            Array.Copy(hash, data, 32);
            Array.Copy(cipherIv, 0, data, 32, cipherIv.Length);
            return data;
        }
        private void Receive(ReceiveModel rm)
        {
            Task.Factory.StartNew(async () =>
            {
                var receiveResult = await _socket.ReceiveAsync(rm.ReceiveData, CancellationToken.None);
                try
                {
                    if (CancellationToken.IsCancellationRequested) return;
                    if (receiveResult.EndOfMessage)
                    {
                        Receive(ReceiveModel.GetReceiveModel());
                        rm.End(receiveResult.Count, receiveResult.MessageType);
                        await CallHandle(rm);
                    }
                    else
                    {
                        rm.Continue(receiveResult.Count);
                        Receive(rm);
                    }
                }
                catch (Exception e)
                {
                    _ = Task.Factory.StartNew(() => CallEvent?.Invoke(this, new CallEventArgs { Content = e, Type = CallEventType.AccountDropped }));
                    Stop();
                }
            });

        }
        private async Task CallHandle(ReceiveModel rm)
        {
            ReceiveManager.Send(rm);
            if (rm.Tag != null && _snapReceiveDictionary.ContainsKey(rm.Tag))
            {
                var result = await Task.Factory.StartNew(() => _snapReceiveDictionary[rm.Tag](rm));
                if (result)
                {
                    lock (_snapReceiveLock)
                    {
                        if (_snapReceiveRemoveCountDictionary.ContainsKey(rm.Tag))
                        {
                            if (_snapReceiveRemoveCountDictionary[rm.Tag] <= 1)
                            {
                                _snapReceiveRemoveCountDictionary.Remove(rm.Tag);
                            }
                            else
                            {
                                _snapReceiveRemoveCountDictionary[rm.Tag] = _snapReceiveRemoveCountDictionary[rm.Tag] - 1;
                                return;
                            }
                        }
                        _snapReceiveDictionary.Remove(rm.Tag);
                    }
                    return;
                }
            }
            Console.WriteLine(_msgCount);
            await Task.Factory.StartNew(()=>CallEvent?.Invoke(this, new CallEventArgs { Content = rm, Type = CallEventType.Handle }));
        }
        public void AddCallback(string tag, Action<ReceiveModel> action = null, int count = 0)
        {
            if (action != null)
            {
                AddSnapReceive(tag, rm =>
                {
                    action?.Invoke(rm);
                    return true;
                }, count);
            }
        }
        private void AddSnapReceive(string tag, Func<ReceiveModel, bool> func, int count = 0)
        {
            if (count != 0)
            {
                _snapReceiveRemoveCountDictionary.Add(tag, count);
            }
            _snapReceiveDictionary.Add(tag, func);
        }

        public async Task<byte[]> DownloadImage(string url, byte[] mediaKey)
        {
            return await Download(url, mediaKey, MediaType.Image);
        }
        public async Task<byte[]> Download(string url, byte[] mediaKey, string info)
        {
            var memory = await url.GetStream();
            var mk = GetMediaKeys(mediaKey, info);
            var data = memory.ToArray();
            var file = data.Take(data.Length - 10).ToArray();
            var mac = data.Skip(file.Length).ToArray();
            var sign = (mk.Iv.Concat(file).ToArray()).HMACSHA256_Encrypt(mk.MacKey);
            if (!sign.Take(10).ToArray().ValueEquals(mac))
            {
                return null;
            }
            var fileData = file.AesCbcDecrypt(mk.CipherKey, mk.Iv);
            return fileData;

        }
        private MediaKeys GetMediaKeys(byte[] mediaKey, string info)
        {
            var sharedSecretExtract = new Hkdf(HashAlgorithmName.SHA256).Extract(mediaKey);
            var sharedSecretExpand = new Hkdf(HashAlgorithmName.SHA256).Expand(sharedSecretExtract, 112, Encoding.UTF8.GetBytes(info));
            return new MediaKeys(sharedSecretExpand);
        }
        public string LoadMediaInfo(string jid, string messageId, string owner)
        {
            return this.SendQuery("media", jid, messageId, "", owner, "", 0, 0);
        }
        public async Task<UploadResponse> Upload(byte[] data, string info)
        {
            return await await Task.Factory.StartNew(async () =>
            {
                var uploadResponse = new UploadResponse();
                uploadResponse.FileLength = (ulong)data.Length;
                uploadResponse.MediaKey = 32.GetRandomByte();
                var mk = GetMediaKeys(uploadResponse.MediaKey, MediaType.Image);
                var enc = data.AesCbcEncrypt(mk.CipherKey, mk.Iv);
                var mac = (mk.Iv.Concat(enc).ToArray()).HMACSHA256_Encrypt(mk.MacKey).Take(10);
                uploadResponse.FileSha256 = data.SHA256_Encrypt();
                var joinData = enc.Concat(mac).ToArray();
                uploadResponse.FileEncSha256 = joinData.SHA256_Encrypt();
                var mediaConnResponse = await QueryMediaConn();
                if (mediaConnResponse == null)
                {
                    return null;
                }
                var token = Convert.ToBase64String(uploadResponse.FileEncSha256).Replace("+", "-").Replace("/", "_");
                var url = $"https://{mediaConnResponse.MediaConn.Hosts[0].Hostname}{MediaType.Map[info]}/{token}?auth={mediaConnResponse.MediaConn.Auth}&token={token}";
                var response = await url.PostHtml(joinData, new Dictionary<string, string> {
                    { "Origin","https://web.whatsapp.com" },
                    { "Referer","https://web.whatsapp.com/"}
                });
                uploadResponse.DownloadUrl = response.RegexGetString("url\":\"([^\"]*)\"");
                return uploadResponse;
            }).ConfigureAwait(false);

        }
        private async Task<MediaConnResponse> QueryMediaConn()
        {
            MediaConnResponse connResponse = null;
            this.SendJson("[\"query\",\"mediaConn\"]", rm => connResponse = JsonConvert.DeserializeObject<MediaConnResponse>(rm.Body));
            await await Task.Factory.StartNew(async () =>
            {
                for (int i = 0; i < 100; i++)
                {
                    if (connResponse != null)
                    {
                        return;
                    }
                    await Task.Delay(100);
                }
            }).ConfigureAwait(false);
            return connResponse;
        }
    }
}
