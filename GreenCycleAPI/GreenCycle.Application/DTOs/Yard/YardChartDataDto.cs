namespace GreenCycle.Application.DTOs.Yard
{
    public class YardChartDataDto
    {
        public string Date { get; set; } = string.Empty;
        public double DropOffKg { get; set; }
        public double DropOffRevenue { get; set; }
        public double PickUpKg { get; set; }
        public double PickUpRevenue { get; set; }
        public double TotalKg { get; set; }
        public double TotalRevenue { get; set; }
    }
}
