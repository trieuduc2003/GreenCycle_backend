using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using GreenCycle.Application.DTOs.Voucher;
using GreenCycle.Application.Interfaces.Services;
using GreenCycle.Domain.Entities;
using GreenCycle.Infrastructure.Data;
using Microsoft.EntityFrameworkCore;

namespace GreenCycle.Infrastructure.Services
{
    public class VoucherService : IVoucherService
    {
        private readonly GreenCycleDbContext _context;

        public VoucherService(GreenCycleDbContext context)
        {
            _context = context;
        }

        public async Task<List<VoucherDto>> GetAvailableVouchersAsync()
        {
            // Seed dữ liệu mẫu nếu bảng Vouchers chưa có gì
            await SeedInitialVouchersIfEmptyAsync();

            var now = DateTime.UtcNow;
            var vouchers = await _context.Vouchers
                .Where(v => (v.IsActive == null || v.IsActive == true)
                         && v.ExpiredAt > now
                         && (v.Quantity == null || v.Quantity > 0))
                .OrderBy(v => v.PointCost)
                .ToListAsync();

            return vouchers.Select(v => new VoucherDto
            {
                VoucherId     = v.VoucherId,
                Title         = v.Title,
                Description   = v.Description,
                PointCost     = v.PointCost,
                DiscountValue = v.DiscountValue,
                Quantity      = v.Quantity,
                IsActive      = v.IsActive,
                ExpiredAt     = v.ExpiredAt,
                Category      = CategorizeVoucher(v.Title, v.Description)
            }).ToList();
        }

        public async Task<RedeemVoucherResponseDto> RedeemVoucherAsync(int userId, int voucherId)
        {
            var voucher = await _context.Vouchers.FindAsync(voucherId)
                ?? throw new Exception("Không tìm thấy thông tin Voucher/Quà tặng này.");

            var now = DateTime.UtcNow;
            if (voucher.IsActive == false || voucher.ExpiredAt <= now)
            {
                throw new Exception("Voucher này đã hết hạn hoặc tạm dừng quy đổi.");
            }

            if (voucher.Quantity.HasValue && voucher.Quantity.Value <= 0)
            {
                throw new Exception("Quà tặng này đã hết số lượng có sẵn trong kho.");
            }

            // Lấy ví GreenPoints của Seller
            var wallet = await _context.Wallets
                .FirstOrDefaultAsync(w => w.UserId == userId && w.WalletType == "GREEN_POINT");

            if (wallet == null)
            {
                throw new Exception("Không tìm thấy ví GreenPoints của bạn.");
            }

            var currentBalance = wallet.Balance ?? 0m;
            if (currentBalance < voucher.PointCost)
            {
                var diff = voucher.PointCost - currentBalance;
                throw new Exception($"Số dư không đủ! Bạn cần tích lũy thêm {diff:N0} GreenPoints nữa để đổi quà này.");
            }

            // Thực hiện giao dịch đổi quà
            using var tx = await _context.Database.BeginTransactionAsync();
            try
            {
                // 1. Giảm số lượng nếu có
                if (voucher.Quantity.HasValue && voucher.Quantity.Value > 0)
                {
                    voucher.Quantity -= 1;
                }

                // 2. Trừ điểm GreenPoints trong ví
                wallet.Balance = currentBalance - voucher.PointCost;
                wallet.LastUpdated = DateTime.UtcNow;

                // 3. Tạo mã Voucher ngẫu nhiên dạng GC-XXXX-XXXX
                var randomPart = Guid.NewGuid().ToString("N").Substring(0, 8).ToUpper();
                var voucherCode = $"GC-{voucher.VoucherId:D2}{randomPart}";

                var userVoucher = new UserVoucher
                {
                    UserId      = userId,
                    VoucherId   = voucher.VoucherId,
                    VoucherCode = voucherCode,
                    IsUsed      = false,
                    ReceivedAt  = DateTime.UtcNow,
                };

                _context.UserVouchers.Add(userVoucher);
                await _context.SaveChangesAsync();

                // 4. Ghi lịch sử giao dịch ví (REDEEM_VOUCHER)
                var walletTx = new WalletTransaction
                {
                    WalletId           = wallet.WalletId,
                    Amount             = -voucher.PointCost,
                    TransactionType    = "REDEEM_VOUCHER",
                    ReferenceVoucherId = userVoucher.UserVoucherId,
                    CreatedAt          = DateTime.UtcNow,
                    Description        = $"Đổi quà: {voucher.Title}"
                };

                _context.WalletTransactions.Add(walletTx);
                await _context.SaveChangesAsync();

                await tx.CommitAsync();

                // Tính toán chỉ số Gamification / Social Flexing (FR-SOC-01)
                // Cứ mỗi 200 GP tương đương việc cứu 1 cây xanh hoặc giảm 2kg CO2
                var treesSaved = Math.Max(0.5, Math.Round((double)voucher.PointCost / 200.0, 1));
                var co2Reduced = Math.Max(1.0, Math.Round((double)voucher.PointCost / 100.0, 1));

                return new RedeemVoucherResponseDto
                {
                    Success         = true,
                    Message         = $"Chúc mừng bạn đã đổi thành công: {voucher.Title}!",
                    RemainingPoints = wallet.Balance ?? 0m,
                    TreesSaved      = treesSaved,
                    Co2ReducedKg    = co2Reduced,
                    UserVoucher     = new UserVoucherDto
                    {
                        UserVoucherId = userVoucher.UserVoucherId,
                        UserId        = userId,
                        VoucherId     = voucher.VoucherId,
                        VoucherCode   = voucherCode,
                        Title         = voucher.Title,
                        Description   = voucher.Description,
                        PointCost     = voucher.PointCost,
                        DiscountValue = voucher.DiscountValue,
                        IsUsed        = false,
                        ReceivedAt    = userVoucher.ReceivedAt,
                        UsedAt        = null,
                        ExpiredAt     = voucher.ExpiredAt,
                        Category      = CategorizeVoucher(voucher.Title, voucher.Description)
                    }
                };
            }
            catch
            {
                await tx.RollbackAsync();
                throw;
            }
        }

        public async Task<List<UserVoucherDto>> GetMyVouchersAsync(int userId)
        {
            var userVouchers = await _context.UserVouchers
                .Include(uv => uv.Voucher)
                .Where(uv => uv.UserId == userId)
                .OrderByDescending(uv => uv.ReceivedAt)
                .ToListAsync();

            return userVouchers.Select(uv => new UserVoucherDto
            {
                UserVoucherId = uv.UserVoucherId,
                UserId        = uv.UserId,
                VoucherId     = uv.VoucherId,
                VoucherCode   = uv.VoucherCode,
                Title         = uv.Voucher?.Title ?? "Voucher GreenCycle",
                Description   = uv.Voucher?.Description,
                PointCost     = uv.Voucher?.PointCost ?? 0m,
                DiscountValue = uv.Voucher?.DiscountValue ?? 0m,
                IsUsed        = uv.IsUsed ?? false,
                ReceivedAt    = uv.ReceivedAt,
                UsedAt        = uv.UsedAt,
                ExpiredAt     = uv.Voucher?.ExpiredAt ?? DateTime.UtcNow.AddDays(30),
                Category      = CategorizeVoucher(uv.Voucher?.Title ?? "", uv.Voucher?.Description)
            }).ToList();
        }

        public async Task<bool> UseVoucherAsync(int userId, int userVoucherId)
        {
            var userVoucher = await _context.UserVouchers
                .Include(uv => uv.Voucher)
                .FirstOrDefaultAsync(uv => uv.UserVoucherId == userVoucherId && uv.UserId == userId);

            if (userVoucher == null)
                throw new Exception("Không tìm thấy mã quà tặng này.");

            if (userVoucher.IsUsed == true)
                throw new Exception("Voucher này đã được sử dụng trước đó.");

            if (userVoucher.Voucher != null && userVoucher.Voucher.ExpiredAt <= DateTime.UtcNow)
                throw new Exception("Voucher này đã quá hạn sử dụng.");

            userVoucher.IsUsed = true;
            userVoucher.UsedAt = DateTime.UtcNow;

            await _context.SaveChangesAsync();
            return true;
        }

        // Helper: Phân loại Voucher theo danh mục thực tế
        private static string CategorizeVoucher(string title, string? desc)
        {
            var text = (title + " " + (desc ?? "")).ToLower();
            if (text.Contains("coffee") || text.Contains("food") || text.Contains("kfc") || text.Contains("uống") || text.Contains("ăn") || text.Contains("trà"))
                return "F&B";
            if (text.Contains("cây") || text.Contains("sen đá") || text.Contains("túi") || text.Contains("tái chế") || text.Contains("bình") || text.Contains("xanh"))
                return "Sống xanh";
            if (text.Contains("grab") || text.Contains("ship") || text.Contains("vận chuyển"))
                return "Vận chuyển";
            if (text.Contains("cgv") || text.Contains("phim") || text.Contains("giải trí") || text.Contains("vé"))
                return "Giải trí";
            return "Mua sắm";
        }

        // Helper: Tự động khởi tạo kho quà tặng phong phú nếu database chưa có
        private async Task SeedInitialVouchersIfEmptyAsync()
        {
            if (await _context.Vouchers.AnyAsync()) return;

            var initialVouchers = new List<Voucher>
            {
                new Voucher
                {
                    Title         = "Highlands Coffee - Giảm 50.000đ",
                    Description   = "Áp dụng cho mọi thức uống cỡ L trên toàn bộ hệ thống Highlands Coffee toàn quốc.",
                    PointCost     = 500,
                    DiscountValue = 50000,
                    Quantity      = 100,
                    IsActive      = true,
                    ExpiredAt     = DateTime.UtcNow.AddDays(60)
                },
                new Voucher
                {
                    Title         = "ShopeeFood - Mã Giảm 30.000đ",
                    Description   = "Giảm trực tiếp 30k cho đơn đồ ăn từ 60k trên ứng dụng ShopeeFood.",
                    PointCost     = 300,
                    DiscountValue = 30000,
                    Quantity      = 150,
                    IsActive      = true,
                    ExpiredAt     = DateTime.UtcNow.AddDays(45)
                },
                new Voucher
                {
                    Title         = "Cây Sen Đá Mini Để Bàn Sống Xanh",
                    Description   = "Chậu sen đá để bàn lọc không khí, kèm chậu gốm tái chế thân thiện môi trường.",
                    PointCost     = 400,
                    DiscountValue = 45000,
                    Quantity      = 50,
                    IsActive      = true,
                    ExpiredAt     = DateTime.UtcNow.AddDays(90)
                },
                new Voucher
                {
                    Title         = "GrabExpress - Miễn Phí Giao Hàng 25k",
                    Description   = "Áp dụng cho đơn giao hàng siêu tốc GrabExpress nội thành.",
                    PointCost     = 250,
                    DiscountValue = 25000,
                    Quantity      = 200,
                    IsActive      = true,
                    ExpiredAt     = DateTime.UtcNow.AddDays(30)
                },
                new Voucher
                {
                    Title         = "Túi Tote Canvas Tái Chế GreenCycle",
                    Description   = "Túi vải Canvas 100% sợi tái chế bền đẹp, phong cách sống xanh tối giản.",
                    PointCost     = 350,
                    DiscountValue = 40000,
                    Quantity      = 80,
                    IsActive      = true,
                    ExpiredAt     = DateTime.UtcNow.AddDays(90)
                },
                new Voucher
                {
                    Title         = "Vé Xem Phim CGV Cinemas 2D",
                    Description   = "Vé xem phim 2D tất cả các ngày trong tuần tại cụm rạp CGV trên toàn quốc.",
                    PointCost     = 800,
                    DiscountValue = 110000,
                    Quantity      = 60,
                    IsActive      = true,
                    ExpiredAt     = DateTime.UtcNow.AddDays(60)
                },
                new Voucher
                {
                    Title         = "WinMart - Phiếu Mua Hàng 100.000đ",
                    Description   = "Giảm 100k cho hóa đơn mua sắm hàng tiêu dùng, rau củ quả tại WinMart/WinMart+.",
                    PointCost     = 1000,
                    DiscountValue = 100000,
                    Quantity      = 40,
                    IsActive      = true,
                    ExpiredAt     = DateTime.UtcNow.AddDays(60)
                },
                new Voucher
                {
                    Title         = "Bình Giữ Nhiệt Inox Tái Sinh 500ml",
                    Description   = "Bình giữ nhiệt 2 lớp giữ nóng/lạnh 12 tiếng, thiết kế logo GreenCycle độc quyền.",
                    PointCost     = 650,
                    DiscountValue = 85000,
                    Quantity      = 30,
                    IsActive      = true,
                    ExpiredAt     = DateTime.UtcNow.AddDays(120)
                },
                new Voucher
                {
                    Title         = "KFC - Combo 1 Miếng Gà + Nước Ngọt",
                    Description   = "Đổi lấy 1 phần gà rán giòn cay truyền thống kèm nước ngọt tươi mát tại KFC.",
                    PointCost     = 450,
                    DiscountValue = 55000,
                    Quantity      = 70,
                    IsActive      = true,
                    ExpiredAt     = DateTime.UtcNow.AddDays(45)
                }
            };

            await _context.Vouchers.AddRangeAsync(initialVouchers);
            await _context.SaveChangesAsync();
        }
    }
}
