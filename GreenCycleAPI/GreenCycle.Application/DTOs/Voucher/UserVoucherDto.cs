namespace GreenCycle.Application.DTOs.Voucher
{
    /// <summary>Thông tin voucher đã được người dùng đổi (gắn với UserVoucher).</summary>
    public class UserVoucherDto
    {
        public int UserVoucherId { get; set; }
        public int UserId { get; set; }
        public int VoucherId { get; set; }
        public string VoucherCode { get; set; } = string.Empty;
        public string Title { get; set; } = string.Empty;
        public string? Description { get; set; }
        public decimal PointCost { get; set; }
        public decimal DiscountValue { get; set; }
        public bool IsUsed { get; set; }
        public DateTime? ReceivedAt { get; set; }
        public DateTime? UsedAt { get; set; }
        public DateTime ExpiredAt { get; set; }
        public string Category { get; set; } = "Khác";
    }
}
