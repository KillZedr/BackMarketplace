using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using Paymant_Module_NEOXONLINE.DTOs.ECommerce;
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


        [HttpGet("AllPaymentBasket")] 

        public async Task<IActionResult> GetAllPaymentBasket()
        {
            var repoPayment = await _unitOfWork.GetAllIncluding<PaymentBasket>(pb => pb.Basket)
                .Include(pay => pay.Basket)
                .ThenInclude(b => b.ProductInBasket)
                .ThenInclude(p => p.Product)
                .ToListAsync();

            return Ok(repoPayment);
        }


        [HttpGet("PaymentBasketById")]


        public async Task<IActionResult> GetPaymentBasketById(int id)
        {
            var findPaymentBasket = await _unitOfWork.GetAllIncluding<PaymentBasket>(pib => pib.Basket)
                .FirstOrDefaultAsync(pib => pib.BasketId == id);

            if (findPaymentBasket != null)
            {
                return Ok(findPaymentBasket);
            }
            else
            {
                return BadRequest(new { message = $"Invalid source data. Not Found Payment with {id} Id" });
            }
        }



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
                return BadRequest(new {message = $"Invalid source data.  Not Found  Payment Basket with {idPaymentBasket} id" });
            }
        }

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
                return BadRequest(new { message = $"Invalid source data. Not Found {source}" });
            }

        }
    }
}
