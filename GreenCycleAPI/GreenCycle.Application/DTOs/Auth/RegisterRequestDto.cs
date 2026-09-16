namespace GreenCycle.Application.DTOs.Auth
{
    public class RegisterRequestDto
    {
        public string FullName { get; set; } = null!;
        public string Phone { get; set; } = null!;
        public string Email { get; set; } = null!;
        public string Password { get; set; } = null!;
        public int RoleId { get; set; } // 1: Seller, 2: Collector, 3: ScrapYard
        public string OtpCode { get; set; } = null!; // Mã OTP xác thực SĐT
    }
}
