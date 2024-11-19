using Payment.Domain.ECommerce;

namespace Paymant_Module_NEOXONLINE.DTOs.ECommerce
{
    public class PaymentCreateDto
    {
        public required int BasketId { get; set; }
        public required DateTime Date { get; set; }
        public required decimal Amount { get; set; }
        public required string Source { get; set; }
        public required string MetaData { get; set; }
    }
}
