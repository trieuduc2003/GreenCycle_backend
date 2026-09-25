namespace GreenCycle.Application.DTOs.Auth
{
    /// <summary>Response trả về sau khi đăng nhập hoặc đăng ký thành công.</summary>
    public class AuthResponseDto
    {
        public int UserId { get; set; }
        public string FullName { get; set; } = null!;
        public string Phone { get; set; } = null!;
        public string RoleName { get; set; } = null!;
        public string Token { get; set; } = null!;
    }
}
