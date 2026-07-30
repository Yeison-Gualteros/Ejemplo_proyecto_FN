using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Entities.Models
{
    public class JwtConfiguration
    {
        public string Section { get; set; } = "JwtSettings";
        public string? Key { get; set; }
        public string? ValidIssuer { get; set; }
        public string? ValidAudience { get; set; }
        public string? Expires { get; set; }
    }
}
