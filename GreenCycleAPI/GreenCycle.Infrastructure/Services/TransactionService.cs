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

        public TransactionService(GreenCycleDbContext context, ITransactionNotifier notifier)
        {
            _context = context;
            _notifier = notifier;
        }

        // ─────────────────────────────────────────────
        // 1. INITIATE: Chủ Vựa nhập số liệu thực tế → gửi Pop-up xác nhận chéo tới Người Bán
        // ─────────────────────────────────────────────
        public async Task<ApiResponse<DoubleConfirmationPayloadDto>> InitiateTransactionAsync(int yardUserId, InitiateTransactionRequestDto request)
        {
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
            await _context.SaveChangesAsync();

            // Xây dựng payload gửi về Người Bán
            var payload = new DoubleConfirmationPayloadDto
            {
                OrderId = order.OrderId,
                YardName = yard.ScrapYardName,
                TotalActualAmount = totalActualAmount,
                PlatformFee = platformFee,
                NetGreenPoints = netGreenPoints
            };

            // Gửi qua SignalR (thông qua abstraction)
            await _notifier.SendDoubleConfirmationAsync(order.SellerId.ToString(), payload);

            return new ApiResponse<DoubleConfirmationPayloadDto>
            {
                Success = true,
                Message = "Đã gửi yêu cầu xác nhận chéo tới khách hàng.",
                Data = payload
            };
        }

        // ─────────────────────────────────────────────
        // 2. CONFIRM: Người Bán bấm "Đồng ý" → Hệ thống cập nhật ví (ACID Transaction)
        // ─────────────────────────────────────────────
        public async Task<ApiResponse<object>> ConfirmTransactionAsync(int sellerUserId, ConfirmTransactionRequestDto request)
        {
            var order = await _context.Orders
                .FirstOrDefaultAsync(o => o.OrderId == request.OrderId)
                ?? throw new Exception("Đơn hàng không tồn tại.");

            if (order.SellerId != sellerUserId)
                throw new Exception("Không có quyền xác nhận đơn hàng này.");

            if (order.StatusId != 1)
                throw new Exception("Đơn hàng không hợp lệ để xác nhận (đã xử lý hoặc đã hủy).");

            using var dbTransaction = await _context.Database.BeginTransactionAsync();
            try
            {
                // --- Bước 1: Xử lý Ví Vựa (chỉ cho Drop-off) ---
                if (order.MethodId == 1)
                {
                    var dropOff = await _context.DropOffOrders.FirstOrDefaultAsync(d => d.OrderId == order.OrderId);
                    if (dropOff != null)
                    {
                        var yard = await _context.ScrapYards.FindAsync(dropOff.YardId);
                        if (yard != null)
                        {
                            var yardWallet = await _context.Wallets
                                .FirstOrDefaultAsync(w => w.UserId == yard.UserId && w.WalletType == "PREPAID_VND");

                            if (yardWallet == null)
                            {
                                // Mock tạo ví Trả Trước với 10 triệu VNĐ để test
                                yardWallet = new Wallet
                                {
                                    UserId = yard.UserId,
                                    WalletType = "PREPAID_VND",
                                    Currency = "VND",
                                    Balance = 10_000_000m
                                };
                                _context.Wallets.Add(yardWallet);
                                await _context.SaveChangesAsync();
                            }

                            // Vựa phải trả = Tiền hàng + 10% phí nền tảng
                            decimal yardDeduction = (order.TotalActualAmount ?? 0) + (order.PlatformFee ?? 0);
                            yardWallet.Balance -= yardDeduction;
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
                    }
                }

                // --- Bước 2: Cộng GreenPoints cho Người Bán ---
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
                decimal netGreenPoints = (order.TotalActualAmount ?? 0);
                if (order.MethodId == 2)
                    netGreenPoints -= (order.PlatformFee ?? 0);

                sellerWallet.Balance += netGreenPoints;
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

                // --- Bước 3: Chuyển Order sang trạng thái Completed (StatusId=4) ---
                order.StatusId = 4;
                order.UpdatedAt = DateTime.UtcNow;

                await _context.SaveChangesAsync();
                await dbTransaction.CommitAsync();

                return new ApiResponse<object>
                {
                    Success = true,
                    Message = $"Giao dịch thành công! Bạn nhận được {netGreenPoints:N0} GreenPoints."
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
