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
        private readonly ITransactionNotifier _notifier;

        public OrderService(GreenCycleDbContext context, ITransactionNotifier notifier)
        {
            _context = context;
            _notifier = notifier;
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

            // Tính tổng tiền và tổng khối lượng ước tính
            decimal totalEstimated = 0;
            decimal totalEstimatedWeight = 0;
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
                totalEstimatedWeight += (decimal)item.EstimatedWeight;

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

            // Giai đoạn 4: Pick-up order requires minimum 5kg
            if (request.MethodId == 2 && totalEstimatedWeight < 5m)
            {
                throw new Exception("Đơn thu gom tận nơi (Pick-up) yêu cầu tổng khối lượng rác tối thiểu là 5kg!");
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
                int? addressId = request.PickupAddressId;
                if (addressId == null || addressId <= 0) 
                {
                    var address = await _context.UserAddresses.FirstOrDefaultAsync(a => a.UserId == sellerId)
                        ?? await _context.UserAddresses.FirstOrDefaultAsync();
                    addressId = address?.AddressId;
                }
                
                if (addressId != null)
                {
                    _context.PickUpOrders.Add(new PickUpOrder
                    {
                        OrderId = order.OrderId,
                        PickupAddressId = addressId.Value
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
        // 3. LẤY LỊCH SỬ ĐƠN HÀNG CỦA SELLER
        // ─────────────────────────────────────────────
        public async Task<List<OrderHistoryDto>> GetSellerOrderHistoryAsync(int sellerId)
        {
            var orders = await _context.Orders
                .Include(o => o.Method)
                .Include(o => o.Status)
                .Include(o => o.OrderDetails)
                .Where(o => o.SellerId == sellerId)
                .OrderByDescending(o => o.CreatedAt)
                .AsNoTracking()
                .ToListAsync();

            return orders.Select(o => new OrderHistoryDto
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
        public async Task<List<OrderHistoryDto>> GetYardOrderHistoryAsync(int yardUserId)
        {
            var yard = await _context.ScrapYards.FirstOrDefaultAsync(y => y.UserId == yardUserId);
            if (yard == null) return new List<OrderHistoryDto>();

            var orders = await _context.DropOffOrders
                .Include(d => d.Order)
                    .ThenInclude(o => o.Method)
                .Include(d => d.Order)
                    .ThenInclude(o => o.Status)
                .Include(d => d.Order)
                    .ThenInclude(o => o.OrderDetails)
                .Where(d => d.YardId == yard.YardId)
                .Select(d => d.Order)
                .OrderByDescending(o => o.CreatedAt)
                .AsNoTracking()
                .ToListAsync();

            return orders.Select(o => new OrderHistoryDto
            {
                OrderId = o.OrderId,
                MethodName = o.Method?.MethodName ?? "",
                StatusName = o.Status?.StatusName ?? "",
                TotalEstimatedAmount = o.TotalEstimatedAmount ?? 0,
                PlatformFee = o.PlatformFee ?? 0,
                CreatedAt = o.CreatedAt ?? DateTime.UtcNow,
                ItemsCount = o.OrderDetails?.Count ?? 0
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
        // 5. LẤY CHI TIẾT TOÀN DIỆN ĐƠN HÀNG (Seller xem chi tiết)
        // ─────────────────────────────────────────────
        public async Task<OrderDetailViewDto?> GetOrderDetailViewAsync(int orderId, int sellerId)
        {
            var order = await _context.Orders
                .Include(o => o.Method)
                .Include(o => o.Status)
                .Include(o => o.Seller)
                .Include(o => o.DropOffOrder)
                    .ThenInclude(d => d!.Yard)
                .Include(o => o.PickUpOrder)
                    .ThenInclude(p => p!.PickupAddress)
                .Include(o => o.PickUpOrder)
                    .ThenInclude(p => p!.Collector)
                        .ThenInclude(c => c!.User)
                .Include(o => o.OrderDetails)
                    .ThenInclude(d => d.Category)
                .AsNoTracking()
                .FirstOrDefaultAsync(o => o.OrderId == orderId && o.SellerId == sellerId);

            if (order == null) return null;

            var items = order.OrderDetails.Select(d => new OrderItemDetailDto
            {
                OrderDetailId = d.OrderDetailId,
                CategoryId = d.CategoryId,
                CategoryName = d.Category?.Name ?? "Không rõ",
                Unit = d.Category?.Unit ?? "kg",
                EstimatedWeight = d.EstimatedWeight,
                ActualWeight = d.ActualWeight,
                UnitPrice = d.UnitPrice,
                EstimatedSubTotal = d.EstimatedSubTotal,
                ActualSubTotal = d.ActualSubTotal,
                Co2ReductionFactor = d.Category?.Co2ReductionFactor ?? 0m
            }).ToList();

            // Tính ước tính kg CO2 giảm phát thải
            double totalCo2 = 0;
            foreach (var item in items)
            {
                var weight = item.ActualWeight ?? item.EstimatedWeight;
                totalCo2 += (double)(weight * item.Co2ReductionFactor);
            }

            var totalEst = order.TotalEstimatedAmount ?? 0m;
            var fee = order.PlatformFee ?? 0m;
            var totalAct = order.TotalActualAmount;

            return new OrderDetailViewDto
            {
                OrderId = order.OrderId,
                SellerId = order.SellerId,
                SellerName = order.Seller?.FullName ?? "",
                SellerPhone = order.Seller?.Phone ?? "",
                MethodId = order.MethodId,
                MethodName = order.Method?.MethodName ?? "",
                StatusId = order.StatusId,
                StatusName = order.Status?.StatusName ?? "",
                TotalEstimatedAmount = totalEst,
                TotalActualAmount = totalAct,
                PlatformFee = fee,
                NetEstimatedAmount = Math.Max(0, totalEst - fee),
                NetActualAmount = totalAct.HasValue ? Math.Max(0, totalAct.Value - fee) : null,
                CreatedAt = order.CreatedAt ?? DateTime.UtcNow,
                UpdatedAt = order.UpdatedAt,
                ScrapYardName = order.DropOffOrder?.Yard?.ScrapYardName,
                ScrapYardAddress = order.DropOffOrder?.Yard?.Address,
                PickupAddress = order.PickUpOrder?.PickupAddress?.FullAddress,
                PickupLatitude = order.PickUpOrder?.PickupAddress?.Location?.Coordinate.Y,
                PickupLongitude = order.PickUpOrder?.PickupAddress?.Location?.Coordinate.X,
                CollectorName = order.PickUpOrder?.Collector?.User?.FullName,
                CollectorPhone = order.PickUpOrder?.Collector?.User?.Phone,
                CollectorVehicleType = order.PickUpOrder?.Collector?.VehicleType,
                CollectorLicensePlate = order.PickUpOrder?.Collector?.LicensePlate,
                EstimatedCo2ReducedKg = Math.Round(totalCo2, 2),
                Items = items
            };
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

        // ─────────────────────────────────────────────
        // 6. CHỌN VỰA RÁC CHO ĐƠN HÀNG (Drop-off)
        // ─────────────────────────────────────────────
        public async Task<bool> SelectYardForOrderAsync(int orderId, int sellerId, int yardId)
        {
            var order = await _context.Orders.FirstOrDefaultAsync(o => o.OrderId == orderId && o.SellerId == sellerId);
            if (order == null)
            {
                throw new Exception("Không tìm thấy đơn hàng hoặc bạn không có quyền thao tác.");
            }

            // Only allow if Method is Drop-off (usually 1) and status is Pending (1)
            // But we'll just check if it's not completed or cancelled.
            if (order.StatusId >= 4)
            {
                throw new Exception("Đơn hàng đã hoàn tất hoặc bị hủy, không thể chọn vựa.");
            }

            // Validate YardId exists
            var yardExists = await _context.ScrapYards.AnyAsync(y => y.YardId == yardId);
            if (!yardExists)
            {
                throw new Exception("Vựa rác không tồn tại.");
            }

            // In our schema, ScrapYard is linked to DropOffOrder.
            var dropOff = await _context.DropOffOrders.FirstOrDefaultAsync(d => d.OrderId == orderId);
            if (dropOff != null)
            {
                dropOff.YardId = yardId;
            }
            else
            {
                // If the user selected Drop-off but we didn't insert a record yet, create it.
                _context.DropOffOrders.Add(new DropOffOrder
                {
                    OrderId = orderId,
                    YardId = yardId
                });
            }

            await _context.SaveChangesAsync();
            return true;
        }

        // ─────────────────────────────────────────────
        // 7. LẤY DANH SÁCH ĐƠN PICK-UP ĐANG CHỜ TÀI XẾ NHẬN
        // ─────────────────────────────────────────────
        public async Task<List<OrderHistoryDto>> GetPendingPickupOrdersAsync(int callerUserId)
        {
            var orders = await _context.Orders
                .Include(o => o.Method)
                .Include(o => o.Status)
                .Include(o => o.OrderDetails)
                .Include(o => o.Seller)
                .Include(o => o.PickUpOrder)
                    .ThenInclude(p => p!.PickupAddress)
                .Where(o => o.MethodId == 2 && (o.StatusId == 1 || o.StatusId == 2))
                .OrderByDescending(o => o.CreatedAt)
                .AsNoTracking()
                .ToListAsync();

            // Tìm vị trí của Caller
            NetTopologySuite.Geometries.Geometry? callerLocation = null;
            var yard = await _context.ScrapYards.FirstOrDefaultAsync(y => y.UserId == callerUserId);
            if (yard != null)
            {
                callerLocation = yard.Location;
            }
            else
            {
                var collectorLocation = await _context.CollectorLiveLocations.FirstOrDefaultAsync(c => c.CollectorId == callerUserId);
                if (collectorLocation != null)
                {
                    callerLocation = collectorLocation.Location;
                }
            }

            var result = new List<OrderHistoryDto>();
            foreach (var o in orders)
            {
                double? distanceKm = null;
                if (callerLocation != null && !callerLocation.IsEmpty && 
                    o.PickUpOrder?.PickupAddress?.Location != null && !o.PickUpOrder.PickupAddress.Location.IsEmpty)
                {
                    // Haversine formula to calculate true distance in kilometers
                    var lat1 = callerLocation.Coordinate.Y * Math.PI / 180.0;
                    var lon1 = callerLocation.Coordinate.X * Math.PI / 180.0;
                    var lat2 = o.PickUpOrder.PickupAddress.Location.Coordinate.Y * Math.PI / 180.0;
                    var lon2 = o.PickUpOrder.PickupAddress.Location.Coordinate.X * Math.PI / 180.0;

                    var dLat = lat2 - lat1;
                    var dLon = lon2 - lon1;

                    var a = Math.Sin(dLat / 2) * Math.Sin(dLat / 2) +
                            Math.Cos(lat1) * Math.Cos(lat2) *
                            Math.Sin(dLon / 2) * Math.Sin(dLon / 2);
                    var c = 2 * Math.Atan2(Math.Sqrt(a), Math.Sqrt(1 - a));
                    distanceKm = 6371.0 * c; // Earth radius in km
                    
                    // Lọc đơn hàng > 10km (chỉ lấy <= 10km)
                    if (distanceKm > 10.0) continue;
                }

                result.Add(new OrderHistoryDto
                {
                    OrderId = o.OrderId,
                    MethodName = o.Method?.MethodName ?? "Pick-up",
                    StatusName = o.Status?.StatusName ?? "",
                    TotalEstimatedAmount = o.TotalEstimatedAmount ?? 0,
                    PlatformFee = o.PlatformFee ?? 0,
                    CreatedAt = o.CreatedAt ?? DateTime.UtcNow,
                    ItemsCount = o.OrderDetails?.Count ?? 0,
                    TotalEstimatedWeight = (double)(o.OrderDetails?.Sum(od => od.EstimatedWeight) ?? 0m),
                    SellerName = o.Seller?.FullName,
                    PickupAddress = o.PickUpOrder?.PickupAddress?.FullAddress,
                    DistanceKm = distanceKm,
                    Latitude = o.PickUpOrder?.PickupAddress?.Location != null && !o.PickUpOrder.PickupAddress.Location.IsEmpty ? o.PickUpOrder.PickupAddress.Location.Coordinate.Y : null,
                    Longitude = o.PickUpOrder?.PickupAddress?.Location != null && !o.PickUpOrder.PickupAddress.Location.IsEmpty ? o.PickUpOrder.PickupAddress.Location.Coordinate.X : null
                });
            }

            return result;
        }

        // ─────────────────────────────────────────────
        // 8. GÁN TÀI XẾ CHO ĐƠN PICK-UP
        // ─────────────────────────────────────────────
        public async Task<bool> AssignCollectorAsync(int orderId, int collectorUserId)
        {
            // Tìm Collector từ UserId, nếu không có thì kiểm tra xem có phải Vựa không
            var collector = await _context.Collectors
                .FirstOrDefaultAsync(c => c.UserId == collectorUserId);

            if (collector == null)
            {
                var isYard = await _context.ScrapYards.AnyAsync(y => y.UserId == collectorUserId);
                if (isYard)
                {
                    // Tự động tạo record Collector cho chủ vựa để họ có thể nhận đơn
                    collector = new Collector
                    {
                        UserId = collectorUserId,
                        VehicleType = "Xe của Vựa",
                        CurrentStatus = "Active",
                        JoinedDate = DateTime.UtcNow
                    };
                    _context.Collectors.Add(collector);
                    await _context.SaveChangesAsync();
                }
                else
                {
                    throw new Exception("Tài khoản không phải Tài xế hoặc Vựa, không thể nhận đơn.");
                }
            }

            var order = await _context.Orders
                .Include(o => o.PickUpOrder)
                .FirstOrDefaultAsync(o => o.OrderId == orderId)
                ?? throw new Exception("Đơn hàng không tồn tại.");

            if (order.MethodId != 2)
                throw new Exception("Đơn hàng này không phải loại Pick-up.");

            if (order.StatusId != 1)
                throw new Exception("Đơn hàng đã được nhận bởi tài xế khác hoặc không còn khả dụng.");

            var pickupOrder = order.PickUpOrder
                ?? throw new Exception("Thông tin Pick-up không tồn tại.");

            if (pickupOrder.CollectorId != null)
                throw new Exception("Đơn hàng đã được tài xế khác nhận.");

            // Gán Collector và chuyển trạng thái DriverAssigned
            pickupOrder.CollectorId = collector.CollectorId;
            pickupOrder.ScheduledTime = DateTime.UtcNow;
            order.StatusId = 2; // DriverAssigned
            order.UpdatedAt = DateTime.UtcNow;

            await _context.SaveChangesAsync();
            return true;
        }

        // ─────────────────────────────────────────────
        // 9. CẬP NHẬT TRẠNG THÁI ĐƠN PICK-UP
        // ─────────────────────────────────────────────
        public async Task<bool> UpdatePickupOrderStatusAsync(int orderId, int collectorUserId, int newStatusId)
        {
            var collector = await _context.Collectors
                .FirstOrDefaultAsync(c => c.UserId == collectorUserId)
                ?? throw new Exception("Tài khoản không phải Tài xế hoặc chưa đăng ký.");

            var order = await _context.Orders
                .Include(o => o.PickUpOrder)
                .FirstOrDefaultAsync(o => o.OrderId == orderId)
                ?? throw new Exception("Đơn hàng không tồn tại.");

            if (order.MethodId != 2)
                throw new Exception("Đơn hàng này không phải loại Pick-up.");

            if (order.PickUpOrder?.CollectorId != collector.CollectorId)
                throw new Exception("Tài xế không có quyền cập nhật đơn hàng này.");

            // Chỉ cho phép chuyển sang InProgress (3) hoặc Cancelled (5)
            // Completed (4) được xử lý riêng qua TransactionService.ConfirmTransactionAsync
            var allowedStatuses = new[] { 3, 5 };
            if (!allowedStatuses.Contains(newStatusId))
                throw new Exception("Trạng thái cập nhật không hợp lệ. Chỉ được phép: InProgress (3) hoặc Cancelled (5).");

            if (order.StatusId >= 4)
                throw new Exception("Đơn hàng đã hoàn tất hoặc bị hủy, không thể cập nhật.");

            order.StatusId = newStatusId;
            order.UpdatedAt = DateTime.UtcNow;

            await _context.SaveChangesAsync();
            return true;
        }

        // ─────────────────────────────────────────────
        // 10. CẬP NHẬT VỊ TRÍ COLLECTOR
        // ─────────────────────────────────────────────
        public async Task<bool> UpdateCollectorLiveLocationAsync(int orderId, int collectorUserId, double latitude, double longitude)
        {
            var collector = await _context.Collectors
                .FirstOrDefaultAsync(c => c.UserId == collectorUserId)
                ?? throw new Exception("Tài khoản không hợp lệ.");

            var order = await _context.Orders
                .Include(o => o.PickUpOrder)
                .Include(o => o.Seller)
                .FirstOrDefaultAsync(o => o.OrderId == orderId)
                ?? throw new Exception("Đơn hàng không tồn tại.");

            if (order.PickUpOrder?.CollectorId != collector.CollectorId)
                throw new Exception("Tài xế không có quyền cập nhật đơn hàng này.");

            if (order.StatusId >= 4)
                throw new Exception("Đơn hàng đã hoàn tất hoặc bị hủy.");

            // Update in DB (CollectorLiveLocation)
            var liveLocation = await _context.CollectorLiveLocations
                .FirstOrDefaultAsync(l => l.CollectorId == collector.CollectorId);

            var point = new NetTopologySuite.Geometries.Point(longitude, latitude) { SRID = 4326 };

            if (liveLocation == null)
            {
                liveLocation = new CollectorLiveLocation
                {
                    CollectorId = collector.CollectorId,
                    Location = point,
                    LastUpdated = DateTime.UtcNow
                };
                _context.CollectorLiveLocations.Add(liveLocation);
            }
            else
            {
                liveLocation.Location = point;
                liveLocation.LastUpdated = DateTime.UtcNow;
            }

            await _context.SaveChangesAsync();

            // Broadcast via SignalR to Seller
            if (order.Seller?.UserId != null)
            {
                // Note: The SignalR service expects string for user id
                await _notifier.NotifyCollectorLocationAsync(order.Seller.UserId.ToString(), orderId, latitude, longitude);
            }

            return true;
        }
    }
}
