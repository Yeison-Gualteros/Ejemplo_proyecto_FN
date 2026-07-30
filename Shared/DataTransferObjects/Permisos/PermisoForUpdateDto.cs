using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Shared.DataTransferObjects.Permisos
{
    public class PermisoForUpdateDto
    {
        public int Id_Permiso { get; set; }
        public string Nombre { get; set; } = null!;
    }

}
