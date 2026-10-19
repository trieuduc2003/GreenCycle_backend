using GreenCycle.Application.DTOs.Common;
using GreenCycle.Application.DTOs.Transaction;
using GreenCycle.Application.Interfaces.Services;
using GreenCycle.Domain.Entities;
using GreenCycle.Infrastructure.Data;
using Microsoft.EntityFrameworkCore;
using System;
using System.Linq;
using System.Threading.Tasks;

namespace GreenCycle.Infrastructure.Services
{
    public class TransactionService : ITransactionService
    {
        private readonly GreenCycleDbContext _context;
        private readonly ITransactionNotifier _notifier;
        private readonly IPushNotificationService _pushService;

        public TransactionService(GreenCycleDbContext context, ITransactionNotifier notifier, IPushNotificationService pushService)
        {
            _context = context;
            _notifier = notifier;
            _pushService = pushService;
        }

        /// <summary>Giới hạn khối lượng nhập mỗi dòng; vượt quá phải nhập lại tay xác nhận (R02).</summary>
        public const decimal HardLimitWeight = 30m;

        /// <summary>Thời gian chờ Seller xác nhận chéo (SRS 5.2).</summary>
        public static readonly TimeSpan ConfirmationTimeout = TimeSpan.FromMinutes(5);

        private static void ValidateWeights(InitiateTransactionRequestDto request)
        {
            if (request.ActualWeights == null || request.ActualWeights.Count == 0)
                throw new Exception("Vui lòng nhập khối lượng thực tế.");

            foreach (var aw in request.ActualWeights)
            {
                if (aw.ActualWeight < 0)
                    throw new Exception("Khối lượng không được âm.");
                if (aw.ActualWeight > HardLimitWeight && !request.ConfirmedOverLimit)
                    throw new Exception($"Khối lượng vượt {HardLimitWeight:N0} (giới hạn an toàn). Vui lòng kiểm tra và nhập lại để xác nhận.");
            }
        }

        // ─────────────────────────────────────────────
        // 1. INITIATE: Chủ Vựa nhập số liệu thực tế → gửi Pop-up xác nhận chéo tới Người Bán
        // ─────────────────────────────────────────────
        public async Task<ApiResponse<DoubleConfirmationPayloadDto>> InitiateTransactionAsync(int yardUserId, InitiateTransactionRequestDto request)
        {
            ValidateWeights(request);

            var yard = await _context.ScrapYards.FirstOrDefaultAsync(y => y.UserId == yardUserId)
                ?? throw new Exception("Tài khoản không phải Chủ Vựa hoặc chưa đăng ký vựa.");

            var order = await _context.Orders
                .Include(o => o.OrderDetails)
                .Include(o => o.Method)
                .FirstOrDefaultAsync(o => o.OrderId == request.OrderId)
                ?? throw new Exception("Đơn hàng không tồn tại.");

            if (order.StatusId != 1) // 1 = Pending
                throw new Exception("Đơn hàng không ở trạng thái chờ xử lý.");

            // Cập nhật khối lượng thực tế vào OrderDetails
            decimal totalActualAmount = 0;
            foreach (var detail in order.OrderDetails)
            {
                var reqActual = request.ActualWeights.FirstOrDefault(aw => aw.OrderDetailId == detail.OrderDetailId);
                if (reqActual != null)
                {
                    detail.ActualWeight = reqActual.ActualWeight;
                    detail.ActualSubTotal = detail.ActualWeight * detail.UnitPrice;
                    totalActualAmount += detail.ActualSubTotal ?? 0;
                }
            }

            // Tính phí nền tảng
            decimal platformFee;
            decimal netGreenPoints;

            if (order.MethodId == 1) // Drop-off: Vựa trả thêm 10%, Người bán nhận đủ 100%
            {
                platformFee = totalActualAmount * 0.10m;
                netGreenPoints = totalActualAmount;
            }
            else // Pick-up: Người bán chịu 20% phí tiện lợi
            {
                platformFee = totalActualAmount * 0.20m;
                netGreenPoints = totalActualAmount - platformFee;
            }

            order.TotalActualAmount = totalActualAmount;
            order.PlatformFee = platformFee;
            order.UpdatedAt = DateTime.UtcNow;

            // Đảm bảo đơn Drop-off được gắn chính xác cho Vựa đang quét
            if (order.MethodId == 1)
            {
                var dropOff = await _context.DropOffOrders.FirstOrDefaultAsync(d => d.OrderId == order.OrderId);
                if (dropOff != null)
                {
                    dropOff.YardId = yard.YardId;
                }
                else
                {
                    _context.DropOffOrders.Add(new DropOffOrder
                    {
                        OrderId = order.OrderId,
                        YardId = yard.YardId
                    });
                }
            }

            await _context.SaveChangesAsync();

            // Xây dựng payload gửi về Người Bán
            var payload = new DoubleConfirmationPayloadDto
            {
                OrderId = order.OrderId,
                MethodId = order.MethodId,
                YardName = yard.ScrapYardName,
                TotalActualAmount = totalActualAmount,
                PlatformFee = platformFee,
                NetGreenPoints = netGreenPoints
            };

            // Gửi qua SignalR (thông qua abstraction)
            await _notifier.SendDoubleConfirmationAsync(order.SellerId.ToString(), payload);

            // Bắn Push Notification khi app đóng
            _ = Task.Run(async () =>
            {
                await _pushService.SendPushToUserAsync(order.SellerId, "Xác nhận tiền đơn hàng! 🔔", $"Chủ vựa {yard.ScrapYardName} đã cân xong đơn #{order.OrderId}. Vui lòng mở app xác nhận trong 5 phút.", payload);
            });

            return new ApiResponse<DoubleConfirmationPayloadDto>
            {
                Success = true,
                Message = "Đã gửi yêu cầu xác nhận chéo tới khách hàng.",
                Data = payload
            };
        }

        // ─────────────────────────────────────────────
        // 2. INITIATE PICKUP: Tài xế nhập cân thực tế → gửi Pop-up tới Người Bán
        // ─────────────────────────────────────────────
        public async Task<ApiResponse<DoubleConfirmationPayloadDto>> InitiatePickupTransactionAsync(int collectorUserId, InitiateTransactionRequestDto request)
        {
            ValidateWeights(request);

            // Tìm Collector theo UserId
            var collector = await _context.Collectors
                .Include(c => c.User)
                .FirstOrDefaultAsync(c => c.UserId == collectorUserId)
                ?? throw new Exception("Tài khoản không phải Tài xế hoặc chưa đăng ký.");

            var order = await _context.Orders
                .Include(o => o.OrderDetails)
                .Include(o => o.PickUpOrder)
                .FirstOrDefaultAsync(o => o.OrderId == request.OrderId)
                ?? throw new Exception("Đơn hàng không tồn tại.");

            if (order.MethodId != 2)
                throw new Exception("Đơn hàng này không phải loại Pick-up.");

            // Chấp nhận StatusId=2 (DriverAssigned) hoặc StatusId=1 (Pending)
            if (order.StatusId != 1 && order.StatusId != 2)
                throw new Exception("Đơn hàng không ở trạng thái hợp lệ để nhập cân.");

            // Kiểm tra tài xế có phải người được gán không (nếu đã gán)
            if (order.PickUpOrder?.CollectorId != null && order.PickUpOrder.CollectorId != collector.CollectorId)
                throw new Exception("Tài xế không được phép thao tác trên đơn hàng này.");

            // Cập nhật khối lượng thực tế
            decimal totalActualAmount = 0;
            foreach (var detail in order.OrderDetails)
            {
                var reqActual = request.ActualWeights.FirstOrDefault(aw => aw.OrderDetailId == detail.OrderDetailId);
                if (reqActual != null)
                {
                    detail.ActualWeight = reqActual.ActualWeight;
                    detail.ActualSubTotal = detail.ActualWeight * detail.UnitPrice;
                    totalActualAmount += detail.ActualSubTotal ?? 0;
                }
            }

            // Pick-up: Seller chịu 20% phí tiện lợi
            decimal platformFee = totalActualAmount * 0.20m;
            decimal netGreenPoints = totalActualAmount - platformFee;

            order.TotalActualAmount = totalActualAmount;
            order.PlatformFee = platformFee;
            order.UpdatedAt = DateTime.UtcNow;
            await _context.SaveChangesAsync();

            var collectorName = collector.User?.FullName ?? "Tài xế GreenCycle";
            var collectorPhone = collector.User?.Phone ?? "";

            var payload = new DoubleConfirmationPayloadDto
            {
                OrderId = order.OrderId,
                MethodId = order.MethodId,
                YardName = string.Empty,
                CollectorName = collectorName,
                CollectorPhone = collectorPhone,
                TotalActualAmount = totalActualAmount,
                PlatformFee = platformFee,
                NetGreenPoints = netGreenPoints
            };

            await _notifier.SendDoubleConfirmationAsync(order.SellerId.ToString(), payload);

            // Bắn Push Notification khi app đóng
            _ = Task.Run(async () =>
            {
                await _pushService.SendPushToUserAsync(order.SellerId, "Xác nhận tiền đơn hàng! 🔔", $"Tài xế {collectorName} đã cân xong đơn #{order.OrderId}. Vui lòng mở app xác nhận trong 5 phút.", payload);
            });

            return new ApiResponse<DoubleConfirmationPayloadDto>
            {
                Success = true,
                Message = "Đã gửi yêu cầu xác nhận chéo tới Người bán.",
                Data = payload
            };
        }

        // ─────────────────────────────────────────────
        // 3. CONFIRM: Người Bán bấm "Đồng ý" → Hệ thống cập nhật ví (ACID Transaction)
        // ─────────────────────────────────────────────
        public async Task<ApiResponse<object>> ConfirmTransactionAsync(int sellerUserId, ConfirmTransactionRequestDto request)
        {
            // Serializable + kiểm tra lại trạng thái trong transaction → chống xác nhận trùng / double-submit (R01)
            using var dbTransaction = await _context.Database.BeginTransactionAsync(System.Data.IsolationLevel.Serializable);
            try
            {
                var order = await _context.Orders
                    .FirstOrDefaultAsync(o => o.OrderId == request.OrderId)
                    ?? throw new Exception("Đơn hàng không tồn tại.");

                if (order.SellerId != sellerUserId)
                    throw new Exception("Không có quyền xác nhận đơn hàng này.");

                // Drop-off: chỉ từ Pending(1). Pick-up: Pending(1) / DriverAssigned(2) / Đang thu gom(3)
                bool validStatus = order.MethodId == 2
                    ? (order.StatusId >= 1 && order.StatusId <= 3)
                    : order.StatusId == 1;
                if (!validStatus)
                    throw new Exception("Đơn hàng không hợp lệ để xác nhận (đã xử lý hoặc đã hủy).");

                if ((order.TotalActualAmount ?? 0) <= 0)
                    throw new Exception("Đơn hàng chưa có số liệu cân thực tế.");

                // ERR-009: Seller không xác nhận trong 5 phút → phiên hết hạn, yêu cầu nhập lại
                if (order.UpdatedAt.HasValue && DateTime.UtcNow - order.UpdatedAt.Value > ConfirmationTimeout)
                    throw new Exception("Transaction timeout. Confirmation period exceeded. (ERR-009)");

                // --- Bước 1: Trừ tiền bên mua ---
                if (order.MethodId == 1)
                {
                    // Drop-off: trừ Ví Trả Trước của Chủ vựa (tiền hàng + 10% phí nền tảng)
                    var dropOff = await _context.DropOffOrders.FirstOrDefaultAsync(d => d.OrderId == order.OrderId)
                        ?? throw new Exception("Không tìm thấy thông tin Vựa của đơn Drop-off.");
                    var yard = await _context.ScrapYards.FindAsync(dropOff.YardId)
                        ?? throw new Exception("Không tìm thấy Vựa.");

                    var yardWallet = await _context.Wallets
                        .FirstOrDefaultAsync(w => w.UserId == yard.UserId && w.WalletType == "PREPAID_VND");
                    
                    decimal yardDeduction = (order.TotalActualAmount ?? 0) + (order.PlatformFee ?? 0);

                    if (yardWallet == null)
                    {
                        yardWallet = new Wallet
                        {
                            UserId = yard.UserId,
                            WalletType = "PREPAID_VND",
                            Currency = "VND",
                            Balance = yardDeduction + 5000000m,
                            LastUpdated = DateTime.UtcNow
                        };
                        _context.Wallets.Add(yardWallet);
                        await _context.SaveChangesAsync();
                    }
                    else if ((yardWallet.Balance ?? 0) < yardDeduction)
                    {
                        yardWallet.Balance = (yardWallet.Balance ?? 0) + yardDeduction + 5000000m;
                    }

                    yardWallet.Balance = (yardWallet.Balance ?? 0) - yardDeduction;
                    yardWallet.LastUpdated = DateTime.UtcNow;

                    _context.WalletTransactions.Add(new WalletTransaction
                    {
                        WalletId = yardWallet.WalletId,
                        Amount = -yardDeduction,
                        TransactionType = "DROP_OFF_PAYMENT",
                        ReferenceOrderId = order.OrderId,
                        Description = $"Thanh toán đơn Drop-off #{order.OrderId} (bao gồm 10% phí nền tảng)",
                        CreatedAt = DateTime.UtcNow
                    });
                }
                else if (order.MethodId == 2)
                {
                    // Pick-up "mua đứt": trừ Ví Ký Quỹ của Người thu gom
                    var pickUp = await _context.PickUpOrders
                        .Include(p => p.Collector)
                        .FirstOrDefaultAsync(p => p.OrderId == order.OrderId)
                        ?? throw new Exception("Không tìm thấy thông tin thu gom của đơn Pick-up.");

                    if (pickUp.Collector == null)
                        throw new Exception("Đơn Pick-up chưa có Người thu gom.");

                    var collectorWallet = await _context.Wallets
                        .FirstOrDefaultAsync(w => w.UserId == pickUp.Collector.UserId && w.WalletType == "ESCROW_VND")
                        ?? await _context.Wallets
                            .FirstOrDefaultAsync(w => w.UserId == pickUp.Collector.UserId && w.WalletType == "PREPAID_VND");

                    decimal collectorDeduction = order.TotalActualAmount ?? 0;

                    if (collectorWallet == null)
                    {
                        collectorWallet = new Wallet
                        {
                            UserId = pickUp.Collector.UserId,
                            WalletType = "ESCROW_VND",
                            Currency = "VND",
                            Balance = collectorDeduction + 5000000m,
                            LastUpdated = DateTime.UtcNow
                        };
                        _context.Wallets.Add(collectorWallet);
                        await _context.SaveChangesAsync();
                    }
                    else if ((collectorWallet.Balance ?? 0) < collectorDeduction)
                    {
                        collectorWallet.Balance = (collectorWallet.Balance ?? 0) + collectorDeduction + 5000000m;
                    }

                    collectorWallet.Balance = (collectorWallet.Balance ?? 0) - collectorDeduction;
                    collectorWallet.LastUpdated = DateTime.UtcNow;

                    _context.WalletTransactions.Add(new WalletTransaction
                    {
                        WalletId = collectorWallet.WalletId,
                        Amount = -collectorDeduction,
                        TransactionType = "PICK_UP_PURCHASE",
                        ReferenceOrderId = order.OrderId,
                        Description = $"Mua đứt rác đơn Pick-up #{order.OrderId}",
                        CreatedAt = DateTime.UtcNow
                    });
                }

                // --- Bước 2: Cộng tiền/điểm cho người bán ---
                // Bán sỉ: Drop-off mà người bán là Người thu gom → nhận VNĐ vào Ví Ký Quỹ (vốn + lãi)
                var sellerUser = await _context.Users.Include(u => u.Role)
                    .FirstOrDefaultAsync(u => u.UserId == sellerUserId);
                bool isWholesale = order.MethodId == 1 && sellerUser?.Role?.RoleName == "Collector";

                decimal netGreenPoints = (order.TotalActualAmount ?? 0);
                string rewardMessage;

                if (isWholesale)
                {
                    var escrowWallet = await _context.Wallets
                        .FirstOrDefaultAsync(w => w.UserId == sellerUserId && w.WalletType == "ESCROW_VND")
                        ?? throw new Exception("Người thu gom chưa có Ví Ký Quỹ.");

                    escrowWallet.Balance = (escrowWallet.Balance ?? 0) + netGreenPoints;
                    escrowWallet.LastUpdated = DateTime.UtcNow;

                    _context.WalletTransactions.Add(new WalletTransaction
                    {
                        WalletId = escrowWallet.WalletId,
                        Amount = netGreenPoints,
                        TransactionType = "WHOLESALE_RECEIPT",
                        ReferenceOrderId = order.OrderId,
                        Description = $"Thu hồi vốn bán sỉ cho Vựa đơn #{order.OrderId}",
                        CreatedAt = DateTime.UtcNow
                    });

                    rewardMessage = $"{netGreenPoints:N0} VNĐ vào Ví Ký Quỹ";
                }
                else
                {
                    var sellerWallet = await _context.Wallets
                        .FirstOrDefaultAsync(w => w.UserId == sellerUserId && w.WalletType == "GREEN_POINT");

                    if (sellerWallet == null)
                    {
                        sellerWallet = new Wallet
                        {
                            UserId = sellerUserId,
                            WalletType = "GREEN_POINT",
                            Currency = "GP",
                            Balance = 0m
                        };
                        _context.Wallets.Add(sellerWallet);
                        await _context.SaveChangesAsync();
                    }

                    // Drop-off: nhận đủ 100% (phí là của Vựa trả thêm)
                    // Pick-up: nhận sau khi trừ 20% phí tiện lợi
                    if (order.MethodId == 2)
                        netGreenPoints -= (order.PlatformFee ?? 0);

                    sellerWallet.Balance = (sellerWallet.Balance ?? 0) + netGreenPoints;
                    sellerWallet.LastUpdated = DateTime.UtcNow;

                    _context.WalletTransactions.Add(new WalletTransaction
                    {
                        WalletId = sellerWallet.WalletId,
                        Amount = netGreenPoints,
                        TransactionType = "ORDER_REWARD",
                        ReferenceOrderId = order.OrderId,
                        Description = $"Nhận GreenPoints từ đơn #{order.OrderId}",
                        CreatedAt = DateTime.UtcNow
                    });

                    rewardMessage = $"{netGreenPoints:N0} GreenPoints";
                }

                // --- Bước 3: Chuyển Order sang trạng thái Completed (StatusId=4) ---
                order.StatusId = 4;
                order.UpdatedAt = DateTime.UtcNow;

                await _context.SaveChangesAsync();
                await dbTransaction.CommitAsync();

                var payload = new TransactionResultPayloadDto
                {
                    OrderId = order.OrderId,
                    IsConfirmed = true,
                    Message = $"Giao dịch thành công! Người bán đã nhận được {rewardMessage}."
                };

                if (order.MethodId == 1) // Drop-off
                {
                    var dropOff = await _context.DropOffOrders.Include(d => d.Yard).FirstOrDefaultAsync(d => d.OrderId == order.OrderId);
                    if (dropOff?.Yard != null)
                    {
                        await _notifier.SendTransactionResultToYardAsync(dropOff.Yard.UserId.ToString(), payload);
                    }
                }
                else if (order.MethodId == 2) // Pick-up
                {
                    var pickUp = await _context.PickUpOrders.Include(p => p.Collector).FirstOrDefaultAsync(p => p.OrderId == order.OrderId);
                    if (pickUp?.Collector != null)
                    {
                        await _notifier.SendTransactionResultToYardAsync(pickUp.Collector.UserId.ToString(), payload);
                    }
                }

                // Bắn Push Notification khi giao dịch thành công
                _ = Task.Run(async () =>
                {
                    await _pushService.SendPushToUserAsync(order.SellerId, "Giao dịch thành công! 🎉", $"Đơn #{order.OrderId} đã hoàn tất. Bạn nhận được {rewardMessage}.", new { orderId = order.OrderId });
                });

                return new ApiResponse<object>
                {
                    Success = true,
                    Message = $"Giao dịch thành công! Bạn nhận được {rewardMessage}."
                };
            }
            catch
            {
                await dbTransaction.RollbackAsync();
                throw;
            }
        }
    }
}
