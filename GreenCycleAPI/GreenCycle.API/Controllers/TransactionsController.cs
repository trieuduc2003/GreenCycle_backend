using GreenCycle.Application.DTOs.Common;
using GreenCycle.Application.DTOs.Transaction;
using GreenCycle.Application.Interfaces.Services;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using System.Security.Claims;
using System.Threading.Tasks;

namespace GreenCycle.API.Controllers
{
    [Route("api/[controller]")]
    [ApiController]
    [Authorize]
    public class TransactionsController : ControllerBase
    {
        private readonly ITransactionService _transactionService;

        public TransactionsController(ITransactionService transactionService)
        {
            _transactionService = transactionService;
        }

        /// <summary>
        /// [ScrapYard] Chủ Vựa nhập cân thực tế → gửi Double Confirmation tới Seller (Drop-off).
        /// </summary>
        [HttpPost("initiate")]
        [Authorize(Roles = "ScrapYard,Collector")]
        public async Task<IActionResult> InitiateTransaction([FromBody] InitiateTransactionRequestDto request)
        {
            var userIdStr = User.FindFirstValue(ClaimTypes.NameIdentifier);
            if (!int.TryParse(userIdStr, out int userId))
                return Unauthorized(new ApiResponse<object> { Success = false, Message = "Lỗi xác thực người dùng." });

            var result = await _transactionService.InitiateTransactionAsync(userId, request);
            return Ok(result);
        }

        /// <summary>
        /// [Collector] Tài xế nhập cân thực tế → gửi Double Confirmation tới Seller (Pick-up).
        /// </summary>
        [HttpPost("pickup-initiate")]
        [Authorize(Roles = "Collector")]
        public async Task<IActionResult> InitiatePickupTransaction([FromBody] InitiateTransactionRequestDto request)
        {
            var userIdStr = User.FindFirstValue(ClaimTypes.NameIdentifier);
            if (!int.TryParse(userIdStr, out int userId))
                return Unauthorized(new ApiResponse<object> { Success = false, Message = "Lỗi xác thực người dùng." });

            var result = await _transactionService.InitiatePickupTransactionAsync(userId, request);
            return Ok(result);
        }

        /// <summary>
        /// [Seller] Xác nhận giao dịch sau khi nhận Double Confirmation popup.
        /// </summary>
        [HttpPost("confirm")]
        [Authorize(Roles = "Seller")]
        public async Task<IActionResult> ConfirmTransaction([FromBody] ConfirmTransactionRequestDto request)
        {
            var userIdStr = User.FindFirstValue(ClaimTypes.NameIdentifier);
            if (!int.TryParse(userIdStr, out int userId))
                return Unauthorized(new ApiResponse<object> { Success = false, Message = "Lỗi xác thực người dùng." });

            var result = await _transactionService.ConfirmTransactionAsync(userId, request);
            return Ok(result);
        }
    }
}
