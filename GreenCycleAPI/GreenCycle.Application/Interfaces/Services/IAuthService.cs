using GreenCycle.Application.DTOs.Auth;

namespace GreenCycle.Application.Interfaces.Services
{
    public interface IAuthService
    {
        /// <summary>Gửi OTP về SĐT để xác thực trước khi đăng ký.</summary>
        Task SendOtpForRegisterAsync(SendOtpRequestDto request);

        /// <summary>Đăng ký tài khoản mới (yêu cầu OTP đã được xác thực).</summary>
        Task<AuthResponseDto> RegisterAsync(RegisterRequestDto request);

        /// <summary>Đăng nhập bằng SĐT và mật khẩu.</summary>
        Task<AuthResponseDto> LoginAsync(LoginRequestDto request);

        /// <summary>Đăng xuất khỏi hệ thống.</summary>
        Task LogoutAsync();
    }
}
