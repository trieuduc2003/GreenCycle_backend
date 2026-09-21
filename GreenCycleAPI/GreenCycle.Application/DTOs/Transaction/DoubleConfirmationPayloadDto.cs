namespace GreenCycle.Application.DTOs.Transaction
{
    public class DoubleConfirmationPayloadDto
    {
        public int OrderId { get; set; }

        /// <summary>MethodId: 1 = Drop-off, 2 = Pick-up</summary>
        public int MethodId { get; set; }

        /// <summary>Tên Vựa rác (cho Drop-off)</summary>
        public string YardName { get; set; } = string.Empty;

        /// <summary>Tên tài xế thu gom (cho Pick-up, nullable)</summary>
        public string? CollectorName { get; set; }

        /// <summary>SĐT tài xế (cho Pick-up, nullable)</summary>
        public string? CollectorPhone { get; set; }

        public decimal TotalActualAmount { get; set; }
        public decimal PlatformFee { get; set; }
        public decimal NetGreenPoints { get; set; }
    }
}
