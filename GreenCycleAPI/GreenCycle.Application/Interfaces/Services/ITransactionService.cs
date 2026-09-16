using System.Threading.Tasks;
using GreenCycle.Application.DTOs.Common;
using GreenCycle.Application.DTOs.Transaction;

namespace GreenCycle.Application.Interfaces.Services
{
    public interface ITransactionService
    {
        Task<ApiResponse<DoubleConfirmationPayloadDto>> InitiateTransactionAsync(int yardUserId, InitiateTransactionRequestDto request);
        Task<ApiResponse<object>> ConfirmTransactionAsync(int sellerUserId, ConfirmTransactionRequestDto request);
    }
}
