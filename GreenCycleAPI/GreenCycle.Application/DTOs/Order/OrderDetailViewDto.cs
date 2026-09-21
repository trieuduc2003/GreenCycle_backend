using System;
using System.Collections.Generic;

namespace GreenCycle.Application.DTOs.Order
{
    public class OrderDetailViewDto
    {
        public int OrderId { get; set; }
        public int SellerId { get; set; }
        public string SellerName { get; set; } = string.Empty;
        public string SellerPhone { get; set; } = string.Empty;
        public int MethodId { get; set; }
        public string MethodName { get; set; } = string.Empty;
        public int StatusId { get; set; }
        public string StatusName { get; set; } = string.Empty;
        public decimal TotalEstimatedAmount { get; set; }
        public decimal? TotalActualAmount { get; set; }
        public decimal PlatformFee { get; set; }
        public decimal NetEstimatedAmount { get; set; }
        public decimal? NetActualAmount { get; set; }
        public DateTime CreatedAt { get; set; }
        public DateTime? UpdatedAt { get; set; }

        // Thông tin địa điểm hoặc đối tác tiếp nhận
        public string? ScrapYardName { get; set; }
        public string? ScrapYardAddress { get; set; }
        public string? PickupAddress { get; set; }
        public string? CollectorName { get; set; }
        public string? CollectorPhone { get; set; }
        public string? CollectorVehicleType { get; set; }
        public string? CollectorLicensePlate { get; set; }

        // Tác động môi trường
        public double EstimatedCo2ReducedKg { get; set; }

        // Danh sách từng loại rác chi tiết
        public List<OrderItemDetailDto> Items { get; set; } = new();
    }

    public class OrderItemDetailDto
    {
        public int OrderDetailId { get; set; }
        public int CategoryId { get; set; }
        public string CategoryName { get; set; } = string.Empty;
        public string Unit { get; set; } = string.Empty;
        public decimal EstimatedWeight { get; set; }
        public decimal? ActualWeight { get; set; }
        public decimal UnitPrice { get; set; }
        public decimal EstimatedSubTotal { get; set; }
        public decimal? ActualSubTotal { get; set; }
        public decimal Co2ReductionFactor { get; set; }
    }
}
