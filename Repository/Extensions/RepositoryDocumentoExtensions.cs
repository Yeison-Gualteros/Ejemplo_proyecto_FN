using Entities.Models;
using System.Linq.Dynamic.Core;
using System.Reflection;
using System.Text;

namespace Repository.Extensions
{
    public static class RepositoryDocumentoExtensions
    {
        public static IQueryable<Documento> FilterDocumento(this IQueryable<Documento> documentos, DateTime minFecha, DateTime maxFecha)=>
            documentos.Where(d=> (d.Fecha_Creacion >= minFecha && d.Fecha_Creacion <= maxFecha));

        public static IQueryable<Documento> Search(this IQueryable<Documento> documentos, string? busqueda)
        {
            if (string.IsNullOrWhiteSpace(busqueda))
                return documentos;

            var terminoEnMinuscula = busqueda.Trim().ToLower();
            return documentos.Where(d => d.Nombre!.ToLower().Contains(terminoEnMinuscula) || (d.Descripcion != null && d.Descripcion.ToLower().Contains(terminoEnMinuscula)));
        }

        public static IQueryable<Documento> Sort(this IQueryable<Documento> documentos, string orden, string direccion)
        {
            if (string.IsNullOrWhiteSpace(orden))
                return documentos.OrderBy(e => e.Fecha_Creacion);

            var propiedadInfos = typeof(Documento)
                .GetProperties(BindingFlags.Public | BindingFlags.Instance);

            var propiedad = propiedadInfos.FirstOrDefault(pi =>
                pi.Name.Equals(orden, StringComparison.InvariantCultureIgnoreCase));

            if (propiedad == null)
                return documentos.OrderBy(e => e.Fecha_Creacion);

            var esDesc = direccion?.ToLower() == "desc";

            var ordenFinal = esDesc
                ? $"{propiedad.Name} descending"
                : $"{propiedad.Name} ascending";

            return documentos.OrderBy(ordenFinal);
        }
    }
}