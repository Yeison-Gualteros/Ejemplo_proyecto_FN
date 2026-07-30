using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Shared.RequestFeatures
{
    public class PermisoParameters : RequestParameters
    {
        public PermisoParameters() 
        {
            Orden = "Nombre";        
            Direccion = "asc";       
        }

        public string? Busqueda { get; set; }
        public string Orden { get; set; }       
        public string Direccion { get; set; }
    }
}
