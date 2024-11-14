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
        public async Task<IActionResult> CreateGooglePayDonation([FromQuery] decimal amount, [FromQuery] string currency, [FromQuery] string googlePayToken, [FromQuery] string customerId)
        {
            if (amount <= 0 || string.IsNullOrEmpty(googlePayToken) || string.IsNullOrEmpty(currency) || string.IsNullOrEmpty(customerId))
            {
                return BadRequest("Invalid donation amount, currency, Google Pay token, or customer ID.");
            }

            // Попытка извлечь только `id` из переданного токена
            string tokenId;
            try
            {
                var tokenData = Newtonsoft.Json.JsonConvert.DeserializeObject<dynamic>(googlePayToken);
                tokenId = tokenData?.id;
                if (string.IsNullOrEmpty(tokenId))
                {
                    return BadRequest("Invalid token format.");
                }
            }
            catch (Exception ex)
            {
                return BadRequest($"Error parsing token: {ex.Message}");
            }

            // Передаем `amount`, `currency`, `tokenId`, и `customerId` в сервис
            var result = await _stripeService.CreateGooglePayDonationAsync(amount, currency, tokenId, customerId);

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