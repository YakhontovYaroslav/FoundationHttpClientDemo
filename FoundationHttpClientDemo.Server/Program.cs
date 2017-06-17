using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using FoundationHttpClientDemo.Common;
using Microsoft.Owin.Hosting;

namespace FoundationHttpClientDemo.Server
{
    class Program
    {
        static void Main(string[] args)
        {
            var certificates = new TemporarySelfSignedCertificates();
            var portBinding = new NetshCertificateBinding();

            if (args.Any(x => x == "-create"))
            {
                var thumbprint = certificates.CreateSelfSignedCertificates().GetAwaiter().GetResult();
                portBinding.CreateNetshSslPortBinding(thumbprint).GetAwaiter().GetResult();
            } else if (args.Any(x => x == "-delete"))
            {
                certificates.RemoveSelfSignedCertificates();
                portBinding.RemoveNetshSslPortBinding().GetAwaiter().GetResult();
            }
            else
            {
                using (WebApp.Start<Startup>($"https://*:{Constants.PORT}"))
                {
                    Console.WriteLine($"Server is up and running on {Constants.PORT} port");
                    Console.WriteLine("Press 'Enter' to exit");
                    Console.ReadLine();
                }
            }
        }
    }
}
