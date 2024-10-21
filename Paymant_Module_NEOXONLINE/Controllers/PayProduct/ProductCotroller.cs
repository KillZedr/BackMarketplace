using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using Payment.Application.Payment_DAL.Contracts;
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
            return Ok(await _unitOfWork.GetRepository<Product>().AsQueryable().ToListAsync());
        }

        [HttpGet("GetProductByName")]
        public async Task<IActionResult> GetProductByName(string productName)
        {
            var product = _unitOfWork.GetRepository<Product>().AsQueryable().Where(p => p.Name.Equals(productName)).First();
            if (product != null)
            {
                return Ok(product);
            }
            else
            {
                return NotFound($"product with name {productName} not found ");
            }
        }

        //[HttpPost("CreateProduct")]
        //public async Task<IActionResult> Product()
        //{
        //need DTOs
        //}

        //[HttpPut("UpdateProduct")]

        //public async Task<IActionResult> UpdateProduct()
        //{
        //need DTOs
        //}


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
