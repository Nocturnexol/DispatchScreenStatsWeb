﻿using System;
using System.Collections.Generic;
using System.Configuration;
using System.Linq;
using System.Linq.Expressions;
using DispatchScreenStats.IRepository;
using MongoDB.Bson;
using MongoDB.Driver;

namespace DispatchScreenStats.Repository
{
    public class MongoRepository<T> : IMongoRepository<T> where T : class
    {
        private readonly IMongoCollection<T> _collection;

        public MongoRepository(string collectionName = null)
        {
            _collection = GetCollection(collectionName);
        }

        private static IMongoCollection<T> GetCollection(string collectionName)
        {
            var mongoUrl = new MongoUrl(ConfigurationManager.AppSettings["mongo"]);
            var mongoClient = new MongoClient(mongoUrl);
            var database = mongoClient.GetDatabase(mongoUrl.DatabaseName);
            return database.GetCollection<T>(collectionName ?? typeof(T).Name);
        }

        public T Get(Expression<Func<T, bool>> filter)
        {
            return _collection.Find(filter).FirstOrDefault();
        }

        public T Get(FilterDefinition<T> filter)
        {
            return _collection.Find(filter).FirstOrDefault();
        }

        public IFindFluent<T, T> Find(Expression<Func<T, bool>> filter)
        {
            return _collection.Find(filter ?? new FilterDefinitionBuilder<T>().Empty);
        }
        public IFindFluent<T, T> Find(FilterDefinition<T> filter)
        {
            return _collection.Find(filter ?? new FilterDefinitionBuilder<T>().Empty);
        }

        public List<TField> Distinct<TField>(Expression<Func<T, TField>> field, Expression<Func<T, bool>> filter)
        {
            var queryFilter = filter ?? new FilterDefinitionBuilder<T>().Empty;
            return _collection.Distinct(field, queryFilter).ToList();
        }

        public object Max(Expression<Func<T, object>> sort)
        {
            var res = _collection.Find(new BsonDocument()).SortByDescending(sort).Limit(1);
            return res.ToList().Select(sort.Compile()).FirstOrDefault();
        }

        public List<T> QueryByPage(int pageIndex, int pageSize, out int rowCount, FilterDefinition<T> filter,
            SortDefinition<T> sort)
        {
            var queryFilter = filter ?? new FilterDefinitionBuilder<T>().Empty;
            rowCount = Convert.ToInt32(_collection.Count(queryFilter));
            var res = _collection.Find(queryFilter);
            if (sort != null)
                res = res.Sort(sort);
            res = res.Skip(pageSize * pageIndex).Limit(pageSize);
            return res.ToList();
        }

        public void Add(T model)
        {
            var propId = typeof(T).GetProperty("_id");
            if (propId != null)
            {
                var typeId = propId.PropertyType;
                if (typeId == typeof(int))
                {
                    var param = Expression.Parameter(typeof(T));
                    var prop = Expression.Property(param, typeof(T), "_id");
                    var id = Max(Expression.Lambda<Func<T, object>>(Expression.Convert(prop, typeof(object)), param));
                    propId.SetValue(model, (int)(id ?? 0) + 1);
                }
            }
            var propRid = typeof(T).GetProperty("Rid");
            if (propRid != null)
            {
                var typeId = propRid.PropertyType;
                if (typeId == typeof(int))
                {
                    var param = Expression.Parameter(typeof(T));
                    var prop = Expression.Property(param, typeof(T), "Rid");
                    var id = Max(Expression.Lambda<Func<T, object>>(Expression.Convert(prop, typeof(object)), param));
                    propRid.SetValue(model, (int)(id ?? 0) + 1);
                }
            }
            _collection.InsertOne(model);
        }

        public void BulkInsert(List<T> list)
        {
            _collection.InsertMany(list);
        }
        public T Update(Expression<Func<T, bool>> filter, UpdateDefinition<T> update)
        {
            var res = _collection.FindOneAndUpdate(filter, update);
            return res;
        }

        public long UpdateMany(Expression<Func<T, bool>> filter, UpdateDefinition<T> update)
        {
            return _collection.UpdateMany(filter, update).ModifiedCount;
        }
        public T Update(FilterDefinition<T> filter, UpdateDefinition<T> update)
        {
            var res = _collection.FindOneAndUpdate(filter, update);
            return res;
        }
        public long Delete(Expression<Func<T, bool>> filter)
        {
            var res = _collection.DeleteMany(filter);
            return res.IsAcknowledged ? res.DeletedCount : 0;
        }
        public long Delete(FilterDefinition<T> filter)
        {
            var res = _collection.DeleteMany(filter);
            return res.IsAcknowledged ? res.DeletedCount : 0;
        }
    }
}