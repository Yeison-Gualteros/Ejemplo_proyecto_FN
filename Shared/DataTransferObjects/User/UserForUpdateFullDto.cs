using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Shared.DataTransferObjects.User
{
    public class UserForUpdateFullDto
    {
        // Datos básicos
        public string Nombre { get; set; }
        public string Apellido { get; set; }
        public string Email { get; set; }
        public int Estado { get; set; }

        public string? IdProceso { get; set; }

        // Seguridad
        public string RoleId { get; set; }

        // Permisos extra
        public List<int> PermisosIds { get; set; } = new();
    }


}
