using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

using FoundationHttpClientDemo.Common;

namespace FoundationHttpClientDemo.Server
{
    public class NetshCertificateBinding : ConsoleCommandRunner
    {
        public Task CreateNetshSslPortBinding(string serverCertificateThumbprint) => RunConsoleCommandAndShowOutputAsync($"/C netsh http add sslcert " +
                                                                                                                         $"ipport=0.0.0.0:{Constants.PORT} " +
                                                                                                                         $"appid={{12345678-db90-4b66-8b01-88f7af2e36bf}} " +
                                                                                                                         $"certhash={serverCertificateThumbprint}");

        public Task RemoveNetshSslPortBinding() => RunConsoleCommandAndShowOutputAsync($"/C netsh http delete sslcert ipport=0.0.0.0:{Constants.PORT}");
    }
}
