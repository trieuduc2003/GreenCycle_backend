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
        Task<List<OrderSummaryDto>> GetSellerOrdersAsync(int sellerId);

        /// <summary>Lấy chi tiết OrderDetails của một đơn hàng (Chủ Vựa dùng để nhập số liệu thực tế).</summary>
        Task<List<OrderDetailDto>> GetOrderDetailsAsync(int orderId);
    }
}
