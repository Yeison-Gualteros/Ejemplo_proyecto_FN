using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Entities.Exceptions
{
    public class Trazabilidad_DocumentoNotFoundException : NotFoundException
    {
        public Trazabilidad_DocumentoNotFoundException(int Id_documento, int Id_Trazabilidad)
            : base($"La trazabilidad del documento con Id_documento: {Id_documento} y Id_Trazabilidad: {Id_Trazabilidad} no fue encontrada.")
        {
        }
    }
}
