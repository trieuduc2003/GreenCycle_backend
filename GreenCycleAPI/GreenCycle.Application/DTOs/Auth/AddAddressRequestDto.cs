namespace GreenCycle.Application.DTOs.Auth
{
    /// <summary>Request thêm hoặc cập nhật địa chỉ — hỗ trợ cả nhập tay lẫn GPS.</summary>
    public class AddAddressRequestDto
    {
        public string? AddressLabel { get; set; }
        public string FullAddress { get; set; } = string.Empty;

        /// <summary>Vĩ độ (GPS latitude). Mặc định là trung tâm TP.HCM nếu không có GPS.</summary>
        public double Latitude { get; set; } = 10.7769;

        /// <summary>Kinh độ (GPS longitude). Mặc định là trung tâm TP.HCM nếu không có GPS.</summary>
        public double Longitude { get; set; } = 106.7009;

        public bool IsDefault { get; set; } = false;
    }
}
