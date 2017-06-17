using System;
using System.Collections.Generic;
using System.Globalization;
using System.IO;
using System.Linq;
using System.Net;
using System.Net.Http;
using System.Runtime.InteropServices.WindowsRuntime;
using System.Text;
using System.Threading;
using System.Threading.Tasks;

using Windows.Web.Http;
using Windows.Web.Http.Filters;
using Windows.Web.Http.Headers;

using HttpResponseMessage = System.Net.Http.HttpResponseMessage;
using HttpStatusCode = System.Net.HttpStatusCode;
using HttpClient = Windows.Web.Http.HttpClient;
using HttpCompletionOption = Windows.Web.Http.HttpCompletionOption;
using HttpMethod = Windows.Web.Http.HttpMethod;
using HttpRequestMessage = Windows.Web.Http.HttpRequestMessage;

namespace Microsoft.AspNet.SignalR.Client.Http
{
    /// <summary>
    /// <see cref="IHttpClient"/> implementation that uses <see cref="HttpClient"/>.
    /// </summary>
    public class FoundationHttpClient : IHttpClient
    {
        private IConnection _connection;
        private readonly HttpBaseProtocolFilter _httpFilter;

        private HttpClient _longRunningClient;
        private HttpClient _shortRunningClient;

        /// <summary>
        /// Creates new <see cref="FoundationHttpClient"/> instance using empty <see cref="HttpBaseProtocolFilter"/>.
        /// </summary>
        public FoundationHttpClient()
            : this(new HttpBaseProtocolFilter())
        {
        }

        /// <summary>
        /// Creates new <see cref="FoundationHttpClient"/> instance using specified <see cref="HttpBaseProtocolFilter"/>.
        /// </summary>
        /// <param name="filter">Filter for <see cref="Windows.Web.Http.HttpClient"/>.</param>
        public FoundationHttpClient(HttpBaseProtocolFilter filter) => _httpFilter = filter ?? throw new ArgumentNullException(nameof(filter), $"{nameof(filter)} is null.");

        /// <summary>
        /// Makes an asynchronous http GET request to the specified url.
        /// </summary>
        /// <param name="url">The url to send the request to.</param>
        /// <param name="prepareRequest">A callback that initializes the request with default values.</param>
        /// <param name="isLongRunning">Indicates whether the request is long running</param>
        /// <returns>A <see cref="T:Task{IResponse}"/>.</returns>
        public async Task<IResponse> Get(string url, Action<IRequest> prepareRequest, bool isLongRunning)
        {
            var responseDisposer = new Disposer();
            var cts = new CancellationTokenSource();
            var httpRequestMessage = new HttpRequestMessage(HttpMethod.Get, new Uri(url));
            var requestMessageWrapper = new FoundationHttpRequestMessageWrapper(httpRequestMessage, () =>
                                                                                                        {
                                                                                                            cts.Cancel();
                                                                                                            responseDisposer.Dispose();
                                                                                                        });

            prepareRequest(requestMessageWrapper);

            var responseMessage = await GetHttpClient(isLongRunning)
                                      .SendRequestAsync(httpRequestMessage, HttpCompletionOption.ResponseContentRead)
                                      .AsTask(cts.Token);

            if (!responseMessage.IsSuccessStatusCode)
            {
                throw new HttpClientException(await ToNetResponse(responseMessage));
            }

            responseDisposer.Set(responseMessage);

            return new FoundationHttpResponseMessageWrapper(responseMessage);
        }

        /// <summary>
        /// Initialize the Http Clients
        /// </summary>
        /// <param name="connection">Connection</param>
        public void Initialize(IConnection connection)
        {
            _connection = connection;

            InitializeFilter(_httpFilter, _connection);

            _longRunningClient = CreateHttpClient(_httpFilter);
            _shortRunningClient = CreateHttpClient(_httpFilter);
        }

        /// <summary>
        /// Makes an asynchronous http POST request to the specified url.
        /// </summary>
        /// <param name="url">The url to send the request to.</param>
        /// <param name="prepareRequest">A callback that initializes the request with default values.</param>
        /// <param name="postData">form url encoded data.</param>
        /// <param name="isLongRunning">Indicates whether the request is long running</param>
        /// <returns>A <see cref="T:Task{IResponse}"/>.</returns>
        public async Task<IResponse> Post(string url, Action<IRequest> prepareRequest,
                                          IDictionary<string, string> postData, bool isLongRunning)
        {
            var responseDisposer = new Disposer();
            var cts = new CancellationTokenSource();
            var httpRequestMessage = new HttpRequestMessage(HttpMethod.Post, new Uri(url));

            httpRequestMessage.Content = postData != null
                                             ? new HttpBufferContent(ProcessPostData(postData).AsBuffer())
                                             : (IHttpContent)new HttpStringContent(string.Empty);

            var requestMessageWrapper = new FoundationHttpRequestMessageWrapper(httpRequestMessage, () =>
                                                                                                        {
                                                                                                            cts.Cancel();
                                                                                                            responseDisposer.Dispose();
                                                                                                        });

            prepareRequest(requestMessageWrapper);

            var responseMessage = await GetHttpClient(isLongRunning)
                                      .SendRequestAsync(httpRequestMessage, HttpCompletionOption.ResponseContentRead)
                                      .AsTask(cts.Token);

            if (!responseMessage.IsSuccessStatusCode)
            {
                throw new HttpClientException(await ToNetResponse(responseMessage));
            }

            responseDisposer.Set(responseMessage);

            return new FoundationHttpResponseMessageWrapper(responseMessage);
        }

        protected virtual HttpClient CreateHttpClient(HttpBaseProtocolFilter filter) => new HttpClient(filter);

        protected virtual void InitializeFilter(HttpBaseProtocolFilter filter, IConnection connection)
        {
            const string NOT_SUPPORTED = "Member {0} is not supported";

            if (connection.Proxy != null)
            {
                throw new NotSupportedException(string.Format(NOT_SUPPORTED,
                                                              nameof(connection.Proxy)));
            }

            if (connection.Credentials != null)
            {
                throw new NotSupportedException(string.Format(NOT_SUPPORTED,
                                                              nameof(connection.Credentials)));
            }

            if (connection.CookieContainer != null)
            {
                throw new NotSupportedException(string.Format(NOT_SUPPORTED,
                                                              nameof(connection.CookieContainer)));
            }
        }

        /// <summary>
        /// Returns the appropriate client based on whether it is a long running request
        /// </summary>
        /// <param name="isLongRunning">Indicates whether the request is long running</param>
        /// <returns></returns>
        private HttpClient GetHttpClient(bool isLongRunning) => isLongRunning
                       ? _longRunningClient
                       : _shortRunningClient;

        private static byte[] ProcessPostData(IDictionary<string, string> postData)
        {
            if (postData == null || postData.Count == 0)
            {
                return null;
            }

            var sb = new StringBuilder();
            foreach (var pair in postData)
            {
                if (sb.Length > 0)
                {
                    sb.Append("&");
                }

                if (string.IsNullOrEmpty(pair.Value))
                {
                    continue;
                }

                sb.AppendFormat(CultureInfo.InvariantCulture, "{0}={1}", pair.Key, WebUtility.UrlEncode(pair.Value));
            }

            return Encoding.UTF8.GetBytes(sb.ToString());
        }

        /// <summary>
        /// Converts <see cref="Windows.Web.Http.HttpResponseMessage"/> to <see cref="HttpResponseMessage"/>
        /// </summary>
        /// <param name="foundationResponse">Message that gets converted</param>
        /// <returns></returns>
        private static async Task<HttpResponseMessage> ToNetResponse(
            Windows.Web.Http.HttpResponseMessage foundationResponse)
        {
            var netResponse = new HttpResponseMessage((HttpStatusCode)foundationResponse.StatusCode)
            {
                Content = new StringContent(await foundationResponse.Content.ReadAsStringAsync()),
                ReasonPhrase = foundationResponse.ReasonPhrase
            };
            switch (foundationResponse.Version)
            {
                case HttpVersion.None:
                    netResponse.Version = new Version(0, 0);
                    break;
                case HttpVersion.Http10:
                    netResponse.Version = new Version(1, 0);
                    break;
                case HttpVersion.Http11:
                    netResponse.Version = new Version(1, 1);
                    break;
                case HttpVersion.Http20:
                    netResponse.Version = new Version(2, 0);
                    break;
            }

            foreach (var header in foundationResponse.Headers)
            {
                netResponse.Headers.Add(header.Key, header.Value);
            }

            var foundationRequest = foundationResponse.RequestMessage;
            netResponse.RequestMessage =
                new System.Net.Http.HttpRequestMessage(new System.Net.Http.HttpMethod(foundationRequest.Method.Method), foundationRequest.RequestUri)
                {
                    Version = new Version(0, 0)
                };
            if (foundationRequest.Content != null)
            {
                netResponse.RequestMessage.Content =
                    new StringContent(await foundationRequest.Content.ReadAsStringAsync());
            }

            foreach (var header in foundationRequest.Headers)
            {
                netResponse.RequestMessage.Headers.Add(header.Key, header.Value);
            }

            foreach (var property in foundationRequest.Properties)
            {
                netResponse.RequestMessage.Properties.Add(property);
            }

            return netResponse;
        }

        private class Disposer : IDisposable
        {
            private static readonly object _disposedSentinel = new object();

            private object _disposable;

            public void Dispose() => Dispose(true);

            public void Set(IDisposable disposable)
            {
                if (disposable == null)
                {
                    throw new ArgumentNullException(nameof(disposable));
                }

                var originalFieldValue = Interlocked.CompareExchange(ref _disposable, disposable, null);
                if (originalFieldValue == null)
                {
                    // this is the first call to Set() and Dispose() hasn't yet been called; do nothing
                }
                else if (originalFieldValue == _disposedSentinel)
                {
                    // Dispose() has already been called, so we need to dispose of the object that was just added
                    disposable.Dispose();
                }
                else
                {
#if !PORTABLE && !NETFX_CORE
                    // Set has been called multiple times, fail
                    Debug.Fail("Multiple calls to Disposer.Set(IDisposable) without calling Disposer.Dispose()");
#endif
                }
            }

            protected virtual void Dispose(bool disposing)
            {
                if (!disposing)
                {
                    return;
                }

                var disposable = Interlocked.Exchange(ref _disposable, _disposedSentinel) as IDisposable;
                disposable?.Dispose();
            }
        }
    }

    public class FoundationHttpResponseMessageWrapper : IResponse, IDisposable
    {
        private readonly Windows.Web.Http.HttpResponseMessage _httpResponseMessage;

        public FoundationHttpResponseMessageWrapper(Windows.Web.Http.HttpResponseMessage httpResponseMessage) => _httpResponseMessage = httpResponseMessage;

        public void Dispose() => Dispose(true);

        public Stream GetStream() => _httpResponseMessage.Content.ReadAsInputStreamAsync().GetResults().AsStreamForRead();

        public string ReadAsString() => _httpResponseMessage.Content.ReadAsStringAsync().GetResults();

        protected virtual void Dispose(bool disposing)
        {
            if (!disposing)
            {
                return;
            }

            _httpResponseMessage.RequestMessage.Dispose();
            _httpResponseMessage.Dispose();
        }
    }

    public class FoundationHttpRequestMessageWrapper : IRequest
    {
        private readonly Action _cancel;
        private readonly HttpRequestMessage _httpRequestMessage;

        public FoundationHttpRequestMessageWrapper(HttpRequestMessage httpRequestMessage, Action cancel)
        {
            _httpRequestMessage = httpRequestMessage;
            _cancel = cancel;
        }

        public string Accept { get; set; }

        public string UserAgent { get; set; }

        public void Abort() => _cancel();

        public void SetRequestHeaders(IDictionary<string, string> headers)
        {
            if (headers == null)
            {
                throw new ArgumentNullException(nameof(headers));
            }

            if (UserAgent != null)
            {
                _httpRequestMessage.Headers.TryAppendWithoutValidation("User-Agent", UserAgent);
            }

            if (Accept != null)
            {
                _httpRequestMessage.Headers.Accept.Add(new HttpMediaTypeWithQualityHeaderValue(Accept));
            }

            foreach (var header in headers)
            {
                _httpRequestMessage.Headers.Add(header.Key, header.Value);
            }
        }
    }
}