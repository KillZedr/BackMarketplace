using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using Payment.Application.Payment_DAL.Contracts;
using Payment.Domain.PayProduct;

namespace Payment_Module_NEOXONLINE.Controllers.PayProduct
{

    [Route("api/[controller]")]
    [ApiController]
    public class CategoryController : ControllerBase
    {

        private readonly IUnitOfWork _unitOfWork;

        public CategoryController(IUnitOfWork unitOfWork)
        {
            _unitOfWork = unitOfWork;
        }


        // GET: api/<CategoryController>
        [HttpGet("GetAllCategory")]
        public async Task<IActionResult> GetAllCategory()
        {
            var category = await _unitOfWork.GetRepository<Category>().AsReadOnlyQueryable().ToListAsync();
            return Ok(category);
        }
        [HttpGet("FindCategoryByName")]

        public async Task<IActionResult> GetCategoryByName(string findCategoryName)
        {
            var findCategory = await _unitOfWork.GetRepository<Category>()
                .AsReadOnlyQueryable()
                .FirstOrDefaultAsync(c => c.Name == findCategoryName);
            if (findCategory != null)
            {
                return Ok(findCategory);
            }
            else
            {
                return BadRequest(new { message = $"Invalid course data. Not Faund {findCategoryName} " });
            }
        }

        // POST api/<CategoryController>
        [HttpPost("CreateCategory")]
        public async Task<IActionResult> CreateCategory(string nameCategory)
        {
            var category = await _unitOfWork.GetRepository<Category>()
                .AsReadOnlyQueryable()
                .FirstOrDefaultAsync(c => c.Name == nameCategory);
            if (category == null)
            {
                var categoryNew = new Category { Name = nameCategory };
                var repoCategory = _unitOfWork.GetRepository<Category>();
                repoCategory.Create(categoryNew);
                await _unitOfWork.SaveShangesAsync();
                return Ok(categoryNew);
            }
            else
            {
                return BadRequest(new { message = $"Invalid course data. Category {nameCategory}  exists " });
            }
        }

        [HttpPut("UpdateCategory")]

        public async Task<IActionResult> UpdateCategory(string nameUpdateCategory, string newNameCategory)
        {
            var updateCategory = await _unitOfWork.GetRepository<Category>()
                .AsReadOnlyQueryable()
                .FirstOrDefaultAsync(c => c.Name == nameUpdateCategory);

            if (updateCategory == null)
            {
                return BadRequest(new { message = $"Invalid course data. Not Faund {nameUpdateCategory}" });
            }
            else
            {
                updateCategory.Name = newNameCategory;
                var repoCategory = _unitOfWork.GetRepository<Category>();
                repoCategory.Update(updateCategory);
                await _unitOfWork.SaveShangesAsync();
                return Ok(updateCategory);
            }
        }


        [HttpDelete("DeleteCategory")]

        public async Task<IActionResult> DeleteCategory(string categoryName)
        {
            var deleteCategory = await _unitOfWork.GetRepository<Category>()
                .AsReadOnlyQueryable()
                .FirstOrDefaultAsync (c => c.Name == categoryName);
            if (deleteCategory == null)
            {
                return BadRequest(new { message = $"Invalid course data. Not Faund {categoryName}" });
            }
            else
            {
                var repoCategory = _unitOfWork.GetRepository<Category>();
                repoCategory.Delete(deleteCategory);
                await _unitOfWork.SaveShangesAsync();
                return Ok(deleteCategory);
            }
        }
    }
}
