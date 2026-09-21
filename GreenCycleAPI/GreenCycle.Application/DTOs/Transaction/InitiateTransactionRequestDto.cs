using System.Collections.Generic;

namespace GreenCycle.Application.DTOs.Transaction
{
    public class InitiateTransactionRequestDto
    {
        public int OrderId { get; set; }
        public List<ActualWeightDto> ActualWeights { get; set; } = new();
    }
}
