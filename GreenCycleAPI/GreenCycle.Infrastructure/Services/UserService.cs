using GreenCycle.Application.DTOs.Auth;
using GreenCycle.Application.Interfaces.Services;
using GreenCycle.Infrastructure.Data;
using Microsoft.EntityFrameworkCore;

namespace GreenCycle.Infrastructure.Services
{
    /// <summary>
    /// Service quản lý hồ sơ cá nhân người dùng.
    /// Tuân thủ SRP — chỉ xử lý profile, không xử lý địa chỉ.
    /// </summary>
    public class UserService : IUserService
    {
        private readonly GreenCycleDbContext _context;

        public UserService(GreenCycleDbContext context)
        {
            _context = context;
        }

        // ─── Lấy hồ sơ cá nhân ────────────────────────────────────────────────
        public async Task<UserProfileDto> GetProfileAsync(int userId)
        {
            var user = await _context.Users
                .Include(u => u.Role)
                .AsNoTracking()
                .FirstOrDefaultAsync(u => u.UserId == userId)
                ?? throw new Exception("Không tìm thấy người dùng.");

            return new UserProfileDto
            {
                UserId    = user.UserId,
                FullName  = user.FullName ?? string.Empty,
                Phone     = user.Phone,
                Email     = user.Email,
                RoleName  = user.Role?.RoleName ?? string.Empty,
                CreatedAt = user.CreatedAt,
            };
        }

        // ─── Cập nhật hồ sơ cá nhân ───────────────────────────────────────────
        public async Task<UserProfileDto> UpdateProfileAsync(int userId, UpdateProfileRequestDto request)
        {
            var user = await _context.Users
                .Include(u => u.Role)
                .FirstOrDefaultAsync(u => u.UserId == userId)
                ?? throw new Exception("Không tìm thấy người dùng.");

            if (!string.IsNullOrWhiteSpace(request.FullName))
                user.FullName = request.FullName.Trim();

            if (!string.IsNullOrWhiteSpace(request.Phone))
            {
                var cleanPhone = request.Phone.Trim().Replace(" ", "").Replace("-", "");
                if (cleanPhone != user.Phone)
                {
                    var isPhoneTaken = await _context.Users
                        .AnyAsync(u => u.Phone == cleanPhone && u.UserId != userId);
                    if (isPhoneTaken)
                        throw new Exception("Số điện thoại này đã được sử dụng bởi một tài khoản khác!");
                    user.Phone = cleanPhone;
                }
            }

            if (request.Email != null)
                user.Email = string.IsNullOrWhiteSpace(request.Email)
                    ? null
                    : request.Email.Trim();

            await _context.SaveChangesAsync();

            return new UserProfileDto
            {
                UserId    = user.UserId,
                FullName  = user.FullName ?? string.Empty,
                Phone     = user.Phone,
                Email     = user.Email,
                RoleName  = user.Role?.RoleName ?? string.Empty,
                CreatedAt = user.CreatedAt,
            };
        }
    }
}
