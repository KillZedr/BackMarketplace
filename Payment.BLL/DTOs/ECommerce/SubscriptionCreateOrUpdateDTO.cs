using Payment.Domain.PayProduct;

namespace Paymant_Module_NEOXONLINE.DTOs.ECommerce
{
    public class SubscriptionCreateOrUpdateDTO
    {

        public Guid UserId { get; set; }
        public int ProductId { get; set; }
        
        public bool IsPaid { get; set; }
    }
}
