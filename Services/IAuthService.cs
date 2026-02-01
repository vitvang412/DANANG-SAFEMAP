using DaNangSafeMap.Models;

namespace DaNangSafeMap.Services
{
    public interface IAuthService
    {
        Task<User?> ValidateUserAsync(string emailOrPhone, string password);
        Task<(bool Success, string Message)> RegisterUserAsync(RegisterViewModel model);
        Task<(bool Success, string Message)> RegisterGoogleUserAsync(RegisterWithGoogleViewModel model);
        Task<(bool Success, string Message)> RegisterStandardUserAsync(RegisterStandardViewModel model);
        Task<User?> FindByEmailAsync(string email);
        Task<bool> DeleteUserAsync(int userId);
        string HashPassword(string password, string salt);
    }
}