using Microsoft.Extensions.Configuration;
using System;
using System.IO;
using System.Security.Cryptography;
using System.Security.Cryptography.X509Certificates;

namespace Smartbox.DeviceProvisioning.API
{
    public interface ICertificateManager
    {
        X509Certificate2 GenerateCertificate(string deviceId);
        void SaveCertificates(X509Certificate2 cert, string deviceId, string password);
        X509Certificate2 ReadPublicCertificate(string deviceId);
        X509Certificate2 ReadPrivateCertificate(string deviceId, string password);
    }

    public class CertificateManager : ICertificateManager
    {
        private readonly string CertificateDirectory;

        public CertificateManager(IConfiguration configuration)
        {
            CertificateDirectory = configuration.GetValue<string>("CertificateDirectory");
        }

        public X509Certificate2 GenerateCertificate(string deviceId)
        {
            var rsa = RSA.Create(2048);
            var subject = $"CN={deviceId}, O=TEST, C=US";
            var req = new CertificateRequest(subject, rsa, HashAlgorithmName.SHA256, RSASignaturePadding.Pkcs1);
            var usages = X509KeyUsageFlags.DataEncipherment | X509KeyUsageFlags.KeyEncipherment | X509KeyUsageFlags.DigitalSignature;
            req.CertificateExtensions.Add(new X509KeyUsageExtension(usages, false));
            return req.CreateSelfSigned(DateTimeOffset.Now, DateTimeOffset.Now.AddYears(5));
        }

        public void SaveCertificates(X509Certificate2 cert, string deviceId, string password)
        {
            var deviceDirectory = Path.Combine(CertificateDirectory, deviceId);
            Directory.CreateDirectory(CertificateDirectory + deviceId);

            File.WriteAllBytes(Path.Combine(deviceDirectory, "cert.pfx"), cert.Export(X509ContentType.Pfx, password));

            File.WriteAllText(Path.Combine(deviceDirectory, "cert.cer"), "-----BEGIN CERTIFICATE-----\r\n"
                + Convert.ToBase64String(cert.Export(X509ContentType.Cert), Base64FormattingOptions.InsertLineBreaks)
                + "\r\n-----END CERTIFICATE-----");
        }

        public X509Certificate2 ReadPublicCertificate(string deviceId)
        {
            var key = Path.Combine(CertificateDirectory, deviceId, "cert.cer");

            return new X509Certificate2(key);
        }

        public X509Certificate2 ReadPrivateCertificate(string deviceId, string password)
        {
            var key = Path.Combine(CertificateDirectory, deviceId, "cert.pfx");

            var certificateCollection = new X509Certificate2Collection();
            certificateCollection.Import(key, password, X509KeyStorageFlags.Exportable | X509KeyStorageFlags.PersistKeySet);

            foreach (var element in certificateCollection)
            {
                if (element.HasPrivateKey)
                {
                    return element;
                }
                else
                {
                    element.Dispose();
                }
            }

            throw new InvalidOperationException("No private key found.");
        }
    }
}
