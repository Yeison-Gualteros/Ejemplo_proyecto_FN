using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Shared.DataTransferObjects.Roles
{
    public class RolForCreationDto
    {
        public string? Name { get; set; }
        public List<int> Permisos { get; set; } = new List<int>();

    }
}
