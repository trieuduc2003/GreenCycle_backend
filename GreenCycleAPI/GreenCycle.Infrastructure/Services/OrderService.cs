using GreenCycle.Application.DTOs.Order;
using GreenCycle.Application.Interfaces.Services;
using GreenCycle.Domain.Entities;
using GreenCycle.Infrastructure.Data;
using Microsoft.EntityFrameworkCore;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

namespace GreenCycle.Infrastructure.Services
{
    public class OrderService : IOrderService
    {
        private readonly GreenCycleDbContext _context;

        public OrderService(GreenCycleDbContext context)
        {
            _context = context;
        }

        // ─────────────────────────────────────────────
        // 1. LẤY DANH SÁCH DANH MỤC RÁC & GIÁ GỐC
        // ─────────────────────────────────────────────
        public async Task<List<WasteCategoryDto>> GetWasteCategoriesAsync()
        {
            // Seed dữ liệu mẫu nếu chưa có danh mục nào
            await EnsureCategoriesAndPricesSeededAsync();

            var categories = await _context.WasteCategories
                .Include(c => c.PricingMatrices)
                .AsNoTracking()
                .ToListAsync();

            var result = new List<WasteCategoryDto>();

            foreach (var cat in categories)
            {
                // Lấy giá active mới nhất không thuộc vựa cụ thể (YardID == null)
                var priceEntry = cat.PricingMatrices
                    .Where(p => p.YardId == null && (p.IsActive ?? true))
                    .OrderByDescending(p => p.EffectiveDate)
                    .FirstOrDefault();

                decimal unitPrice = priceEntry?.UnitPrice ?? (cat.Name.Contains("điện tử") ? 5000 : 200);

                string iconName = cat.Name.ToLower() switch
                {
                    var n when n.Contains("giấy") || n.Contains("carton") => "description_outlined",
                    var n when n.Contains("điện tử") => "devices_outlined",
                    var n when n.Contains("nhựa") => "local_drink_outlined",
                    var n when n.Contains("kim loại") || n.Contains("lon") => "takeout_dining_outlined",
                    _ => "eco_outlined"
                };

                result.Add(new WasteCategoryDto
                {
                    CategoryId = cat.CategoryId,
                    Name = cat.Name,
                    Unit = cat.Unit,
                    UnitPrice = unitPrice,
                    Co2ReductionFactor = cat.Co2ReductionFactor,
                    IconName = iconName
                });
            }

            return result;
        }

        // ─────────────────────────────────────────────
        // 2. KHỞI TẠO ĐƠN HÀNG BÁN RÁC
        // ─────────────────────────────────────────────
        public async Task<CreateOrderResponseDto> CreateOrderAsync(int sellerId, CreateOrderRequestDto request)
        {
            if (request.Details == null || !request.Details.Any())
            {
                throw new Exception("Vui lòng chọn ít nhất một danh mục rác!");
            }

            // Kiểm tra Người bán
            var seller = await _context.Users.FindAsync(sellerId)
                ?? throw new Exception("Tài khoản người bán không tồn tại!");

            // Seed / Lấy Method & Status
            await EnsureOrderMethodsAndStatusesSeededAsync();

            var method = await _context.OrderMethods.FindAsync(request.MethodId)
                ?? throw new Exception("Phương thức thu gom không hợp lệ!");

            var status = await _context.OrderStatuses.FirstOrDefaultAsync(s => s.StatusName == "Pending")
                ?? await _context.OrderStatuses.FirstAsync();

            // Tính tổng tiền ước tính
            decimal totalEstimated = 0;
            var orderDetailsList = new List<OrderDetail>();

            foreach (var item in request.Details)
            {
                if (item.EstimatedWeight <= 0) continue;

                var cat = await _context.WasteCategories
                    .Include(c => c.PricingMatrices)
                    .FirstOrDefaultAsync(c => c.CategoryId == item.CategoryId)
                    ?? throw new Exception($"Danh mục rác ID {item.CategoryId} không tồn tại!");

                var priceEntry = cat.PricingMatrices
                    .Where(p => p.YardId == null && (p.IsActive ?? true))
                    .OrderByDescending(p => p.EffectiveDate)
                    .FirstOrDefault();

                decimal unitPrice = priceEntry?.UnitPrice ?? (cat.Name.Contains("điện tử") ? 5000 : 200);
                decimal subtotal = item.EstimatedWeight * unitPrice;

                totalEstimated += subtotal;

                orderDetailsList.Add(new OrderDetail
                {
                    CategoryId = cat.CategoryId,
                    EstimatedWeight = item.EstimatedWeight,
                    UnitPrice = unitPrice,
                    EstimatedSubTotal = subtotal
                });
            }

            if (!orderDetailsList.Any())
            {
                throw new Exception("Số lượng rác khai báo phải lớn hơn 0!");
            }

            // Phí nền tảng (0% cho Drop-off, 20% cho Pick-up)
            decimal platformFeeRate = request.MethodId == 2 ? 0.20m : 0.00m;
            decimal platformFee = totalEstimated * platformFeeRate;

            var order = new Order
            {
                SellerId = sellerId,
                MethodId = request.MethodId,
                StatusId = status.StatusId,
                TotalEstimatedAmount = totalEstimated,
                TotalActualAmount = 0,
                PlatformFee = platformFee,
                CreatedAt = DateTime.UtcNow,
                UpdatedAt = DateTime.UtcNow,
                OrderDetails = orderDetailsList
            };

            _context.Orders.Add(order);
            await _context.SaveChangesAsync();

            // Thêm bản ghi chi tiết theo Method
            if (request.MethodId == 1) // Drop-off
            {
                int yardId = request.YardId ?? 1; // Default yard if not specified
                var yard = await _context.ScrapYards.FirstOrDefaultAsync(y => y.YardId == yardId)
                    ?? await _context.ScrapYards.FirstOrDefaultAsync();

                if (yard != null)
                {
                    _context.DropOffOrders.Add(new DropOffOrder
                    {
                        OrderId = order.OrderId,
                        YardId = yard.YardId
                    });
                }
            }
            else if (request.MethodId == 2) // Pick-up
            {
                var address = await _context.UserAddresses.FirstOrDefaultAsync(a => a.UserId == sellerId)
                    ?? await _context.UserAddresses.FirstOrDefaultAsync();

                if (address != null)
                {
                    _context.PickUpOrders.Add(new PickUpOrder
                    {
                        OrderId = order.OrderId,
                        PickupAddressId = address.AddressId
                    });
                }
            }

            await _context.SaveChangesAsync();

            return new CreateOrderResponseDto
            {
                OrderId = order.OrderId,
                MethodId = order.MethodId,
                MethodName = method.MethodName,
                StatusId = order.StatusId,
                StatusName = status.StatusName,
                TotalEstimatedAmount = totalEstimated,
                PlatformFee = platformFee,
                NetEstimatedAmount = totalEstimated - platformFee,
                CreatedAt = order.CreatedAt ?? DateTime.UtcNow
            };
        }

        // ─────────────────────────────────────────────
        // 3. LẤY DANH SÁCH ĐƠN HÀNG CỦA SELLER
        // ─────────────────────────────────────────────
        public async Task<List<OrderSummaryDto>> GetSellerOrdersAsync(int sellerId)
        {
            var orders = await _context.Orders
                .Include(o => o.Method)
                .Include(o => o.Status)
                .Include(o => o.OrderDetails)
                .Where(o => o.SellerId == sellerId)
                .OrderByDescending(o => o.CreatedAt)
                .AsNoTracking()
                .ToListAsync();

            return orders.Select(o => new OrderSummaryDto
            {
                OrderId = o.OrderId,
                MethodName = o.Method.MethodName,
                StatusName = o.Status.StatusName,
                TotalEstimatedAmount = o.TotalEstimatedAmount ?? 0,
                PlatformFee = o.PlatformFee ?? 0,
                CreatedAt = o.CreatedAt ?? DateTime.UtcNow,
                ItemsCount = o.OrderDetails.Count
            }).ToList();
        }

        // ─────────────────────────────────────────────
        // 4. LẤY CHI TIẾT ĐƠN HÀNG (Chủ Vựa dùng để nhập cân)
        // ─────────────────────────────────────────────
        public async Task<List<OrderDetailDto>> GetOrderDetailsAsync(int orderId)
        {
            var details = await _context.OrderDetails
                .Include(d => d.Category)
                .Where(d => d.OrderId == orderId)
                .AsNoTracking()
                .ToListAsync();

            return details.Select(d => new OrderDetailDto
            {
                OrderDetailId = d.OrderDetailId,
                CategoryId = d.CategoryId,
                CategoryName = d.Category?.Name ?? "Không rõ",
                Unit = d.Category?.Unit ?? "kg",
                EstimatedWeight = d.EstimatedWeight,
                ActualWeight = d.ActualWeight,
                UnitPrice = d.UnitPrice
            }).ToList();
        }

        // ─────────────────────────────────────────────
        // HELPER SEED DATA
        // ─────────────────────────────────────────────
        private async Task EnsureCategoriesAndPricesSeededAsync()
        {
            if (!await _context.WasteCategories.AnyAsync())
            {
                var cat1 = new WasteCategory { Name = "Giấy / Carton", Unit = "kg", Co2ReductionFactor = 1.2m };
                var cat2 = new WasteCategory { Name = "Rác điện tử", Unit = "món", Co2ReductionFactor = 2.5m };
                var cat3 = new WasteCategory { Name = "Nhựa các loại", Unit = "kg", Co2ReductionFactor = 1.5m };
                var cat4 = new WasteCategory { Name = "Kim loại / Lon", Unit = "kg", Co2ReductionFactor = 2.0m };

                _context.WasteCategories.AddRange(cat1, cat2, cat3, cat4);
                await _context.SaveChangesAsync();

                _context.PricingMatrices.AddRange(
                    new PricingMatrix { CategoryId = cat1.CategoryId, UnitPrice = 200, IsActive = true, EffectiveDate = DateTime.UtcNow },
                    new PricingMatrix { CategoryId = cat2.CategoryId, UnitPrice = 5000, IsActive = true, EffectiveDate = DateTime.UtcNow },
                    new PricingMatrix { CategoryId = cat3.CategoryId, UnitPrice = 300, IsActive = true, EffectiveDate = DateTime.UtcNow },
                    new PricingMatrix { CategoryId = cat4.CategoryId, UnitPrice = 500, IsActive = true, EffectiveDate = DateTime.UtcNow }
                );
                await _context.SaveChangesAsync();
            }
        }

        private async Task EnsureOrderMethodsAndStatusesSeededAsync()
        {
            if (!await _context.OrderMethods.AnyAsync())
            {
                _context.OrderMethods.AddRange(
                    new OrderMethod { MethodName = "Drop-off" },
                    new OrderMethod { MethodName = "Pick-up" }
                );
                await _context.SaveChangesAsync();
            }

            if (!await _context.OrderStatuses.AnyAsync())
            {
                _context.OrderStatuses.AddRange(
                    new OrderStatus { StatusName = "Pending" },
                    new OrderStatus { StatusName = "DriverAssigned" },
                    new OrderStatus { StatusName = "InProgress" },
                    new OrderStatus { StatusName = "Completed" },
                    new OrderStatus { StatusName = "Cancelled" }
                );
                await _context.SaveChangesAsync();
            }
        }
    }
}
