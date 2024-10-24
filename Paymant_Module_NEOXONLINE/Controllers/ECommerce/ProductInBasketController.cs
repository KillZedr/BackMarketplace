using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using Payment.Application.Payment_DAL.Contracts;
using Payment.Domain.ECommerce;

namespace Paymant_Module_NEOXONLINE.Controllers.ECommerce
{
    [Route("api/[controller]")]
    [ApiController]
    public class ProductInBasketController : ControllerBase
    {

        private readonly IUnitOfWork _unitOfWork;

        public ProductInBasketController(IUnitOfWork unitOfWork)
        {
            _unitOfWork = unitOfWork;
        }


        [HttpGet("AllProductInBasket")]

        public async Task<IActionResult> GetAllProductInBasket()
        {
            var repoProductInBasket = await _unitOfWork.GetAllIncluding<ProductInBasket>(pib => pib.Product).ToListAsync();
            return Ok(repoProductInBasket);
        }



    }
}
