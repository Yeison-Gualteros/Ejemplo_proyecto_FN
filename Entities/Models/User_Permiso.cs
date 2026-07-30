using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Entities.Models
{
    public class User_Permiso
    {
        public string UserId { get; set; } = null!;
        public int Id_Permiso { get; set; }

        public User User { get; set; } = null!;
        public Permiso Permiso { get; set; } = null!;
    }

}
