using Contracts.Interface;
using Entities.Models;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Repository.Clases
{
    public class SolicitudRepository(RepositoryContext repositoryContext)
            : RepositoryBase<Solicitud>(repositoryContext), ISolicitudRepository
    {
        public IEnumerable<Solicitud> GetAllSolicitudes(bool trackChanges) =>
            FindAll(trackChanges)
            .OrderBy(s => s.Id_Solicitud)
            .ToList();

        public IEnumerable<Solicitud> GetSolicitudes(int Id_Solicitud, bool trackChanges) =>
            FindByCondition(s => s.Id_Solicitud.Equals(Id_Solicitud), trackChanges)
            .OrderBy(s => s.Id_Solicitud)
            .ToList();

        public Solicitud? GetSolicitud(int Id_Usuario, int Id_Solicitud, bool trackChanges) =>
            FindByCondition(s => s.Id_Usuario.Equals(Id_Usuario) && s.Id_Solicitud.Equals(Id_Solicitud), trackChanges)
            .SingleOrDefault();
        public Solicitud? GetSolicutudById(int Id_Solicitud, bool trackChanges) =>
            FindByCondition(s => s.Id_Solicitud.Equals(Id_Solicitud), trackChanges)
            .SingleOrDefault();

        public void CreateSolicutud(int Id_solicitud)
        {
            Solicitud solicitud = new Solicitud();
            solicitud.Id_Solicitud = Id_solicitud;
            Create(solicitud);
        }
    }
}
