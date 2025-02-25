﻿#nullable enable
using LiteDB;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Linq.Expressions;

namespace Hohoema.Infra;

public abstract class LiteDBServiceBase<T>
{
    protected ILiteCollection<T> _collection;

    public LiteDBServiceBase(LiteDatabase liteDatabase, string? collectionName = null)
    {
        if (collectionName != null)
        {
            _collection = liteDatabase.GetCollection<T>(collectionName);
        }
        else
        {
            _collection = liteDatabase.GetCollection<T>();
        }
    }



    public virtual BsonValue CreateItem(T item)
    {
        return _collection.Insert(item);
    }

    public virtual bool UpdateItem(T item)
    {
        return _collection.Upsert(item);
    }

    public virtual int UpdateItem(IEnumerable<T> items)
    {
        return _collection.Upsert(items);
    }

    public virtual int UpdateMany(Expression<Func<T, T>> extend, Expression<Func<T, bool>> predicate)
    {
        return _collection.UpdateMany(extend, predicate);
    }

    public virtual int InsertBulk(IEnumerable<T> entities, int batchSize = 5000)
    {
        return _collection.InsertBulk(entities, batchSize);
    }

    public virtual bool DeleteItem(T item)
    {
        return _collection.DeleteMany(i => i.Equals(item)) > 0;
    }

    public virtual bool DeleteMany(System.Linq.Expressions.Expression<Func<T, bool>> predicate)
    {
        return _collection.DeleteMany(predicate) > 0;
    }

    public virtual bool DeleteItem(BsonValue id)
    {
        return _collection.Delete(id);
    }

    public virtual List<T> ReadAllItems()
    {
        return _collection.FindAll().ToList();
    }

    public bool Exists(System.Linq.Expressions.Expression<Func<T, bool>> predicate)
    {
        return _collection.Exists(predicate);
    }

    public int CountSafe()
    {
        try
        {
            return _collection.Count();
        }
        catch
        {
            return 0;
        }
    }

    public int CountSafe(System.Linq.Expressions.Expression<Func<T, bool>> predicate)
    {
        try
        {
            return _collection.Count(predicate);
        }
        catch
        {
            return 0;
        }
    }

    public T FindById(BsonValue id)
    {
        return _collection.FindById(id);
    }

    public IEnumerable<T> Find(System.Linq.Expressions.Expression<Func<T, bool>> predicate, int skip = 0, int limit = int.MaxValue)
    {
        return _collection.Find(predicate, skip, limit);
    }

    public IEnumerable<T> Find(Query query, int skip = 0, int limit = int.MaxValue)
    {
        return _collection.Find(query, skip, limit);
    }
}

public static class LiteCollectionExtensions
{
    public static int DeleteMany<T>(this LiteCollection<T> collection, Expression<Func<T, bool>> predicate)
    {
        return collection.DeleteMany(predicate);
    }

    public static int DeleteAll<T>(this LiteCollection<T> collection)
    {
        return collection.DeleteAll();
    }
}
