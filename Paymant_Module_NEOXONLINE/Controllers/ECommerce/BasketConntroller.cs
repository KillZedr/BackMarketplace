using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using Payment.Application.Payment_DAL.Contracts;
using Payment.Domain.ECommerce;
using Payment.Domain.Identity;

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

        /// <summary>
        /// Gets info about all baskets in db
        /// </summary> 
        /// <response code="200">returns info about all baskets in db</response>
        /// <response code="500">server error</response>
        [HttpGet("AllBaskets")]
        public async Task<IActionResult> GetAllBaskets()
        {
            var repoBasket = await _unitOfWork.GetAllIncluding<Basket>(b => b.ProductInBasket).ToListAsync();
            return Ok(repoBasket);
        }

        /// <summary>
        /// Creates empty basket for user
        /// </summary> 
        /// <param name="username">name of the user that the basket will be associated with</param>
        /// <response code="200">basket created successfully</response>
        /// <response code="400">user with such username alredy has a basket</response>
        /// <response code="500">server error</response>
        [HttpPost("CreateBasket")]
        public async Task<IActionResult> CreateBasket(string username)
        {
            var basket = _unitOfWork.GetRepository<Basket>()
                .AsReadOnlyQueryable().First(b => b.User.FirstName.Equals(username));
            if (basket != null)
            {
                return BadRequest($"user {username} alredy has basket");
            }
            else
            {
                _unitOfWork.GetRepository<Basket>().Create(new Basket() 
                { 
                    User = _unitOfWork.GetRepository<User>()
                        .AsReadOnlyQueryable().First(u => u.FirstName.Equals(username)) 
                });
                return Ok();
            }
        }

        /// <summary>
        /// Deletes basket for user
        /// </summary> 
        /// <param name="guidId">id of the user whose basket should be deleted</param>
        /// <response code="200">basket deleted successfully</response>
        /// <response code="404">user with such id not found</response>
        /// <response code="500">server error</response>
        [HttpDelete("BasketByUserId")]
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
                return NotFound(new { message = $"Invalid source data. Not Found User with {guidId} Id" });
            }
        }
    }
}
