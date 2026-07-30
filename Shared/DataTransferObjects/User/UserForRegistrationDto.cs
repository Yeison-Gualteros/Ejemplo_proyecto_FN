using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Shared.DataTransferObjects.User
{
    public class UserForRegistrationDto
    {
        public string? Nombre { get; init; }
        public string? Apellido { get; init; }
        [Required(ErrorMessage = "Username is required")]
        public string? UserName { get; init; }
        [Required(ErrorMessage = "Password is required")]
        public string? Password { get; init; }
        public string? Email { get; init; }
        public string? PhoneNumber { get; init; }
        public int Estado { get; init; }
        public List<string> RoleIds { get; set; }
        public string? IdProceso { get; set; }


        public List<int>? Permisos { get; init; } = new List<int>();

    }
}
