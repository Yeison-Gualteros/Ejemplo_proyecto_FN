using Shared.DataTransferObjects;
using Shared.DataTransferObjects.User;
using Shared.RequestFeatures;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Service.Contracts
{
    public interface IUserService
    {
        Task<PagedList<UserDto>> GetAllAsync(UserParameters parameters);
        Task<UserDto> GetByIdAsync(string id);
        Task<ServiceResultDto<UserDto>> CreateAsync(UserForRegistrationDto dto);
        Task<UserDto> UpdateAsync(string id, UserForUpdateDto dto);
        Task UpdateFullAsync(string userId, UserForUpdateFullDto dto);
        Task<object> GetPermisosEdicionAsync(string userId);

        Task<IEnumerable<UserDto>> GetUsuariosConPermiso(string permiso);

        Task<IEnumerable<object>> GetUsuariosConPermisosDashboardAsync();
        Task<IEnumerable<UserDto>> GetUsuariosConPermisoByProcesoAsync(string proceso, string IdProceso);
    }
}
