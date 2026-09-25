namespace GreenCycle.Application.DTOs.Yard
{
    public class YardStatsDto
    {
        public int TotalCustomers { get; set; }
        
        public double DropOffKg { get; set; }
        public double DropOffRevenue { get; set; }
        
        public double PickUpKg { get; set; }
        public double PickUpRevenue { get; set; }
        
        public double TotalKgCollected { get; set; }
        public double TotalRevenue { get; set; }
    }
}
