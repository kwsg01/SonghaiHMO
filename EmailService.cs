using SonghaiHMO.Data;
using SonghaiHMO.Models;
using Microsoft.Extensions.Configuration;
using System;
using System.Net;
using System.Net.Mail;
using System.Threading.Tasks;

namespace SonghaiHMO.Services
{
    public class EmailService
    {
        private readonly ApplicationDbContext _context;
        private readonly IConfiguration _configuration;

        public EmailService(ApplicationDbContext context, IConfiguration configuration)
        {
            _context = context;
            _configuration = configuration;
        }

        private SmtpClient GetSmtpClient()
        {
            var smtpServer = _configuration["EmailSettings:SmtpServer"] ?? "smtp.gmail.com";
            var smtpPort = int.Parse(_configuration["EmailSettings:SmtpPort"] ?? "587");
            var username = _configuration["EmailSettings:Username"] ?? "";
            var password = _configuration["EmailSettings:Password"] ?? "";

            var client = new SmtpClient(smtpServer, smtpPort);
            client.EnableSsl = true;
            client.Credentials = new NetworkCredential(username, password);
            return client;
        }

        public async Task SendEmailAsync(string toEmail, string subject, string body, bool isHtml = true)
        {
            try
            {
                var fromEmail = _configuration["EmailSettings:SenderEmail"] ?? "";
                var fromName = _configuration["EmailSettings:SenderName"] ?? "Songhai Health Trust HMO";

                if (string.IsNullOrEmpty(fromEmail) || string.IsNullOrEmpty(toEmail))
                {
                    Console.WriteLine($"⚠️ Email not sent - missing sender or recipient");
                    return;
                }

                var mailMessage = new MailMessage();
                mailMessage.From = new MailAddress(fromEmail, fromName);
                mailMessage.To.Add(toEmail);
                mailMessage.Subject = subject;
                mailMessage.Body = body;
                mailMessage.IsBodyHtml = isHtml;

                using (var client = GetSmtpClient())
                {
                    await client.SendMailAsync(mailMessage);
                }

                Console.WriteLine($"📧 EMAIL SENT SUCCESSFULLY!");
                Console.WriteLine($"   To: {toEmail}");
                Console.WriteLine($"   Subject: {subject}");
            }
            catch (Exception ex)
            {
                Console.WriteLine($"❌ EMAIL FAILED: {ex.Message}");
            }
        }

        public async Task SendTwoFactorCodeEmail(string toEmail, string code)
        {
            var subject = "Your Two-Factor Authentication Code - Songhai HMO";
            var body = $@"
                <div style='font-family: Arial, sans-serif; padding: 20px; max-width: 500px; margin: 0 auto; border: 1px solid #ddd; border-radius: 10px;'>
                    <div style='background: linear-gradient(135deg, #667eea 0%, #764ba2 100%); padding: 20px; text-align: center; border-radius: 10px 10px 0 0;'>
                        <h2 style='color: white; margin: 0;'>🏥 Songhai Health Trust HMO</h2>
                    </div>
                    <div style='padding: 20px;'>
                        <h3 style='color: #1a73e8;'>Two-Factor Authentication</h3>
                        <p>Your verification code is:</p>
                        <div style='background: #f5f5f5; padding: 15px; text-align: center; font-size: 32px; font-weight: bold; letter-spacing: 5px; border-radius: 8px;'>
                            {code}
                        </div>
                        <p style='color: #666; font-size: 12px; margin-top: 20px;'>This code will expire in 5 minutes.</p>
                    </div>
                </div>";
            
            await SendEmailAsync(toEmail, subject, body);
        }

        public async Task SendPasswordResetEmail(string toEmail, string resetLink)
        {
            var subject = "Password Reset Request - Songhai HMO";
            var body = $@"
                <div style='font-family: Arial, sans-serif; padding: 20px; max-width: 500px; margin: 0 auto; border: 1px solid #ddd; border-radius: 10px;'>
                    <div style='background: linear-gradient(135deg, #667eea 0%, #764ba2 100%); padding: 20px; text-align: center; border-radius: 10px 10px 0 0;'>
                        <h2 style='color: white; margin: 0;'>🏥 Songhai Health Trust HMO</h2>
                    </div>
                    <div style='padding: 20px;'>
                        <h3 style='color: #1a73e8;'>Password Reset Request</h3>
                        <p>Click the button below to reset your password:</p>
                        <p style='text-align: center;'>
                            <a href='{resetLink}' style='background: #1a73e8; color: white; padding: 12px 25px; text-decoration: none; border-radius: 5px; display: inline-block;'>Reset Password</a>
                        </p>
                        <p style='color: #666; font-size: 12px; margin-top: 20px;'>This link expires in 1 hour.</p>
                    </div>
                </div>";
            
            await SendEmailAsync(toEmail, subject, body);
        }

        public async Task SendClaimStatusEmail(string providerEmail, string providerName, int claimId, string status, string notes, string enrolleeName, decimal amount)
        {
            var statusColor = status == "Approved" ? "#28a745" : "#dc3545";
            var subject = $"Claim #{claimId} {status} - Songhai HMO";
            var body = $@"
                <div style='font-family: Arial, sans-serif; padding: 20px; max-width: 600px; margin: 0 auto; border: 1px solid #ddd; border-radius: 10px;'>
                    <div style='background: linear-gradient(135deg, #667eea 0%, #764ba2 100%); padding: 20px; text-align: center; border-radius: 10px 10px 0 0;'>
                        <h2 style='color: white; margin: 0;'>🏥 Songhai Health Trust HMO</h2>
                    </div>
                    <div style='padding: 20px;'>
                        <h3 style='color: #1a73e8;'>Claim Status Update</h3>
                        <p>Dear <strong>{providerName}</strong>,</p>
                        <p>Your claim <strong>#{claimId}</strong> has been <strong style='color: {statusColor};'>{status}</strong>.</p>
                        <div style='background: #f5f5f5; padding: 15px; border-radius: 8px; margin: 15px 0;'>
                            <p><strong>📋 Claim Details:</strong></p>
                            <p>Enrollee: {enrolleeName}</p>
                            <p>Amount: ₦{amount:N0}</p>
                            {(string.IsNullOrEmpty(notes) ? "" : $"<p>Notes: {notes}</p>")}
                        </div>
                        <p><a href='http://localhost:5000/Provider/Dashboard' style='background: #1a73e8; color: white; padding: 10px 20px; text-decoration: none; border-radius: 5px; display: inline-block;'>Login to Portal</a></p>
                    </div>
                </div>";
            
            await SendEmailAsync(providerEmail, subject, body);
        }

        public async Task SendWelcomeEmail(string email, string name, string role)
        {
            var subject = $"Welcome to Songhai HMO - {role} Portal";
            var body = $@"
                <div style='font-family: Arial, sans-serif; padding: 20px; max-width: 500px; margin: 0 auto; border: 1px solid #ddd; border-radius: 10px;'>
                    <div style='background: linear-gradient(135deg, #667eea 0%, #764ba2 100%); padding: 20px; text-align: center; border-radius: 10px 10px 0 0;'>
                        <h2 style='color: white; margin: 0;'>🏥 Songhai Health Trust HMO</h2>
                    </div>
                    <div style='padding: 20px;'>
                        <h3 style='color: #1a73e8;'>Welcome, {name}!</h3>
                        <p>Your {role} account has been successfully created.</p>
                        <p>You can now login to the portal using your email address.</p>
                        <p><a href='http://localhost:5000/Home/Portal' style='background: #1a73e8; color: white; padding: 10px 20px; text-decoration: none; border-radius: 5px; display: inline-block;'>Access Portal</a></p>
                    </div>
                </div>";
            
            await SendEmailAsync(email, subject, body);
        }
    }
}