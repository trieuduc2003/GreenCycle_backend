using GreenCycle.Application.DTOs.Auth;

namespace GreenCycle.Application.Interfaces.Services
{
    /// <summary>
    /// Interface quản lý địa chỉ của người dùng.
    /// Tách biệt với IUserService theo nguyên tắc Interface Segregation (ISP).
    /// </summary>
    public interface IUserAddressService
    {
        /// <summary>Lấy danh sách địa chỉ đã lưu của người dùng.</summary>
        Task<List<UserAddressDto>> GetAddressesAsync(int userId);

        /// <summary>Thêm địa chỉ mới (nhập tay hoặc từ GPS).</summary>
        Task<UserAddressDto> AddAddressAsync(int userId, AddAddressRequestDto request);

        /// <summary>Xóa địa chỉ theo ID.</summary>
        Task DeleteAddressAsync(int userId, int addressId);

        /// <summary>Đặt địa chỉ làm mặc định.</summary>
        Task SetDefaultAddressAsync(int userId, int addressId);
    }
}
