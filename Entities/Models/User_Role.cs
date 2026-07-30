using Microsoft.AspNetCore.Identity;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Entities.Models
{
    public class User_Role 
    {
        public string UserId { get; set; }
        public virtual User User { get; set; }

        public string RoleId { get; set; }
        public virtual Role Role { get; set; }
    }
}
