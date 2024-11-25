using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using Payment.Application.Payment_DAL.Contracts;
using Payment.Domain.ECommerce;

namespace Paymant_Module_NEOXONLINE.Controllers.ECommerce
{
    [Route("billing/swagger/api/[controller]")]
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
        /// Creates a new payment basket for the specified basket.
        /// </summary>
        /// <param name="basketId">The ID of the basket for which the payment basket will be created.</param>
        /// <returns>The created payment basket.</returns>
        /// <response code="200">Payment basket successfully created.</response>
        /// <response code="400">A payment basket for the specified basket already exists.</response>
        /// <response code="500">An internal server error occurred.</response>

        [HttpPost("Payment")]

        public async Task<IActionResult> CreatePaymentBasket([FromForm] int basketId)
        {
            var findPaumentBasket = await _unitOfWork.GetRepository<PaymentBasket>()
                .AsQueryable()
                .FirstOrDefaultAsync(pay => pay.Basket.Id == basketId);

            if (findPaumentBasket != null)
            {
                return BadRequest(new { message = $"Invalid source data.  Payment with {basketId} alredy exists" });
            }
            else
            {
                var repoPayment = _unitOfWork.GetRepository<PaymentBasket>();
                var repoBasket = _unitOfWork.GetRepository<Basket>();

                var findBasket = await _unitOfWork.GetRepository<Basket>()
                    .AsQueryable()
                    .FirstOrDefaultAsync(b => b.Id == basketId);

                var findBasketProduct = findBasket.ProductInBasket.ToList();
                decimal totalPrice = (decimal)findBasketProduct.Sum(pib => pib.Product.Price);




                var paymentBasket = new PaymentBasket
                {
                    Basket = findBasket,
                    Amount = totalPrice,
                    BasketId = findBasket.Id,
                    Date = DateTime.UtcNow,
                    MetaData = "",
                    Source = ""
                };

                repoPayment.Create(paymentBasket);
                await _unitOfWork.SaveShangesAsync();


                return Ok(paymentBasket);

            }
        }
        /// <summary>
        /// Updates the total price (Amount) of an existing payment basket.
        /// </summary>
        /// <param name="idPaymentBasket">The ID of the payment basket to update.</param>
        /// <returns>The updated payment basket details.</returns>
        /// <response code="200">Successfully updated the payment basket price.</response>
        /// <response code="400">Payment basket with the specified ID was not found.</response>
        /// <response code="500">An internal server error occurred.</response>
        [HttpPut("UpdatePrice")]
        public async Task<IActionResult> UpdatePricePaymentBasket([FromQuery] int idPaymentBasket)
        {
            var findPaymentBasket = await _unitOfWork.GetRepository<PaymentBasket>()
                .AsQueryable()
                .FirstOrDefaultAsync(pb => pb.Id == idPaymentBasket);

            if (findPaymentBasket != null)
            {
                var repoBasket = _unitOfWork.GetRepository<Basket>();
                var findBasket = await _unitOfWork.GetRepository<Basket>()
                    .AsQueryable()
                    .Include(b => b.ProductInBasket)
                    .ThenInclude(pib => pib.Product)
                    .FirstOrDefaultAsync(b => b.Id == findPaymentBasket.BasketId);
                var findBasketProduct = findBasket.ProductInBasket.ToList();
                decimal totalPrice = (decimal)findBasketProduct.Sum(pib => pib.Product.Price);
                findPaymentBasket.Amount = totalPrice;

                var repoPayment = _unitOfWork.GetRepository<PaymentBasket>();
                repoPayment.Update(findPaymentBasket);
                await _unitOfWork.SaveShangesAsync();
                return Ok(findPaymentBasket);
            }
            else
            {
                return BadRequest(new { message = $"Invalid source data.  Not Found  Payment Basket with {idPaymentBasket} id" });
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
