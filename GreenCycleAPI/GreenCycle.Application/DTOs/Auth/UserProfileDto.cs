namespace GreenCycle.Application.DTOs.Auth
{
    /// <summary>Thông tin hồ sơ cá nhân của người dùng (response).</summary>
    public class UserProfileDto
    {
        public int UserId { get; set; }
        public string FullName { get; set; } = string.Empty;
        public string Phone { get; set; } = string.Empty;
        public string? Email { get; set; }
        public string RoleName { get; set; } = string.Empty;
        public DateTime? CreatedAt { get; set; }
    }
}
