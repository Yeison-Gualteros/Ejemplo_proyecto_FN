using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Entities.Exceptions
{
    public sealed class UserNotFoundException : NotFoundException
    {
        public UserNotFoundException(string Id_User) : base($"El documento con Id_User: {Id_User} no fue encontrado.")
        {
        }
    }
}
