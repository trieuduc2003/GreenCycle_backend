using Google.Apis.Auth;
using GreenCycle.Application.DTOs.Auth;
using GreenCycle.Application.Interfaces.Services;
using GreenCycle.Domain.Entities;
using GreenCycle.Infrastructure.Data;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Caching.Memory;
using Microsoft.Extensions.Configuration;

namespace GreenCycle.Application.Interfaces.Services
{
    public class AuthService : IAuthService
    {
        private readonly GreenCycleDbContext _context;
        private readonly IJwtTokenGenerator _jwtTokenGenerator;
        private readonly IMemoryCache _cache;
        private readonly IConfiguration _configuration;

        // Key prefix lưu OTP trong cache
        private const string OtpCacheKeyPrefix = "reg_otp:";
        // Thời gian OTP còn hiệu lực
        private static readonly TimeSpan OtpExpiry = TimeSpan.FromMinutes(5);

        public AuthService(
            GreenCycleDbContext context,
            IJwtTokenGenerator jwtTokenGenerator,
            IMemoryCache cache,
            IConfiguration configuration)
        {
            _context = context;
            _jwtTokenGenerator = jwtTokenGenerator;
            _cache = cache;
            _configuration = configuration;
        }

        // ─────────────────────────────────────────────
        // GỬI OTP (Bước 1 của đăng ký)
        // ─────────────────────────────────────────────
        public async Task SendOtpForRegisterAsync(SendOtpRequestDto request)
        {
            // Kiểm tra SĐT đã đăng ký chưa
            if (await _context.Users.AnyAsync(u => u.Phone == request.Phone))
            {
                throw new Exception("Số điện thoại này đã được đăng ký!");
            }

            // Tạo OTP ngẫu nhiên 6 chữ số
            var otp = new Random().Next(100000, 999999).ToString();

            // Lưu OTP vào In-Memory Cache với TTL 5 phút
            var cacheKey = OtpCacheKeyPrefix + request.Phone;
            _cache.Set(cacheKey, otp, OtpExpiry);

            // ── DEV: In OTP ra Console để test ──────────────────────────────
            // PROD: Thay bằng SMS Gateway (Twilio / ESMS / Viettel)
            Console.WriteLine("======================================");
            Console.WriteLine($"  [OTP] SĐT: {request.Phone}");
            Console.WriteLine($"  [OTP] Mã OTP: {otp}  (hiệu lực 5 phút)");
            Console.WriteLine("======================================");
            // ────────────────────────────────────────────────────────────────

            await Task.CompletedTask;
        }

        // ─────────────────────────────────────────────
        // ĐĂNG KÝ TÀI KHOẢN (Bước 2 — kiểm tra OTP)
        // ─────────────────────────────────────────────
        public async Task<AuthResponseDto> RegisterAsync(RegisterRequestDto request)
        {
            // 1. Kiểm tra OTP có hợp lệ không
            var cacheKey = OtpCacheKeyPrefix + request.Phone;
            if (!_cache.TryGetValue(cacheKey, out string? storedOtp) || storedOtp != request.OtpCode)
            {
                throw new Exception("Mã OTP không hợp lệ hoặc đã hết hạn!");
            }

            // 2. Kiểm tra SĐT chưa tồn tại (double-check)
            if (await _context.Users.AnyAsync(u => u.Phone == request.Phone))
            {
                throw new Exception("Số điện thoại này đã được đăng ký!");
            }

            // 3. Kiểm tra RoleId hợp lệ
            var role = await _context.Roles.FindAsync(request.RoleId)
                ?? throw new Exception("Vai trò (Role) không hợp lệ!");

            // 4. Tạo User mới
            var user = new User
            {
                FullName = request.FullName,
                Phone = request.Phone,
                Email = request.Email,
                PassWordHash = BCrypt.Net.BCrypt.HashPassword(request.Password),
                RoleId = request.RoleId,
                IsActive = true,
                CreatedAt = DateTime.UtcNow
            };

            _context.Users.Add(user);
            await _context.SaveChangesAsync();

            // 5. Tạo Wallet theo Role
            var (walletType, currency) = request.RoleId switch
            {
                1 => ("GREEN_POINT", "GP"),     // Seller (Người bán)
                2 => ("ESCROW_VND", "VND"),      // Collector (Tài xế)
                3 => ("PREPAID_VND", "VND"),     // ScrapYard (Chủ vựa)
                _ => ("GREEN_POINT", "GP")
            };

            var wallet = new Wallet
            {
                UserId = user.UserId,
                WalletType = walletType,
                Currency = currency,
                Balance = 0,
                LastUpdated = DateTime.UtcNow
            };
            _context.Wallets.Add(wallet);

            // 6. Tạo hồ sơ đặc thù theo Role
            if (request.RoleId == 2)
            {
                _context.Collectors.Add(new Collector { UserId = user.UserId, CurrentStatus = "OFFLINE" });
            }
            else if (request.RoleId == 3)
            {
                _context.ScrapYards.Add(new ScrapYard
                {
                    UserId = user.UserId,
                    ScrapYardName = $"Vựa rác {request.FullName}",
                    Address = "Chưa cập nhật",
                    Location = new NetTopologySuite.Geometries.Point(106.660172, 10.762622) { SRID = 4326 }
                });
            }

            await _context.SaveChangesAsync();

            // 7. Xóa OTP khỏi cache (dùng một lần)
            _cache.Remove(cacheKey);

            // 8. Tạo JWT token và trả về
            var token = _jwtTokenGenerator.GenerateToken(user, role.RoleName!);

            return new AuthResponseDto
            {
                UserId = user.UserId,
                FullName = user.FullName,
                Phone = user.Phone,
                RoleName = role.RoleName!,
                Token = token
            };
        }

        // ─────────────────────────────────────────────
        // ĐĂNG NHẬP (Không thay đổi)
        // ─────────────────────────────────────────────
        public async Task<AuthResponseDto> LoginAsync(LoginRequestDto request)
        {
            var user = await _context.Users
                .Include(u => u.Role)
                .FirstOrDefaultAsync(u => u.Phone == request.Phone);

            if (user == null || !BCrypt.Net.BCrypt.Verify(request.Password, user.PassWordHash))
            {
                throw new Exception("Số điện thoại hoặc mật khẩu không chính xác!");
            }

            if (user.IsActive == false)
            {
                throw new Exception("Tài khoản của bạn đã bị khóa!");
            }

            var token = _jwtTokenGenerator.GenerateToken(user, user.Role!.RoleName!);

            return new AuthResponseDto
            {
                UserId = user.UserId,
                FullName = user.FullName!,
                Phone = user.Phone,
                RoleName = user.Role.RoleName!,
                Token = token
            };
        }

        // ─────────────────────────────────────────────
        // ĐĂNG NHẬP GOOGLE (OAuth 2.0)
        // ─────────────────────────────────────────────
        public async Task<AuthResponseDto> GoogleLoginAsync(GoogleLoginRequestDto request)
        {
            if (string.IsNullOrWhiteSpace(request.IdToken))
            {
                throw new Exception("Mã xác thực Google (IdToken) không được để trống!");
            }

            var backendClientId = _configuration["GoogleAuth:BackendClientId"] ?? "348318262413-l1g4gv3ij3op2uev9end4rusujti2fvb.apps.googleusercontent.com";
            var frontendClientId = _configuration["GoogleAuth:FrontendClientId"] ?? "348318262413-vep193tl9593jihtf1bbmjsl175asnna.apps.googleusercontent.com";

            GoogleJsonWebSignature.Payload payload;
            try
            {
                var validationSettings = new GoogleJsonWebSignature.ValidationSettings
                {
                    Audience = new List<string> { backendClientId, frontendClientId }
                };
                payload = await GoogleJsonWebSignature.ValidateAsync(request.IdToken, validationSettings);
            }
            catch (Exception ex)
            {
                throw new Exception($"Xác thực tài khoản Google thất bại: {ex.Message}");
            }

            if (string.IsNullOrWhiteSpace(payload.Email))
            {
                throw new Exception("Không thể lấy email từ tài khoản Google này!");
            }

            // Làm sạch họ tên từ Google (loại bỏ mã sinh viên / thông tin trong ngoặc như (HE186507))
            var rawGoogleName = payload.Name ?? string.Empty;
            var cleanFullName = System.Text.RegularExpressions.Regex.Replace(rawGoogleName, @"\s*[\(\[][^()\[\]]*[\)\]]\s*", "").Trim();
            if (string.IsNullOrWhiteSpace(cleanFullName))
            {
                cleanFullName = !string.IsNullOrWhiteSpace(rawGoogleName) ? rawGoogleName.Trim() : "Người dùng Google";
            }

            // 1. Kiểm tra tài khoản đã tồn tại theo Email chưa
            var user = await _context.Users
                .Include(u => u.Role)
                .FirstOrDefaultAsync(u => u.Email == payload.Email);

            if (user != null)
            {
                if (user.IsActive == false)
                {
                    throw new Exception("Tài khoản của bạn đã bị khóa!");
                }

                // Cập nhật lại họ tên sạch nếu trước đó bị dính mã ngoặc đơn như (HE186507) hoặc để trống
                if (string.IsNullOrWhiteSpace(user.FullName) || user.FullName.Contains("(") || user.FullName.Contains("["))
                {
                    user.FullName = cleanFullName;
                    await _context.SaveChangesAsync();
                }

                var userRoleName = user.Role?.RoleName ?? "Seller";
                var jwtToken = _jwtTokenGenerator.GenerateToken(user, userRoleName);

                return new AuthResponseDto
                {
                    UserId = user.UserId,
                    FullName = user.FullName ?? cleanFullName,
                    Phone = user.Phone,
                    RoleName = userRoleName,
                    Token = jwtToken
                };
            }

            // 2. Nếu chưa có tài khoản, tự động tạo mới tài khoản
            var targetRoleId = request.RoleId ?? 1; // Mặc định là Seller (1)
            var role = await _context.Roles.FindAsync(targetRoleId)
                ?? throw new Exception("Vai trò người dùng không hợp lệ!");

            // Tạo số điện thoại giữ chỗ tạm thời cho Google User (người dùng có thể cập nhật trong hồ sơ)
            var basePhone = "00" + Math.Abs(payload.Subject.GetHashCode()).ToString("D8");
            var phone = basePhone;
            var counter = 1;
            while (await _context.Users.AnyAsync(u => u.Phone == phone))
            {
                phone = basePhone.Substring(0, 8) + counter.ToString("D2");
                counter++;
            }

            var newUser = new User
            {
                FullName = cleanFullName,
                Email = payload.Email,
                Phone = phone,
                RoleId = targetRoleId,
                IsActive = true,
                CreatedAt = DateTime.UtcNow
            };

            _context.Users.Add(newUser);
            await _context.SaveChangesAsync();

            // Khởi tạo ví theo Role
            var (walletType, currency) = targetRoleId switch
            {
                1 => ("GREEN_POINT", "GP"),
                2 => ("ESCROW_VND", "VND"),
                3 => ("PREPAID_VND", "VND"),
                _ => ("GREEN_POINT", "GP")
            };

            var wallet = new Wallet
            {
                UserId = newUser.UserId,
                WalletType = walletType,
                Currency = currency,
                Balance = 0,
                LastUpdated = DateTime.UtcNow
            };
            _context.Wallets.Add(wallet);

            // Tạo hồ sơ đặc thù theo Role nếu có
            if (targetRoleId == 2)
            {
                _context.Collectors.Add(new Collector { CollectorId = newUser.UserId, CurrentStatus = "OFFLINE" });
            }
            else if (targetRoleId == 3)
            {
                _context.ScrapYards.Add(new ScrapYard
                {
                    YardId = newUser.UserId,
                    ScrapYardName = $"Vựa rác {newUser.FullName}",
                    Address = "Chưa cập nhật",
                    Location = new NetTopologySuite.Geometries.Point(106.660172, 10.762622) { SRID = 4326 }
                });
            }

            await _context.SaveChangesAsync();

            var token = _jwtTokenGenerator.GenerateToken(newUser, role.RoleName!);

            return new AuthResponseDto
            {
                UserId = newUser.UserId,
                FullName = newUser.FullName,
                Phone = newUser.Phone,
                RoleName = role.RoleName!,
                Token = token
            };
        }

        // ─────────────────────────────────────────────
        // ĐĂNG XUẤT
        // ─────────────────────────────────────────────
        public async Task LogoutAsync()
        {
            // Token JWT là stateless. Nếu cần lưu danh sách token thu hồi (blacklist)
            // hoặc xóa Refresh Token trong tương lai, xử lý tại đây.
            await Task.CompletedTask;
        }
    }
}
