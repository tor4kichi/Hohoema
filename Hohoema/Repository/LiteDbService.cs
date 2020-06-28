using LiteDB;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Windows.Storage;

namespace Hohoema.Repository
{
    public abstract class LocalLiteDBService<T> : LiteDBServiceBase<T>
    {
        static readonly string LocalConnectionString = $"Filename={Path.Combine(ApplicationData.Current.LocalFolder.Path, "_v3")}; Async=false;";
        public LocalLiteDBService()
            : base(LocalConnectionString)
        {

        }
    }


    public abstract class TempraryLiteDBService<T> : LiteDBServiceBase<T>
    {
        static readonly string TempraryConnectionString = $"Filename={Path.Combine(ApplicationData.Current.TemporaryFolder.Path, "_v3")}; Async=false;";
        public TempraryLiteDBService()
            : base(TempraryConnectionString)
        {

        }

    }
    public abstract class LiteDBServiceBase<T>
    {

        protected LiteCollection<T> _collection;

        public LiteDBServiceBase(string connectionString)
        {
            var db = new LiteDatabase(connectionString);
            _collection = db.GetCollection<T>();
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
            return _collection.Delete(i => i.Equals(item)) > 0;
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
