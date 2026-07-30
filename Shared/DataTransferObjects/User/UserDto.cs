using Shared.DataTransferObjects.Permisos;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Shared.DataTransferObjects.User
{
    public class UserDto
    {
        public string? Id { get; set; }
        public string? UserName { get; set; }
        public string? Email { get; set; }
        public string? Nombre { get; set; }
        public string? Apellido { get; set; }
        public int Estado { get; set; }

        public string? IdProceso { get; set; }
        public string? NombreProceso { get; set; }

        public List<PermisosDto> Permisos { get; set; } = new();
        public string Rol { get; set; }
    }

}
