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
    /// Lấy lịch sử các đơn bán rác của người dùng hiện tại.
    /// </summary>
    [HttpGet("history")]
    [Authorize]
    public async Task<IActionResult> GetOrderHistory()
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

            var orders = await _orderService.GetSellerOrderHistoryAsync(sellerId);
            return Ok(new { Success = true, Data = orders });
        }
        catch (Exception ex)
        {
            return BadRequest(new { Success = false, Message = ex.Message });
        }
    }

    /// <summary>
    /// Lấy lịch sử các đơn hàng thu mua của Vựa hiện tại.
    /// </summary>
    [HttpGet("yard-history")]
    [Authorize(Roles = "ScrapYard")]
    public async Task<IActionResult> GetYardOrderHistory()
    {
        try
        {
            var userIdClaim = User.FindFirst(ClaimTypes.NameIdentifier)
                           ?? User.FindFirst("userId")
                           ?? User.FindFirst("sub");

            if (userIdClaim == null || !int.TryParse(userIdClaim.Value, out var yardUserId))
            {
                return Unauthorized(new { Success = false, Message = "Không thể xác định người dùng." });
            }

            var orders = await _orderService.GetYardOrderHistoryAsync(yardUserId);
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

    /// <summary>
    /// Lấy chi tiết toàn diện của đơn hàng cho Người bán.
    /// </summary>
    [HttpGet("{orderId:int}")]
    [Authorize]
    public async Task<IActionResult> GetOrderDetail(int orderId)
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

            var order = await _orderService.GetOrderDetailViewAsync(orderId, sellerId);
            if (order == null)
            {
                return NotFound(new { Success = false, Message = "Không tìm thấy đơn hàng hoặc bạn không có quyền xem." });
            }

            return Ok(new { Success = true, Data = order });
        }
        catch (Exception ex)
        {
            return BadRequest(new { Success = false, Message = ex.Message });
        }
    }

    /// <summary>
    /// Chọn vựa rác cho đơn hàng Drop-off.
    /// </summary>
    [HttpPut("{orderId:int}/select-yard")]
    [Authorize]
    public async Task<IActionResult> SelectYard(int orderId, [FromBody] int yardId)
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

            var result = await _orderService.SelectYardForOrderAsync(orderId, sellerId, yardId);
            return Ok(new { Success = result, Message = "Đã chọn vựa rác thành công." });
        }
        catch (Exception ex)
        {
            return BadRequest(new { Success = false, Message = ex.Message });
        }
    }

    // ═══════════════════════════════════════════════════════════
    // PICK-UP LIFECYCLE ENDPOINTS
    // ═══════════════════════════════════════════════════════════

    /// <summary>
    /// [Collector, YardOwner] Lấy danh sách đơn Pick-up đang chờ tài xế nhận (StatusId = 1 hoặc 2).
    /// </summary>
    [HttpGet("pickup/pending")]
    [Authorize(Roles = "Collector,ScrapYard")]
    public async Task<IActionResult> GetPendingPickupOrders()
    {
        try
        {
            var userIdClaim = User.FindFirst(ClaimTypes.NameIdentifier)
                ?? throw new Exception("Không xác định được UserId từ Token.");
            int callerUserId = int.Parse(userIdClaim.Value);

            var orders = await _orderService.GetPendingPickupOrdersAsync(callerUserId);
            return Ok(new { Success = true, Data = orders });
        }
        catch (Exception ex)
        {
            return BadRequest(new { Success = false, Message = ex.Message });
        }
    }

    /// <summary>
    /// [Collector, YardOwner] Nhận đơn Pick-up — gán CollectorId, chuyển status → DriverAssigned.
    /// </summary>
    [HttpPost("{orderId}/assign-collector")]
    [Authorize(Roles = "Collector,ScrapYard")]
    public async Task<IActionResult> AssignCollector(int orderId)
    {
        try
        {
            var userIdClaim = User.FindFirst(ClaimTypes.NameIdentifier)
                           ?? User.FindFirst("userId")
                           ?? User.FindFirst("sub");

            if (userIdClaim == null || !int.TryParse(userIdClaim.Value, out var collectorUserId))
            {
                return Unauthorized(new { Success = false, Message = "Không thể xác định người dùng." });
            }

            var result = await _orderService.AssignCollectorAsync(orderId, collectorUserId);
            return Ok(new { Success = result, Message = "Đã nhận đơn thu gom thành công. Trạng thái: Đã có tài xế nhận." });
        }
        catch (Exception ex)
        {
            return BadRequest(new { Success = false, Message = ex.Message });
        }
    }

    /// <summary>
    /// [Collector, YardOwner] Cập nhật trạng thái đơn Pick-up (InProgress=3, Cancelled=5).
    /// Completed (4) được xử lý qua POST /transactions/pickup-initiate.
    /// </summary>
    [HttpPut("{orderId:int}/pickup-status")]
    [Authorize(Roles = "Collector,ScrapYard")]
    public async Task<IActionResult> UpdatePickupStatus(int orderId, [FromBody] int newStatusId)
    {
        try
        {
            var userIdClaim = User.FindFirst(ClaimTypes.NameIdentifier)
                           ?? User.FindFirst("userId")
                           ?? User.FindFirst("sub");

            if (userIdClaim == null || !int.TryParse(userIdClaim.Value, out var collectorUserId))
            {
                return Unauthorized(new { Success = false, Message = "Không thể xác định người dùng." });
            }

            var result = await _orderService.UpdatePickupOrderStatusAsync(orderId, collectorUserId, newStatusId);
            return Ok(new { Success = result, Message = "Đã cập nhật trạng thái đơn hàng." });
        }
        catch (Exception ex)
        {
            return BadRequest(new { Success = false, Message = ex.Message });
        }
    }
}
