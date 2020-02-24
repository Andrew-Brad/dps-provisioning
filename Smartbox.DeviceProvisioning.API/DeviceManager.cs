using Microsoft.Azure.Devices.Client;
using Microsoft.Azure.Devices.Provisioning.Client;
using Microsoft.Extensions.Configuration;
using Newtonsoft.Json;
using Smartbox.DeviceProvisioning.API.Models;
using System;
using System.IO;
using System.Security.Cryptography;
using System.Security.Cryptography.X509Certificates;
using System.Text;
using System.Threading.Tasks;

namespace Smartbox.DeviceProvisioning.API
{
    public interface IDeviceManager
    {
        X509Certificate2 GenerateCertificate(string deviceId);
        void SaveCertificates(X509Certificate2 cert, string deviceId, string password);
        X509Certificate2 ReadPublicCertificate(string deviceId);
        X509Certificate2 ReadPrivateCertificate(string deviceId, string password);
        void SaveRegistration(RegistrationInfo registrationResult);
        void SavePassword(string deviceId, string password);
        Task<string> SendMessage(string deviceId, string message);
    }

    public class DeviceManager : IDeviceManager
    {
        private readonly string Directory;

        public DeviceManager(IConfiguration configuration)
        {
            Directory = configuration.GetValue<string>("CertificateDirectory");
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
            var deviceDirectory = Path.Combine(Directory, deviceId);
            System.IO.Directory.CreateDirectory(Directory + deviceId);

            File.WriteAllBytes(Path.Combine(deviceDirectory, "cert.pfx"), cert.Export(X509ContentType.Pfx, password));

            File.WriteAllText(Path.Combine(deviceDirectory, "cert.cer"), "-----BEGIN CERTIFICATE-----\r\n"
                + Convert.ToBase64String(cert.Export(X509ContentType.Cert), Base64FormattingOptions.InsertLineBreaks)
                + "\r\n-----END CERTIFICATE-----");
        }

        public X509Certificate2 ReadPublicCertificate(string deviceId)
        {
            var key = Path.Combine(Directory, deviceId, "cert.cer");

            return new X509Certificate2(key);
        }

        public X509Certificate2 ReadPrivateCertificate(string deviceId, string password)
        {
            var key = Path.Combine(Directory, deviceId, "cert.pfx");

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

        public void SaveRegistration(RegistrationInfo registrationInfo)
        {
            var path = Path.Combine(Directory, registrationInfo.DeviceId, "registration.info");
            File.WriteAllText(path, JsonConvert.SerializeObject(registrationInfo));
        }

        public void SavePassword(string deviceId, string password)
        {
            var path = Path.Combine(Directory, deviceId, "password.info");
            File.WriteAllText(path, password);
        }

        private string GetPassword(string deviceId)
        {
            var path = Path.Combine(Directory, deviceId, "password.info");
            return File.ReadAllText(path);
        }

        public async Task<string> SendMessage(string deviceId, string message)
        {
            var path = Path.Combine(Directory, deviceId, "registration.info");
            var registrationInfo = JsonConvert.DeserializeObject<RegistrationInfo>(File.ReadAllText(path));
            var auth = new DeviceAuthenticationWithX509Certificate(deviceId, ReadPrivateCertificate(deviceId, GetPassword(deviceId)));

            using (DeviceClient iotClient = DeviceClient.Create(registrationInfo.AssignedHub, auth, TransportType.Amqp))
            {
                await iotClient.OpenAsync().ConfigureAwait(false);
                await iotClient.SendEventAsync(new Microsoft.Azure.Devices.Client.Message(Encoding.UTF8.GetBytes(message))).ConfigureAwait(false);
                await iotClient.CloseAsync().ConfigureAwait(false);
            }

            return "sent";
        }
    }
}
