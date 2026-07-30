using Entities.Models;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Contracts.Interface
{
    public interface ISolicitudRepository
    {
        IEnumerable<Solicitud> GetSolicitudes(int Id_Solicitud, bool trackChanges);
        IEnumerable<Solicitud> GetAllSolicitudes(bool trackChanges);

        Solicitud? GetSolicitud(int Id_Usuario, int Id_Solicitud, bool trackChanges);
        Solicitud? GetSolicutudById(int Id_Solicitud, bool trackChanges);

        void CreateSolicutud(int Id_solicitud);


    }
}
