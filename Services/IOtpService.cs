using DaNangSafeMap.Models;

namespace DaNangSafeMap.Services
{
    public interface IOtpService
    {
        string GenerateOtp();
        void StoreOtp(string email, string otp, RegisterStandardViewModel registrationData);
        bool ValidateOtp(string email, string otp);
        RegisterStandardViewModel? GetRegistrationData(string email);
        void ClearOtp(string email);
    }
}
