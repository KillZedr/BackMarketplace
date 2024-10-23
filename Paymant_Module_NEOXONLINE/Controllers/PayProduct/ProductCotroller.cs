using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using Payment.Application.Payment_DAL.Contracts;
using Payment.Domain.DTOs;
using Payment.Domain.ECommerce;
using Payment.Domain.PayProduct;

namespace Payment_Module_NEOXONLINE.Controllers.PayProduct
{
    [Route("api/[controller]")]
    [ApiController]
    public class ProductCotroller : ControllerBase
    {
        private readonly IUnitOfWork _unitOfWork;

        public ProductCotroller(IUnitOfWork unitOfWork)
        {
            _unitOfWork = unitOfWork;
        }

        [HttpGet("GetAllProducts")]
        public async Task<IActionResult> GetAllProducts()
        {
            return Ok(await _unitOfWork.GetRepository<Product>()
                .AsQueryable()
                .Include(p => p.Category)
                .ToListAsync());
        }

        [HttpGet("GetProductByName")]
        public async Task<IActionResult> GetProductByName(string productName)
        {
            var product = _unitOfWork.GetRepository<Product>()
                .AsQueryable()
                .Include(p => p.Category)
                .Where(p => p.Name.Equals(productName)).First();
            if (product != null)
            {
                return Ok(product);
            }
            else
            {
                return NotFound($"product with name {productName} not found ");
            }
        }

        [HttpPost("CreateProduct")]
        public async Task<IActionResult> Product(ProductCreationDto productCreationDto)
        {
            var category = await _unitOfWork.GetRepository<Category>()
                .AsQueryable()
                .FirstOrDefaultAsync(c => c.Name == productCreationDto.categoryName);

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
                var repoProduct = _unitOfWork.GetRepository<Product>();
                repoProduct.Create(newProduct);
                await _unitOfWork.SaveShangesAsync();

                return Ok(newProduct);
            }
            else
            {
                return NotFound($"category with name {productCreationDto.categoryName} not found");
            }
        }


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
                    .FirstOrDefaultAsync(c => c.Name.Equals(productDto.categoryName));
                if (category != null)
                {
                    product.Name = productDto.Name;
                    product.Description = productDto.Description;
                    product.Price = productDto.Price;
                    //product.Category = category;

                    _unitOfWork.GetRepository<Product>().Update(product);
                    await _unitOfWork.SaveShangesAsync();

                    return Ok(product);
                }
                else
                {
                    return NotFound($"category with name {productDto.categoryName} not found");
                }
            }
            else
            {
                return NotFound($"product with name {productDto.Name} not found");
            }
        }


        [HttpDelete("DeleteProduct")]

        public async Task<IActionResult> DeleteProduct(string productName)
        {
            var deletedProduct = await _unitOfWork.GetRepository<Product>()
                .AsQueryable()
                .FirstOrDefaultAsync(p => p.Name.Equals(productName));
            if (deletedProduct == null)
            {
                return BadRequest(new { message = $"Invalid source data. Not Found {productName}" });
            }
            else
            {
                _unitOfWork.GetRepository<Product>().Delete(deletedProduct);
                await _unitOfWork.SaveShangesAsync();
                return Ok($"{deletedProduct} has been deleted");
            }
        }
    }
}
