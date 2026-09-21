using System.Collections.Generic;

namespace GreenCycle.Application.DTOs.Order
{
    public class CreateOrderRequestDto
    {
        public int MethodId { get; set; } // 1: Drop-off (Tự mang đi), 2: Pick-up (Gọi thu gom)
        public int? YardId { get; set; } // Selected ScrapYard ID if Drop-off
        public int? PickupAddressId { get; set; } // Selected UserAddress ID if Pick-up
        public List<CreateOrderDetailDto> Details { get; set; } = new();
    }
}
