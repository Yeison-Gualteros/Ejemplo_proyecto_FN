using Shared.DataTransferObjects.Roles;
using Shared.RequestFeatures;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Service.Contracts
{
    public interface IRoleService
    {
        Task<(IEnumerable<RolDto> roles, MetaData metaData)> GetAllRoles(RoleParameters roleParameters, bool trackChanges);
        Task<RolDto> GetRoleById(string id, bool trackChanges);

        Task<RolDto> ActualizarRol(string id, RolForUpdateDto rolForUpdate, bool trackChanges);

        Task<RolDto> CreateRol(RolForCreationDto rolForCreation, bool trackChanges);

        Task DeleteRol(string id, bool trackChanges);
    }
}
