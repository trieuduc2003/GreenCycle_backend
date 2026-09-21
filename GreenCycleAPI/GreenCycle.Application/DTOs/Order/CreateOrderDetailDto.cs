namespace GreenCycle.Application.DTOs.Order
{
    public class CreateOrderDetailDto
    {
        public int CategoryId { get; set; }
        public decimal EstimatedWeight { get; set; } // Estimated quantity (kg or item count)
    }
}
