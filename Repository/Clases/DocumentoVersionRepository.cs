using Contracts.Interface;
using Entities.Models;
using Microsoft.EntityFrameworkCore;
using Shared.DataTransferObjects.Documentos;
using Shared.RequestFeatures;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Repository.Clases
{
    public class DocumentoVersionRepository : RepositoryBase<DocumentoVersion>, IDocumentoVersionRepository
    {
        public DocumentoVersionRepository(RepositoryContext repositoryContext)
            : base(repositoryContext)
        {
        }
        public async Task<DocumentoVersion> GetActual(int Id_Documento) =>
            await FindByCondition(v => v.Id_Documento == Id_Documento && v.EsActual, true)
                .OrderByDescending(v => v.Fecha_Creacion)
                .FirstOrDefaultAsync();                 

        public async Task<IEnumerable<DocumentoVersion>> GetByDocumentoId(int idDocumento, bool trackChanges) =>
            await FindByCondition(v => v.Id_Documento == idDocumento, trackChanges)
                .ToListAsync();

        public async Task<DocumentoVersion> GetVersionById(int documentoId, int versionId)
        {
            return await FindByCondition(
                v => v.Id_Documento == documentoId && v.Id_Version == versionId,
                false
            ).SingleOrDefaultAsync();
        }

        public void Create(DocumentoVersion version)
        {
            version.Fecha_Creacion = DateTime.Now;
            base.Create(version);
        }

        public async Task DesactivarVersionesActuales(int idDocumento)
        {
            var versiones = await FindByCondition(
                v => v.Id_Documento == idDocumento && v.EsActual,
                true
            ).ToListAsync();

            foreach (var v in versiones)
            {
                v.EsActual = false;
            }
        }
    }
}
