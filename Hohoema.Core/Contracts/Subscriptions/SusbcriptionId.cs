using LiteDB;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using UnitGenerator;
namespace Hohoema.Contracts.Subscriptions;

[UnitOf(typeof(ObjectId), UnitGenerator.UnitGenerateOptions.Comparable)]
public readonly partial struct SubscriptionId
{
    public static SubscriptionId NewObjectId()
    {
        return new SubscriptionId(ObjectId.NewObjectId());
    }

    public static SubscriptionId Parse(string s)
    {
        return new SubscriptionId(new LiteDB.ObjectId(s));
    }

    public static bool TryParse(string value, out SubscriptionId result)
    {
        if (string.IsNullOrEmpty(value))
        {
            result = default(SubscriptionId);
            return false;
        }
        else if (value.Length != 24)
        {
            result = default(SubscriptionId);
            return false;
        }
        else
        {
            result = new SubscriptionId(new ObjectId(value));
            return true;
        }
    }

}
