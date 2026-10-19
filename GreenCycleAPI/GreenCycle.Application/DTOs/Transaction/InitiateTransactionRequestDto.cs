using System.Collections.Generic;

namespace GreenCycle.Application.DTOs.Transaction
{
    public class InitiateTransactionRequestDto
    {
        public int OrderId { get; set; }
        public List<ActualWeightDto> ActualWeights { get; set; } = new();

        /// <summary>
        /// True khi người dùng đã nhập lại tay để xác nhận khối lượng vượt hard limit (&gt; 30).
        /// </summary>
        public bool ConfirmedOverLimit { get; set; }
    }
}
