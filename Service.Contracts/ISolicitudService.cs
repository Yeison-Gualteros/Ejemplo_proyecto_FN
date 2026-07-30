using Shared.DataTransferObjects;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Service.Contracts
{
    public interface ISolicitudService
    {
        IEnumerable<SolicitudDTO> GetSolicitudes(int Id_Usuario, bool trackChanges);
        IEnumerable<SolicitudDTO> GetAllSolicitudes(bool trackChanges);
        SolicitudDTO? GetSolicitud(int Id_Usuario, int Id_Solicitud, bool trackChanges);
        SolicitudDTO? GetSolicitudById(int Id_Solicitud, bool trackChanges);
        SolicitudDTO CreateSolicitud(int Id_Usuario, SolicitudForCreationDto solicitudForCreation, bool trackChanges);
    }
}
