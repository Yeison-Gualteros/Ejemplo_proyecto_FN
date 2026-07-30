using AutoMapper;
using Contracts;
using Entities.Models;
using Service.Contracts;
using Shared.DataTransferObjects;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Service
{
    public class SolicitudService : ISolicitudService
    {
        private readonly IRepositoryManager _repository;
        private readonly ILoggerManager _logger;
        private readonly IMapper _mapper;

        public SolicitudService(IRepositoryManager repository, ILoggerManager logger, IMapper mapper)
        {
            _repository = repository;
            _logger = logger;
            _mapper = mapper;
        }

        public IEnumerable<SolicitudDTO> GetSolicitudes(int Id_Usuario, bool trackChanges)
        {
            var solicitud = _repository.Solicitud.GetSolicitudes(Id_Usuario, trackChanges);
            if (solicitud is null)
                throw new Exception($"No se encontraron solicitudes para el usuario con ID {Id_Usuario}.");
            var solicitudesFromDb = _repository.Solicitud.GetSolicitudes(Id_Usuario, trackChanges);
            return _mapper.Map<IEnumerable<SolicitudDTO>>(solicitudesFromDb);
        }

        public IEnumerable<SolicitudDTO> GetAllSolicitudes(bool trackChanges)
        {
            var solicitudesFromDb = _repository.Solicitud.GetAllSolicitudes(trackChanges);
            return _mapper.Map<IEnumerable<SolicitudDTO>>(solicitudesFromDb);
        }

        public SolicitudDTO? GetSolicitud(int Id_Usuario, int Id_Solicitud, bool trackChanges)
        {
            var solicitudFromDb = _repository.Solicitud.GetSolicitud(Id_Usuario, Id_Solicitud, trackChanges);
            if (solicitudFromDb is null)
                throw new Exception($"No se encontró la solicitud con ID {Id_Solicitud} para el usuario con ID {Id_Usuario}.");
            return _mapper.Map<SolicitudDTO>(solicitudFromDb);
        }

        public SolicitudDTO? GetSolicitudById(int Id_Solicitud, bool trackChanges)
        {
            var solicitudFromDb = _repository.Solicitud.GetSolicutudById(Id_Solicitud, trackChanges);
            if (solicitudFromDb is null)
                throw new Exception($"No se encontró la solicitud con ID {Id_Solicitud}.");
            return _mapper.Map<SolicitudDTO>(solicitudFromDb);
        }

        public SolicitudDTO CreateSolicitud(int Id_Usuario, SolicitudForCreationDto solicitudForCreation, bool trackChanges)
        {
            var solicitudEntity = _mapper.Map<Solicitud>(solicitudForCreation);
            if (solicitudEntity is null)
                throw new Exception("No se pudo crear la solicitud debido a datos inválidos."); 
            _repository.Solicitud.CreateSolicutud(Id_Usuario);
            _repository.SaveAsync();
            var solicitudToReturn = _mapper.Map<SolicitudDTO>(solicitudEntity);
            return solicitudToReturn;
        }
    }
}   
