using System.Collections.Generic;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Azure.Devices.Provisioning.Client;
using Microsoft.Extensions.Configuration;
using Smartbox.DeviceProvisioning.API.Models;

namespace Smartbox.DeviceProvisioning.API.Controllers
{
    [Route("api/[controller]")]
    [ApiController]
    public class RegistrationController : ControllerBase
    {
        private readonly IDeviceProvisioningService deviceProvisioningService;
        private readonly IConfiguration configuration;

        public RegistrationController(IConfiguration configuration)
        {
            this.deviceProvisioningService = new DeviceProvisioningService(configuration);
            this.configuration = configuration;
        }

        // GET: api/Registration
        [HttpGet]
        public IEnumerable<string> Get()
        {
            return new string[] { "value1", "value2" };
        }

        // POST: api/Registration
        [HttpPost]
        public DeviceRegistrationResult RegisterDevice([FromBody] Registration registration)
        {
            return deviceProvisioningService.RegisterDevice(registration.DeviceId, registration.Password);
        }
    }
}
