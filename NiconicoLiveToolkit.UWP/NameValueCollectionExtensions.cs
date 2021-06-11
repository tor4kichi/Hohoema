using System;
using System.Collections.Generic;
using System.Collections.Specialized;
using System.Text;
using System.Web;

namespace NiconicoToolkit
{
    internal static class NameValueCollectionExtensions
    {
        public static StringBuilder ToQueryString(this NameValueCollection nvc, StringBuilder sb = null)
        {
            sb ??= new StringBuilder();

            bool isFirst = true;
            foreach (string key in nvc.Keys)
            {
                if (string.IsNullOrWhiteSpace(key)) continue;

                string[] values = nvc.GetValues(key);
                if (values == null) continue;

                foreach (string value in values)
                {
                    sb.Append(isFirst ? "?" : "&");
                    sb.Append(Uri.EscapeDataString(key));
                    sb.Append("=");
                    sb.Append(Uri.EscapeDataString(value));

                    isFirst = false;
                }
            }

            return sb;
        }

        public static void AddIfNotNull<T>(this NameValueCollection nvc, string key, T value)
            where T : class
        {
            if (value is not null)
            {
                nvc.Add(key, value.ToString());
            }
        }

        public static void AddIfNotNull<T>(this NameValueCollection nvc, string key, T? value)
            where T : struct
        {
            if (value is not null and T realValue)
            {
                nvc.Add(key, realValue.ToString());
            }
        }

        public static void AddEnumWithDescription<T>(this NameValueCollection nvc, string key, T value)
            where T : Enum
        {
            nvc.Add(key, value.GetDescription());
        }

        public static void AddEnumIfNotNullWithDescription<T>(this NameValueCollection nvc, string key, T? value)
            where T : struct, Enum
        {
            if (value is not null and T realValue)
            {
                nvc.Add(key, realValue.GetDescription());
            }
        }
    }

    internal static class StringBuilderExtensions
    {
        public static StringBuilder AppendQueryString(this StringBuilder sb, NameValueCollection nvc)
        {
            return nvc.ToQueryString(sb);
        }
    }
}
