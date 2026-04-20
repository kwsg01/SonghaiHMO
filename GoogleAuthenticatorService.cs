using OtpNet;
using System;

namespace SonghaiHMO.Services
{
    public class GoogleAuthenticatorService
    {
        public string GenerateSecretKey()
        {
            var key = KeyGeneration.GenerateRandomKey(20);
            return Base32Encoding.ToString(key);
        }

        public string GenerateQrCodeUri(string email, string secretKey, string issuer = "SonghaiHMO")
        {
            var encodedIssuer = Uri.EscapeDataString(issuer);
            var encodedEmail = Uri.EscapeDataString(email);
            return $"otpauth://totp/{encodedIssuer}:{encodedEmail}?secret={secretKey}&issuer={encodedIssuer}&algorithm=SHA1&digits=6&period=30";
        }

        public bool ValidateCode(string secretKey, string code)
        {
            if (string.IsNullOrEmpty(secretKey) || string.IsNullOrEmpty(code))
                return false;

            try
            {
                var key = Base32Encoding.ToBytes(secretKey);
                var totp = new Totp(key);
                return totp.VerifyTotp(code, out _);
            }
            catch
            {
                return false;
            }
        }

        public string GenerateBackupCodes()
        {
            var random = new Random();
            var codes = new string[10];
            for (int i = 0; i < 10; i++)
            {
                codes[i] = random.Next(10000000, 99999999).ToString();
            }
            return string.Join(",", codes);
        }
    }
}