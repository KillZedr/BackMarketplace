using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using Payment.Application.Payment_DAL.Contracts;
using Payment.BLL.Contracts.PayProduct;
using Payment.BLL.DTOs;
using Payment.Domain.ECommerce;
using Payment.Domain.PayProduct;
using Payment.BLL.Services.PayProduct;
using System.Xml.Linq;
using Payment.BLL.Contracts.PayProduct;
using Payment.BLL.Contracts.Payment;
using Payment.Domain.DTOs;

namespace Payment_Module_NEOXONLINE.Controllers.PayProduct
{
    [Route("billing/swagger/api/[controller]")]
    [ApiController]
    public class ProductCotroller : ControllerBase
    {
        private readonly IUnitOfWork _unitOfWork;
        private readonly IProductService _productService;

        public ProductCotroller(IUnitOfWork unitOfWork, IProductService productService)
        {
            _unitOfWork = unitOfWork;
            _productService = productService;
        }

        /// <summary>
        /// Gets info about all products in db
        /// </summary> 
        /// <response code="200">Returns info about all products in db</response>
        /// <response code="500">Server error</response>
        [HttpGet("GetAllProducts")]
        public async Task<IActionResult> GetAllProducts()
        {
            return Ok(_unitOfWork.GetAllIncluding<Product>(p => p.Category).ToList());
        }

        /// <summary>
        /// Gets info about all products in the specified price range in db
        /// </summary> 
        /// <param name="lowerLimit">Lower limit of price</param>
        /// <param name="higherLimit">Higher limit of price</param>
        /// <response code="200">Returns info about suitable products</response>
        /// <response code="400">Invalid value of lower or higher limit</response>
        /// <response code="404">There are no products in such range</response>
        /// <response code="500">Server error</response>
        [HttpGet("GetProductsFromPriceToPrice")]
        public async Task<IActionResult> GetProductsFromPriceToPrice(decimal lowerLimit, decimal higherLimit)
        {
            if (lowerLimit < higherLimit)
            {
                var products = await _productService.GetProductsFromPriceToPrice(lowerLimit, higherLimit);
                if(products.Count != 0)
                {
                    return Ok(products);
                }
                else 
                { 
                    return NotFound($"There are no products in range from {lowerLimit} to {higherLimit}"); 
                }
            }
            else
            {
                return BadRequest($"Lower limit must be less than higher limit");
            }
        }

        /// <summary>
        /// Gets info about certain product in db
        /// </summary> 
        /// <param name="productName">Name of product to get information about</param>
        /// <response code="200">Returns info about certain product</response>
        /// <response code="404">Product with such name not found</response>
        /// <response code="500">Server error</response>
        [HttpGet("GetProductByName")]
        public async Task<IActionResult> GetProductByName(string productName)
        {
            var product = _unitOfWork.GetAllIncluding<Product>(p=>p.Category).First();
            if (product != null)
            {
                return Ok(product);
            }
            else
            {
                return NotFound($"Product with name {productName} not found ");
            }
        }

        /// <summary>
        /// Creates product in db
        /// </summary> 
        /// <response code="200">Returns info about created product</response>
        /// <response code="404">Category with such name not found</response>
        /// <response code="500">Server error</response>
        [HttpPost("CreateProduct")]
        public async Task<IActionResult> CreateProduct(ProductCreationDto productCreationDto)
        {
            var category = await _unitOfWork.GetRepository<Category>()
                .AsQueryable()
                .FirstOrDefaultAsync(c => c.Name == productCreationDto.CategoryName);
            //todo
            //check if product name is unique
            if (category != null)
            {
                var newProduct = new Product
                {
                    Name = productCreationDto.Name,
                    Description = productCreationDto.Description,
                    Price = productCreationDto.Price,
                    Category = category,
                    ProductInBasket = new List<ProductInBasket>(),
                    Subscription = new List<Subscription>()
                };
                _unitOfWork.GetRepository<Product>().Create(newProduct);
                await _unitOfWork.SaveShangesAsync();

                return Ok(newProduct);
            }
            else
            {
                return NotFound($"category with name {productCreationDto.CategoryName} not found");
            }
        }

        /// <summary>
        /// Updates product in db
        /// </summary> 
        /// <response code="200">Returns info about updated product</response>
        /// <response code="404">Product with such name or category with such name not found</response>
        /// <response code="500">Server error</response>
        [HttpPut("UpdateProduct")]
        public async Task<IActionResult> UpdateProduct(ProductCreationDto productDto)
        {
            var product = await _unitOfWork.GetRepository<Product>()
                .AsReadOnlyQueryable()
                .FirstOrDefaultAsync(p => p.Name.Equals(productDto.Name));
            if (product != null)
            {
                var category = await _unitOfWork.GetRepository<Category>()
                    .AsQueryable()
                    .FirstOrDefaultAsync(c => c.Name.Equals(productDto.CategoryName));
                if (category != null)
                {
                    //todo
                    //check if new product name is unique
                    product.Name = productDto.Name;
                    product.Description = productDto.Description;
                    product.Price = productDto.Price;
                    product.Category = category;

                    _unitOfWork.GetRepository<Product>().Update(product);
                    await _unitOfWork.SaveShangesAsync();

                    return Ok(product);
                }
                else
                {
                    return NotFound($"Category with name {productDto.CategoryName} not found");
                }
            }
            else
            {
                return NotFound($"Product with name {productDto.Name} not found");
            }
        }

        /// <summary>
        /// Deletes product from db
        /// </summary> 
        /// <response code="200">Product deleted successfully</response>
        /// <response code="404">Product with such name not found</response>
        /// <response code="500">Server error</response>
        [HttpDelete("DeleteProduct")]

        public async Task<IActionResult> DeleteProduct(string productName)
        {
            var deletedProduct = await _unitOfWork.GetRepository<Product>()
                .AsQueryable()
                .FirstOrDefaultAsync(p => p.Name.Equals(productName));
            if (deletedProduct == null)
            {
                return NotFound(new { message = $"Invalid source data. Not Found {productName}" });
            }
            else
            {
                _unitOfWork.GetRepository<Product>().Delete(deletedProduct);
                await _unitOfWork.SaveShangesAsync();
                //await _stripeService.DeleteStripeProductAsync(deletedProduct.Id);
                return Ok($"{deletedProduct} has been deleted");
            }
        }
    }
}
