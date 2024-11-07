using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Payment.BLL.Contracts.Notifications
{
    public interface IEmailNotificationService : IService
    {
        Task SendSuccessNotificationAsync(string toEmail, string paymentId);

        Task SendErrorNotificationAsync(string toEmail, string paymentId, string errorMessage);

        Task SendInsufficientFundsNotificationAsync(string toEmail, string paymentId);

        /*Task SendEmailAsync(string toEmail, string subject, string body);*/
    }
}
