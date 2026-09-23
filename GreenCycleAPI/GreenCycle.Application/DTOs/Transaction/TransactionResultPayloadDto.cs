namespace GreenCycle.Application.DTOs.Transaction
{
    public class TransactionResultPayloadDto
    {
        public int OrderId { get; set; }
        public bool IsConfirmed { get; set; }
        public string Message { get; set; } = string.Empty;
    }
}
