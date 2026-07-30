using Entities.Enums;
using Microsoft.AspNetCore.Identity;
using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Entities.Models
{
    public class Rol_Permiso
    {
        public string Id_Rol { get; set; } = null!;
        public Role Role { get; set; } = null!;

        public int Id_Permiso { get; set; }
        public Permiso Permiso { get; set; } = null!;

        public NivelAccesoDocumento Nivel { get; set; }
    }

}
