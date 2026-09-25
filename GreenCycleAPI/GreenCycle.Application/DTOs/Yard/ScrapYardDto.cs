namespace GreenCycle.Application.DTOs.Yard
{
    public class ScrapYardDto
    {
        public int YardId { get; set; }
        public string Name { get; set; } = string.Empty;
        public string Address { get; set; } = string.Empty;
        public double Latitude { get; set; }
        public double Longitude { get; set; }
        
        /// <summary>Khoảng cách (mét) từ vị trí người dùng đến vựa.</summary>
        public double DistanceMeters { get; set; }
        
        /// <summary>Khoảng cách định dạng chuỗi (VD: 1.2km).</summary>
        public string DistanceFormatted { get; set; } = string.Empty;
        
        public bool IsOpening { get; set; }
    }
}
