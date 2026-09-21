namespace GreenCycle.Application.DTOs.Auth
{
    /// <summary>
    /// DTO yêu cầu đăng nhập bằng tài khoản Google (OAuth 2.0).
    /// </summary>
    public class GoogleLoginRequestDto
    {
        /// <summary>
        /// ID Token trả về từ Google Sign-In SDK sau khi người dùng xác thực thành công.
        /// </summary>
        public string IdToken { get; set; } = string.Empty;

        /// <summary>
        /// Vai trò người dùng muốn đăng ký nếu tài khoản chưa từng tồn tại (mặc định: 1 - Người bán).
        /// </summary>
        public int? RoleId { get; set; }
    }
}
