using LiteDB;
using Hohoema.Models.Subscriptions;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Hohoema.Models.Repository.Subscriptions
{
    public sealed class SubscriptionRegistrationRepository : LocalLiteDBService<SubscriptionSourceEntity>
    {
        public void ClearAll()
        {
            _collection.DeleteAll();
        }


        public bool IsExist(SubscriptionSourceEntity other)
        {
            return _collection.Exists(x => x.SourceType == other.SourceType && x.SourceParameter == other.SourceParameter);
        }

        public override SubscriptionSourceEntity CreateItem(SubscriptionSourceEntity entity)
        {
            if (IsExist(entity))
            {
                return _collection.FindOne(x => x.SourceType == entity.SourceType && x.SourceParameter == entity.SourceParameter);
            }
            else
            {
                entity.Id = ObjectId.NewObjectId();
                return base.CreateItem(entity); 
            }
        }
    }
}
