using System.Collections.Generic;

namespace GreenCycle.Application.DTOs.Transaction
{
    public class ActualWeightDto
    {
        public int OrderDetailId { get; set; }
        public decimal ActualWeight { get; set; }
    }

    public class InitiateTransactionRequestDto
    {
        public int OrderId { get; set; }
        public List<ActualWeightDto> ActualWeights { get; set; } = new();
    }

    public class DoubleConfirmationPayloadDto
    {
        public int OrderId { get; set; }
        public string YardName { get; set; } = null!;
        public decimal TotalActualAmount { get; set; } // VND
        public decimal PlatformFee { get; set; } // VND
        public decimal NetGreenPoints { get; set; } // Điểm thực nhận
    }

    public class ConfirmTransactionRequestDto
    {
        public int OrderId { get; set; }
    }
}
