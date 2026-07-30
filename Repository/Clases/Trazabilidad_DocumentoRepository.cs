using Contracts.Interface;
using Entities.Models;
using Microsoft.EntityFrameworkCore;
using Shared.RequestFeatures;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Repository.Clases
{
    public class Trazabilidad_DocumentoRepository : RepositoryBase<Trazabilidad_Documento>, ITrazabilidad_DocumentoRepository
    {
        public Trazabilidad_DocumentoRepository(RepositoryContext repositoryContext) : base(repositoryContext)
        {

        }

        public void CreateTrazabilidadDocumento(Trazabilidad_Documento trazabilidad_Documento) => Create(trazabilidad_Documento);

        public Trazabilidad_Documento GetAllTrazabilidad_Documentos(int Id_documento, int Id_Trazabilidad, bool trackChanges) =>
            FindByCondition(t => t.Id_Documento.Equals(Id_documento) && t.Id_Trazabilidad.Equals(Id_Trazabilidad), trackChanges)
            .SingleOrDefault();


        public async Task<IEnumerable<Trazabilidad_Documento>> GetAllByDocumentoAsync(int Id_documento, string userId, bool trackChanges)
        {
            var result = await FindByCondition(
                    td => td.Id_Documento == Id_documento,
                    trackChanges)
                .Include(td => td.User) 
                .OrderBy(td => td.Fecha_Cambio)
                .ToListAsync();

            Console.WriteLine($"Registros obtenidos para documento {Id_documento}: {result.Count}");
            return result;
        }

        public async Task<IEnumerable<Trazabilidad_Documento>> GetTrazabilidadesPorDocumentoAsync(int documentoId, bool trackChanges)
        {
            return await FindByCondition(t => t.Id_Documento == documentoId, trackChanges)
                .OrderBy(t => t.Fecha_Cambio)
                .ToListAsync();
        }

    }
}
