using System;
using System.Collections.ObjectModel;
using System.Linq;
using System.Reflection;
using Windows.UI.Xaml;
using Windows.UI.Xaml.Controls;
using Windows.UI.Xaml.Markup;

namespace NicoPlayerHohoema.Views.TemplateSelector
{
    [ContentProperty(Name ="Template")]
    public class ValueDataTemplate : DependencyObject
    {
        public object Value { get; set; }
        public DataTemplate Template { get; set; }
    }

    public class ValueDataTemplateCollection : Collection<ValueDataTemplate> { }

    [ContentProperty(Name = "Templates")]
    public class ValueDataTemplateSelector : DataTemplateSelector
    {
        public string FieldName { get; set; }
        public string PropertyName { get; set; }

        public DataTemplate Default { get; set; }

        public ValueDataTemplateCollection Templates { get; set; } = new ValueDataTemplateCollection();

        protected override DataTemplate SelectTemplateCore(object item, DependencyObject container)
        {
            if (!Templates.Any())
            {
                throw new Exception("not contain any ValueDataTemplate in Templates.");
            }

            if (string.IsNullOrEmpty(FieldName) && string.IsNullOrEmpty(PropertyName))
            {
                throw new Exception($"{nameof(FieldName)} and {nameof(PropertyName)} is empty. require either one, or both.");
            }

            if (item == null)
            {
                return Default ?? base.SelectTemplateCore(item, container);
            }

            var itemType = item.GetType();
            object value = null;

            // check field member value
            if (!string.IsNullOrEmpty(FieldName))
            {
                var fieldInfo = itemType.GetField(FieldName);
                if (fieldInfo?.IsPublic ?? false)
                {
                    value = fieldInfo.GetValue(item);
                }
            }

            // check property member value
            if (value == null && !string.IsNullOrEmpty(PropertyName))
            {
                var propInfo = itemType.GetProperty(PropertyName);
                if (propInfo?.CanRead ?? false)
                {
                    value = propInfo.GetValue(item);
                }
            }

            // compare values, and choose template
            if (value != null)
            {
                foreach (var valueDataTemplate in Templates)
                {
                    if (valueDataTemplate.Value.Equals(value))
                    {
                        return valueDataTemplate.Template;
                    }
                }
            }

            return Default ?? base.SelectTemplateCore(item, container);
        }
    }
}
