using LiteDB;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Windows.Storage;

namespace NicoPlayerHohoema.Repository
{
    public class LiteDBService<T>
    {
        static readonly string LocalConnectionString = $"Filename={Path.Combine(ApplicationData.Current.LocalFolder.Path, "_v3")}; Async=false;";

        protected LiteCollection<T> _collection;

        public LiteDBService()
        {
            var db = new LiteDatabase(LocalConnectionString);
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
