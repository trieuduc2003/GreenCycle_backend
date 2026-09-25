namespace GreenCycle.Application.DTOs.Voucher
{
    /// <summary>Kết quả sau khi đổi voucher thành công.</summary>
    public class RedeemVoucherResponseDto
    {
        public bool Success { get; set; }
        public string Message { get; set; } = string.Empty;
        public UserVoucherDto? UserVoucher { get; set; }
        public decimal RemainingPoints { get; set; }
        public double TreesSaved { get; set; }
        public double Co2ReducedKg { get; set; }
    }
}
