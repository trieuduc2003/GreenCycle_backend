namespace GreenCycle.Application.DTOs.Voucher
{
    /// <summary>Thông tin voucher hiển thị trong cửa hàng đổi quà.</summary>
    public class VoucherDto
    {
        public int VoucherId { get; set; }
        public string Title { get; set; } = string.Empty;
        public string? Description { get; set; }
        public decimal PointCost { get; set; }
        public decimal DiscountValue { get; set; }
        public int? Quantity { get; set; }
        public bool? IsActive { get; set; }
        public DateTime ExpiredAt { get; set; }

        /// <summary>Danh mục: F&B, Mua sắm, Sống xanh, Giải trí, Vận chuyển.</summary>
        public string Category { get; set; } = "Khác";
    }
}
