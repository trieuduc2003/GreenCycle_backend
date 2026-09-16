using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace GreenCycle.Application.DTOs.Auth
{
    public class LoginRequestDto
    {
        public string Phone { get; set; } = null!;
        public string Password { get; set; } = null!;
    }
}
