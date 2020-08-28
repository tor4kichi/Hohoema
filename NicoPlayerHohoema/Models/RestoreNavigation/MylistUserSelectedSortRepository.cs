using LiteDB;
using Mntone.Nico2.Users.Mylist;
using NicoPlayerHohoema.Repository;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace NicoPlayerHohoema.Models.RestoreNavigation
{
    public sealed class MylistUserSelectedSortRepository 
    {
        private readonly MylistUserSelectedSortRepository_Internal _mylistUserSelectedSortRepository_Internal;

        public class MylistUserSelectedSortRepository_Internal : LocalLiteDBService<MylistUserSelectedSortEntry>
        {
            public void Set(string mylistId, MylistSortKey sortKey, MylistSortOrder sortOrder)
            {
                _collection.Upsert(new MylistUserSelectedSortEntry() { MylistId = mylistId, SortKey = sortKey, SortOrder = sortOrder });
            }

            public MylistUserSelectedSortEntry Get(string mylistId)
            {
                return _collection.FindById(mylistId);
            }
        }


        public MylistUserSelectedSortRepository(MylistUserSelectedSortRepository_Internal mylistUserSelectedSortRepository_Internal)
        {
            _mylistUserSelectedSortRepository_Internal = mylistUserSelectedSortRepository_Internal;
        }

        public void SetMylistSort(string mylistId, MylistSortKey sortKey, MylistSortOrder sortOrder)
        {
            _mylistUserSelectedSortRepository_Internal.Set(mylistId, sortKey, sortOrder);
        }

        public (MylistSortKey? SortKey, MylistSortOrder? SortOrder) GetMylistSort(string mylistId)
        {
            var entry = _mylistUserSelectedSortRepository_Internal.Get(mylistId);
            return (entry?.SortKey, entry?.SortOrder);
        }
    }

    public class MylistUserSelectedSortEntry
    {
        [BsonId]
        public string MylistId { get; set; }

        [BsonField]
        public MylistSortKey SortKey { get; set; }
        
        [BsonField]
        public MylistSortOrder SortOrder { get; set; }
    }
}
