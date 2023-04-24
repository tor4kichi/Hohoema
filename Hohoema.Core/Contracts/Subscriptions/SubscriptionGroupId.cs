using LiteDB;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using UnitGenerator;

namespace Hohoema.Contracts.Subscriptions;

[UnitOf(typeof(ObjectId), UnitGenerateOptions.Comparable | UnitGenerateOptions.JsonConverter)]
public readonly partial struct SubscriptionGroupId
{
    public static readonly SubscriptionGroupId DefaultGroupId = new SubscriptionGroupId(ObjectId.Empty);

    public static SubscriptionGroupId NewGroupId()
    {
        return new SubscriptionGroupId(ObjectId.NewObjectId());
    }



    public static SubscriptionGroupId Parse(string s)
    {
        return new SubscriptionGroupId(new LiteDB.ObjectId(s));
    }

    public static bool TryParse(string value, out SubscriptionGroupId result)
    {
        if (string.IsNullOrEmpty(value))
        {
            result = default(SubscriptionGroupId);
            return false;
        }
        else if (value.Length != 24)
        {
            result = default(SubscriptionGroupId);
            return false;
        }
        else
        {
            result = new SubscriptionGroupId(new ObjectId(value));
            return true;
        }
    }

}
