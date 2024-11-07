using Microsoft.Extensions.Logging;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Net.Mail;
using System.Net;
using System.Text;
using System.Threading.Tasks;
using Payment.BLL.Contracts.Notifications;
using Payment.BLL.Settings.NotificationSettings;
using Microsoft.Extensions.Options;

namespace Payment.BLL.Services.Notifications
{
    public class EmailNotificationService : IEmailNotificationService
    {
        private readonly ILogger<EmailNotificationService> _logger;
        private readonly SmtpSettings _smtpSettings;
        private readonly SmtpClient _smtpClient;

        public EmailNotificationService(ILogger<EmailNotificationService> logger, IOptions<SmtpSettings> smtpSettings)
        {
            _logger = logger;
            _smtpSettings = smtpSettings.Value;

            _smtpClient = new SmtpClient(_smtpSettings.Server, _smtpSettings.Port)
            {
                Credentials = new NetworkCredential(_smtpSettings.User, _smtpSettings.Password),
                EnableSsl = true
            };
        }


        public async Task SendSuccessNotificationAsync(string toEmail, string paymentId)
        {
            string subject = "Payment Successful";
            string body = $"Your payment with ID {paymentId} was successful.";
            await SendEmailAsync(toEmail, subject, body);
        }

        public async Task SendErrorNotificationAsync(string toEmail, string paymentId, string errorMessage)
        {
            string subject = "Payment Error";
            string body = $"There was an error with your payment ID {paymentId}. Error: {errorMessage}";
            await SendEmailAsync(toEmail, subject, body);
        }

        public async Task SendInsufficientFundsNotificationAsync(string toEmail, string paymentId)
        {
            string subject = "Payment Failed - Insufficient Funds";
            string body = $"Your payment with ID {paymentId} failed due to insufficient funds.";
            await SendEmailAsync(toEmail, subject, body);
        }

        private async Task SendEmailAsync(string toEmail, string subject, string body)
        {
            try
            {
                var mailMessage = new MailMessage(_smtpSettings.FromEmail, toEmail, subject, body);
                await _smtpClient.SendMailAsync(mailMessage);
                _logger.LogInformation($"Email sent to {toEmail} with subject: {subject}");
            }
            catch (Exception ex)
            {
                _logger.LogError($"Error sending email to {toEmail}: {ex.Message}");
            }
        }
    }
}
