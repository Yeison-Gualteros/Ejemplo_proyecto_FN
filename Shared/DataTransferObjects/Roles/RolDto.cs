using Shared.DataTransferObjects.Permisos;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Shared.DataTransferObjects.Roles
{
    public record class RolDto
    {
        public string? Id { get; init; }
        public string? Name { get; init; }
        public string? NormalizedName { get; init; }
        public List<PermisosDto> Permisos { get; set; } = new();
    }
}
