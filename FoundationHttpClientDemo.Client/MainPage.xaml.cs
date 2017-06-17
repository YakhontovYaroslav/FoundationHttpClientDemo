//#define WILL_NOT_COMPILE
#define UNSUPPORTED_EXCEPTION_IN_RUNTIME
//#define WINDOWS_WEB_HTTP_CLIENT

using System;
using System.Collections;
using System.Collections.Generic;
using System.Collections.Specialized;
using System.ComponentModel;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Net.Http;
using System.Net.Security;
using System.Runtime.CompilerServices;
using System.Runtime.InteropServices.WindowsRuntime;
using System.Security.Cryptography.X509Certificates;
using System.Text;

using Windows.ApplicationModel;
using Windows.Foundation;
using Windows.Foundation.Collections;
using Windows.Security.Cryptography.Certificates;
using Windows.Storage;
using Windows.UI.Xaml;
using Windows.UI.Xaml.Controls;
using Windows.UI.Xaml.Controls.Primitives;
using Windows.UI.Xaml.Data;
using Windows.UI.Xaml.Input;
using Windows.UI.Xaml.Media;
using Windows.UI.Xaml.Navigation;
using Windows.Web.Http.Filters;
using FoundationHttpClientDemo.Common;
using Microsoft.AspNet.SignalR.Client;
using Microsoft.AspNet.SignalR.Client.Http;
using Microsoft.AspNet.SignalR.Client.Hubs;
using Newtonsoft.Json.Linq;

// The Blank Page item template is documented at https://go.microsoft.com/fwlink/?LinkId=402352&clcid=0x409

namespace FoundationHttpClientDemo.Client
{
    /// <summary>
    /// An empty page that can be used on its own or navigated to within a Frame.
    /// </summary>
    public sealed partial class MainPage : Page
    {
        private HubConnection connection;
        private IHubProxy hub;
        private Subscription subscription;
        private Certificate remoteServerCertificate;

        public MainPage()
        {
            this.InitializeComponent();
#if WILL_NOT_COMPILE
            ServicePointManager.ServerCertificateValidationCallback +=
                (sender, certificate, chain, sslPolicyErrors) =>
                true;
#endif
        }

        protected override async void OnNavigatedTo(NavigationEventArgs e)
        {
            var certificateFile = await (await Package.Current.InstalledLocation.GetFolderAsync("Certs")).GetFileAsync("DevServer.cer");
            remoteServerCertificate = new Certificate(await FileIO.ReadBufferAsync(certificateFile));

            base.OnNavigatedTo(e);
        }

        protected override void OnNavigatedFrom(NavigationEventArgs e)
        {
            if (connection == null)
            {
                return;
            }

            connection.StateChanged -= Connection_StateChanged;
            connection.Error -= Connection_Error;
            subscription.Received -= Subscription_Received;

            (subscription as IDisposable)?.Dispose();
            (hub as IDisposable)?.Dispose();
            connection.Dispose();

            base.OnNavigatedFrom(e);
        }

        private void Subscription_Received(IList<JToken> obj)
        {
            Debug.WriteLine("Message from server: ");
            foreach (var token in obj)
            {
                Debug.WriteLine(token.ToString());
            }
        }

        private void Connection_Error(Exception ex)
        {
            Debug.WriteLine(ex.Message);
        }

        private void Connection_StateChanged(StateChange state)
        {
            Debug.WriteLine(state.NewState);
        }

        private void CreateHubProxy_Click(object sender, RoutedEventArgs e)
        {
            connection = new HubConnection($"https://{Constants.LOCAL_AREA_SERVER_IP}:{Constants.PORT}/")
                             {
                                 TraceLevel = TraceLevels.All,
                                 TraceWriter = new DebugTextWriter(),
                             };
            connection.Headers.Add(Constants.AUTH_TOKEN_HEADER_NAME,
                                   AuthTokenUtils.GenerateAuthToken(Constants.USER_NAME, Constants.PASSWORD));
            connection.StateChanged += Connection_StateChanged;
            connection.Error += Connection_Error;

            hub = connection.CreateHubProxy(Constants.HUB_NAME);

            subscription = hub.Subscribe(nameof(IHubClient.SayHello));
            subscription.Received += Subscription_Received;
        }

        private void Connect_Click(object sender, RoutedEventArgs e)
        {
            try
            {
#if UNSUPPORTED_EXCEPTION_IN_RUNTIME
                Func<HttpRequestMessage, X509Certificate2, X509Chain, SslPolicyErrors, bool> validation =
                    (message, certificate, chain, policy) => { return true; };
                connection?.Start(new HttpClientOverrideCertificateValidation(validation));
#elif WINDOWS_WEB_HTTP_CLIENT

                var filter = new HttpBaseProtocolFilter();
                filter.IgnorableServerCertificateErrors.Add(ChainValidationResult.InvalidName);
                filter.ServerCustomValidationRequested += (s, args) =>
                                                              {
                                                                  if (args.ServerCertificateErrors
                                                                          .Any(error => error == ChainValidationResult.InvalidName &&
                                                                                        !StructuralComparisons.StructuralEqualityComparer
                                                                                            .Equals(remoteServerCertificate.SerialNumber, args.ServerCertificate.SerialNumber)))
                                                                  {
                                                                      args.Reject();
                                                                  }
                                                              };
                connection?.Start(new FoundationHttpClient(filter));
#else
                connection?.Start();
#endif
            }
            catch (Exception ex)
            {
                Debug.WriteLine(ex.Message);
            }
        }

        private void Say_Click(object sender, RoutedEventArgs e)
        {
            try
            {
                hub?.Invoke(nameof(IHubServer.SayHelloAsync), "Hello!");
            }
            catch (Exception ex)
            {
                Debug.WriteLine(ex.Message);
            }
        }

#if UNSUPPORTED_EXCEPTION_IN_RUNTIME
        private class HttpClientOverrideCertificateValidation : DefaultHttpClient
        {
            private readonly Func<HttpRequestMessage, X509Certificate2, X509Chain, SslPolicyErrors, bool> _callback;

            public HttpClientOverrideCertificateValidation(
                Func<HttpRequestMessage, X509Certificate2, X509Chain, SslPolicyErrors, bool> callback)
            {
                _callback = callback;
            }

            protected override HttpMessageHandler CreateHandler()
            {
                var handler = base.CreateHandler();
                try
                {
                    if (handler is HttpClientHandler http)
                    {
                        http.ServerCertificateCustomValidationCallback = _callback;
                    }
                }
                catch (Exception ex)
                {
                    Debug.WriteLine(ex.Message);
                }

                return handler;
            }
        }
#endif

        private class DebugTextWriter : StreamWriter
        {
            public DebugTextWriter()
                : base(new DebugOutStream(), Encoding.Unicode, 1024)
            {
                this.AutoFlush = true;
            }

            private class DebugOutStream : Stream
            {
                public override void Write(byte[] buffer, int offset, int count) => 
                    Debug.WriteLine(Encoding.Unicode.GetString(buffer, offset, count));

                public override bool CanRead => false;
                public override bool CanSeek => false;
                public override bool CanWrite => true;

                public override void Flush()
                {
                }

                public override long Length => throw new InvalidOperationException();
                public override int Read(byte[] buffer, int offset, int count) => throw new InvalidOperationException();
                public override long Seek(long offset, SeekOrigin origin) => throw new InvalidOperationException();
                public override void SetLength(long value) => throw new InvalidOperationException();

                public override long Position
                {
                    get => throw new InvalidOperationException();
                    set => throw new InvalidOperationException();
                }
            };
        }
    }
}
