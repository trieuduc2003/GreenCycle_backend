using GreenCycle.Application.DTOs.Order;
using GreenCycle.Application.Interfaces.Services;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using System;
using System.Security.Claims;
using System.Threading.Tasks;

namespace GreenCycle.API.Controllers;

[ApiController]
[Route("api/[controller]")]
public class OrdersController : ControllerBase
{
    private readonly IOrderService _orderService;

    public OrdersController(IOrderService orderService)
    {
        _orderService = orderService;
    }

    /// <summary>
    /// Lấy danh sách danh mục rác và đơn giá GreenPoints hiện tại.
    /// </summary>
    [HttpGet("waste-categories")]
    public async Task<IActionResult> GetWasteCategories()
    {
        try
        {
            var categories = await _orderService.GetWasteCategoriesAsync();
            return Ok(new { Success = true, Data = categories });
        }
        catch (Exception ex)
        {
            return BadRequest(new { Success = false, Message = ex.Message });
        }
    }

    /// <summary>
    /// Khởi tạo đơn khai báo rác mới (Tạo đơn bán rác).
    /// </summary>
    [HttpPost("create")]
    [Authorize]
    public async Task<IActionResult> CreateOrder([FromBody] CreateOrderRequestDto request)
    {
        try
        {
            var userIdClaim = User.FindFirst(ClaimTypes.NameIdentifier)
                           ?? User.FindFirst("userId")
                           ?? User.FindFirst("sub");

            if (userIdClaim == null || !int.TryParse(userIdClaim.Value, out var sellerId))
            {
                return Unauthorized(new { Success = false, Message = "Không thể xác định người dùng." });
            }

            var result = await _orderService.CreateOrderAsync(sellerId, request);
            return Ok(new { Success = true, Message = "Tạo đơn bán rác thành công!", Data = result });
        }
        catch (Exception ex)
        {
            var msg = ex.InnerException != null ? ex.InnerException.Message : ex.Message;
            return BadRequest(new { Success = false, Message = msg });
        }
    }

    /// <summary>
    /// Lấy danh sách các đơn bán rác của người dùng hiện tại.
    /// </summary>
    [HttpGet("my-orders")]
    [Authorize]
    public async Task<IActionResult> GetMyOrders()
    {
        try
        {
            var userIdClaim = User.FindFirst(ClaimTypes.NameIdentifier)
                           ?? User.FindFirst("userId")
                           ?? User.FindFirst("sub");

            if (userIdClaim == null || !int.TryParse(userIdClaim.Value, out var sellerId))
            {
                return Unauthorized(new { Success = false, Message = "Không thể xác định người dùng." });
            }

            var orders = await _orderService.GetSellerOrdersAsync(sellerId);
            return Ok(new { Success = true, Data = orders });
        }
        catch (Exception ex)
        {
            return BadRequest(new { Success = false, Message = ex.Message });
        }
    }

    /// <summary>
    /// Lấy chi tiết các loại rác trong đơn (Chủ Vựa dùng để nhập cân thực tế).
    /// </summary>
    [HttpGet("{orderId}/details")]
    [Authorize]
    public async Task<IActionResult> GetOrderDetails(int orderId)
    {
        try
        {
            var details = await _orderService.GetOrderDetailsAsync(orderId);
            return Ok(new { Success = true, Data = details });
        }
        catch (Exception ex)
        {
            return BadRequest(new { Success = false, Message = ex.Message });
        }
    }
}
