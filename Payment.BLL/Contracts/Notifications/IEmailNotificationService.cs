using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Payment.BLL.Contracts.Notifications
{
    public interface IEmailNotificationService : IService
    {
        Task SendTestSuccessNotificationAsync(string toEmail, DateTime creationDate, string approvalUrl);

        Task SendErrorNotificationAsync(string toEmail, string paymentId, string errorMessage);

        Task SendInsufficientFundsNotificationAsync(string toEmail, string paymentId);

        Task SendSuccessNotificationAsync(string email, decimal amount, string currency, DateTime paymentTime);
        Task SendDonationSuccessNotificationAsync(string email, string amount, string currency, DateTime paymentTime);
    }
}
