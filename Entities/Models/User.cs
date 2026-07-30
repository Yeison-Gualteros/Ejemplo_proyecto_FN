using Microsoft.AspNetCore.Identity;
using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Entities.Models
{
    public class User : IdentityUser
    {
        public string? Nombre { get; set; }
        public string? Apellido { get; set; }
        public int Estado { get; set; }
        public DateTime FechaCreacion { get; set; }
        public override string? Email { get; set; }
        public bool IsLdapUser { get; set; }

        public string? RefreshTokken { get; set; }
        public DateTime RefreshTokenExpiryTime { get; set; }

        public bool MustChangePassword { get; set; } = true;

        public string? IdProceso { get; set; }
        [ForeignKey("IdProceso")]
        public virtual Proceso? Proceso { get; set; }


        public ICollection<Documento>? Documentos { get; set; }
        public ICollection<Trazabilidad_Documento>? Trazabilidad_Documentos { get; set; }
        public ICollection<Solicitud>? Solicitudes { get; set; }
        public ICollection<User_Permiso> User_Permisos { get; set; } = new List<User_Permiso>();
        

    }
}
