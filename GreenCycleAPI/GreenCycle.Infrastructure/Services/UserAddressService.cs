using GreenCycle.Application.DTOs.Auth;
using GreenCycle.Application.Interfaces.Services;
using GreenCycle.Infrastructure.Data;
using Microsoft.EntityFrameworkCore;
using NetTopologySuite.Geometries;

namespace GreenCycle.Infrastructure.Services
{
    /// <summary>
    /// Service quản lý địa chỉ người dùng.
    /// Tách biệt khỏi UserService theo nguyên tắc SRP + ISP (SOLID).
    /// </summary>
    public class UserAddressService : IUserAddressService
    {
        private readonly GreenCycleDbContext _context;
        private static readonly GeometryFactory _geometryFactory =
            new GeometryFactory(new PrecisionModel(), 4326);

        public UserAddressService(GreenCycleDbContext context)
        {
            _context = context;
        }

        // ─── Lấy danh sách địa chỉ ───────────────────────────────────────────
        public async Task<List<UserAddressDto>> GetAddressesAsync(int userId)
        {
            var addresses = await _context.UserAddresses
                .Where(a => a.UserId == userId)
                .OrderByDescending(a => a.IsDefault)
                .ThenByDescending(a => a.CreatedAt)
                .AsNoTracking()
                .ToListAsync();

            return addresses.Select(a => new UserAddressDto
            {
                AddressId    = a.AddressId,
                AddressLabel = a.AddressLabel,
                FullAddress  = a.FullAddress,
                IsDefault    = a.IsDefault ?? false,
                Latitude     = a.Location?.Coordinate?.Y,
                Longitude    = a.Location?.Coordinate?.X,
                CreatedAt    = a.CreatedAt,
            }).ToList();
        }

        // ─── Thêm địa chỉ mới (nhập tay hoặc từ GPS) ─────────────────────────
        public async Task<UserAddressDto> AddAddressAsync(int userId, AddAddressRequestDto request)
        {
            // Nếu đặt default, bỏ default cũ
            if (request.IsDefault)
            {
                var existing = await _context.UserAddresses
                    .Where(a => a.UserId == userId && a.IsDefault == true)
                    .ToListAsync();
                foreach (var ea in existing)
                    ea.IsDefault = false;
            }

            // Nếu là địa chỉ đầu tiên, tự động set default
            var hasAny = await _context.UserAddresses.AnyAsync(a => a.UserId == userId);
            if (!hasAny)
                request.IsDefault = true;

            // Tạo Point từ tọa độ GPS (Longitude = X, Latitude = Y)
            var point = _geometryFactory.CreatePoint(
                new Coordinate(request.Longitude, request.Latitude));

            var address = new Domain.Entities.UserAddress
            {
                UserId       = userId,
                AddressLabel = request.AddressLabel?.Trim(),
                FullAddress  = request.FullAddress.Trim(),
                Location     = point,
                IsDefault    = request.IsDefault,
                CreatedAt    = DateTime.UtcNow,
            };

            _context.UserAddresses.Add(address);
            await _context.SaveChangesAsync();

            return new UserAddressDto
            {
                AddressId    = address.AddressId,
                AddressLabel = address.AddressLabel,
                FullAddress  = address.FullAddress,
                IsDefault    = address.IsDefault ?? false,
                Latitude     = request.Latitude,
                Longitude    = request.Longitude,
                CreatedAt    = address.CreatedAt,
            };
        }

        // ─── Xóa địa chỉ ──────────────────────────────────────────────────────
        public async Task DeleteAddressAsync(int userId, int addressId)
        {
            var address = await _context.UserAddresses
                .FirstOrDefaultAsync(a => a.AddressId == addressId && a.UserId == userId)
                ?? throw new Exception("Không tìm thấy địa chỉ hoặc bạn không có quyền xóa.");

            _context.UserAddresses.Remove(address);
            await _context.SaveChangesAsync();
        }

        // ─── Đặt địa chỉ mặc định ─────────────────────────────────────────────
        public async Task SetDefaultAddressAsync(int userId, int addressId)
        {
            var allAddresses = await _context.UserAddresses
                .Where(a => a.UserId == userId)
                .ToListAsync();

            if (!allAddresses.Any(a => a.AddressId == addressId))
                throw new Exception("Không tìm thấy địa chỉ này trong danh sách của bạn.");

            foreach (var a in allAddresses)
                a.IsDefault = (a.AddressId == addressId);

            await _context.SaveChangesAsync();
        }
    }
}
