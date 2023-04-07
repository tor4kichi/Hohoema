using System;
using Windows.UI.Xaml.Data;
using Windows.UI.Xaml.Markup;

namespace I18NPortable.Xaml.Extensions 
{
    [MarkupExtensionReturnType(ReturnType = typeof(string))]
    public class LocalizeExtension : MarkupExtension
    {
        public LocalizeExtension() { }

        public LocalizeExtension(string key)
        {
            Key = key;
        }



        public object Key { get; set; }

        public object[] Parameters { get; set; }

        protected override object ProvideValue()
        {
            if (Key is Enum enumValue)
            {
                return enumValue.Translate();
            }
            else if (Key is string keyStr)
            {
                if (Parameters is null)
                {
                    return I18N.Current.Translate(keyStr);
                }
                else
                {
                    return I18N.Current.Translate(keyStr, Parameters);
                }
            }
            else
            {
                 throw new NotSupportedException("not supported localize Type: " + Key?.GetType().Name);
            }
        }
    }
}