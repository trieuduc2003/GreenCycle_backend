using System;

namespace GreenCycle.Application.DTOs.Order
{
    public class CreateOrderResponseDto
    {
        public int OrderId { get; set; }
        public int MethodId { get; set; }
        public string MethodName { get; set; } = null!;
        public int StatusId { get; set; }
        public string StatusName { get; set; } = null!;
        public decimal TotalEstimatedAmount { get; set; }
        public decimal PlatformFee { get; set; }
        public decimal NetEstimatedAmount { get; set; }
        public DateTime CreatedAt { get; set; }
    }
}
