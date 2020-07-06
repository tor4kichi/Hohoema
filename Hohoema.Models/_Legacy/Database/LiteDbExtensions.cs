using LiteDB;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Linq.Expressions;
using System.Text;
using System.Threading.Tasks;

namespace Hohoema.Database
{
    public static class LiteCollectionExtensions
    {
        public static int DeleteMany<T>(this LiteCollection<T> collection, Expression<Func<T, bool>> expression)
        {
            return collection.Delete(expression);
        }

        public static int DeleteMany<T>(this LiteCollection<T> collection)
        {
            return collection.Delete(x => true);
        }

        public static int DeleteMany<T>(this LiteRepository collection, Expression<Func<T, bool>> expression)
        {
            return collection.Delete(expression);
        }
        public static int DeleteAll<T>(this LiteRepository collection)
        {
            return collection.Delete<T>(x => true);
        }
    }
}
