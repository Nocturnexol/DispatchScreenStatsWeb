﻿using System;
using System.Collections.Generic;
using System.Linq.Expressions;
using MongoDB.Driver;

namespace DispatchScreenStats.IRepository
{
    public interface IMongoRepository<T> where T : class
    {
        T Get(FilterDefinition<T> filter);
        T Get(Expression<Func<T, bool>> filter);
        IFindFluent<T, T> Find(Expression<Func<T, bool>> filter);
        IFindFluent<T, T> Find(FilterDefinition<T> filter = null);
        List<TField> Distinct<TField>(Expression<Func<T, TField>> field, Expression<Func<T, bool>> filter = null);
        object Max(Expression<Func<T, object>> sort);

        List<T> QueryByPage(int pageIndex, int pageSize, out int rowCount, FilterDefinition<T> filter = null,
            SortDefinition<T> sort = null);
        void Add(T model);
        void BulkInsert(List<T> list);
        T Update(Expression<Func<T, bool>> filter, UpdateDefinition<T> update);
        T Update(FilterDefinition<T> filter, UpdateDefinition<T> update);
        long UpdateMany(Expression<Func<T, bool>> filter, UpdateDefinition<T> update);
        long Delete(Expression<Func<T, bool>> filter);
        long Delete(FilterDefinition<T> filter);
    }
}
