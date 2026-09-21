namespace GreenCycle.Application.DTOs.Order
{
    public class WasteCategoryDto
    {
        public int CategoryId { get; set; }
        public string Name { get; set; } = null!;
        public string Unit { get; set; } = null!; // "kg" or "món"
        public decimal UnitPrice { get; set; } // Points per unit
        public decimal? Co2ReductionFactor { get; set; }
        public string? IconName { get; set; }
    }
}
