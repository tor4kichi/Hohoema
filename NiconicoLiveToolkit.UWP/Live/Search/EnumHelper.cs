using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Reflection;
using System.Text;

namespace NiconicoLiveToolkit.Live.Search
{
    public static class EnumHelper
    {
        public static string GetDescription(this Enum value)
        {
            // Get the type
            Type type = value.GetType();

            // Get fieldinfo for this type
            System.Reflection.FieldInfo fieldInfo = type.GetField(value.ToString());

            //範囲外の値チェック
            if (fieldInfo == null) return null;

            DescriptionAttribute[] attribs = fieldInfo.GetCustomAttributes(typeof(DescriptionAttribute), false) as DescriptionAttribute[];

            // Return the first if there was a match.
            return attribs.Length > 0 ? attribs[0].Description : null;

        }
    }
}
