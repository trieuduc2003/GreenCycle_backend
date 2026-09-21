namespace GreenCycle.Application.DTOs.Order
{
    public class OrderDetailDto
    {
        public int OrderDetailId { get; set; }
        public int CategoryId { get; set; }
        public string CategoryName { get; set; } = null!;
        public string Unit { get; set; } = null!;
        public decimal EstimatedWeight { get; set; }
        public decimal? ActualWeight { get; set; }
        public decimal UnitPrice { get; set; }
    }
}
