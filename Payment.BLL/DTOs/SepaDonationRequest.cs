using Payment.BLL.DTOs;

public class SepaDonationRequest
{
    public decimal Amount { get; set; }
    public string Currency { get; set; }
    public SepaPaymentRequest SepaRequest { get; set; }
    public string CustomerId { get; set; }  // Добавляем поле для передачи customerId
}   