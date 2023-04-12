using LiteDB;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using UnitGenerator;
namespace Hohoema.Contracts.Subscriptions;

[UnitOf(typeof(ObjectId), UnitGenerator.UnitGenerateOptions.Comparable)]
public readonly partial struct SusbcriptionId
{
    public static SusbcriptionId NewObjectId()
    {
        return new SusbcriptionId(ObjectId.NewObjectId());
    }

    public static SusbcriptionId Parse(string s)
    {
        return new SusbcriptionId(new LiteDB.ObjectId(s));
    }

    public static bool TryParse(string value, out SusbcriptionId result)
    {
        if (string.IsNullOrEmpty(value))
        {
            result = default(SusbcriptionId);
            return false;
        }
        else if (value.Length != 24)
        {
            result = default(SusbcriptionId);
            return false;
        }
        else
        {
            result = new SusbcriptionId(new ObjectId(value));
            return true;
        }
    }

}
