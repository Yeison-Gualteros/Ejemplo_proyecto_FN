using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Entities.Exceptions
{
    public sealed class CollectionByIdsBadRequestException:BadRequestException
    {
        public CollectionByIdsBadRequestException():base("El número de colecciones no coincide con los ids")
        {

        }
    }
}
