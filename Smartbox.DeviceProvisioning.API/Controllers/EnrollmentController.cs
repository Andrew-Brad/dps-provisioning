using System.Collections.Generic;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Azure.Devices.Provisioning.Service;
using Microsoft.Extensions.Configuration;
using Smartbox.DeviceProvisioning.API.Models;

namespace Smartbox.DeviceProvisioning.API.Controllers
{
    [Route("api/[controller]")]
    [ApiController]
    public class EnrollmentController : ControllerBase
    {
        private readonly IDeviceProvisioningService deviceProvisioningService;
        private readonly IConfiguration configuration;

        public EnrollmentController(IConfiguration configuration)
        {
            var certificateManager = new CertificateManager(configuration);

            this.deviceProvisioningService = new DeviceProvisioningService(configuration, certificateManager);
            this.configuration = configuration;
        }

        // GET: api/Enrollment
        [HttpGet]
        public IEnumerable<string> Get()
        {
            return new string[] { configuration.GetValue<string>("DeviceProvisioningEndpoint"), configuration.GetValue<string>("DeviceProvisioningScope") };
        }

        // POST: api/Enrollment
        [HttpPost]
        public IndividualEnrollment Enroll([FromBody] Enrollment enrollment)
        {
            return deviceProvisioningService.EnrollDevice(enrollment.DeviceId, enrollment.Password);
        }
    }
}
