using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Payment.Domain.ECommerce;

namespace Paymant_Module_NEOXONLINE.Controllers.Payment
{
    [Route("api/[controller]")]
    [ApiController]
    public class StripeController : ControllerBase
    {
        [HttpGet("GetInfo")]

        public async Task<IActionResult> Get()
        {
            return Ok("this is the stripe controller. here will be logic for interacting with the stripe api");
        }
    }
}
