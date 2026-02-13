using DaNangSafeMap.Models;
using Microsoft.Extensions.Caching.Memory;

namespace DaNangSafeMap.Services
{
    public class OtpService : IOtpService
    {
        private readonly IMemoryCache _cache;
        private const int OTP_EXPIRY_MINUTES = 5;

        public OtpService(IMemoryCache cache)
        {
            _cache = cache;
        }

        public string GenerateOtp()
        {
            var random = new Random();
            return random.Next(100000, 999999).ToString();
        }

        public void StoreOtp(string email, string otp, RegisterStandardViewModel registrationData)
        {
            var cacheKey = $"otp_{email.ToLower()}";
            var dataKey = $"reg_{email.ToLower()}";

            var cacheOptions = new MemoryCacheEntryOptions()
                .SetAbsoluteExpiration(TimeSpan.FromMinutes(OTP_EXPIRY_MINUTES));

            _cache.Set(cacheKey, otp, cacheOptions);
            _cache.Set(dataKey, registrationData, cacheOptions);
        }

        public bool ValidateOtp(string email, string otp)
        {
            var cacheKey = $"otp_{email.ToLower()}";

            if (_cache.TryGetValue(cacheKey, out string? storedOtp))
            {
                return storedOtp == otp;
            }

            return false;
        }

        public RegisterStandardViewModel? GetRegistrationData(string email)
        {
            var dataKey = $"reg_{email.ToLower()}";
            _cache.TryGetValue(dataKey, out RegisterStandardViewModel? data);
            return data;
        }

        public void ClearOtp(string email)
        {
            var cacheKey = $"otp_{email.ToLower()}";
            var dataKey = $"reg_{email.ToLower()}";
            _cache.Remove(cacheKey);
            _cache.Remove(dataKey);
        }
    }
}
