using Entities.Models;
using Microsoft.EntityFrameworkCore;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Linq.Dynamic.Core;
using System.Text;
using System.Threading.Tasks;

namespace Repository.Extensions
{
    public static class RepositoryUserExtensions
    {
        public static IQueryable<User> FilteUser(this IQueryable<User> users) =>
            users.Where(u => u.Estado != 0); 
        

        public static IQueryable<User> SearchUser(this IQueryable<User> users, string? searchTerm)
        {
            if (string.IsNullOrWhiteSpace(searchTerm))
                return users;

            var lowerTerm = searchTerm.ToLower();

            return users.Where(u =>
                EF.Functions.Like(u.Nombre!.ToLower(), $"%{lowerTerm}%") ||
                EF.Functions.Like(u.Apellido!.ToLower(), $"%{lowerTerm}%") ||
                EF.Functions.Like(u.Email!.ToLower(), $"%{lowerTerm}%") ||
                EF.Functions.Like(u.UserName!.ToLower(), $"%{lowerTerm}%")
            );
        }

        public static IQueryable<User> SortUser(this IQueryable<User> users, string orderByQueryString)
        {
            if (string.IsNullOrWhiteSpace(orderByQueryString))
                return users.OrderBy(u => u.Nombre);

            try
            {
                var orderParams = orderByQueryString.Trim().Split(',');
                var propertyInfos = typeof(User)
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

                    var direction = param.Trim().EndsWith(" desc", StringComparison.InvariantCultureIgnoreCase)
                        ? "desc"
                        : "asc";

                    orderQueryBuilder.Append($"{property.Name} {direction}, ");
                }

                var orderQuery = orderQueryBuilder.ToString().TrimEnd(',', ' ');

                if (string.IsNullOrWhiteSpace(orderQuery))
                    return users.OrderBy(u => u.Nombre);

                return users.OrderBy(orderQuery);
            }
            catch
            {

                return users.OrderBy(u => u.Nombre);
            }
        }
    }
}