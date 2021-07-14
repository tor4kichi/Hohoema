using System;
using System.ComponentModel;
using System.Linq;

namespace NiconicoToolkit
{
    public static class EnumDescriptionExtensions
    {
        public static string GetDescription<E>(this E enumValue)
            where E : Enum
        {
            var gm = enumValue.GetType().GetMember(enumValue.ToString());
            var attributes = gm[0].GetCustomAttributes(typeof(DescriptionAttribute), false);
            var description = ((DescriptionAttribute)attributes.ElementAt(0)).Description;
            return description;
        }


    }



}
