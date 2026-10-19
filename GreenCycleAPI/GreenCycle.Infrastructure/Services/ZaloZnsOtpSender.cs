using System.Net.Http.Headers;
using System.Text;
using System.Text.Json;
using GreenCycle.Application.Interfaces.Services;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Logging;

namespace GreenCycle.Infrastructure.Services
{
    /// <summary>
    /// Gửi OTP qua Zalo ZNS (Zalo Notification Service) bằng template đã được Zalo duyệt.
    /// Cấu hình trong appsettings, mục "Zalo":
    ///   Enabled, AccessToken, RefreshToken, AppId, SecretKey, TemplateId,
    ///   OtpParamName (mặc định "otp"), ValidTimeParamName (tuỳ chọn), Mode ("development" khi test).
    /// Khi Enabled = false (hoặc thiếu cấu hình) sẽ in OTP ra console để phát triển cục bộ.
    /// </summary>
    public class ZaloZnsOtpSender : IOtpSender
    {
        private const string SendUrl = "https://business.openapi.zalo.me/message/template";
        private const string RefreshUrl = "https://oauth.zaloapp.com/v4/oa/access_token";

        // Mã lỗi Zalo liên quan đến access token không hợp lệ / hết hạn
        private static readonly HashSet<int> TokenErrorCodes = new() { -124, -216 };

        // Token được giữ ở mức static vì sender được tạo mới theo mỗi request (typed HttpClient)
        private static readonly SemaphoreSlim TokenLock = new(1, 1);
        private static string? _accessToken;
        private static string? _refreshToken;
        private static bool _tokenLoaded;
        private static readonly string TokenFile = Path.Combine(AppContext.BaseDirectory, "zalo_token.json");

        private readonly HttpClient _http;
        private readonly IConfiguration _config;
        private readonly ILogger<ZaloZnsOtpSender> _logger;

        public ZaloZnsOtpSender(HttpClient http, IConfiguration config, ILogger<ZaloZnsOtpSender> logger)
        {
            _http = http;
            _config = config;
            _logger = logger;
        }

        public async Task SendOtpAsync(string phone, string otp, int validMinutes, string? subject = null)
        {
            var normalizedPhone = NormalizePhone(phone);

            if (!IsEnabled())
            {
                // DEV fallback: không gọi Zalo, in ra console
                Console.WriteLine("======================================");
                Console.WriteLine($"  [OTP-DEV] SĐT: {normalizedPhone}");
                Console.WriteLine($"  [OTP-DEV] Mã OTP: {otp}  (hiệu lực {validMinutes} phút)");
                Console.WriteLine("  (Bật Zalo:Enabled=true để gửi qua Zalo ZNS)");
                Console.WriteLine("======================================");
                return;
            }

            await EnsureTokenLoadedAsync();

            var result = await PostSendAsync(normalizedPhone, otp, validMinutes);

            // Token hết hạn → refresh một lần rồi gửi lại
            if (TokenErrorCodes.Contains(result.Error) && CanRefresh())
            {
                _logger.LogWarning("Zalo access token không hợp lệ/hết hạn (error {Code}). Đang refresh...", result.Error);
                await RefreshAccessTokenAsync();
                result = await PostSendAsync(normalizedPhone, otp, validMinutes);
            }

            if (result.Error != 0)
            {
                _logger.LogError("Gửi OTP Zalo ZNS thất bại. error={Code}, message={Message}", result.Error, result.Message);
                throw new Exception(MapFriendlyError(result.Error));
            }
        }

        // ─────────────────────────────────────────────
        // Gọi API gửi tin
        // ─────────────────────────────────────────────
        private async Task<(int Error, string? Message)> PostSendAsync(string phone, string otp, int validMinutes)
        {
            var templateData = new Dictionary<string, string>
            {
                [_config["Zalo:OtpParamName"] ?? "otp"] = otp
            };
            var validTimeParam = _config["Zalo:ValidTimeParamName"];
            if (!string.IsNullOrWhiteSpace(validTimeParam))
                templateData[validTimeParam] = validMinutes.ToString();

            var body = new Dictionary<string, object>
            {
                ["phone"] = phone,
                ["template_id"] = _config["Zalo:TemplateId"] ?? "",
                ["template_data"] = templateData,
                ["tracking_id"] = Guid.NewGuid().ToString("N")
            };

            // Chế độ development: Zalo chỉ gửi tới SĐT admin của OA/App
            var mode = _config["Zalo:Mode"];
            if (!string.IsNullOrWhiteSpace(mode))
                body["mode"] = mode;

            using var req = new HttpRequestMessage(HttpMethod.Post, SendUrl)
            {
                Content = new StringContent(JsonSerializer.Serialize(body), Encoding.UTF8, "application/json")
            };
            req.Headers.Add("access_token", _accessToken ?? "");

            try
            {
                using var resp = await _http.SendAsync(req);
                var json = await resp.Content.ReadAsStringAsync();
                using var doc = JsonDocument.Parse(json);
                var root = doc.RootElement;
                int error = root.TryGetProperty("error", out var e) && e.TryGetInt32(out var code) ? code : -1;
                string? message = root.TryGetProperty("message", out var m) ? m.GetString() : null;
                return (error, message);
            }
            catch (Exception ex) when (ex is HttpRequestException or TaskCanceledException or JsonException)
            {
                _logger.LogError(ex, "Không kết nối được Zalo ZNS");
                throw new Exception("Không thể kết nối dịch vụ Zalo để gửi OTP. Vui lòng thử lại sau.");
            }
        }

        // ─────────────────────────────────────────────
        // Quản lý access token / refresh token
        // ─────────────────────────────────────────────
        private bool IsEnabled() =>
            string.Equals(_config["Zalo:Enabled"], "true", StringComparison.OrdinalIgnoreCase)
            && !string.IsNullOrWhiteSpace(_config["Zalo:TemplateId"]);

        private bool CanRefresh() =>
            !string.IsNullOrWhiteSpace(_refreshToken)
            && !string.IsNullOrWhiteSpace(_config["Zalo:AppId"])
            && !string.IsNullOrWhiteSpace(_config["Zalo:SecretKey"]);

        private async Task EnsureTokenLoadedAsync()
        {
            if (_tokenLoaded) return;
            await TokenLock.WaitAsync();
            try
            {
                if (_tokenLoaded) return;

                // Ưu tiên file đã lưu sau lần refresh gần nhất (refresh token của Zalo chỉ dùng được 1 lần)
                if (File.Exists(TokenFile))
                {
                    try
                    {
                        using var doc = JsonDocument.Parse(await File.ReadAllTextAsync(TokenFile));
                        _accessToken = doc.RootElement.GetProperty("access_token").GetString();
                        _refreshToken = doc.RootElement.GetProperty("refresh_token").GetString();
                    }
                    catch (Exception ex)
                    {
                        _logger.LogWarning(ex, "Không đọc được {File}, dùng token từ cấu hình", TokenFile);
                    }
                }

                _accessToken ??= _config["Zalo:AccessToken"];
                _refreshToken ??= _config["Zalo:RefreshToken"];
                _tokenLoaded = true;
            }
            finally
            {
                TokenLock.Release();
            }
        }

        private async Task RefreshAccessTokenAsync()
        {
            await TokenLock.WaitAsync();
            try
            {
                using var req = new HttpRequestMessage(HttpMethod.Post, RefreshUrl)
                {
                    Content = new FormUrlEncodedContent(new Dictionary<string, string>
                    {
                        ["refresh_token"] = _refreshToken!,
                        ["app_id"] = _config["Zalo:AppId"]!,
                        ["grant_type"] = "refresh_token"
                    })
                };
                req.Headers.Add("secret_key", _config["Zalo:SecretKey"]!);
                req.Content!.Headers.ContentType = new MediaTypeHeaderValue("application/x-www-form-urlencoded");

                using var resp = await _http.SendAsync(req);
                var json = await resp.Content.ReadAsStringAsync();
                using var doc = JsonDocument.Parse(json);
                var root = doc.RootElement;

                if (!root.TryGetProperty("access_token", out var at) || string.IsNullOrWhiteSpace(at.GetString()))
                {
                    _logger.LogError("Refresh Zalo token thất bại: {Body}", json);
                    throw new Exception("Dịch vụ gửi OTP Zalo chưa sẵn sàng (không làm mới được token). Vui lòng liên hệ quản trị viên.");
                }

                _accessToken = at.GetString();
                if (root.TryGetProperty("refresh_token", out var rt) && !string.IsNullOrWhiteSpace(rt.GetString()))
                    _refreshToken = rt.GetString();

                // Lưu lại vì refresh token cũ đã bị vô hiệu
                await File.WriteAllTextAsync(TokenFile, JsonSerializer.Serialize(new
                {
                    access_token = _accessToken,
                    refresh_token = _refreshToken,
                    updated_at = DateTime.UtcNow
                }));
            }
            finally
            {
                TokenLock.Release();
            }
        }

        // ─────────────────────────────────────────────
        // Tiện ích
        // ─────────────────────────────────────────────
        /// <summary>Chuẩn hoá SĐT về dạng 84xxxxxxxxx theo yêu cầu của Zalo.</summary>
        public static string NormalizePhone(string phone)
        {
            var digits = new string((phone ?? "").Where(char.IsDigit).ToArray());
            if (digits.StartsWith("84")) return digits;
            if (digits.StartsWith("0")) return "84" + digits[1..];
            return "84" + digits;
        }

        private static string MapFriendlyError(int code) => code switch
        {
            -124 or -216 => "Dịch vụ gửi OTP Zalo chưa sẵn sàng (token không hợp lệ). Vui lòng liên hệ quản trị viên.",
            -118 => "Số điện thoại này chưa đăng ký Zalo hoặc không nhận được tin Zalo.",
            -137 => "Tài khoản Zalo OA không đủ số dư để gửi tin.",
            -139 => "Người dùng chưa cho phép nhận tin từ ứng dụng.",
            -141 => "Người dùng đã từ chối nhận loại tin này.",
            -144 => "Đã vượt giới hạn số tin trong ngày cho số điện thoại này.",
            -108 => "Số điện thoại không hợp lệ.",
            _ => "Không gửi được mã OTP qua Zalo. Vui lòng thử lại sau."
        };
    }
}
