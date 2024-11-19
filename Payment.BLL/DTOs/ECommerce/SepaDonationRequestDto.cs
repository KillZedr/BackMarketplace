using Payment.BLL.DTOs;

public class SepaDonationRequestDto
{
    public decimal Amount { get; set; }
    public string Currency { get; set; }
    public SepaPaymentRequestDto SepaRequest { get; set; }
    public string CustomerId { get; set; }  // Добавляем поле для передачи customerId
}   