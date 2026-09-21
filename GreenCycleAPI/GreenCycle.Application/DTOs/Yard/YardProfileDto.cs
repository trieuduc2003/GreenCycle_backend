namespace GreenCycle.Application.DTOs.Yard
{
    public class YardProfileDto
    {
        public int YardId { get; set; }
        public string ScrapYardName { get; set; } = string.Empty;
        public string Address { get; set; } = string.Empty;
        public string? OperatingHours { get; set; }
        public bool IsOpening { get; set; }
        public double Ranking { get; set; }
        public double? Latitude { get; set; }
        public double? Longitude { get; set; }
    }
}
