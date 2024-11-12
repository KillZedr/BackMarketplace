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
using System.Net.Sockets;

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


        public async Task SendTestSuccessNotificationAsync(string toEmail, DateTime creationDate, string approvalUrl)
        {
            var mailMessage = new MailMessage
            {
                From = new MailAddress(_smtpSettings.FromEmail, _smtpSettings.SenderName),
                Subject = "Please Confirm Your Payment",

                Body = $@"
            <h3>Your Payment is Pending Confirmation</h3>
            <p>Thank you for initiating your payment.</p>
            <p><strong>Payment Details:</strong></p>
            <ul>
                <li><strong>Date and Time:</strong> {creationDate.ToString("f")}</li>
            </ul>
            <p>Please confirm your payment by clicking the following link:</p>
            <p><a href='{approvalUrl}'>Confirm Payment</a></p>
            <p>If you did not initiate this payment, please disregard this email.</p>",
                IsBodyHtml = true,
            };
            mailMessage.To.Add(toEmail);
            await _smtpClient.SendMailAsync(mailMessage);
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


        public async Task SendSuccessNotificationAsync(string email, decimal amount, string currency, DateTime paymentTime)
        {
            try
            {
                var mailMessage = new MailMessage
                {
                    From = new MailAddress(_smtpSettings.FromEmail, _smtpSettings.SenderName),
                    Subject = "Payment Successful",
                    Body = $@"
                <h3>Thank you for your payment!</h3>
                <p>Your payment has been successfully processed.</p>
                <p><strong>Payment Details:</strong></p>
                <ul>
                    <li><strong>Amount:</strong> {amount} {currency}</li>
                    <li><strong>Date and Time:</strong> {paymentTime.ToString("f")}</li>
                </ul>",
                    IsBodyHtml = true,
                };
                mailMessage.To.Add(email);

                await _smtpClient.SendMailAsync(mailMessage);  // Используем _smtpClient, созданный в конструкторе
                _logger.LogInformation($"Email sent to {email}");
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, $"Failed to send email to {email}");
                throw;
            }
        }


        public async Task SendDonationSuccessNotificationAsync(string email, string amount, string currency, DateTime paymentTime)
        {
            if (await CheckEmailExistsAsync(email))
            {
                var mailMessage = new MailMessage
                {
                    From = new MailAddress(_smtpSettings.FromEmail, _smtpSettings.SenderName),
                    Subject = "Thank You for Your Donation!",
                    Body = $@"
            <h3>Thank you for your donation!</h3>
            <p>Your donation of {amount} {currency} has been successfully received.</p>
            <p><strong>Date and Time:</strong> {paymentTime.ToString("f")}</p>",
                    IsBodyHtml = true,
                };
                mailMessage.To.Add(email);

                await _smtpClient.SendMailAsync(mailMessage);
                _logger.LogInformation($"Donation confirmation email sent to {email}");
            }
            else
            {
                _logger.LogWarning($"Email address {email} does not exist.");
                
            }
        }

        private async Task<bool> CheckEmailExistsAsync(string email)
        {

            try
            {
                using (var client = new TcpClient())
                {
                    await client.ConnectAsync(_smtpSettings.Server, _smtpSettings.Port);
                    using (var stream = client.GetStream())
                    using (var reader = new StreamReader(stream))
                    using (var writer = new StreamWriter(stream) { AutoFlush = true })
                    {
                        // Приветствие от сервера
                        await reader.ReadLineAsync();

                        // Отправляем HELO
                        await writer.WriteLineAsync($"HELO {_smtpSettings.Server}");
                        await reader.ReadLineAsync();

                        // Отправляем MAIL FROM
                        await writer.WriteLineAsync($"MAIL FROM:<{_smtpSettings.FromEmail}>");
                        await reader.ReadLineAsync();

                        // Отправляем RCPT TO (запрос на существование email)
                        await writer.WriteLineAsync($"RCPT TO:<{email}>");
                        var response = await reader.ReadLineAsync();

                        // Если ответ начинается с "250", то email существует
                        return response.StartsWith("250");
                    }
                }
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error checking email existence");
                return false;
            }
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
