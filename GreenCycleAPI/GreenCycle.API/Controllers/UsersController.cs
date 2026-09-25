using GreenCycle.Application.DTOs.Auth;
using GreenCycle.Application.Interfaces.Services;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using System.Security.Claims;

namespace GreenCycle.API.Controllers;

[ApiController]
[Route("api/[controller]")]
[Authorize]
public class UsersController : ControllerBase
{
    private readonly IUserService _userService;
    private readonly IUserAddressService _addressService;

    public UsersController(IUserService userService, IUserAddressService addressService)
    {
        _userService    = userService;
        _addressService = addressService;
    }

    // ─── Helper lấy userId từ JWT ──────────────────────────────────────────
    private int? GetCurrentUserId()
    {
        var claim = User.FindFirst(ClaimTypes.NameIdentifier)
                 ?? User.FindFirst("userId")
                 ?? User.FindFirst("sub");

        return claim != null && int.TryParse(claim.Value, out var id) ? id : null;
    }

    // ══════════════════════════════════════════════════════════════════════
    // PROFILE
    // ══════════════════════════════════════════════════════════════════════

    /// <summary>Lấy thông tin hồ sơ cá nhân của người dùng hiện tại.</summary>
    [HttpGet("profile")]
    public async Task<IActionResult> GetProfile()
    {
        try
        {
            var userId = GetCurrentUserId();
            if (userId == null)
                return Unauthorized(new { Success = false, Message = "Không thể xác định người dùng." });

            var profile = await _userService.GetProfileAsync(userId.Value);
            return Ok(new { Success = true, Data = profile });
        }
        catch (Exception ex)
        {
            return BadRequest(new { Success = false, Message = ex.Message });
        }
    }

    /// <summary>Cập nhật thông tin hồ sơ cá nhân.</summary>
    [HttpPut("profile")]
    public async Task<IActionResult> UpdateProfile([FromBody] UpdateProfileRequestDto request)
    {
        try
        {
            var userId = GetCurrentUserId();
            if (userId == null)
                return Unauthorized(new { Success = false, Message = "Không thể xác định người dùng." });

            var profile = await _userService.UpdateProfileAsync(userId.Value, request);
            return Ok(new { Success = true, Message = "Cập nhật thông tin thành công!", Data = profile });
        }
        catch (Exception ex)
        {
            return BadRequest(new { Success = false, Message = ex.Message });
        }
    }

    // ══════════════════════════════════════════════════════════════════════
    // ADDRESS MANAGEMENT
    // ══════════════════════════════════════════════════════════════════════

    /// <summary>Lấy danh sách địa chỉ đã lưu của người dùng hiện tại.</summary>
    [HttpGet("addresses")]
    public async Task<IActionResult> GetAddresses()
    {
        try
        {
            var userId = GetCurrentUserId();
            if (userId == null)
                return Unauthorized(new { Success = false, Message = "Không thể xác định người dùng." });

            var addresses = await _addressService.GetAddressesAsync(userId.Value);
            return Ok(new { Success = true, Data = addresses });
        }
        catch (Exception ex)
        {
            return BadRequest(new { Success = false, Message = ex.Message });
        }
    }

    /// <summary>Thêm địa chỉ mới — nhập tay hoặc từ GPS điện thoại.</summary>
    [HttpPost("addresses")]
    public async Task<IActionResult> AddAddress([FromBody] AddAddressRequestDto request)
    {
        try
        {
            var userId = GetCurrentUserId();
            if (userId == null)
                return Unauthorized(new { Success = false, Message = "Không thể xác định người dùng." });

            var address = await _addressService.AddAddressAsync(userId.Value, request);
            return Ok(new { Success = true, Message = "Thêm địa chỉ thành công!", Data = address });
        }
        catch (Exception ex)
        {
            return BadRequest(new { Success = false, Message = ex.Message });
        }
    }

    /// <summary>Xóa địa chỉ theo ID.</summary>
    [HttpDelete("addresses/{addressId:int}")]
    public async Task<IActionResult> DeleteAddress(int addressId)
    {
        try
        {
            var userId = GetCurrentUserId();
            if (userId == null)
                return Unauthorized(new { Success = false, Message = "Không thể xác định người dùng." });

            await _addressService.DeleteAddressAsync(userId.Value, addressId);
            return Ok(new { Success = true, Message = "Đã xóa địa chỉ." });
        }
        catch (Exception ex)
        {
            return BadRequest(new { Success = false, Message = ex.Message });
        }
    }

    /// <summary>Đặt địa chỉ làm mặc định.</summary>
    [HttpPatch("addresses/{addressId:int}/set-default")]
    public async Task<IActionResult> SetDefaultAddress(int addressId)
    {
        try
        {
            var userId = GetCurrentUserId();
            if (userId == null)
                return Unauthorized(new { Success = false, Message = "Không thể xác định người dùng." });

            await _addressService.SetDefaultAddressAsync(userId.Value, addressId);
            return Ok(new { Success = true, Message = "Đã đặt địa chỉ mặc định." });
        }
        catch (Exception ex)
        {
            return BadRequest(new { Success = false, Message = ex.Message });
        }
    }
}
