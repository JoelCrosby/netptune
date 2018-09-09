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
        public static void IncludeBaseObjects<T>(this IQueryable<T> queryable) where T : class
        {
            if (queryable is IQueryable<IBaseEntity> dbSet)
            {
                dbSet.Include(m => m.CreatedByUser);
                dbSet.Include(m => m.ModifiedByUser);
                dbSet.Include(m => m.Owner);
                dbSet.Include(m => m.DeletedByUser);
            }
        }
    }
}