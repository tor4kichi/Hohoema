using LiteDB;
using Hohoema.Models.Subscriptions;
using Hohoema.Infra;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Microsoft.Toolkit.Diagnostics;
using CommunityToolkit.Mvvm.Messaging;
using CommunityToolkit.Mvvm.Messaging.Messages;
using System.Linq.Expressions;

namespace Hohoema.Models.Subscriptions
{
    public sealed class NewSubscMessage : ValueChangedMessage<SubscriptionSourceEntity>
    {
        public NewSubscMessage(SubscriptionSourceEntity value) : base(value)
        {
        }
    }

    public sealed class DeleteSubscMessage : ValueChangedMessage<ObjectId>
    {
        public DeleteSubscMessage(ObjectId value) : base(value)
        {
        }
    }

    public sealed class UpdateSubscMessage : ValueChangedMessage<SubscriptionSourceEntity>
    {
        public UpdateSubscMessage(SubscriptionSourceEntity value) : base(value)
        {
        }
    }

    public sealed class SubscriptionRegistrationRepository : LiteDBServiceBase<SubscriptionSourceEntity>
    {
        private readonly IMessenger _messenger;

        public SubscriptionRegistrationRepository(
            LiteDatabase database,
            IMessenger messenger
            )
            : base(database)
        {
            _messenger = messenger;

            _collection.EnsureIndex(x => x.SortIndex);
        }

        public void ClearAll()
        {
            _collection.DeleteAll();
        }


        public bool IsExist(SubscriptionSourceEntity other)
        {
            return _collection.Exists(x => x.SourceType == other.SourceType && x.SourceParameter == other.SourceParameter);
        }

        public override BsonValue CreateItem(SubscriptionSourceEntity entity)
        {
            Guard.IsFalse(IsExist(entity), "IsExist(entity)");
            
            entity.Id = ObjectId.NewObjectId();
            entity.SortIndex = _collection.Max(x => x.SortIndex) + 1;
            var result = base.CreateItem(entity);
            _messenger.Send(new NewSubscMessage(entity));
            return result;
        }

        public override bool DeleteItem(BsonValue id)
        {
            var result = base.DeleteItem(id);
            if (result)
            {
                _messenger.Send(new DeleteSubscMessage(id));
            }
            return result;
        }

        public override bool DeleteItem(SubscriptionSourceEntity item)
        {
            var result = base.DeleteItem(item);
            if (result)
            {
                _messenger.Send(new DeleteSubscMessage(item.Id));
            }

            return result;
        }

        public override bool DeleteMany(Expression<Func<SubscriptionSourceEntity, bool>> predicate)
        {
            List<ObjectId> ids = Find(predicate).Select(x => x.Id).ToList();
            var deleted = base.DeleteMany(predicate);
            if (deleted)
            {
                foreach (var id in ids)
                {
                    _messenger.Send(new DeleteSubscMessage(id));
                }
            }

            return deleted;
        }

        public override bool UpdateItem(SubscriptionSourceEntity item)
        {
            var result = base.UpdateItem(item);
            if (result)
            {
                _messenger.Send(new UpdateSubscMessage(item));
            }

            return result;
        }

        public override int UpdateItem(IEnumerable<SubscriptionSourceEntity> items)
        {
            var result = base.UpdateItem(items);
            if (result > 0)
            {
                foreach (var item in items)
                {
                    _messenger.Send(new UpdateSubscMessage(item));
                }
            }
            return result;
        }
    }
}
