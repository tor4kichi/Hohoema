using LiteDB;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Linq.Expressions;
using System.Text;
using System.Threading.Tasks;
using Windows.Storage;

namespace Hohoema.Models.Repository
{
    public abstract class LiteDBServiceBase<T>
    {
        protected ILiteCollection<T> _collection;
        private readonly ILiteDatabase _liteDatabase;

        public LiteDBServiceBase(ILiteDatabase liteDatabase)
        {
            _collection = liteDatabase.GetCollection<T>();
            _liteDatabase = liteDatabase;
        }

        public virtual T CreateItem(T item)
        {
            var val = _collection.Insert(item);
            return item;
        }

        public virtual T UpdateItem(T item)
        {
            _collection.Upsert(item);
            return item;
        }

        public virtual int UpdateItem(IEnumerable<T> items)
        {
            return _collection.Upsert(items);
        }

        public virtual bool DeleteItem(T item)
        {
            return _collection.DeleteMany(i => i.Equals(item)) > 0;
        }

        public virtual bool DeleteItem(BsonValue id)
        {
            return _collection.Delete(id);
        }

        public virtual List<T> ReadAllItems()
        {
            var all = _collection.FindAll();
            return new List<T>(all);
        }

        public bool Exists(System.Linq.Expressions.Expression<Func<T, bool>> predicate)
        {
            return _collection.Exists(predicate);
        }

        public int Count()
        {
            return _collection.Count();
        }
    }
    
}
