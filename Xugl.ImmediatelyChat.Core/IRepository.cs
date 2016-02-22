using System;
using System.Collections.Generic;
using System.Linq;
using System.Linq.Expressions;
using System.Text;
using System.Threading.Tasks;

namespace Xugl.ImmediatelyChat.Core
{
    public interface IRepository<T>
    {
        T Find(params object[] keyValues);

        T Find(Expression<Func<T, bool>> predicate);

        int Insert(T TEntity);

        int BatchInsert(IList<T> entitys);

        int Upade(T TEntity);

        int Delete(T TEntity);

        IQueryable<T> Table { get; }

    }
}
