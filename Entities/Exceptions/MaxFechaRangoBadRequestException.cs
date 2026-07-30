using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Entities.Exceptions
{
    public sealed class MaxFechaRangoBadRequestException:BadRequestException
    {
        public MaxFechaRangoBadRequestException() : base("La fecha máxima no puede ser menor que la fecha mínima.")
        {

        }
    }
}
