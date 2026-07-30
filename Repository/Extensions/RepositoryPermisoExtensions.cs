using Microsoft.AspNetCore.Identity;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Linq.Dynamic.Core;
using Entities.Models;


namespace Repository.Extensions
{
    public static class RepositoryPermisoExtensions
    {
        public static IQueryable<Permiso> FilteRole(this IQueryable<Permiso> permiso) =>
            permiso.Where(p => true);

        public static IQueryable<Permiso> SearchRole(this IQueryable<Permiso> permiso, string searchTerm)
        {
            if (string.IsNullOrWhiteSpace(searchTerm))
                return permiso;

            var lowerTerm = searchTerm.ToLower();
            return permiso.Where(p => p.Nombre.ToLower().Contains(lowerTerm));
        }

        public static IQueryable<Permiso> SortPermiso(this IQueryable<Permiso> permisos, string orderByQueryString)
        {
            if (string.IsNullOrWhiteSpace(orderByQueryString))
                return permisos.OrderBy(p => p.Nombre);

            var orderParams = orderByQueryString.Trim().Split(',');
            var propertyInfos = typeof(Permiso)
                .GetProperties(System.Reflection.BindingFlags.Public | System.Reflection.BindingFlags.Instance);

            var orderQueryBuilder = new StringBuilder();

            foreach (var param in orderParams)
            {
                if (string.IsNullOrWhiteSpace(param))
                    continue;

                var propertyName = param.Split(" ")[ 0 ];

                var property = propertyInfos.FirstOrDefault(pi =>
                    pi.Name.Equals(propertyName, StringComparison.InvariantCultureIgnoreCase));

                if (property == null)
                    continue;

                var direction = param.EndsWith(" desc", StringComparison.InvariantCultureIgnoreCase)
                    ? "desc"
                    : "asc";

                orderQueryBuilder.Append($"{property.Name} {direction}, ");
            }

            var orderQuery = orderQueryBuilder.ToString().TrimEnd(',', ' ');

            if (string.IsNullOrWhiteSpace(orderQuery))
                return permisos.OrderBy(p => p.Nombre);

            return permisos.OrderBy(orderQuery);

        }
    }
}