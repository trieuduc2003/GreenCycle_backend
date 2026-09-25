namespace GreenCycle.Application.DTOs.Auth
{
    /// <summary>Thông tin một địa chỉ đã lưu của người dùng (response).</summary>
    public class UserAddressDto
    {
        public int AddressId { get; set; }
        public string? AddressLabel { get; set; }
        public string FullAddress { get; set; } = string.Empty;
        public bool IsDefault { get; set; }
        public double? Latitude { get; set; }
        public double? Longitude { get; set; }
        public DateTime? CreatedAt { get; set; }
    }
}
