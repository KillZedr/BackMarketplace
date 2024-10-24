using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using Payment.Application.Payment_DAL.Contracts;
using Payment.Domain.ECommerce;

namespace Paymant_Module_NEOXONLINE.Controllers.ECommerce
{
    [Route("api/[controller]")]
    [ApiController]
    public class BasketConntroller : ControllerBase
    {

        private readonly IUnitOfWork _unitOfWork;

        public BasketConntroller(IUnitOfWork unitOfWork)
        {
            _unitOfWork = unitOfWork;
        }

        [HttpGet("AllBaskets")]

        public async Task<IActionResult> GetAllBascets()
        {
            var repoBasket = await _unitOfWork.GetAllIncluding<Basket>(b => b.ProductInBasket).ToListAsync();
            return Ok(repoBasket);
        }


        [HttpDelete("BasketByIdUser")]

        public async Task<IActionResult> DeleteBasket(Guid guidId)
        {
            var findBasket = await _unitOfWork.GetRepository<Basket>()
                .AsQueryable()
                .FirstOrDefaultAsync(b => b.User.Id == guidId);


            if (findBasket != null)
            {
                var repoBasket = _unitOfWork.GetRepository<Basket>();
                repoBasket.Delete(findBasket);
                await _unitOfWork.SaveShangesAsync();

                return Ok();

            }
            else
            {
                return BadRequest(new { message = $"Invalid source data. Not Found User with {guidId} Id" });
            }
        }
    }
}
