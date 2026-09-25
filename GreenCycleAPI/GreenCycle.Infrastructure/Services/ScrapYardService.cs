using GreenCycle.Application.DTOs.Yard;
using GreenCycle.Application.Interfaces.Services;
using GreenCycle.Infrastructure.Data;
using Microsoft.EntityFrameworkCore;
using NetTopologySuite;
using NetTopologySuite.Geometries;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using GreenCycle.Domain.Entities;

namespace GreenCycle.Infrastructure.Services
{
    public class ScrapYardService : IScrapYardService
    {
        private readonly GreenCycleDbContext _context;

        public ScrapYardService(GreenCycleDbContext context)
        {
            _context = context;
        }

        public async Task<List<ScrapYardDto>> GetNearbyYardsAsync(double latitude, double longitude, double radiusMeters, int limit)
        {
            // Create a Point for the user's location (SRID 4326 for WGS84 GPS coords)
            var geometryFactory = NtsGeometryServices.Instance.CreateGeometryFactory(srid: 4326);
            // Note: NTS Point takes (Longitude, Latitude)
            var userLocation = geometryFactory.CreatePoint(new Coordinate(longitude, latitude));

            // Fetch yards within radius, ordered by distance
            var yards = await _context.ScrapYards
                .Where(y => y.Location != null && y.Location.IsWithinDistance(userLocation, radiusMeters))
                .OrderBy(y => y.Location.Distance(userLocation))
                .Take(limit)
                .Select(y => new
                {
                    y.YardId,
                    y.ScrapYardName,
                    y.Address,
                    y.Location,
                    DistanceMeters = y.Location.Distance(userLocation),
                    IsOpening = y.IsOpening ?? false
                })
                .ToListAsync();

            var result = new List<ScrapYardDto>();
            foreach (var y in yards)
            {
                var point = y.Location as Point;
                result.Add(new ScrapYardDto
                {
                    YardId = y.YardId,
                    Name = y.ScrapYardName,
                    Address = y.Address,
                    Latitude = point?.Y ?? 0,
                    Longitude = point?.X ?? 0,
                    DistanceMeters = y.DistanceMeters,
                    DistanceFormatted = FormatDistance(y.DistanceMeters),
                    IsOpening = y.IsOpening
                });
            }

            return result;
        }

        private string FormatDistance(double distanceInMeters)
        {
            if (distanceInMeters < 1000)
            {
                return $"{Math.Round(distanceInMeters)}m";
            }
            return $"{Math.Round(distanceInMeters / 1000.0, 1)}km";
        }

        public async Task<bool> ToggleYardStatusAsync(int userId, bool isOpening)
        {
            var yard = await _context.ScrapYards.FirstOrDefaultAsync(y => y.UserId == userId);
            if (yard == null) throw new Exception("Không tìm thấy vựa của bạn.");

            yard.IsOpening = isOpening;
            await _context.SaveChangesAsync();
            return true;
        }

        public async Task<YardStatsDto> GetYardStatsTodayAsync(int userId)
        {
            var yard = await _context.ScrapYards.FirstOrDefaultAsync(y => y.UserId == userId);
            if (yard == null) throw new Exception("Không tìm thấy vựa của bạn.");

            var collector = await _context.Collectors.FirstOrDefaultAsync(c => c.UserId == userId);

            var today = DateTime.UtcNow.Date;
            
            var dropOffOrders = await _context.DropOffOrders
                .Include(d => d.Order)
                .ThenInclude(o => o.OrderDetails)
                .Where(d => d.YardId == yard.YardId 
                       && d.Order.StatusId == 4 // 4 = Completed
                       && d.Order.CreatedAt >= today)
                .Select(d => d.Order)
                .ToListAsync();

            var pickUpOrders = collector != null ? await _context.PickUpOrders
                .Include(p => p.Order)
                .ThenInclude(o => o.OrderDetails)
                .Where(p => p.CollectorId == collector.CollectorId
                       && p.Order.StatusId == 4
                       && p.Order.CreatedAt >= today)
                .Select(p => p.Order)
                .ToListAsync() : new List<Order>();

            var totalCustomers = dropOffOrders.Select(o => o.SellerId)
                .Concat(pickUpOrders.Select(o => o.SellerId))
                .Distinct().Count();
            
            var dropOffKg = dropOffOrders.SelectMany(o => o.OrderDetails)
                .Where(od => od.ActualWeight.HasValue)
                .Sum(od => od.ActualWeight.Value);
            var dropOffRevenue = dropOffOrders.Sum(o => o.TotalActualAmount ?? 0);

            var pickUpKg = pickUpOrders.SelectMany(o => o.OrderDetails)
                .Where(od => od.ActualWeight.HasValue)
                .Sum(od => od.ActualWeight.Value);
            var pickUpRevenue = pickUpOrders.Sum(o => o.TotalActualAmount ?? 0);

            return new YardStatsDto
            {
                TotalCustomers = totalCustomers,
                DropOffKg = (double)dropOffKg,
                DropOffRevenue = (double)dropOffRevenue,
                PickUpKg = (double)pickUpKg,
                PickUpRevenue = (double)pickUpRevenue,
                TotalKgCollected = (double)(dropOffKg + pickUpKg),
                TotalRevenue = (double)(dropOffRevenue + pickUpRevenue)
            };
        }

        public async Task<List<YardChartDataDto>> GetYardChartStatsAsync(int userId, int? month, int? year)
        {
            var yard = await _context.ScrapYards.FirstOrDefaultAsync(y => y.UserId == userId);
            if (yard == null) throw new Exception("Không tìm thấy vựa của bạn.");

            var collector = await _context.Collectors.FirstOrDefaultAsync(c => c.UserId == userId);
            
            var result = new List<YardChartDataDto>();
            var today = DateTime.UtcNow.Date;

            if (month.HasValue && year.HasValue)
            {
                // Lấy dữ liệu theo từng ngày trong tháng
                int daysInMonth = DateTime.DaysInMonth(year.Value, month.Value);
                for (int i = 1; i <= daysInMonth; i++)
                {
                    var targetDate = new DateTime(year.Value, month.Value, i, 0, 0, 0, DateTimeKind.Utc);
                    var nextDate = targetDate.AddDays(1);

                    var dropOffOrders = await _context.DropOffOrders
                        .Include(d => d.Order)
                            .ThenInclude(o => o.OrderDetails)
                        .Where(d => d.YardId == yard.YardId
                                 && d.Order.StatusId == 4
                                 && d.Order.CreatedAt >= targetDate
                                 && d.Order.CreatedAt < nextDate)
                        .Select(d => d.Order)
                        .ToListAsync();

                    var pickUpOrders = collector != null ? await _context.PickUpOrders
                        .Include(p => p.Order)
                        .ThenInclude(o => o.OrderDetails)
                        .Where(p => p.CollectorId == collector.CollectorId
                                 && p.Order.StatusId == 4
                                 && p.Order.CreatedAt >= targetDate
                                 && p.Order.CreatedAt < nextDate)
                        .Select(p => p.Order)
                        .ToListAsync() : new List<Order>();

                    var dropOffKg = dropOffOrders.SelectMany(o => o.OrderDetails).Where(od => od.ActualWeight.HasValue).Sum(od => od.ActualWeight.Value);
                    var dropOffRevenue = dropOffOrders.Sum(o => o.TotalActualAmount ?? 0);

                    var pickUpKg = pickUpOrders.SelectMany(o => o.OrderDetails).Where(od => od.ActualWeight.HasValue).Sum(od => od.ActualWeight.Value);
                    var pickUpRevenue = pickUpOrders.Sum(o => o.TotalActualAmount ?? 0);

                    result.Add(new YardChartDataDto
                    {
                        Date = targetDate.ToString("dd/MM"),
                        DropOffKg = (double)dropOffKg,
                        DropOffRevenue = (double)dropOffRevenue,
                        PickUpKg = (double)pickUpKg,
                        PickUpRevenue = (double)pickUpRevenue,
                        TotalKg = (double)(dropOffKg + pickUpKg),
                        TotalRevenue = (double)(dropOffRevenue + pickUpRevenue)
                    });
                }
            }
            else
            {
                // Lấy dữ liệu 7 ngày gần nhất (như cũ)
                for (int i = 6; i >= 0; i--)
                {
                    var targetDate = today.AddDays(-i);
                    var nextDate = targetDate.AddDays(1);

                    var dropOffOrders = await _context.DropOffOrders
                        .Include(d => d.Order)
                            .ThenInclude(o => o.OrderDetails)
                        .Where(d => d.YardId == yard.YardId
                                 && d.Order.StatusId == 4
                                 && d.Order.CreatedAt >= targetDate
                                 && d.Order.CreatedAt < nextDate)
                        .Select(d => d.Order)
                        .ToListAsync();

                    var pickUpOrders = collector != null ? await _context.PickUpOrders
                        .Include(p => p.Order)
                        .ThenInclude(o => o.OrderDetails)
                        .Where(p => p.CollectorId == collector.CollectorId
                                 && p.Order.StatusId == 4
                                 && p.Order.CreatedAt >= targetDate
                                 && p.Order.CreatedAt < nextDate)
                        .Select(p => p.Order)
                        .ToListAsync() : new List<Order>();

                    var dropOffKg = dropOffOrders.SelectMany(o => o.OrderDetails).Where(od => od.ActualWeight.HasValue).Sum(od => od.ActualWeight.Value);
                    var dropOffRevenue = dropOffOrders.Sum(o => o.TotalActualAmount ?? 0);

                    var pickUpKg = pickUpOrders.SelectMany(o => o.OrderDetails).Where(od => od.ActualWeight.HasValue).Sum(od => od.ActualWeight.Value);
                    var pickUpRevenue = pickUpOrders.Sum(o => o.TotalActualAmount ?? 0);

                    result.Add(new YardChartDataDto
                    {
                        Date = targetDate.ToString("dd/MM"),
                        DropOffKg = (double)dropOffKg,
                        DropOffRevenue = (double)dropOffRevenue,
                        PickUpKg = (double)pickUpKg,
                        PickUpRevenue = (double)pickUpRevenue,
                        TotalKg = (double)(dropOffKg + pickUpKg),
                        TotalRevenue = (double)(dropOffRevenue + pickUpRevenue)
                    });
                }
            }

            return result;
        }

        public async Task<List<YardActivityDto>> GetRecentActivitiesAsync(int userId)
        {
            var yard = await _context.ScrapYards.FirstOrDefaultAsync(y => y.UserId == userId);
            if (yard == null) throw new Exception("Không tìm thấy vựa của bạn.");

            var collector = await _context.Collectors.FirstOrDefaultAsync(c => c.UserId == userId);

            var dropOffOrders = await _context.DropOffOrders
                .Include(d => d.Order).ThenInclude(o => o.Seller)
                .Include(d => d.Order).ThenInclude(o => o.OrderDetails).ThenInclude(od => od.Category)
                .Where(d => d.YardId == yard.YardId && d.Order.StatusId == 4)
                .Select(d => new { Order = d.Order, Type = "Drop-off" })
                .ToListAsync();

            var pickUpOrdersDynamic = new List<dynamic>();
            if (collector != null) 
            {
                var pickUpOrders = await _context.PickUpOrders
                    .Include(p => p.Order).ThenInclude(o => o.Seller)
                    .Include(p => p.Order).ThenInclude(o => o.OrderDetails).ThenInclude(od => od.Category)
                    .Where(p => p.CollectorId == collector.CollectorId && p.Order.StatusId == 4)
                    .Select(p => new { Order = p.Order, Type = "Pick-up" })
                    .ToListAsync();
                pickUpOrdersDynamic = pickUpOrders.Cast<dynamic>().ToList();
            }

            var recentOrders = dropOffOrders.Cast<dynamic>()
                .Concat(pickUpOrdersDynamic)
                .OrderByDescending(x => (DateTime?)x.Order.UpdatedAt ?? (DateTime?)x.Order.CreatedAt ?? DateTime.UtcNow)
                .Take(5)
                .ToList();

            var result = new List<YardActivityDto>();
            foreach(var d in recentOrders)
            {
                var o = d.Order as Order;
                var type = d.Type as string;
                if (o == null) continue;

                var wasteDesc = string.Join(", ", o.OrderDetails.Where(od => od.ActualWeight.HasValue).Select(od => $"{od.ActualWeight} {od.Category.Unit} {od.Category.Name}"));
                if (string.IsNullOrEmpty(wasteDesc)) wasteDesc = "Không xác định";

                // Giả sử 1 GP = 10 VND
                var points = (o.TotalActualAmount ?? 0) / 10.0M;
                
                var timeSpan = DateTime.UtcNow - (o.UpdatedAt ?? o.CreatedAt ?? DateTime.UtcNow);
                string timeAgo = timeSpan.TotalMinutes < 60 ? $"{(int)timeSpan.TotalMinutes} phút trước" : 
                                 timeSpan.TotalHours < 24 ? $"{(int)timeSpan.TotalHours} giờ trước" : 
                                 $"{(int)timeSpan.TotalDays} ngày trước";

                result.Add(new YardActivityDto
                {
                    CustomerName = o.Seller?.FullName ?? "Khách hàng",
                    WasteDescription = wasteDesc,
                    PointsAwarded = $"{points:N0} GP",
                    TimeAgo = timeSpan.TotalMinutes < 1 ? "Vừa xong" : timeAgo,
                    OrderType = type
                });
            }

            return result;
        }
        public async Task<YardProfileDto> GetYardProfileAsync(int userId)
        {
            var yard = await _context.ScrapYards.FirstOrDefaultAsync(y => y.UserId == userId);
            if (yard == null) throw new Exception("Không tìm thấy vựa của bạn.");

            return new YardProfileDto
            {
                YardId = yard.YardId,
                ScrapYardName = yard.ScrapYardName,
                Address = yard.Address,
                OperatingHours = yard.OperatingHours,
                IsOpening = yard.IsOpening ?? false,
                Ranking = (double)(yard.Ranking ?? 0),
                Latitude = yard.Location != null && !yard.Location.IsEmpty ? yard.Location.Coordinate.Y : null,
                Longitude = yard.Location != null && !yard.Location.IsEmpty ? yard.Location.Coordinate.X : null
            };
        }

        public async Task<bool> UpdateYardProfileAsync(int userId, string name, string address, string operatingHours, double? latitude = null, double? longitude = null)
        {
            var yard = await _context.ScrapYards.FirstOrDefaultAsync(y => y.UserId == userId);
            if (yard == null) throw new Exception("Không tìm thấy vựa của bạn.");

            yard.ScrapYardName = name;
            yard.Address = address;
            yard.OperatingHours = operatingHours;

            if (latitude.HasValue && longitude.HasValue)
            {
                yard.Location = new NetTopologySuite.Geometries.Point(longitude.Value, latitude.Value) { SRID = 4326 };
            }

            await _context.SaveChangesAsync();
            return true;
        }
    }
}
