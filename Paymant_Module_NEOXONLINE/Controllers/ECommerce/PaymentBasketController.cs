using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using Payment.Application.Payment_DAL.Contracts;
using Payment.Domain.ECommerce;

namespace Paymant_Module_NEOXONLINE.Controllers.ECommerce
{
    [Route("api/[controller]")]
    [ApiController]
    public class PaymentBasketController : ControllerBase
    {

        private readonly IUnitOfWork _unitOfWork;

        public PaymentBasketController(IUnitOfWork unitOfWork)
        {
            _unitOfWork = unitOfWork;
        }

        /// <summary>
        /// Gets info about all payment baskets in db
        /// </summary> 
        /// <response code="200">Returns info about all payment baskets in db</response>
        /// <response code="500">Server error</response>
        [HttpGet("AllPaymentBasket")]
        public async Task<IActionResult> GetAllPaymentBasket()
        {
            var repoPayment = await _unitOfWork.GetAllIncluding<PaymentBasket>(pb => pb.Basket).ToListAsync();

            return Ok(repoPayment);
        }

        /// <summary>
        /// Gets info about certain payment basket
        /// </summary> 
        /// <param name="id">Id of payment basket</param>
        /// <response code="200">Gets info about payment basket</response>
        /// <response code="404">Payment basket with such id not found</response>
        /// <response code="500">Server error</response>
        [HttpGet("PaymentBasketById")]
        public async Task<IActionResult> GetPaymentBasketById (int id)
        {
            var findPaymentBasket = await _unitOfWork.GetAllIncluding<PaymentBasket>(pib => pib.Basket)
                .FirstOrDefaultAsync(pib => pib.BasketId == id);

            if (findPaymentBasket != null)
            {
                return Ok(findPaymentBasket);
            }
            else
            {
                return NotFound(new { message = $"Invalid source data. Not Found Payment with {id} Id"});
            }
        }

        /// <summary>
        /// Deletes payment basket
        /// </summary> 
        /// <param name="source">Source of payment basket</param>
        /// <response code="200">Payment basket deleted successfully</response>
        /// <response code="404">Source not found</response>
        /// <response code="500">Server error</response>
        [HttpDelete("Payment")]
        public async Task<IActionResult> DeletePayment(string source)
        {
            var findPayment = await _unitOfWork.GetRepository<PaymentBasket>()
                .AsQueryable()
                .FirstOrDefaultAsync(pb => pb.Source == source);

            if (findPayment != null)
            {
                var repoPayment = _unitOfWork.GetRepository<PaymentBasket>();
                repoPayment.Delete(findPayment);
                await _unitOfWork.SaveShangesAsync();
                return Ok();
            }
            else
            {
                return NotFound(new { message = $"Invalid source data. Not Found {source}" });
            }
        }
    }
}
