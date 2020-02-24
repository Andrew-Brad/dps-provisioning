using System.Collections.Generic;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Configuration;
using Smartbox.DeviceProvisioning.API.Models;

namespace Smartbox.DeviceProvisioning.API.Controllers
{
    [Route("api/[controller]")]
    [ApiController]
    public class MessageController : ControllerBase
    {
        private readonly IDeviceManager deviceManager;

        public MessageController(IConfiguration configuration)
        {
            this.deviceManager = new DeviceManager(configuration);
        }

        // GET: api/Message
        [HttpGet]
        public IEnumerable<string> Get()
        {
            return new string[] { "value1", "value2" };
        }

        // POST: api/Message
        [HttpPost]
        public async Task<string> Post([FromBody] Message message)
        {
            return await deviceManager.SendMessage(message.DeviceId, message.MessageContents);
        }
    }
}
