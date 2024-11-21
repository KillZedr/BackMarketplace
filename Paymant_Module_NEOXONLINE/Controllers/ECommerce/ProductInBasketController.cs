using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using Payment.Application.Payment_DAL.Contracts;
using Payment.Application.Payment_DAL.RealisationInterfaces;
using Payment.Domain.ECommerce;
using Payment.Domain.PayProduct;

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

        /// <summary>
        /// Gets info about all products in baskets in db
        /// </summary> 
        /// <response code="200">Returns info about all products in baskets in db</response>
        /// <response code="500">Server error</response>
        [HttpGet("AllProductsInBasket")]
        public async Task<IActionResult> GetAllProductsInBasket()
        {
            var repoProductInBasket = await _unitOfWork.GetAllIncluding<ProductInBasket>(pib => pib.Product)
                .Include(pib => pib.Basket)
                .ThenInclude(b => b.PaymentBasket)
                .ToListAsync();
            return Ok(repoProductInBasket);
        }

        /// <summary>
        /// Gets info about all products in baskets in db for certain user
        /// </summary> 
        /// <param name="username">First name of user whose products in basket need to be shown</param>
        /// <response code="200">Returns info about all products in baskets in db</response>
        /// <response code="500">Server error</response>
        [HttpGet("GetUsersProductsInBasket")]
        public async Task<IActionResult> GetUsersProductsInBasket(string username)
        {
            var repoProductInBasket = await _unitOfWork.GetAllIncluding<ProductInBasket>(pib => pib.Product).Where(pib=>pib.Basket.User.FirstName.Equals(username)).ToListAsync();
            return Ok(repoProductInBasket);
        }

        /// <summary>
        /// Adds certain product in basket for certain user
        /// </summary> 
        /// <param name="productName">Name of product that is being added to the basket</param>
        /// <param name="username">First name of user who adds product in basket</param>
        /// <response code="200">Returns info about added product in basket in db</response>
        /// <response code="404">User with such username or product with such name not found</response>
        /// <response code="500">Server error</response>
        [HttpPost("CreateProductInBasket")]
        public async Task<IActionResult> CreateProductInBasket(string productName, string username)
        {
            var product = _unitOfWork.GetRepository<Product>()
                .AsQueryable().First(p => p.Name.Equals(productName));
            if(product != null)
            {
                var basket = _unitOfWork.GetAllIncluding<Basket>(b => b.User)
                    .AsQueryable().First(b => b.User.FirstName.Equals(username));
                if (basket != null)
                {
                    var productInBasket = new ProductInBasket()
                    {
                        Basket = basket,
                        Product = product
                    };
                    _unitOfWork.GetRepository<ProductInBasket>().Create(productInBasket);
                    await _unitOfWork.SaveShangesAsync();
                    return Ok(productInBasket);
                }
                else
                {
                    return NotFound($"user with name {username} not found");
                }
            }
            else
            {
                return NotFound($"product with name {productName} not found");
            }
        }

        /// <summary>
        /// Deletes certain product from basket for certain user
        /// </summary> 
        /// <param name="productName">Name of product that is being deleted from the basket</param>
        /// <param name="username">First name of user who deletes product from basket</param>
        /// <response code="200">Product deleted from basket successfully</response>
        /// <response code="404">User with such username does not have product with such name in basket</response>
        /// <response code="500">Server error</response>
        [HttpDelete("DeleteProductInBasket")]
        public async Task<IActionResult> DeleteProductInBasket (string productName, string username)
        {
            var findProductInBasket = await _unitOfWork.GetRepository<ProductInBasket>()
                .AsQueryable()
                .FirstOrDefaultAsync(pib => pib.Product.Name.Equals(productName) && pib.Basket.User.FirstName.Equals(username));
            if (findProductInBasket != null)
            {

                var repoProductInBasket = _unitOfWork.GetRepository<ProductInBasket>();
                repoProductInBasket.Delete(findProductInBasket);
                await _unitOfWork.SaveShangesAsync();
                return Ok();
            }
            else
            {
                return NotFound(new { message = $"Invalid source data. Not Found Product in basket with {productName} name and {username} username" });
            }           
        }
    }
}
