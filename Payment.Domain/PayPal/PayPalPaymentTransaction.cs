using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;


namespace Payment.Domain.PayPal
{
   
    public class PayPalPaymentTransaction : IEntity
    {
        
        public int Id { get; set; }
        public  int PaymentBasketId { get; set; }
        public required string PaymentId { get; set; } // ID платежа в PayPal

        public required string PayerId { get; set; } // ID плательщика

        public required  string SaleId { get; set; } // ID продажи для возвратов
         
        public required string Status { get; set; } // Статус платежа (например, Completed, Refunded)

        public required decimal Amount { get; set; } // Сумма транзакции

        public required string Currency { get; set; } // Валюта транзакции (например, USD, EUR)

        public DateTime CreatedDate { get; set; } = DateTime.UtcNow; // Дата и время создания транзакции

        public DateTime? RefundedDate { get; set; } // Дата и время возврата (если применимо)

        public string RefundId { get; set; } // ID возврата (если применимо)

        public string Description { get; set; } // Описание транзакции
    }

}
