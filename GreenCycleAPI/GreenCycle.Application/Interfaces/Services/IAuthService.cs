using GreenCycle.Application.DTOs.Auth;

namespace GreenCycle.Application.Interfaces.Services
{
    public interface IAuthService
    {
        /// <summary>Gửi OTP về Email để xác thực trước khi đăng ký.</summary>
        Task SendOtpForRegisterAsync(SendOtpRequestDto request);

        /// <summary>Đăng ký tài khoản mới (yêu cầu OTP đã được xác thực qua Email).</summary>
        Task<AuthResponseDto> RegisterAsync(RegisterRequestDto request);

        /// <summary>Đăng nhập bằng SĐT và mật khẩu.</summary>
        Task<AuthResponseDto> LoginAsync(LoginRequestDto request);

        /// <summary>Đăng nhập hoặc đăng ký bằng tài khoản Google.</summary>
        Task<AuthResponseDto> GoogleLoginAsync(GoogleLoginRequestDto request);

        /// <summary>Gửi mã OTP qua Email để quên/đặt lại mật khẩu.</summary>
        Task SendForgotPasswordOtpAsync(ForgotPasswordOtpRequestDto request);

        /// <summary>Đặt lại mật khẩu mới bằng mã OTP Email.</summary>
        Task ResetPasswordAsync(ResetPasswordRequestDto request);

        /// <summary>Đăng xuất khỏi hệ thống.</summary>
        Task LogoutAsync();
    }
}
