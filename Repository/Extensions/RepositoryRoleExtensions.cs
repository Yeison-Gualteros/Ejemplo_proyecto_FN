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
    public static class RepositoryRoleExtensions
    {
        public static IQueryable<Role> FilteRole(this IQueryable<Role> roles) =>
            roles.Where(r => true); 

        public static IQueryable<Role> SearchRole(this IQueryable<Role> roles, string searchTerm)
        {
            if (string.IsNullOrWhiteSpace(searchTerm))
                return roles;

            var lowerTerm = searchTerm.ToLower();
            return roles.Where(r => r.Name.ToLower().Contains(lowerTerm));
        }

        public static IQueryable<Role> SortRole(this IQueryable<Role> roles, string orderByQueryString)
        {
            if (string.IsNullOrWhiteSpace(orderByQueryString))
                return roles.OrderBy(r => r.Name);

            var orderParams = orderByQueryString.Trim().Split(',');
            var propertyInfos = typeof(Role)
                .GetProperties(System.Reflection.BindingFlags.Public | System.Reflection.BindingFlags.Instance);

            var orderQueryBuilder = new StringBuilder();

            foreach (var param in orderParams)
            {
                if (string.IsNullOrWhiteSpace(param))
                    continue;

                var propertyName = param.Split(" ")[0];

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
                return roles.OrderBy(r => r.Name);

            return roles.OrderBy(orderQuery);
        }
    }
}