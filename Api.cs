using AronParker.Hkdf;
using Elliptic;
using Google.Protobuf;
using Newtonsoft.Json;
using Proto;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Net;
using System.Net.WebSockets;
using System.Security.Cryptography;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using WAX.APIs;
using WAX.Enum;
using WAX.Messages;
using WAX.Models;
using WAX.Serialization;
using WAX.Utils;
using WAX.Consts;

namespace WAX
{
    public class Api : IDisposable
    {
        public event Action<string> OnQRUpdate;
        public event Action<Session> OnLoginSuccess;
        public event Action<ReceiveModel> OnRemainingMessages;
        public event Action<TextMessage> OnTextMessage;
        public event Action<Messages.ImageMessage> OnImageMessage;
        public event Action OnAccountDropped;
        public Session Session { set; get; }
        internal ClientWebSocket _socket;
        //private object _sendObj = new object();
        internal int _msgCount;
        private bool _loginSuccess;
        private object _snapReceiveLock = new object();
        private Dictionary<string, Func<ReceiveModel, bool>> _snapReceiveDictionary = new Dictionary<string, Func<ReceiveModel, bool>>();
        private Dictionary<string, int> _snapReceiveRemoveCountDictionary = new Dictionary<string, int>();
        private static Dictionary<string, string> MediaTypeMap = new Dictionary<string, string>{
            { MediaTypeConst.MediaImage,"/mms/image" },
            { MediaTypeConst.MediaVideo,"/mms/video" },
            { MediaTypeConst.MediaAudio,"/mms/document" },
            { MediaTypeConst.MediaDocument,"/mms/audio" },
        };
        static Api()
        {
            ServicePointManager.DefaultConnectionLimit = int.MaxValue;
        }
        public Api()
        {
            _socket = new ClientWebSocket();
            _socket.Options.SetRequestHeader("Origin", "https://web.whatsapp.com");
            Message = new MessageApi(this);
            User = new UserApi(this);
        }

        public MessageApi Message;
        public UserApi User;
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
        public async void Login()
        {
            _snapReceiveDictionary.Clear();
            if (!await Connect())
            {
                throw new Exception("Connect Error");
            }
            if (Session == null)
            {
                Session = new Session();
            }
            if (string.IsNullOrEmpty(Session.ClientToken))
            {
                WhatsAppLogin();
            }
            else
            {
                ReLogin();
            }
        }
        public string LoadMediaInfo(string jid, string messageId, string owner, Action<ReceiveModel> act = null)
        {
            return this.SendQuery("media", jid, messageId, "", owner, "", 0, 0, act);
        }
        public string GetFullChatHistory(string jid, int count = 300)
        {
            if (string.IsNullOrWhiteSpace(jid))
            {
                return string.Empty;
            }
            var beforeMsg = "";
            var beforeMsgIsOwner = true;
            //while (true)
            {
                this.SendQuery("message", jid, beforeMsg, "before", beforeMsgIsOwner ? "true" : "false", "", count, 0, async rm =>
                           {
                               var node = await GetDecryptNode(rm);
                               if (node != null)
                               {
                                   if (node.Content is List<Node> nodeList)
                                   {
                                       foreach (var item in nodeList)
                                       {
                                           if (item.Description == "message")
                                           {
                                               var messageData = item.Content as byte[];
                                               var ms = WebMessageInfo.Parser.ParseFrom(messageData);
                                               if (ms.Message != null)
                                               {
                                               }
                                           }
                                       }
                                   }
                               }
                           }, 2);
            }
            return string.Empty;
        }

        public void UploadProfilePicture()
        {

        }

        public void Dispose()
        {
            _socket.CloseAsync(WebSocketCloseStatus.Empty, null, CancellationToken.None);
        }
        private async Task<byte[]> DownloadImage(string url, byte[] mediaKey)
        {
            return await Download(url, mediaKey, MediaTypeConst.MediaImage);
        }
        private async Task<byte[]> Download(string url, byte[] mediaKey, string info)
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
        internal async Task<UploadResponse> Upload(byte[] data, string info)
        {
            return await await Task.Factory.StartNew(async () =>
            {
                var uploadResponse = new UploadResponse();
                uploadResponse.FileLength = (ulong)data.Length;
                uploadResponse.MediaKey = Rand.GetRandomByte(32);
                var mk = GetMediaKeys(uploadResponse.MediaKey, MediaTypeConst.MediaImage);
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
                var url = $"https://{mediaConnResponse.MediaConn.Hosts[0].Hostname}{MediaTypeMap[info]}/{token}?auth={mediaConnResponse.MediaConn.Auth}&token={token}";
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
        internal void AddCallback(string tag, Action<ReceiveModel> act, int count = 0)
        {
            if (act != null)
            {
                AddSnapReceive(tag, rm =>
                {
                    act(rm);
                    return true;
                }, count);
            }
        }
        private void Receive(ReceiveModel receiveModel)
        {
            Task.Factory.StartNew(async () =>
            {
                var receiveResult = await _socket.ReceiveAsync(receiveModel.ReceiveData, CancellationToken.None);
                try
                {
                    if (receiveResult.EndOfMessage)
                    {
                        Receive(ReceiveModel.GetReceiveModel());
                        receiveModel.End(receiveResult.Count, receiveResult.MessageType);
                        await ReceiveHandle(receiveModel);
                    }
                    else
                    {
                        receiveModel.Continue(receiveResult.Count);
                        Receive(receiveModel);
                    }
                }
                catch
                {
                    _socket.Dispose();
                    _ = Task.Factory.StartNew(() => OnAccountDropped?.Invoke());
                }
            });

        }
        internal byte[] EncryptBinaryMessage(Node node)
        {
            var b = node.Marshal();
            var iv = Convert.FromBase64String("aKs1sBxLFMBHVkUQwS/YEg=="); //GetRandom(16);
            var cipher = b.AesCbcEncrypt(Session.EncKey, iv);
            var cipherIv = iv.Concat(cipher).ToArray();
            var hash = cipherIv.HMACSHA256_Encrypt(Session.MacKey);
            var data = new byte[cipherIv.Length + 32];
            Array.Copy(hash, data, 32);
            Array.Copy(cipherIv, 0, data, 32, cipherIv.Length);
            return data;
        }
        private bool LoginResponseHandle(ReceiveModel receive)
        {
            var challenge = receive.Body.RegexGetString("\"challenge\":\"([^\"]*)\"");
            if (challenge.IsNullOrWhiteSpace())
            {
                var jsData = JsonConvert.DeserializeObject<dynamic>(receive.Body);
                Session.ClientToken = jsData[1]["clientToken"];
                Session.ServerToken = jsData[1]["serverToken"];
                Session.Id = jsData[1]["wid"];
                _ = Task.Factory.StartNew(() => OnLoginSuccess?.Invoke(Session));
                _loginSuccess = true;
            }
            else
            {
                AddSnapReceive("s2", LoginResponseHandle);
                ResolveChallenge(challenge);
            }
            return true;
        }
        private void ReLogin()
        {
            AddSnapReceive("s1", LoginResponseHandle);
            this.SendJson($"[\"admin\",\"init\",[2,2033,7],[\"Windows\",\"Chrome\",\"10\"],\"{Session.ClientId}\",true]");
            Task.Delay(5000).ContinueWith(t =>
            {
                this.SendJson($"[\"admin\",\"login\",\"{Session.ClientToken}\",\"{Session.ServerToken}\",\"{Session.ClientId}\",\"takeover\"]");
            });

        }
        private void ResolveChallenge(string challenge)
        {
            var decoded = Convert.FromBase64String(challenge);
            var loginChallenge = decoded.HMACSHA256_Encrypt(Session.MacKey);
            this.SendJson($"[\"admin\",\"challenge\",\"{Convert.ToBase64String(loginChallenge)}\",\"{Session.ServerToken}\",\"{Session.ClientId}\"]");
        }
        private void WhatsAppLogin()
        {
            Task.Factory.StartNew(async () =>
            {
                var clientId = Rand.GetRandomByte(16);
                Session.ClientId = Convert.ToBase64String(clientId);
                var tag = this.SendJson($"[\"admin\",\"init\",[2,2033,7],[\"Windows\",\"Chrome\",\"10\"],\"{Session.ClientId}\",true]");
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
                    Session.ClientToken = jsData[1]["clientToken"];
                    Session.ServerToken = jsData[1]["serverToken"];
                    Session.Id = jsData[1]["wid"];
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
                    Session.EncKey = keyDecrypted.Take(32).ToArray();
                    Session.MacKey = keyDecrypted.Skip(32).ToArray();
                    _ = Task.Factory.StartNew(() => OnLoginSuccess?.Invoke(Session));
                    _loginSuccess = true;
                    return true;
                });
                while (refUrl.IsNullOrWhiteSpace())
                {
                    await Task.Delay(100);
                }
                var loginUrl = $"{refUrl},{Convert.ToBase64String(publicKey)},{Session.ClientId}";
                _ = Task.Factory.StartNew(() => OnQRUpdate?.Invoke(loginUrl));

            });

        }
        private async Task<Node> GetDecryptNode(ReceiveModel rm)
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
                if (!wd.Take(32).ToArray().ValueEquals(data.HMACSHA256_Encrypt(Session.MacKey)))
                {
                    return null;
                }
                var decryptData = data.AesCbcDecrypt(Session.EncKey);
                var bd = new BinaryDecoder(decryptData);
                var node = bd.ReadNode();
                rm.Nodes = node;
                return rm.Nodes;
            }
            return null;
        }
        private async Task ReceiveHandle(ReceiveModel rm)
        {
            var node = await GetDecryptNode(rm);
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
            if (node != null)
            {
                if (node.Content is List<Node> nodeList)
                {
                    foreach (var item in nodeList)
                    {
                        if (item.Description == "message")
                        {
                            var messageData = item.Content as byte[];
                            var ms = WebMessageInfo.Parser.ParseFrom(messageData);
                            if (ms.Message != null)
                            {
                                if (ms.Message.ImageMessage != null && OnImageMessage != null)
                                {
                                    _ = Task.Factory.StartNew(async () =>
                                    {
                                        try
                                        {

                                            var fileData = await DownloadImage(ms.Message.ImageMessage.Url, ms.Message.ImageMessage.MediaKey.ToArray());
                                            OnImageMessage.Invoke(new Messages.ImageMessage
                                            {
                                                TimeStamp = ms.MessageTimestamp.ToString().GetDateTime(),
                                                ChatId = ms.Key.RemoteJid,
                                                Text = ms.Message.ImageMessage.Caption,
                                                ImageData = fileData,
                                                MessageId = ms.Key.Id,
                                                IsIncoming = !ms.Key.FromMe,
                                                Status = (int)ms.Status,
                                            });
                                        }
                                        catch (Exception ex)
                                        {
                                            LoadMediaInfo(ms.Key.RemoteJid, ms.Key.Id, ms.Key.FromMe ? "true" : "false", async _ =>
                                             {
                                                 try
                                                 {
                                                     var fileData = await DownloadImage(ms.Message.ImageMessage.Url, ms.Message.ImageMessage.MediaKey.ToArray());
                                                     var ignore = Task.Factory.StartNew(() => OnImageMessage.Invoke(new Messages.ImageMessage
                                                     {
                                                         TimeStamp = ms.MessageTimestamp.ToString().GetDateTime(),
                                                         ChatId = ms.Key.RemoteJid,
                                                         Text = ms.Message.ImageMessage.Caption,
                                                         ImageData = fileData,
                                                     }));
                                                 }
                                                 catch
                                                 {
                                                     return;
                                                 }
                                             });
                                        }
                                    });
                                }
                                else if (ms.Message.HasConversation && OnTextMessage != null)
                                {
                                    _ = Task.Factory.StartNew(() => OnTextMessage?.Invoke(new TextMessage
                                    {
                                        TimeStamp = ms.MessageTimestamp.ToString().GetDateTime(),
                                        ChatId = ms.Key.RemoteJid,
                                        Text = ms.Message.Conversation,
                                        MessageId = ms.Key.Id,
                                        IsIncoming = !ms.Key.FromMe,
                                        Status = (int)ms.Status,
                                    }));
                                }
                                else
                                {
                                    InvokeReceiveRemainingMessagesEvent(messageData);
                                }
                            }
                            else
                            {
                                InvokeReceiveRemainingMessagesEvent(messageData);
                            }
                        }
                        else if (item.Content is byte[] bs)
                        {
                            InvokeReceiveRemainingMessagesEvent(bs);
                        }
                    }
                }
                else
                {
                    InvokeReceiveRemainingMessagesEvent(rm);
                }
            }
            else
            {
                InvokeReceiveRemainingMessagesEvent(rm);
            }
        }
        private void InvokeReceiveRemainingMessagesEvent(ReceiveModel receiveModel)
        {
            Task.Factory.StartNew(() => OnRemainingMessages?.Invoke(receiveModel));
        }
        private void InvokeReceiveRemainingMessagesEvent(byte[] data)
        {
            InvokeReceiveRemainingMessagesEvent(ReceiveModel.GetReceiveModel(data));
        }
        private void AddSnapReceive(string tag, Func<ReceiveModel, bool> func, int count = 0)
        {
            if (count != 0)
            {
                _snapReceiveRemoveCountDictionary.Add(tag, count);
            }
            _snapReceiveDictionary.Add(tag, func);
        }
    }
}
