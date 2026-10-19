namespace GreenCycle.Application.DTOs.Auth
{
    public class ForgotPasswordOtpRequestDto
    {
        public string Email { get; set; } = null!;
    }

    public class ResetPasswordRequestDto
    {
        public string Email { get; set; } = null!;
        public string OtpCode { get; set; } = null!;
        public string NewPassword { get; set; } = null!;
    }
}
