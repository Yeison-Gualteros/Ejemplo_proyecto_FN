using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Entities.Models
{
    public class Permiso
    {
        [Key]
        public int Id_Permiso { get; set; }

        [Required]
        public string Nombre { get; set; } = string.Empty;

        public bool Heredado { get; set; }
        public bool Selected { get; set; }
        public bool Disabled { get; set; }

        public ICollection<Rol_Permiso> Rol_Permisos { get; set; } = new List<Rol_Permiso>();
        public ICollection<User_Permiso> User_Permisos { get; set; } = new List<User_Permiso>();
        public virtual ICollection<Role> Roles { get; set; }
    }
}
