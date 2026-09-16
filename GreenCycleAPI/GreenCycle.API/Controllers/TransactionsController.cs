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
