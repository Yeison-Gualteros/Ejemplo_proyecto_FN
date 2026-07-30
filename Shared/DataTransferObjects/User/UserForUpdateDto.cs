using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Shared.DataTransferObjects.User
{
    public class UserForUpdateDto
    {
        public string? UserName { get; set; }
        public string? Email { get; set; }
        public string? Nombre { get; set; }
        public string? Apellido { get; set; }
        public string? Password { get; set; }
        public List<string>? RoleIds { get; init; }
        public int? Estado { get; set; }
        public string? IdProceso { get; set; }
        public List<int> Permisos { get; set; } = new List<int>();
        public List<int>? ExtraPermissionIds { get; set; }
    }
}
