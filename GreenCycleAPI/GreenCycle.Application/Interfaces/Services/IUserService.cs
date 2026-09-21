using GreenCycle.Application.DTOs.Auth;

namespace GreenCycle.Application.Interfaces.Services
{
    /// <summary>
    /// Interface quản lý hồ sơ cá nhân người dùng.
    /// Tuân thủ Single Responsibility Principle (SRP) — chỉ xử lý profile.
    /// </summary>
    public interface IUserService
    {
        /// <summary>Lấy hồ sơ cá nhân của người dùng.</summary>
        Task<UserProfileDto> GetProfileAsync(int userId);

        /// <summary>Cập nhật thông tin hồ sơ cá nhân.</summary>
        Task<UserProfileDto> UpdateProfileAsync(int userId, UpdateProfileRequestDto request);
    }
}
