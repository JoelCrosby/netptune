using Microsoft.EntityFrameworkCore;
using DataPlane.Models;
using System.Linq;
using Microsoft.AspNetCore.Identity.EntityFrameworkCore;
using System.Threading.Tasks;
using System;
using System.Collections.Generic;
using System.Threading;
using DataPlane.Interfaces;
using DataPlane.Models.Relationships;

namespace DataPlane.Entites
{
    public static class Extensions
    {
        public static IQueryable<T> IncludeBaseObjects<T>(this IQueryable<T> queryable) where T : class
        {
            if (queryable is IQueryable<IBaseEntity> dbSet)
            {
                return dbSet.Include(m => m.CreatedByUser)
                        .Include(m => m.ModifiedByUser)
                        .Include(m => m.Owner)
                        .Include(m => m.DeletedByUser) as IQueryable<T>;

            }

            return null;
        }
    }
}