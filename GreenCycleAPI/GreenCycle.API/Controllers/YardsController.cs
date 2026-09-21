using GreenCycle.Application.Interfaces.Services;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using System;
using System.Threading.Tasks;
using System.Security.Claims;

namespace GreenCycle.API.Controllers
{
    [ApiController]
    [Route("api/[controller]")]
    [Authorize]
    public class YardsController : ControllerBase
    {
        private readonly IScrapYardService _yardService;

        public YardsController(IScrapYardService yardService)
        {
            _yardService = yardService;
        }

        /// <summary>
        /// Tìm các vựa rác gần vị trí người dùng.
        /// </summary>
        [HttpGet("nearby")]
        public async Task<IActionResult> GetNearbyYards([FromQuery] double lat, [FromQuery] double lng, [FromQuery] double radius = 5000, [FromQuery] int limit = 10)
        {
            try
            {
                var yards = await _yardService.GetNearbyYardsAsync(lat, lng, radius, limit);
                return Ok(new { Success = true, Data = yards });
            }
            catch (Exception ex)
            {
                return BadRequest(new { Success = false, Message = ex.ToString() });
            }
        }
        [HttpPut("status")]
        [Authorize(Roles = "ScrapYard")]
        public async Task<IActionResult> UpdateStatus([FromBody] bool isOpening)
        {
            try
            {
                var userIdStr = User.FindFirstValue(ClaimTypes.NameIdentifier) ?? User.FindFirst("userId")?.Value ?? User.FindFirst("sub")?.Value;
                if (!int.TryParse(userIdStr, out int userId)) return Unauthorized();

                var result = await _yardService.ToggleYardStatusAsync(userId, isOpening);
                return Ok(new { Success = result });
            }
            catch (Exception ex)
            {
                return BadRequest(new { Success = false, Message = ex.Message });
            }
        }

        [HttpGet("stats")]
        [Authorize(Roles = "ScrapYard")]
        public async Task<IActionResult> GetStatsToday()
        {
            try
            {
                var userIdStr = User.FindFirstValue(ClaimTypes.NameIdentifier) ?? User.FindFirst("userId")?.Value ?? User.FindFirst("sub")?.Value;
                if (!int.TryParse(userIdStr, out int userId)) return Unauthorized();

                var stats = await _yardService.GetYardStatsTodayAsync(userId);
                return Ok(new { Success = true, Data = stats });
            }
            catch (Exception ex)
            {
                return BadRequest(new { Success = false, Message = ex.Message });
            }
        }

        [HttpGet("stats/chart")]
        [Authorize(Roles = "ScrapYard")]
        public async Task<IActionResult> GetStatsChart([FromQuery] int? month, [FromQuery] int? year)
        {
            try
            {
                var userIdStr = User.FindFirstValue(ClaimTypes.NameIdentifier) ?? User.FindFirst("userId")?.Value ?? User.FindFirst("sub")?.Value;
                if (!int.TryParse(userIdStr, out int userId)) return Unauthorized();

                var chartData = await _yardService.GetYardChartStatsAsync(userId, month, year);
                return Ok(new { Success = true, Data = chartData });
            }
            catch (Exception ex)
            {
                return BadRequest(new { Success = false, Message = ex.Message });
            }
        }

        [HttpGet("recent-activities")]
        [Authorize(Roles = "ScrapYard")]
        public async Task<IActionResult> GetRecentActivities()
        {
            try
            {
                var userIdStr = User.FindFirstValue(ClaimTypes.NameIdentifier) ?? User.FindFirst("userId")?.Value ?? User.FindFirst("sub")?.Value;
                if (!int.TryParse(userIdStr, out int userId)) return Unauthorized();

                var activities = await _yardService.GetRecentActivitiesAsync(userId);
                return Ok(new { Success = true, Data = activities });
            }
            catch (Exception ex)
            {
                return BadRequest(new { Success = false, Message = ex.Message });
            }
        }
        [HttpGet("profile")]
        [Authorize(Roles = "ScrapYard")]
        public async Task<IActionResult> GetYardProfile()
        {
            try
            {
                var userIdStr = User.FindFirstValue(ClaimTypes.NameIdentifier) ?? User.FindFirst("userId")?.Value ?? User.FindFirst("sub")?.Value;
                if (!int.TryParse(userIdStr, out int userId)) return Unauthorized();

                var profile = await _yardService.GetYardProfileAsync(userId);
                return Ok(new { Success = true, Data = profile });
            }
            catch (Exception ex)
            {
                return BadRequest(new { Success = false, Message = ex.Message });
            }
        }

        public class UpdateYardProfileRequest
        {
            public string Name { get; set; } = null!;
            public string Address { get; set; } = null!;
            public string OperatingHours { get; set; } = null!;
            public double? Latitude { get; set; }
            public double? Longitude { get; set; }
        }

        [HttpPut("profile")]
        [Authorize(Roles = "ScrapYard")]
        public async Task<IActionResult> UpdateYardProfile([FromBody] UpdateYardProfileRequest req)
        {
            try
            {
                var userIdStr = User.FindFirstValue(ClaimTypes.NameIdentifier) ?? User.FindFirst("userId")?.Value ?? User.FindFirst("sub")?.Value;
                if (!int.TryParse(userIdStr, out int userId)) return Unauthorized();

                var success = await _yardService.UpdateYardProfileAsync(userId, req.Name, req.Address, req.OperatingHours, req.Latitude, req.Longitude);
                return Ok(new { Success = success, Message = "Cập nhật thành công." });
            }
            catch (Exception ex)
            {
                return BadRequest(new { Success = false, Message = ex.Message });
            }
        }
    }
}
