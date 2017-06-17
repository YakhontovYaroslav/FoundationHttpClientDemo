using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Security.Cryptography.X509Certificates;
using System.Text;
using System.Threading.Tasks;

namespace FoundationHttpClientDemo.Server
{
    public class TemporarySelfSignedCertificates : ConsoleCommandRunner
    {
        private const string PFX_PASSWORD = "qwerty";

        private const string CA_ROOT_CN = "CARoot";
        private const string DEV_SERVER_CN = "DevServer";

        public async Task<string> CreateSelfSignedCertificates()
        {
            await GenerateRootCertificateAsync();
            var certThumbprint = await GenerateServerCertificateAsync();

            AddCertificateToStorage(CA_ROOT_CN, StoreName.Root, StoreLocation.LocalMachine);
            AddCertificateToStorage(DEV_SERVER_CN, StoreName.My, StoreLocation.LocalMachine);

            return certThumbprint;
        }

        public void RemoveSelfSignedCertificates()
        {
            RemoveCertificateFromStorage(new X509Certificate2($"{CA_ROOT_CN}.pfx", PFX_PASSWORD).Thumbprint, StoreName.Root, StoreLocation.LocalMachine);
            RemoveCertificateFromStorage(new X509Certificate2($"{DEV_SERVER_CN}.pfx", PFX_PASSWORD).Thumbprint, StoreName.My, StoreLocation.LocalMachine);

            DeleteCertificates(CA_ROOT_CN);
            DeleteCertificates(DEV_SERVER_CN);
        }

        private static void DeleteCertificates(string name)
        {
            DeleteFile($"{name}.cer");
            DeleteFile($"{name}.pvk");
            DeleteFile($"{name}.pfx");
        }

        private static void DeleteFile(string fileName)
        {
            if (File.Exists(fileName))
            {
                File.Delete(fileName);
            }
        }

        private static void AddCertificateToStorage(string certificateName, StoreName storeName, StoreLocation location)
        {
            var flags = X509KeyStorageFlags.Exportable;
            if (location == StoreLocation.LocalMachine)
            {
                flags |= X509KeyStorageFlags.MachineKeySet;
            }

            var privateCert = new X509Certificate2($"{certificateName}.pfx", PFX_PASSWORD, flags);

            using (var store = new X509Store(storeName, location))
            {
                store.Open(OpenFlags.ReadWrite | OpenFlags.OpenExistingOnly);
                store.Add(privateCert);
            }
        }

        private static void RemoveCertificateFromStorage(string certificateThumbprint, StoreName storeName,
                                                         StoreLocation location)
        {
            using (var store = new X509Store(storeName, location))
            {
                store.Open(OpenFlags.ReadWrite | OpenFlags.OpenExistingOnly);
                var currentCerts = store.Certificates.Find(X509FindType.FindByThumbprint, certificateThumbprint, false);
                foreach (var cert in currentCerts)
                {
                    store.Remove(cert);
                }
            }
        }

        private async Task<string> GenerateRootCertificateAsync()
        {
            await RunConsoleCommandAndShowOutputAsync($"/C makecert.exe " +
                                                      $"-n \"CN={CA_ROOT_CN}\" " +
                                                      $"-b {DateTime.Now.AddDays(-3 * 7):MM\\/dd\\/yyyy} " +
                                                      $"-e {DateTime.Now.AddDays(3 * 7):MM\\/dd\\/yyyy} " +
                                                      $"-r " +
                                                      $"-pe " +
                                                      $"-a sha512 " +
                                                      $"-len 4096 " +
                                                      $"-cy authority " +
                                                      $"-sv {CA_ROOT_CN}.pvk " +
                                                      $"{CA_ROOT_CN}.cer");

            await GeneratePfxAsync(CA_ROOT_CN);

            return new X509Certificate2($"{CA_ROOT_CN}.pfx", PFX_PASSWORD).Thumbprint;
        }

        private async Task<string> GenerateServerCertificateAsync()
        {
            await RunConsoleCommandAndShowOutputAsync($"/C makecert.exe " +
                                                      $"-n \"CN={DEV_SERVER_CN}\" " +
                                                      $"-b {DateTime.Now.AddDays(-2 * 7):MM\\/dd\\/yyyy} " +
                                                      $"-e {DateTime.Now.AddDays(2 * 7):MM\\/dd\\/yyyy} " +
                                                      $"-iv {CA_ROOT_CN}.pvk " +
                                                      $"-ic {CA_ROOT_CN}.cer " +
                                                      $"-pe " +
                                                      $"-a sha512 " +
                                                      $"-len 4096 " +
                                                      $"-cy end " +
                                                      $"-sv {DEV_SERVER_CN}.pvk " +
                                                      $"{DEV_SERVER_CN}.cer");

            await GeneratePfxAsync(DEV_SERVER_CN);

            return new X509Certificate2($"{DEV_SERVER_CN}.pfx", PFX_PASSWORD).Thumbprint;
        }

        private Task GeneratePfxAsync(string certificateName) =>
            RunConsoleCommandAndShowOutputAsync($"/C pvk2pfx.exe " +
                                                $"-pvk {certificateName}.pvk " +
                                                $"-spc {certificateName}.cer " +
                                                $"-pfx {certificateName}.pfx " +
                                                $"-po {PFX_PASSWORD}");

    }
}
