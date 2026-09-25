namespace GreenCycle.Application.DTOs.Auth
{
    /// <summary>Request cập nhật thông tin hồ sơ cá nhân.</summary>
    public class UpdateProfileRequestDto
    {
        public string FullName { get; set; } = string.Empty;
        public string? Phone { get; set; }
        public string? Email { get; set; }
    }
}
