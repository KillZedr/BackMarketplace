using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using Payment.Application.Payment_DAL.Contracts;
using Payment.Domain.PayProduct;

namespace Payment_Module_NEOXONLINE.Controllers.PayProduct
{

    [Route("billing/swagger/api/[controller]")]
    [ApiController]
    public class CategoryController : ControllerBase
    {
        private readonly IUnitOfWork _unitOfWork;

        public CategoryController(IUnitOfWork unitOfWork)
        {
            _unitOfWork = unitOfWork;
        }

        /// <summary>
        /// Gets info about all categories in db
        /// </summary> 
        /// <response code="200">Returns info about all categories in db</response>
        /// <response code="500">Server error</response>
        [HttpGet("GetAllCategories")]
        public async Task<IActionResult> GetAllCategories()
        {
            var categories = await _unitOfWork.GetAllIncluding<Category>(c => c.Products).ToListAsync();
                
            return Ok(categories);
        }

        /// <summary>
        /// Gets info about certain category by name
        /// </summary> 
        /// <param name="categoryName">Name of category</param>
        /// <response code="200">Returns info category</response>
        /// <response code="404">Category with such name not found</response>
        /// <response code="500">Server error</response>
        [HttpGet("FindCategoryByName")]
        public async Task<IActionResult> GetCategoryByName(string categoryName)
        {
            var foundCategory = await _unitOfWork.GetRepository<Category>()
                .AsReadOnlyQueryable()
                .Include(c => c.Products)
                .FirstOrDefaultAsync(c => c.Name == categoryName);
            if (foundCategory != null)
            {
                return Ok(foundCategory);
            }
            else
            {
                return NotFound(new { message = $"Invalid source data. Not Found {categoryName} " });
            }
        }

        /// <summary>
        /// Creates category in db
        /// </summary> 
        /// <param name="categoryName">Name of the category being created</param>
        /// <response code="200">Returns info about created category</response>
        /// <response code="400">Category with such name alredy exists in db</response>
        /// <response code="500">Server error</response>
        [HttpPost("CreateCategory")]
        public async Task<IActionResult> CreateCategory(string categoryName)
        {
            var category = await _unitOfWork.GetRepository<Category>()
                .AsReadOnlyQueryable()
                .FirstOrDefaultAsync(c => c.Name == categoryName);
            if (category == null)
            {
                var newCategory = new Category { Name = categoryName };
                var repoCategory = _unitOfWork.GetRepository<Category>();
                repoCategory.Create(newCategory);
                await _unitOfWork.SaveShangesAsync();
                return Ok(newCategory);
            }
            else
            {
                return BadRequest(new { message = $"Invalid source data. Category {categoryName} alredy exists " });
            }
        }

        /// <summary>
        /// Updates category in db
        /// </summary> 
        /// <param name="oldCategoryName">Current name of the category</param>
        /// <param name="oldCategoryName">New name of the category</param>
        /// <response code="200">Returns info about created category</response>
        /// <response code="404">Category with such old name not found</response>
        /// <response code="500">Server error</response>
        [HttpPut("UpdateCategory")]
        public async Task<IActionResult> UpdateCategory(string oldCategoryName, string newCategoryName)
        {
            var updateCategory = await _unitOfWork.GetRepository<Category>()
                .AsReadOnlyQueryable()
                .FirstOrDefaultAsync(c => c.Name == oldCategoryName);

            if (updateCategory == null)
            {
                return BadRequest(new { message = $"Invalid source data. Not Found {oldCategoryName}" });
            }
            else
            {
                //todo
                //check if newCategoryName is unique
                updateCategory.Name = newCategoryName;
                var repoCategory = _unitOfWork.GetRepository<Category>();
                repoCategory.Update(updateCategory);
                await _unitOfWork.SaveShangesAsync();
                return Ok(updateCategory);
            }
        }

        /// <summary>
        /// Deletes category in db
        /// </summary> 
        /// <param name="categoryName">Name of the category being deleted</param>
        /// <response code="200">Returns info about deleted category</response>
        /// <response code="404">Category with such name not found</response>
        /// <response code="500">Server error</response>
        [HttpDelete("DeleteCategory")]
        public async Task<IActionResult> DeleteCategory(string categoryName)
        {
            var deleteCategory = await _unitOfWork.GetRepository<Category>()
                .AsReadOnlyQueryable()
                .FirstOrDefaultAsync (c => c.Name == categoryName);
            if (deleteCategory == null)
            {
                return NotFound(new { message = $"Invalid source data. Not Found {categoryName}" });
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
