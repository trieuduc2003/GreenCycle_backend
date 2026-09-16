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