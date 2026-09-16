using GreenCycle.Application.Interfaces.Services;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using System.Security.Claims;

namespace GreenCycle.API.Controllers;

[ApiController]
[Route("api/[controller]")]
[Authorize]
public class WalletController : ControllerBase
{
    private readonly IWalletService _walletService;

    public WalletController(IWalletService walletService)
    {
        _walletService = walletService;
    }

    /// <summary>
    /// Lấy số dư ví của người dùng đang đăng nhập.
    /// </summary>
    [HttpGet("balance")]
    public async Task<IActionResult> GetBalance()
    {
        try
        {
            // Lấy userId từ JWT claims
            var userIdClaim = User.FindFirst(ClaimTypes.NameIdentifier)
                           ?? User.FindFirst("userId")
                           ?? User.FindFirst("sub");

            if (userIdClaim == null || !int.TryParse(userIdClaim.Value, out var userId))
                return Unauthorized(new { Success = false, Message = "Không thể xác định người dùng." });

            var result = await _walletService.GetBalanceAsync(userId);
            return Ok(new { Success = true, Data = result });
        }
        catch (Exception ex)
        {
            return BadRequest(new { Success = false, Message = ex.Message });
        }
    }
}
