
namespace Payment.BLL.DTOs
{
    public class SepaPaymentRequestDto
    {
        public string Iban { get; set; }
        public string IpAddress { get; set; }
        public string UserAgent { get; set; }
    }
}