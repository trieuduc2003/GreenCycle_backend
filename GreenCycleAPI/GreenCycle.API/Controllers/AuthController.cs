using GreenCycle.Application.DTOs.Auth;
using GreenCycle.Application.Interfaces.Services;
using Microsoft.AspNetCore.Mvc;

namespace GreenCycle.API.Controllers;

[ApiController]
[Route("api/[controller]")]
public class AuthController : ControllerBase
{
    private readonly IAuthService _authService;

    public AuthController(IAuthService authService)
    {
        _authService = authService;
    }

    /// <summary>
    /// Bước 1 đăng ký: Gửi OTP về SĐT để xác thực số điện thoại.
    /// </summary>
    [HttpPost("send-otp")]
    public async Task<IActionResult> SendOtp([FromBody] SendOtpRequestDto request)
    {
        try
        {
            await _authService.SendOtpForRegisterAsync(request);
            return Ok(new { Success = true, Message = "Mã OTP đã được gửi! Vui lòng kiểm tra điện thoại của bạn." });
        }
        catch (Exception ex)
        {
            return BadRequest(new { Success = false, Message = ex.Message });
        }
    }

    /// <summary>
    /// Bước 2 đăng ký: Xác thực OTP và tạo tài khoản mới.
    /// </summary>
    [HttpPost("register")]
    public async Task<IActionResult> Register([FromBody] RegisterRequestDto request)
    {
        try
        {
            var result = await _authService.RegisterAsync(request);
            return Ok(new { Success = true, Message = "Đăng ký thành công!", Data = result });
        }
        catch (Exception ex)
        {
            return BadRequest(new { Success = false, Message = ex.Message });
        }
    }

    /// <summary>
    /// Đăng nhập bằng Số điện thoại và Mật khẩu.
    /// </summary>
    [HttpPost("login")]
    public async Task<IActionResult> Login([FromBody] LoginRequestDto request)
    {
        try
        {
            var result = await _authService.LoginAsync(request);
            return Ok(new { Success = true, Message = "Đăng nhập thành công!", Data = result });
        }
        catch (Exception ex)
        {
            var msg = ex.Message;
            if (msg.Contains("SHUTDOWN") || msg.Contains("Login failed") || msg.Contains("administrators may connect") || msg.Contains("network-related"))
            {
                msg = "Máy chủ cơ sở dữ liệu đang bảo trì/khởi động lại trong giây lát. Vui lòng thử lại sau 1-2 phút.";
            }
            return BadRequest(new { Success = false, Message = msg });
        }
    }

    /// <summary>
    /// Đăng nhập / Đăng ký bằng Google (OAuth 2.0 IdToken).
    /// </summary>
    [HttpPost("google-login")]
    public async Task<IActionResult> GoogleLogin([FromBody] GoogleLoginRequestDto request)
    {
        try
        {
            var result = await _authService.GoogleLoginAsync(request);
            return Ok(new { Success = true, Message = "Đăng nhập Google thành công!", Data = result });
        }
        catch (Exception ex)
        {
            return BadRequest(new { Success = false, Message = ex.Message });
        }
    }

    /// <summary>
    /// Health check để Mobile App đánh thức server Render từ trạng thái Sleep.
    /// </summary>
    [HttpGet("health")]
    public IActionResult HealthCheck()
    {
        return Ok(new { Status = "Healthy", ServerTime = DateTime.UtcNow });
    }

    /// <summary>
    /// Quên mật khẩu: Gửi OTP xác thực đến Email của tài khoản.
    /// </summary>
    [HttpPost("forgot-password/send-otp")]
    public async Task<IActionResult> ForgotPasswordSendOtp([FromBody] ForgotPasswordOtpRequestDto request)
    {
        try
        {
            await _authService.SendForgotPasswordOtpAsync(request);
            return Ok(new { Success = true, Message = "Mã OTP đã được gửi đến email của bạn! Vui lòng kiểm tra hòm thư." });
        }
        catch (Exception ex)
        {
            return BadRequest(new { Success = false, Message = ex.Message });
        }
    }

    /// <summary>
    /// Đặt lại mật khẩu mới sau khi xác thực mã OTP gửi về Email.
    /// </summary>
    [HttpPost("forgot-password/reset")]
    public async Task<IActionResult> ResetPassword([FromBody] ResetPasswordRequestDto request)
    {
        try
        {
            await _authService.ResetPasswordAsync(request);
            return Ok(new { Success = true, Message = "Đặt lại mật khẩu thành công! Vui lòng đăng nhập với mật khẩu mới." });
        }
        catch (Exception ex)
        {
            return BadRequest(new { Success = false, Message = ex.Message });
        }
    }

    /// <summary>
    /// Đăng xuất khỏi hệ thống.
    /// </summary>
    [HttpPost("logout")]
    public async Task<IActionResult> Logout()
    {
        try
        {
            await _authService.LogoutAsync();
            return Ok(new { Success = true, Message = "Đăng xuất thành công!" });
        }
        catch (Exception ex)
        {
            return BadRequest(new { Success = false, Message = ex.Message });
        }
    }
}