using SonghaiHMO.Data;
using SonghaiHMO.Models;
using System;
using System.Linq;
using System.Threading.Tasks;

namespace SonghaiHMO.Services
{
    public class TwoFactorService
    {
        private readonly ApplicationDbContext _context;
        private readonly EmailService _emailService;

        public TwoFactorService(ApplicationDbContext context, EmailService emailService)
        {
            _context = context;
            _emailService = emailService;
        }

        public string GenerateCode()
        {
            var random = new Random();
            return random.Next(100000, 999999).ToString();
        }

        public async Task<string> SendTwoFactorCode(string userId, string email)
        {
            var code = GenerateCode();
            var expiresAt = DateTime.Now.AddMinutes(5);

            var twoFactorCode = new TwoFactorCode
            {
                UserId = userId,
                Code = code,
                ExpiresAt = expiresAt,
                IsUsed = false
            };

            _context.TwoFactorCodes.Add(twoFactorCode);
            await _context.SaveChangesAsync();

            await _emailService.SendTwoFactorCodeEmail(email, code);

            return code;
        }

        public async Task<bool> VerifyCode(string userId, string code)
        {
            var twoFactorCode = _context.TwoFactorCodes
                .FirstOrDefault(c => c.UserId == userId && c.Code == code && !c.IsUsed && c.ExpiresAt > DateTime.Now);

            if (twoFactorCode != null)
            {
                twoFactorCode.IsUsed = true;
                await _context.SaveChangesAsync();
                return true;
            }
            return false;
        }

        public async Task EnableTwoFactor(string userId)
        {
            var profile = await _context.UserProfiles.FindAsync(userId);
            if (profile != null)
            {
                profile.IsTwoFactorEnabled = true;
                await _context.SaveChangesAsync();
            }
        }

        public async Task DisableTwoFactor(string userId)
        {
            var profile = await _context.UserProfiles.FindAsync(userId);
            if (profile != null)
            {
                profile.IsTwoFactorEnabled = false;
                await _context.SaveChangesAsync();
            }
        }

        public async Task<bool> IsTwoFactorEnabled(string userId)
        {
            var profile = await _context.UserProfiles.FindAsync(userId);
            return profile?.IsTwoFactorEnabled ?? false;
        }
    }
}