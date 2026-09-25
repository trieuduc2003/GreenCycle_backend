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

    private int? GetCurrentUserId()
    {
        var claim = User.FindFirst(ClaimTypes.NameIdentifier)
                 ?? User.FindFirst("userId")
                 ?? User.FindFirst("sub");
        return claim != null && int.TryParse(claim.Value, out var id) ? id : null;
    }

    /// <summary>Lấy số dư ví của người dùng đang đăng nhập.</summary>
    [HttpGet("balance")]
    public async Task<IActionResult> GetBalance()
    {
        try
        {
            var userId = GetCurrentUserId();
            if (userId == null)
                return Unauthorized(new { Success = false, Message = "Không thể xác định người dùng." });

            var result = await _walletService.GetBalanceAsync(userId.Value);
            return Ok(new { Success = true, Data = result });
        }
        catch (Exception ex)
        {
            return BadRequest(new { Success = false, Message = ex.Message });
        }
    }

    /// <summary>Lấy lịch sử giao dịch ví — dùng làm nguồn thông báo cho Seller.</summary>
    [HttpGet("transactions")]
    public async Task<IActionResult> GetTransactions([FromQuery] int limit = 50)
    {
        try
        {
            var userId = GetCurrentUserId();
            if (userId == null)
                return Unauthorized(new { Success = false, Message = "Không thể xác định người dùng." });

            var transactions = await _walletService.GetTransactionsAsync(userId.Value, limit);
            return Ok(new { Success = true, Data = transactions });
        }
        catch (Exception ex)
        {
            return BadRequest(new { Success = false, Message = ex.Message });
        }
    }

    public class DepositRequest
        {
            public decimal Amount { get; set; }
        }

        /// <summary>Nạp tiền vào ví.</summary>
        [HttpPost("deposit")]
        public async Task<IActionResult> Deposit([FromBody] DepositRequest request)
        {
            try
            {
                var userId = GetCurrentUserId();
                if (userId == null)
                    return Unauthorized(new { Success = false, Message = "Không thể xác định người dùng." });

                if (request.Amount <= 0)
                    return BadRequest(new { Success = false, Message = "Số tiền nạp phải lớn hơn 0." });

                await _walletService.DepositAsync(userId.Value, request.Amount, $"Nạp {request.Amount:N0} GP vào ví");
                return Ok(new { Success = true, Message = "Nạp tiền thành công!" });
            }
            catch (Exception ex)
            {
                return BadRequest(new { Success = false, Message = ex.Message });
            }
        }
    }
