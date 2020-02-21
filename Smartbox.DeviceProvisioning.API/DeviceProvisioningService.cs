using Microsoft.Azure.Devices.Provisioning.Client;
using Microsoft.Azure.Devices.Provisioning.Client.Transport;
using Microsoft.Azure.Devices.Provisioning.Service;
using Microsoft.Azure.Devices.Shared;
using Microsoft.Extensions.Configuration;
using System;
using System.IO;
using System.Security.Cryptography;
using System.Security.Cryptography.X509Certificates;

namespace Smartbox.DeviceProvisioning.API
{
    public interface IDeviceProvisioningService
    {
        IndividualEnrollment EnrollDevice(string deviceId, string password);
        DeviceRegistrationResult RegisterDevice(string deviceId, string password);
    }

    public class DeviceProvisioningService : IDeviceProvisioningService
    {
        private readonly IConfiguration configuration;

        private readonly string PFXDirectory = "C:\\Helper Tools\\Smartbox.DeviceProvisioning\\Smartbox.DeviceProvisioning.API\\Certificates\\";

        public DeviceProvisioningService(IConfiguration configuration)
        {
            this.configuration = configuration;
        }

        public IndividualEnrollment EnrollDevice(string deviceId, string password)
        {
            var cert = GenerateCertificate(deviceId);

            SavePrivateCert(cert, deviceId, password);

            var publicCert = new X509Certificate2(cert.Export(X509ContentType.Cert, password));

            var attestation = X509Attestation.CreateFromClientCertificates(publicCert);
            var individualEnrollment = new IndividualEnrollment($"{deviceId}", attestation)
            {
                DeviceId = deviceId
            };

            using (var provisioningClientService = 
                ProvisioningServiceClient.CreateFromConnectionString(configuration.GetValue<string>("DeviceProvisioningServiceConnection")))
            {
                var individualEnrollmentResult =
                    provisioningClientService.CreateOrUpdateIndividualEnrollmentAsync(individualEnrollment).GetAwaiter().GetResult();

                return individualEnrollment;
            }            
        }

        public DeviceRegistrationResult RegisterDevice(string deviceId, string password)
        {
            var cert = ReadCertificate(deviceId, password);

            using (var security = new SecurityProviderX509Certificate(cert))
            {
                using (var transport = new ProvisioningTransportHandlerAmqp(TransportFallbackType.TcpOnly))
                {
                    ProvisioningDeviceClient provClient =
                        ProvisioningDeviceClient.Create(configuration.GetValue<string>("DeviceProvisioningEndpoint"), configuration.GetValue<string>("DeviceProvisioningScope"), security, transport);

                    var result = provClient.RegisterAsync().GetAwaiter().GetResult();

                    return result;
                }
            }
        }

        private X509Certificate2 GenerateCertificate(string deviceId)
        {
            var ecdsa = ECDsa.Create();
            var subject = $"CN={deviceId}, O=TEST, C=US";
            var req = new CertificateRequest(subject, ecdsa, HashAlgorithmName.SHA256);
            return req.CreateSelfSigned(DateTimeOffset.Now, DateTimeOffset.Now.AddYears(5));
        }

        private void SavePrivateCert(X509Certificate2 cert, string deviceId, string password)
        {
            var deviceDirectory = Path.Combine(PFXDirectory, deviceId);
            Directory.CreateDirectory(PFXDirectory + deviceId);

            File.WriteAllBytes(Path.Combine(deviceDirectory, "cert.pfx"), cert.Export(X509ContentType.Pfx, password));
        }

        private X509Certificate2 ReadCertificate(string deviceId, string password)
        {
            var deviceDirectory = Path.Combine(PFXDirectory, deviceId);
            var certBytes = File.ReadAllBytes(Path.Combine(deviceDirectory, "cert.pfx"));

            return new X509Certificate2(certBytes, password, X509KeyStorageFlags.PersistKeySet);
        }

        private void Something()
        {

        }
    }
}
