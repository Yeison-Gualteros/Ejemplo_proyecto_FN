using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Shared.DataTransferObjects.Permisos
{
    public class SaveUserPermisosDto
    {
        public string? UserId { get; set; }
        public List<int> PermisosIds { get; set; } = new();
    }
}
