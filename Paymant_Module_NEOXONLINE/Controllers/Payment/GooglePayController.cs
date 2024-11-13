using Microsoft.AspNetCore.Mvc;
using Payment.BLL.Contracts.Payment;
using Payment.Domain.ECommerce;
using System.Threading.Tasks;

namespace Paymant_Module_NEOXONLINE.Controllers.Payment
{
    [Route("api/[controller]")]
    [ApiController]
    public class GooglePayController : ControllerBase
    {
        private readonly IStripeService _stripeService;

        public GooglePayController(IStripeService stripeService)
        {
            _stripeService = stripeService;
        }

        [HttpPost("googlepay")]
        public async Task<IActionResult> ProcessGooglePayPayment([FromBody] PaymentBasket basket, [FromQuery] string googlePayToken)
        {
            if (basket == null || string.IsNullOrEmpty(googlePayToken))
            {
                return BadRequest("Invalid basket data or Google Pay token.");
            }

            var result = await _stripeService.ProcessGooglePayPaymentAsync(basket, googlePayToken);

            if (result.Contains("completed successfully"))
            {
                return Ok(new { message = result });
            }
            else if (result.Contains("processing"))
            {
                return Accepted(new { message = result });
            }
            else
            {
                return BadRequest(new { message = result });
            }
        }

        [HttpPost("create-donation")]
        public async Task<IActionResult> CreateGooglePayDonation([FromQuery] decimal amount, [FromQuery] string currency, [FromQuery] string googlePayToken)
        {
            if (amount <= 0 || string.IsNullOrEmpty(googlePayToken) || string.IsNullOrEmpty(currency))
            {
                return BadRequest("Invalid donation amount, currency, or Google Pay token.");
            }

            var result = await _stripeService.CreateGooglePayDonationAsync(amount, currency, googlePayToken);

            if (result.Contains("completed successfully"))
            {
                return Ok(new { message = result });
            }
            else if (result.Contains("processing"))
            {
                return Accepted(new { message = result });
            }
            else
            {
                return BadRequest(new { message = result });
            }
        }
    }
}
