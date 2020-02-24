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
            var deviceManager = new DeviceManager(configuration);

            this.deviceProvisioningService = new DeviceProvisioningService(configuration, deviceManager);
            this.configuration = configuration;
        }

        // GET: api/Registration
        [HttpGet]
        public IEnumerable<string> Get()
        {
            return new string[] { "Use POST api/Registration to register/provision an  enrolled device." };
        }

        // POST: api/Registration
        [HttpPost]
        public DeviceRegistrationResult RegisterDevice([FromBody] Registration registration)
        {
            var registrationResult = deviceProvisioningService.RegisterDevice(registration.DeviceId, registration.Password);

            return registrationResult;
        }
    }
}
