using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using Paymant_Module_NEOXONLINE.DTOs.ECommerce;
using Payment.Application.Payment_DAL.Contracts;
using Payment.Domain.ECommerce;
using Payment.Domain.Identity;
using Payment.Domain.PayProduct;

namespace Paymant_Module_NEOXONLINE.Controllers.ECommerce
{
    [Route("billing/swagger/api/[controller]")]
    [ApiController]
    public class SubscriptionController : ControllerBase
    {
        private readonly IUnitOfWork _unitOfWork;

        public SubscriptionController(IUnitOfWork unitOfWork)
        {
            _unitOfWork = unitOfWork;
        }

        /// <summary>
        /// Retrieves all subscriptions with their associated products.
        /// </summary>
        /// <returns>A list of all subscriptions with product details.</returns>
        /// <response code="200">Successfully retrieved all subscriptions.</response>
        /// <response code="500">An internal server error occurred.</response>
        [HttpGet("AllSubscriptions")]

        public async Task<IActionResult> GetAllSubscriptions()
        {
            var subscriptions = await _unitOfWork.GetAllIncluding<Subscription>(sub => sub.Product).ToListAsync();

            return Ok(subscriptions);
        }
        /// <summary>
        /// Retrieves all subscriptions associated with a specific product name.
        /// </summary>
        /// <param name="productName">The name of the product.</param>
        /// <returns>The subscriptions associated with the specified product.</returns>
        /// <response code="200">Successfully retrieved the subscriptions for the product.</response>
        /// <response code="400">No product with the specified name was found.</response>
        /// <response code="500">An internal server error occurred.</response>
        [HttpGet("AllSubscriptionsByProduct")]

        public async Task<IActionResult> GetAllSubscriptionsByProduct(string productName)
        {
            var subsvriptionByBroduct = await _unitOfWork.GetRepository<Subscription>()
                .AsQueryable()
                .FirstOrDefaultAsync(p => p.Product.Name == productName);
            if (subsvriptionByBroduct != null)
            {
                return Ok(subsvriptionByBroduct);
            }
            else
            {
                return BadRequest(new { message = $"Invalid source data. Not Found Product With {productName} Name" });
            }
        }
        /// <summary>
        /// Creates a new subscription for a specified product and user.
        /// </summary>
        /// <param name="subscription">The subscription details, including User ID, Product ID, and IsPaid status.</param>
        /// <returns>The created subscription.</returns>
        /// <response code="200">Subscription successfully created.</response>
        /// <response code="400">Invalid data was provided (e.g., product or user not found).</response>
        /// <response code="500">An internal server error occurred.</response>
        [HttpPost("NewSubscription")]

        public async Task<IActionResult> CreateSubscription([FromForm] SubscriptionCreateOrUpdateDTO subscription)
        {
            var subscriptionProduct = await _unitOfWork.GetRepository<Subscription>()
                .AsQueryable()
                .FirstOrDefaultAsync(sub => sub.Product.Id == subscription.ProductId);

            var findUser = await _unitOfWork.GetRepository<User>()
                   .AsQueryable()
                   .FirstOrDefaultAsync(u => u.Id == subscription.UserId);

            var findProduct = await _unitOfWork.GetRepository<Product>()
                .AsQueryable()
                .FirstOrDefaultAsync(p => p.Id == subscription.ProductId);
            var result = new Subscription();

            if (subscriptionProduct == null)
            {
                var subscriptionNew = new Subscription
                {
                    StartDate = DateTime.UtcNow,
                    EndDate = DateTime.UtcNow.AddMonths(1),
                    IsPaid = subscription.IsPaid,
                    Product = findProduct,
                    UserId = findUser.Id

                };

                var repoSubscription = _unitOfWork.GetRepository<Subscription>();
                repoSubscription.Create(subscriptionNew);
                await _unitOfWork.SaveShangesAsync();
                return Ok(subscriptionNew);


            }
            else
            {
                if (findUser == null && findProduct != null)
                {
                    return BadRequest(new { message = $"Invalid source data. Not Found User With {subscription.UserId} Id" });
                }
                else if (findProduct == null && findUser != null)
                {
                    return BadRequest(new { message = $"Invalid source data. Not Found Product With {subscription.ProductId} Id" });
                }
                else
                {
                    return BadRequest(new { message = $"Invalid source data. Not Found Product With {subscription.ProductId} Id and User with {subscription.UserId} id" });
                }

            }
        }

        

        /// <summary>
        /// Deletes a subscription by its ID.
        /// </summary>
        /// <param name="id">The ID of the subscription to delete.</param>
        /// <returns>The result of the deletion process.</returns>
        /// <response code="200">Subscription successfully deleted.</response>
        /// <response code="400">Subscription with the specified ID was not found.</response>
        /// <response code="500">An internal server error occurred.</response>

        [HttpDelete("Subscription")]

        public async Task<IActionResult> DeleteSubcription(int id)
        {
            var findSubscription = await _unitOfWork.GetRepository<Subscription>()
                .AsQueryable()
                .FirstOrDefaultAsync(sub => sub.Id == id);

            if (findSubscription != null)
            {
                var repoSubskription = _unitOfWork.GetRepository<Subscription>();
                repoSubskription.Delete(findSubscription);
                await _unitOfWork.SaveShangesAsync();

                return Ok();
            }
            else
            {
                return BadRequest(new { message = $"Invalid source data. Not Found Subscription  with {findSubscription.Id} id" });
            }
        }

    }
}
