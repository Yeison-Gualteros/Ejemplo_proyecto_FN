using AutoMapper;
using Contracts;
using Entities.Models;
using Microsoft.AspNetCore.Identity;
using Service.Contracts;
using Shared.DataTransferObjects.Roles;
using Shared.RequestFeatures;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Service
{
    public class RolService : IRoleService
    {
        private readonly IRepositoryManager _repository;
        private readonly IMapper _mapper;
        private readonly ILoggerManager _logger;
        private readonly UserManager<User> _userManager;

        public RolService(IRepositoryManager repository, IMapper mapper, ILoggerManager logger, UserManager<User> userManager)
        {
            _repository = repository;
            _mapper = mapper;
            _logger = logger;
            _userManager = userManager;
        }

        public async Task<(IEnumerable<RolDto> roles, MetaData metaData)> GetAllRoles(RoleParameters roleParameters, bool trackChanges)
        {

            var rolesWithMetaData = await _repository.Role.GetAllRoles(roleParameters, trackChanges);
            var rolesDto = _mapper.Map<IEnumerable<RolDto>>(rolesWithMetaData);
            return (roles: rolesDto, metaData: rolesWithMetaData.MetaData);
        }

        public async Task<RolDto> GetRoleById(string id, bool trackChanges)
        {
            var roleFromDb = await _repository.Role.GetRoleById(id.ToString(), trackChanges);
            if (roleFromDb is null)
            {
                _logger.LogError($"Role with id: {id} doesn't exist in the database.");
                throw new KeyNotFoundException($"Role with id: {id} doesn't exist in the database.");
            }
            return _mapper.Map<RolDto>(roleFromDb);
        }

        public async Task<RolDto> CreateRol(RolForCreationDto rolForCreation, bool trackChanges)
        {
            var rolEntity = _mapper.Map<Role>(rolForCreation);

            _repository.Role.CreateRol(rolEntity);

            await _repository.SaveAsync();

            return _mapper.Map<RolDto>(rolEntity);
        }

        public async Task DeleteRol(string id, bool trackChanges)
        {
            var roleToDelete = await _repository.Role.GetRoleById(id, trackChanges: true);

            if (roleToDelete == null)
            {
                _logger.LogError($"El rol con id {id} no existe en la base de datos.");
                throw new KeyNotFoundException($"El rol con id {id} no existe en la base de datos.");
            }

            _repository.Role.DeleteRol(roleToDelete);
            await _repository.SaveAsync();
        }

        public async Task<RolDto> ActualizarRol(string id, RolForUpdateDto rolForUpdate, bool trackChanges)
        {
            // Obtener el rol de forma asíncrona
            var rolEntity = await _repository.Role.GetRoleById(id, trackChanges);

            if (rolEntity is null)
                throw new KeyNotFoundException($"Role with id: {id} doesn't exist in the database.");

            // Solo mapear campos simples, evitar sobrescribir relaciones
            rolEntity.Name = rolForUpdate.Name ?? rolEntity.Name;
            rolEntity.NormalizedName = rolForUpdate.Name?.ToUpper() ?? rolEntity.NormalizedName;

            // Guardar cambios en la base de datos
            await _repository.SaveAsync();

            // Retornar el DTO actualizado
            return _mapper.Map<RolDto>(rolEntity);
        }
    }
}
