using GreenCycle.Application.DTOs.Order;
using System.Collections.Generic;
using System.Threading.Tasks;

namespace GreenCycle.Application.Interfaces.Services
{
    public interface IOrderService
    {
        /// <summary>Lấy danh sách các danh mục rác kèm giá GreenPoints hiện tại.</summary>
        Task<List<WasteCategoryDto>> GetWasteCategoriesAsync();

        /// <summary>Khởi tạo đơn khai báo rác mới (Drop-off hoặc Pick-up).</summary>
        Task<CreateOrderResponseDto> CreateOrderAsync(int sellerId, CreateOrderRequestDto request);

        /// <summary>Lấy danh sách các đơn hàng của Người bán.</summary>
        Task<List<OrderHistoryDto>> GetSellerOrderHistoryAsync(int sellerId);

        /// <summary>Lấy danh sách các đơn hàng thu mua của Vựa.</summary>
        Task<List<OrderHistoryDto>> GetYardOrderHistoryAsync(int yardUserId);

        /// <summary>Lấy chi tiết OrderDetails của một đơn hàng (Chủ Vựa dùng để nhập số liệu thực tế).</summary>
        Task<List<OrderDetailDto>> GetOrderDetailsAsync(int orderId);

        /// <summary>Lấy thông tin chi tiết toàn diện của một đơn hàng (cho Người bán / Quản trị).</summary>
        Task<OrderDetailViewDto?> GetOrderDetailViewAsync(int orderId, int sellerId);

        /// <summary>Chọn vựa rác (Yard) cho đơn Drop-off (khi người bán bắt đầu đi).</summary>
        Task<bool> SelectYardForOrderAsync(int orderId, int sellerId, int yardId);

        // ─── Pick-up specific methods ───────────────────────────────────────

        /// <summary>Lấy danh sách đơn Pick-up đang chờ tài xế nhận (StatusId=1, MethodId=2).</summary>
        Task<List<OrderHistoryDto>> GetPendingPickupOrdersAsync(int callerUserId);

        /// <summary>Tài xế nhận đơn Pick-up — gán CollectorId, chuyển StatusId → 2 (DriverAssigned).</summary>
        Task<bool> AssignCollectorAsync(int orderId, int collectorUserId);

        /// <summary>Cập nhật trạng thái đơn Pick-up (InProgress=3, Cancelled=5). Completed xử lý qua TransactionService.</summary>
        Task<bool> UpdatePickupOrderStatusAsync(int orderId, int collectorUserId, int newStatusId);
    }
}
