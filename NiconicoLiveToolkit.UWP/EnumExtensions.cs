using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using System.Text;
using System.Threading.Tasks;

namespace NiconicoToolkit
{
    public static class EnumExtension
    {
        // 指定した属性を指定したEnumが持っていたらtrue、持っていない場合falseを返す
        public static bool HasAttribute<T>(this Enum e) where T : Attribute
        {
            return GetAttrubute<T>(e) != null;
        }

        // Tで指定した属性をEnumから取得する、見つからない場合nullを返す
        public static T GetAttrubute<T>(this Enum e) where T : Attribute
        {
            FieldInfo field = e.GetType().GetField(e.ToString());
            if (field.GetCustomAttribute<T>() is T att)
            {
                return att;
            }

            return null;
        }

        // Try - Parse パターンで指定した型を取得する
        public static bool TryGetAttribute<T>(this Enum e, out T type) where T : Attribute
        {
            FieldInfo field = e.GetType().GetField(e.ToString());
            if (field.GetCustomAttribute<T>() is T att)
            {
                type = att;
                return true;
            }

            type = null;
            return false;
        }
    }
}
