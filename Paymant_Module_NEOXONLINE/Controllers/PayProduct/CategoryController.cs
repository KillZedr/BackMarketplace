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
        [HttpGet]
        public async Task<IActionResult> GetAllCategory()
        {
            var category = await _unitOfWork.GetRepository<Category>().AsReadOnlyQueryable().ToListAsync();
            return Ok(category);
        }
        [HttpGet("FindCategoryByName")]

        public async Task<IActionResult> GetCategoryById(string findCategoryName)
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
                return BadRequest(new { message = "Invalid course data" });
            }
        }

        // POST api/<CategoryController>
        [HttpPost]
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
                return BadRequest(new { message = "Invalid course data" });
            }
        }
    }
}
