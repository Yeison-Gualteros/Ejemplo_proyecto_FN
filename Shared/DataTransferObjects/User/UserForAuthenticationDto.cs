using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Shared.DataTransferObjects.User
{
    public record UserForAuthenticationDto
    {
        [Required(ErrorMessage = "Nombre de usuario es requerido")]
        public string? UserName { get; init; }
        [Required(ErrorMessage = "La contraseña es requerida")]
        public string? Password { get; init; }
    }
}
