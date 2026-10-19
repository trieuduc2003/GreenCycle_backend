using System.Net;
using System.Net.Mail;
using GreenCycle.Application.Interfaces.Services;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Logging;

namespace GreenCycle.Infrastructure.Services
{
    /// <summary>
    /// Dịch vụ gửi mã xác thực OTP qua Email (hỗ trợ Gmail SMTP miễn phí hoặc bất kỳ máy chủ SMTP nào).
    /// </summary>
    public class EmailOtpSender : IOtpSender
    {
        private readonly IConfiguration _config;
        private readonly ILogger<EmailOtpSender> _logger;

        public EmailOtpSender(IConfiguration config, ILogger<EmailOtpSender> logger)
        {
            _config = config;
            _logger = logger;
        }

        public async Task SendOtpAsync(string destination, string otp, int validMinutes, string? subject = null)
        {
            var email = destination.Trim();
            var emailSubject = subject ?? "GreenCycle - Mã xác thực OTP của bạn";

            var smtpServer = _config["EmailSettings:SmtpServer"] ?? "smtp.gmail.com";
            var smtpPortStr = _config["EmailSettings:SmtpPort"] ?? "587";
            int.TryParse(smtpPortStr, out int smtpPort);
            if (smtpPort == 0) smtpPort = 587;

            var senderEmail = _config["EmailSettings:SenderEmail"];
            var senderName = _config["EmailSettings:SenderName"] ?? "GreenCycle App";
            var appPassword = _config["EmailSettings:AppPassword"];
            var enableSsl = bool.Parse(_config["EmailSettings:EnableSsl"] ?? "true");

            // Nếu chưa cấu hình SenderEmail hoặc AppPassword, in ra Console để lập trình viên test
            if (string.IsNullOrWhiteSpace(senderEmail) || string.IsNullOrWhiteSpace(appPassword))
            {
                _logger.LogWarning("[EMAIL-OTP] Chưa cấu hình EmailSettings:SenderEmail hoặc AppPassword trong appsettings.json.");
                Console.WriteLine("=================================================");
                Console.WriteLine($"  [EMAIL-OTP] Người nhận: {email}");
                Console.WriteLine($"  [EMAIL-OTP] Tiêu đề   : {emailSubject}");
                Console.WriteLine($"  [EMAIL-OTP] Mã OTP    : {otp} (Hiệu lực: {validMinutes} phút)");
                Console.WriteLine("=================================================");
                return;
            }

            try
            {
                var htmlBody = GenerateOtpEmailTemplate(otp, validMinutes);

                using var client = new SmtpClient(smtpServer, smtpPort)
                {
                    Credentials = new NetworkCredential(senderEmail, appPassword),
                    EnableSsl = enableSsl,
                    Timeout = 15000 // 15 giây
                };

                using var mailMessage = new MailMessage
                {
                    From = new MailAddress(senderEmail, senderName),
                    Subject = emailSubject,
                    Body = htmlBody,
                    IsBodyHtml = true
                };

                mailMessage.To.Add(email);

                await client.SendMailAsync(mailMessage);
                _logger.LogInformation("[EMAIL-OTP] Đã gửi mã OTP thành công đến email: {Email}", email);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "[EMAIL-OTP] Lỗi khi gửi email tới {Email}: {Message}", email, ex.Message);
                // Fallback in ra console để quá trình test không bị nghẽn
                Console.WriteLine("=================================================");
                Console.WriteLine($"  [EMAIL-OTP-FALLBACK] Người nhận: {email}");
                Console.WriteLine($"  [EMAIL-OTP-FALLBACK] Mã OTP    : {otp}");
                Console.WriteLine("=================================================");
                throw new Exception($"Không thể gửi email OTP: {ex.Message}. Vui lòng kiểm tra lại địa chỉ email hoặc cấu hình gửi thư.");
            }
        }

        private static string GenerateOtpEmailTemplate(string otp, int validMinutes)
        {
            return $@"
<!DOCTYPE html>
<html>
<head>
    <meta charset='utf-8'>
    <style>
        body {{ font-family: 'Segoe UI', Tahoma, Geneva, Verdana, sans-serif; background-color: #f4f6f8; margin: 0; padding: 20px; }}
        .container {{ max-width: 520px; margin: 0 auto; background: #ffffff; border-radius: 16px; overflow: hidden; box-shadow: 0 4px 20px rgba(0,0,0,0.06); }}
        .header {{ background: linear-gradient(135deg, #1B5E20, #2E7D32); padding: 32px 20px; text-align: center; color: #ffffff; }}
        .header h1 {{ margin: 0; font-size: 26px; font-weight: 700; letter-spacing: 0.5px; }}
        .content {{ padding: 32px 28px; text-align: center; color: #333333; }}
        .title {{ font-size: 18px; font-weight: 600; margin-bottom: 8px; color: #1e293b; }}
        .subtitle {{ font-size: 14px; color: #64748b; margin-bottom: 24px; line-height: 1.5; }}
        .otp-box {{ background: #e8f5e9; border: 2px dashed #4caf50; border-radius: 12px; padding: 18px 24px; display: inline-block; margin: 12px 0 24px 0; }}
        .otp-code {{ font-size: 34px; font-weight: 800; color: #1b5e20; letter-spacing: 6px; font-family: 'Courier New', Courier, monospace; }}
        .warning {{ font-size: 13px; color: #d32f2f; background: #ffebee; padding: 10px 14px; border-radius: 8px; margin-top: 16px; display: inline-block; }}
        .footer {{ background: #f8fafc; padding: 20px; text-align: center; font-size: 12px; color: #94a3b8; border-top: 1px solid #e2e8f0; }}
    </style>
</head>
<body>
    <div class='container'>
        <div class='header'>
            <h1>🌱 GreenCycle</h1>
            <p style='margin: 4px 0 0 0; font-size: 14px; opacity: 0.9;'>Nền tảng Thu gom & Tái chế Phế liệu Thông minh</p>
        </div>
        <div class='content'>
            <div class='title'>Xác thực tài khoản của bạn</div>
            <div class='subtitle'>Vui lòng sử dụng mã xác thực bên dưới để hoàn tất xác minh tài khoản của bạn trên GreenCycle.</div>
            <div class='otp-box'>
                <div class='otp-code'>{otp}</div>
            </div>
            <div class='subtitle'>Mã OTP này có hiệu lực trong vòng <strong>{validMinutes} phút</strong>.</div>
            <div class='warning'>⚠️ Không chia sẻ mã này với bất kỳ ai để đảm bảo an toàn cho tài khoản của bạn.</div>
        </div>
        <div class='footer'>
            &copy; 2026 GreenCycle Platform. Mọi quyền được bảo lưu.<br>
            Email này được gửi tự động, vui lòng không trả lời.
        </div>
    </div>
</body>
</html>";
        }
    }
}
