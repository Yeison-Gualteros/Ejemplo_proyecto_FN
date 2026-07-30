using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Entities.Exceptions
{
    public sealed class RefreshTokenBadRequest : BadRequestException
    {
        public RefreshTokenBadRequest() : base("Solicitud de cliente no válida. El tokenDto tiene algunos valores no válidos.")
        {
        }
    }
}
