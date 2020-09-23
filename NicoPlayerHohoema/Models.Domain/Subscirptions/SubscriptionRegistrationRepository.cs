using LiteDB;
using Hohoema.Models.Domain.Subscriptions;
using Hohoema.Models.Infrastructure;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Hohoema.Models.Domain.Subscriptions
{
    public sealed class SubscriptionRegistrationRepository : LiteDBServiceBase<SubscriptionSourceEntity>
    {
        public SubscriptionRegistrationRepository(LiteDatabase database)
            : base(database)
        {

        }

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
