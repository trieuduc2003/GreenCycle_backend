using System;
using System.Security.Claims;
using System.Threading.Tasks;
using GreenCycle.Application.DTOs.Voucher;
using GreenCycle.Application.Interfaces.Services;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace GreenCycle.API.Controllers
{
    [ApiController]
    [Route("api/[controller]")]
    public class VouchersController : ControllerBase
    {
        private readonly IVoucherService _voucherService;

        public VouchersController(IVoucherService voucherService)
        {
            _voucherService = voucherService;
        }

        /// <summary>
        /// Lấy danh sách toàn bộ quà tặng/voucher khả dụng trong Reward Store.
        /// </summary>
        [HttpGet]
        public async Task<IActionResult> GetAvailableVouchers()
        {
            try
            {
                var vouchers = await _voucherService.GetAvailableVouchersAsync();
                return Ok(new { Success = true, Data = vouchers });
            }
            catch (Exception ex)
            {
                return BadRequest(new { Success = false, Message = ex.Message });
            }
        }

        /// <summary>
        /// Thực hiện đổi quà tặng bằng GreenPoints.
        /// </summary>
        [HttpPost("redeem")]
        [Authorize]
        public async Task<IActionResult> RedeemVoucher([FromBody] RedeemVoucherRequestDto request)
        {
            try
            {
                var userId = GetCurrentUserId();
                if (userId == null)
                    return Unauthorized(new { Success = false, Message = "Không thể xác định danh tính người dùng." });

                var result = await _voucherService.RedeemVoucherAsync(userId.Value, request.VoucherId);
                return Ok(result);
            }
            catch (Exception ex)
            {
                return BadRequest(new { Success = false, Message = ex.Message });
            }
        }

        /// <summary>
        /// Lấy danh sách voucher trong "Kho quà của tôi" của người dùng.
        /// </summary>
        [HttpGet("my-vouchers")]
        [Authorize]
        public async Task<IActionResult> GetMyVouchers()
        {
            try
            {
                var userId = GetCurrentUserId();
                if (userId == null)
                    return Unauthorized(new { Success = false, Message = "Không thể xác định danh tính người dùng." });

                var vouchers = await _voucherService.GetMyVouchersAsync(userId.Value);
                return Ok(new { Success = true, Data = vouchers });
            }
            catch (Exception ex)
            {
                return BadRequest(new { Success = false, Message = ex.Message });
            }
        }

        /// <summary>
        /// Đánh dấu một voucher đã được sử dụng.
        /// </summary>
        [HttpPost("use/{id:int}")]
        [Authorize]
        public async Task<IActionResult> UseVoucher([FromRoute] int id)
        {
            try
            {
                var userId = GetCurrentUserId();
                if (userId == null)
                    return Unauthorized(new { Success = false, Message = "Không thể xác định danh tính người dùng." });

                var success = await _voucherService.UseVoucherAsync(userId.Value, id);
                return Ok(new { Success = success, Message = "Đã sử dụng voucher thành công." });
            }
            catch (Exception ex)
            {
                return BadRequest(new { Success = false, Message = ex.Message });
            }
        }

        private int? GetCurrentUserId()
        {
            var claim = User.FindFirst(ClaimTypes.NameIdentifier)
                     ?? User.FindFirst("userId")
                     ?? User.FindFirst("sub");

            if (claim != null && int.TryParse(claim.Value, out var id))
            {
                return id;
            }
            return null;
        }
    }
}
