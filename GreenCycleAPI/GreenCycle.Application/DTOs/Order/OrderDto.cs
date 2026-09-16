using System;
using System.Collections.Generic;

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

    public class CreateOrderDetailDto
    {
        public int CategoryId { get; set; }
        public decimal EstimatedWeight { get; set; } // Estimated quantity (kg or item count)
    }

    public class CreateOrderRequestDto
    {
        public int MethodId { get; set; } // 1: Drop-off (Tự mang đi), 2: Pick-up (Gọi thu gom)
        public int? YardId { get; set; } // Selected ScrapYard ID if Drop-off
        public int? PickupAddressId { get; set; } // Selected UserAddress ID if Pick-up
        public List<CreateOrderDetailDto> Details { get; set; } = new();
    }

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

    public class OrderSummaryDto
    {
        public int OrderId { get; set; }
        public string MethodName { get; set; } = null!;
        public string StatusName { get; set; } = null!;
        public decimal TotalEstimatedAmount { get; set; }
        public decimal PlatformFee { get; set; }
        public DateTime CreatedAt { get; set; }
        public int ItemsCount { get; set; }
    }

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
