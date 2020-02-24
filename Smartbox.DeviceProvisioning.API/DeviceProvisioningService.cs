using Microsoft.Azure.Devices.Provisioning.Client;
using Microsoft.Azure.Devices.Provisioning.Client.Transport;
using Microsoft.Azure.Devices.Provisioning.Service;
using Microsoft.Azure.Devices.Shared;
using Microsoft.Extensions.Configuration;

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
        private readonly ICertificateManager certificateManager;

        public DeviceProvisioningService(IConfiguration configuration, ICertificateManager certificateManager)
        {
            this.configuration = configuration;
            this.certificateManager = certificateManager;
        }

        public IndividualEnrollment EnrollDevice(string deviceId, string password)
        {
            var cert = certificateManager.GenerateCertificate(deviceId);

            certificateManager.SaveCertificates(cert, deviceId, password);

            var publicCert = certificateManager.ReadPublicCertificate(deviceId);

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
            var cert = certificateManager.ReadPrivateCertificate(deviceId, password);

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
    }
}
