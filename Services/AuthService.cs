using DaNangSafeMap.Data;
using DaNangSafeMap.Models;
using Microsoft.EntityFrameworkCore;
using System.Security.Cryptography;
using System.Text;

namespace DaNangSafeMap.Services
{
    public class AuthService : IAuthService
    {
        private readonly ApplicationDbContext _context;
        private readonly ILogger<AuthService> _logger;

        public AuthService(ApplicationDbContext context, ILogger<AuthService> logger)
        {
            _context = context;
            _logger = logger;
        }

        public async Task<User?> ValidateUserAsync(string emailOrPhone, string password)
        {
            try
            {
                // Try to find user by email or phone (via UserProfile)
                var user = await _context.Users
                    .Include(u => u.UserProfile)
                    .FirstOrDefaultAsync(u => u.Email == emailOrPhone ||
                                            (u.UserProfile != null && u.UserProfile.PhoneNumber == emailOrPhone));

                if (user == null)
                {
                    _logger.LogWarning("User with email/phone '{EmailOrPhone}' not found", emailOrPhone);
                    return null;
                }

                var hashedPassword = HashPassword(password, user.Salt);

                if (hashedPassword == user.Password_Hash)
                {
                    _logger.LogInformation("User '{Username}' logged in successfully", user.Username);
                    return user;
                }

                _logger.LogWarning("Invalid password for user '{EmailOrPhone}'", emailOrPhone);
                return null;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error validating user '{EmailOrPhone}'", emailOrPhone);
                return null;
            }
        }

        public async Task<(bool Success, string Message)> RegisterUserAsync(RegisterViewModel model)
        {
            try
            {
                // Kiểm tra username đã tồn tại
                if (await _context.Users.AnyAsync(u => u.Username == model.Username))
                    return (false, "Tên đăng nhập đã tồn tại");

                // Kiểm tra email đã tồn tại
                if (await _context.Users.AnyAsync(u => u.Email == model.Email))
                    return (false, "Email đã được sử dụng");

                // Tạo salt và hash password
                var salt = Guid.NewGuid().ToString();
                var hashedPassword = HashPassword(model.Password, salt);

                var user = new User
                {
                    Username = model.Username,
                    Email = model.Email,
                    Password_Hash = hashedPassword,
                    Salt = salt,
                    Role = "user",
                    Created_At = DateTime.UtcNow
                };

                await _context.Users.AddAsync(user);
                await _context.SaveChangesAsync();

                _logger.LogInformation("New user registered: {Username}", model.Username);
                return (true, "Đăng ký thành công!");
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error registering user '{Username}'", model.Username);
                return (false, "Đã xảy ra lỗi trong quá trình đăng ký");
            }
        }

        public string HashPassword(string password, string salt)
        {
            using (var sha256 = SHA256.Create())
            {
                var saltedPassword = password + salt;
                var bytes = Encoding.UTF8.GetBytes(saltedPassword);
                var hash = sha256.ComputeHash(bytes);
                return Convert.ToHexString(hash).ToLower();
            }
        }


        public async Task<(bool Success, string Message)> RegisterGoogleUserAsync(RegisterWithGoogleViewModel model)
        {
            using var transaction = await _context.Database.BeginTransactionAsync();
            try
            {
                if (await _context.Users.AnyAsync(u => u.Email == model.Email))
                    return (false, "Email đã tồn tại trong hệ thống.");

                // Create User (Account)
                var salt = Guid.NewGuid().ToString();
                var hashedPassword = HashPassword(model.Password, salt);

                // Username strategy: Use email prefix or unique string if conflict? 
                // For simplicity, let's try email prefix, if exists append random.
                string baseUsername = model.Email.Split('@')[0];
                string uniqueUsername = baseUsername;
                int counter = 1;
                while (await _context.Users.AnyAsync(u => u.Username == uniqueUsername))
                {
                    uniqueUsername = $"{baseUsername}{counter++}";
                }

                var user = new User
                {
                    Username = uniqueUsername,
                    Email = model.Email,
                    Password_Hash = hashedPassword,
                    Salt = salt,
                    Role = "user",
                    Created_At = DateTime.UtcNow,
                    GoogleId = model.GoogleId
                };

                await _context.Users.AddAsync(user);
                await _context.SaveChangesAsync();

                // Create UserProfile
                var profile = new UserProfile
                {
                    UserId = user.Id,
                    FullName = model.FullName,
                    PhoneNumber = model.PhoneNumber,
                    Address = model.Address,
                    DateOfBirth = model.DateOfBirth,
                    Gender = model.Gender
                };

                await _context.UserProfiles.AddAsync(profile);
                await _context.SaveChangesAsync();

                await transaction.CommitAsync();
                return (true, "Đăng ký thành công.");
            }
            catch (Exception ex)
            {
                await transaction.RollbackAsync();
                _logger.LogError(ex, "Error registering Google user {Email}", model.Email);
                return (false, "Lỗi khi tạo tài khoản.");
            }
        }

        public async Task<(bool Success, string Message)> RegisterStandardUserAsync(RegisterStandardViewModel model, string googleId)
        {
            using var transaction = await _context.Database.BeginTransactionAsync();
            try
            {
                if (await _context.Users.AnyAsync(u => u.Email == model.Email))
                    return (false, "Email đã tồn tại.");

                var salt = Guid.NewGuid().ToString();
                var hashedPassword = HashPassword(model.Password, salt);

                // Generate username same way
                string baseUsername = model.Email.Split('@')[0];
                string uniqueUsername = baseUsername;
                int counter = 1;
                while (await _context.Users.AnyAsync(u => u.Username == uniqueUsername))
                {
                    uniqueUsername = $"{baseUsername}{counter++}";
                }

                var user = new User
                {
                    Username = uniqueUsername,
                    Email = model.Email,
                    Password_Hash = hashedPassword,
                    Salt = salt,
                    Role = "user",
                    Created_At = DateTime.UtcNow,
                    GoogleId = googleId // Linked immediately
                };

                await _context.Users.AddAsync(user);
                await _context.SaveChangesAsync();

                var profile = new UserProfile
                {
                    UserId = user.Id, // Foreign key
                    FullName = model.FullName,
                    PhoneNumber = model.PhoneNumber,
                    Address = model.Address, // Can be null
                    DateOfBirth = model.DateOfBirth,
                    Gender = model.Gender
                };

                await _context.UserProfiles.AddAsync(profile);
                await _context.SaveChangesAsync();

                await transaction.CommitAsync();
                return (true, "Đăng ký thành công.");
            }
            catch (Exception ex)
            {
                await transaction.RollbackAsync();
                _logger.LogError(ex, "Error registering standard user {Email}", model.Email);
                return (false, "Lỗi khi tạo tài khoản.");
            }
        }

        public async Task<User?> FindByGoogleIdAsync(string googleId)
        {
            return await _context.Users
                .Include(u => u.UserProfile) // Include profile if needed, though strictly not required for login
                .FirstOrDefaultAsync(u => u.GoogleId == googleId);
        }

        public async Task<User?> FindByEmailAsync(string email)
        {
            return await _context.Users.FirstOrDefaultAsync(u => u.Email == email);
        }

        public async Task<bool> DeleteUserAsync(int userId)
        {
            try
            {
                var user = await _context.Users
                    .Include(u => u.UserProfile)
                    .FirstOrDefaultAsync(u => u.Id == userId);

                if (user == null)
                {
                    _logger.LogWarning("User with ID {UserId} not found for deletion", userId);
                    return false;
                }

                // Delete user profile first (if exists)
                if (user.UserProfile != null)
                {
                    _context.UserProfiles.Remove(user.UserProfile);
                }

                // Delete user account
                _context.Users.Remove(user);
                await _context.SaveChangesAsync();

                _logger.LogInformation("User {Username} (ID: {UserId}) deleted successfully", user.Username, userId);
                return true;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error deleting user with ID {UserId}", userId);
                return false;
            }
        }
    }
}