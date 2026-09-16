using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace GreenCycle.Application.DTOs.Auth
{
    public class AuthResponseDto
    {
        public int UserId { get; set; }
        public string FullName { get; set; } = null!;
        public string Phone { get; set; } = null!;
        public string RoleName { get; set; } = null!;
        public string Token { get; set; } = null!;
    }
}
