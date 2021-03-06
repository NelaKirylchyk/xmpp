﻿using System;
using System.Collections.Concurrent;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using System.Xml.Linq;
using XMPP.states;
using XMPP.tags;
using XMPP.tags.bosh;

using Windows.Web.Http;
using Windows.Storage.Streams;
using Windows.Web.Http.Headers;

namespace XMPP.common
{
    public class BoSH : IConnection
    {
        public BoSH(Manager manager)
        {
            _manager = manager;
        }

        /// <summary>
        /// Gets a value indicating whether is connected.
        /// </summary>
        public bool IsConnected
        {
            get;
            private set;
        }

        /// <summary>
        /// Gets the hostname.
        /// </summary>
        public string Hostname
        {
            get { return _manager.Settings.Hostname; }
        }

        /// <summary>
        /// Gets or sets a value indicating whether is ssl enabled.
        /// </summary>
        public bool IsSSLEnabled { get; set; }

        public void Connect()
        {
            if (IsConnected)
            {
#if DEBUG
                _manager.Events.LogMessage(this, LogType.Warn, "Already connected");
#endif
                return;
            }

            Init();

            SendSessionCreationRequest();
        }

        public void Disconnect()
        {
            if (!IsConnected)
            {
                return;
            }

            _disconnecting.Set();

            if (null != _pollingTask)
            {
                _pollingTask.Wait();
                _pollingTask = null;
            }

            if (!_connectionError.IsSet)
            {
                SendSessionTerminationRequest();
            }

            CleanupState();

            _manager.Events.Disconnected(this);
        }

        public void Restart()
        {
            SendRestartRequest();
        }

        public void StartPolling()
        {
            _pollingTask = StartPollingInternal();
        }

        public void Send(Tag tag)
        {
            Task.Run(() => Flush(tag));
        }

        public void Send(string message)
        {
            throw new NotImplementedException();
        }

        public void EnableSSL()
        {
            throw new NotImplementedException();
        }

        public void EnableCompression(string algorithm)
        {
            throw new NotImplementedException();
        }

        public void Dispose()
        {
            CleanupState();
        }

        private void Init()
        {
            _client = new HttpClient();

            _disconnecting = new ManualResetEventSlim(false);
            _connectionError = new ManualResetEventSlim(false);
            _canFetch = new AutoResetEvent(true);
            _tags = new ConcurrentQueue<XElement>();
            _rid = new Random().Next(StartRid, EndRid);
        }

        private XElement RemoveComments(Tag tag)
        {
            var copy = new XElement(tag);
            var comments = copy.DescendantNodesAndSelf()
                               .Where(node => node.NodeType == System.Xml.XmlNodeType.Comment);

            while (comments.Any())
            {
                comments.First().Remove();
            }

            return copy;
        }

        private void ConnectionError(ErrorType type, ErrorPolicyType policy, string cause = "")
        {
            if (_disconnecting.IsSet)
            {
                return;
            }

            if (!_connectionError.IsSet)
            {
                _connectionError.Set();

                _manager.Events.Error(this, type, policy, cause);
            }
        }

        private void CleanupState()
        {
            IsConnected = false;

            if (null != _client)
            {
                _client.Dispose();
                _client = null;
            }
        }

        private void SendRestartRequest()
        {
            var body = new body
            {
                sid = _sid,
                rid = Interlocked.Increment(ref _rid),
                restart = true,
                to = _manager.Settings.Id.Server
            };

            var resp = SendRequest(body);
            if (null != resp)
            {
                var payload = resp.Element<tags.streams.features>(tags.streams.Namespace.features);

                _manager.State = new ServerFeaturesState(_manager);
                _manager.State.Execute(payload);
            }
        }

        private body SendRequest(body body)
        {
            _manager.Events.Chunk(this, new ChunkLogEventArgs(body, ChunkDirection.Outgoing));

            var req = new HttpRequestMessage
            {
                RequestUri = new Uri(_manager.Settings.Hostname),
                Method = new HttpMethod("POST"),
                Content = new HttpStringContent(body, UnicodeEncoding.Utf8),
            };

            req.Content.Headers.ContentType = new HttpMediaTypeHeaderValue("text/xml")
            {
                CharSet = "utf-8",
            };

            try
            {
                var resp = _client.SendRequestAsync(req).AsTask().Result;

                if (resp.IsSuccessStatusCode)
                {
                    var data = resp.Content.ReadAsStringAsync().AsTask().Result;

                   _manager.Events.Chunk(this, new ChunkLogEventArgs(data, ChunkDirection.Incomming));

                    return Tag.Get(data) as body;
                }

                ConnectionError(ErrorType.ConnectToServerFailed, ErrorPolicyType.Reconnect, string.Format("Connection error:\r\nStatus: {0}\r\nReason Phrase: {1}", resp.StatusCode, resp.ReasonPhrase));
            }
            catch (AggregateException e)
            {
                if (!(e.InnerException is TaskCanceledException))
                {
                    ConnectionError(ErrorType.ConnectToServerFailed, ErrorPolicyType.Reconnect, e.Message);
                }
            }

            return null;
        }

        private void SendSessionCreationRequest()
        {
            var body = new body
            {
                rid = _rid,
                wait = Wait,
                hold = Hold,
                from = _manager.Settings.Id.Bare,
                to = _manager.Settings.Id.Server
            };

            var resp = SendRequest(body);

            if (null != resp)
            {
                IsConnected = true;

                _manager.Events.Connected(this);

                _sid = resp.sid;
                _requests = resp.requests;

                _connectionsCounter = new SemaphoreSlim(_requests.Value, _requests.Value);

                var payload = resp.Element<tags.streams.features>(tags.streams.Namespace.features);

                _manager.State = new ServerFeaturesState(_manager);
                _manager.State.Execute(payload);
            }
        }

        private void SendSessionTerminationRequest()
        {
            var body = new body
            {
                sid = _sid,
                rid = Interlocked.Increment(ref _rid),
                type = "terminate"
            };

            CombineBody(body);

            SendRequest(body);
        }

        private void Flush(Tag tag = null)
        {
            if (null != tag)
            {
                _tags.Enqueue(RemoveComments(tag));
            }

            FlushInternal();
        }

        private void FlushInternal()
        {
            if (_disconnecting.IsSet || _connectionError.IsSet)
            {
                return;
            }

            if (_connectionsCounter.Wait(TimeSpan.FromMilliseconds(1d)))
            {
                try
                {
                    _canFetch.WaitOne();

                    body body = CombineBody();

                    _canFetch.Set();

                    var resp = SendRequest(body);

                    ExtractBody(resp);
                }
                finally
                {
                    _connectionsCounter.Release();

                    if (!_tags.IsEmpty)
                    {
                        Task.Run(() => Flush());
                    }
                }
            }
        }

        private void CombineBody(body body)
        {
            int counter = _manager.Settings.QueryCount;

            while (!_tags.IsEmpty)
            {
                XElement tag;
                if (_tags.TryDequeue(out tag))
                {
                    body.Add(tag);
                    if (--counter == 0)
                    {
                        break;
                    }
                }
            }
        }

        private body CombineBody()
        {
            var body = new body
            {
                sid = _sid,
                rid = Interlocked.Increment(ref _rid),
                from = _manager.Settings.Id,
                to = _manager.Settings.Id.Server
            };

            CombineBody(body);

            return body;
        }

        private void ExtractBody(body resp)
        {
            if (null != resp)
            {
                foreach (var element in resp.Elements<XElement>())
                {
                    _manager.Events.NewTag(this, Tag.Get(element));
                }
            }
        }

        private Task StartPollingInternal()
        {
            return Task.Run(() =>
            {
                while (true)
                {
                    if (_disconnecting.IsSet || _connectionError.IsSet)
                    {
                        return;
                    }

                    if (_connectionsCounter.CurrentCount == _requests) //no active requests
                    {
                        Task.Run(() => FlushInternal());
                    }

                    Task.Delay(TimeSpan.FromMilliseconds(10)).Wait();
                };
            });
        }

        private readonly Manager _manager;

        private string _sid;
        private long _rid;
        private int? _requests;

        private HttpClient _client;
        private SemaphoreSlim _connectionsCounter;
        private ManualResetEventSlim _connectionError;
        private ManualResetEventSlim _disconnecting;
        private AutoResetEvent _canFetch;
        private ConcurrentQueue<XElement> _tags;
        private Task _pollingTask;

        private const int StartRid = 1000000;
        private const int EndRid = 99999999;
        private const int Hold = 1;
        private const int Wait = 60;
    }
}
