using System.Collections.Generic;
using System.Threading.Tasks;
using GreenCycle.Application.DTOs.Voucher;

namespace GreenCycle.Application.Interfaces.Services
{
    public interface IVoucherService
    {
        /// <summary>
        /// Lấy danh sách các voucher/quà tặng đang hoạt động trong Reward Store.
        /// </summary>
        Task<List<VoucherDto>> GetAvailableVouchersAsync();

        /// <summary>
        /// Thực hiện đổi voucher bằng điểm GreenPoints của người dùng.
        /// </summary>
        Task<RedeemVoucherResponseDto> RedeemVoucherAsync(int userId, int voucherId);

        /// <summary>
        /// Lấy danh sách các voucher mà người dùng đã đổi ("Kho quà của tôi").
        /// </summary>
        Task<List<UserVoucherDto>> GetMyVouchersAsync(int userId);

        /// <summary>
        /// Đánh dấu voucher đã được sử dụng (tại quầy / đối tác).
        /// </summary>
        Task<bool> UseVoucherAsync(int userId, int userVoucherId);
    }
}
