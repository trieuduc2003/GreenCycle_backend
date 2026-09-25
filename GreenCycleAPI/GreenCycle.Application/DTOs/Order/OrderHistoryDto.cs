using System;

namespace GreenCycle.Application.DTOs.Order
{
    public class OrderHistoryDto
    {
        public int OrderId { get; set; }
        public string MethodName { get; set; } = null!;
        public string StatusName { get; set; } = null!;
        public decimal TotalEstimatedAmount { get; set; }
        public decimal PlatformFee { get; set; }
        public DateTime CreatedAt { get; set; }
        public int ItemsCount { get; set; }
        public double TotalEstimatedWeight { get; set; }

        
        // Extended info for Pick-up
        public string? SellerName { get; set; }
        public string? PickupAddress { get; set; }
        public double? DistanceKm { get; set; }
        public double? Latitude { get; set; }
        public double? Longitude { get; set; }
    }
}
