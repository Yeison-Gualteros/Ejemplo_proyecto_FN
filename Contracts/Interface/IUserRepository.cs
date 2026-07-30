using Entities.Models;
using Microsoft.AspNetCore.Identity;
using Shared.RequestFeatures;
using System;
using System.Collections.Generic;
using System.Data;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Contracts.Interface
{
    public interface IUserRepository : IRepositoryBase<User>
    {
        Task<PagedList<User>> GetUsersAsync(UserParameters parameters);
        Task<User?> GetByIdAsync(string userId);
        Task<Role?> GetUserRole(string userId);
        Task<IEnumerable<User>> GetUsuariosConPermisByProcesoAsync(string permiso, string idProceso);

        //Obtiene todos los usuarios activos con permisos directos
        Task <IEnumerable<User>>GetAllActiveUsersWithPermissionsAsync();

    }
}
