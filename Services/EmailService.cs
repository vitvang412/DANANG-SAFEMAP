using System.Net;
using System.Net.Mail;

namespace DaNangSafeMap.Services
{
    public class EmailService : IEmailService
    {
        private readonly IConfiguration _configuration;

        public EmailService(IConfiguration configuration)
        {
            _configuration = configuration;
        }

        public async Task<bool> SendOtpAsync(string toEmail, string otp)
        {
            try
            {
                var senderEmail = _configuration["EmailSettings:SenderEmail"];
                var senderName = _configuration["EmailSettings:SenderName"];
                var smtpHost = _configuration["EmailSettings:SmtpHost"];
                var smtpPort = int.Parse(_configuration["EmailSettings:SmtpPort"] ?? "587");
                var appPassword = _configuration["EmailSettings:AppPassword"];

                using var client = new SmtpClient(smtpHost, smtpPort)
                {
                    EnableSsl = true,
                    Credentials = new NetworkCredential(senderEmail, appPassword)
                };

                var mailMessage = new MailMessage
                {
                    From = new MailAddress(senderEmail!, senderName),
                    Subject = "Mã xác nhận đăng ký - DaNangSafeMap",
                    Body = $@"
                        <html>
                        <body style='font-family: Arial, sans-serif; padding: 20px;'>
                            <h2 style='color: #333;'>Xác nhận đăng ký tài khoản</h2>
                            <p>Mã xác nhận của bạn là:</p>
                            <h1 style='color: #007bff; font-size: 32px; letter-spacing: 5px;'>{otp}</h1>
                            <p>Mã này có hiệu lực trong <strong>5 phút</strong>.</p>
                            <p>Nếu bạn không yêu cầu đăng ký, vui lòng bỏ qua email này.</p>
                            <hr/>
                            <p style='color: #666; font-size: 12px;'>DaNangSafeMap - Hệ thống bản đồ an toàn đô thị</p>
                        </body>
                        </html>
                    ",
                    IsBodyHtml = true
                };
                mailMessage.To.Add(toEmail);

                await client.SendMailAsync(mailMessage);
                return true;
            }
            catch (Exception ex)
            {
                Console.WriteLine($"Email Error: {ex.Message}");
                return false;
            }
        }
    }
}
