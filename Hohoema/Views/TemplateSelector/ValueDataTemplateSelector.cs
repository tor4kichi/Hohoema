using System.Collections.ObjectModel;
using Windows.UI.Xaml;
using Windows.UI.Xaml.Controls;
using Windows.UI.Xaml.Markup;

namespace Hohoema.Views.TemplateSelector;

[ContentProperty(Name ="Template")]
public class ValueDataTemplate : DependencyObject
{
    public object Value { get; set; }
    public Windows.UI.Xaml.DataTemplate Template { get; set; }
}

public class ValueDataTemplateCollection : Collection<ValueDataTemplate> { }

[ContentProperty(Name = "Templates")]
public class ValueDataTemplateSelector : DataTemplateSelector
{
    public string FieldName { get; set; }
    public string PropertyName { get; set; }

    public Windows.UI.Xaml.DataTemplate Default { get; set; }

    public bool ForceCompereWithString { get; set; }

    public ValueDataTemplateCollection Templates { get; set; } = new ValueDataTemplateCollection();

    protected override Windows.UI.Xaml.DataTemplate SelectTemplateCore(object item, DependencyObject container)
    {
        if (item == null)
        {
            return Default ?? base.SelectTemplateCore(item, container);
        }

        object value = null;
        if (string.IsNullOrEmpty(FieldName) && string.IsNullOrEmpty(PropertyName))
        {
            value = item;
        }
        else
        {
            var itemType = item.GetType();

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
        }
        

        // compare values, and choose template
        if (value != null)
        {
            bool valueIsString = value is string;
            var strSourceValue = value.ToString();
            foreach (var valueDataTemplate in Templates)
            {
                if (ForceCompereWithString || valueIsString)
                {
                    if (valueDataTemplate.Value.ToString() == strSourceValue)
                    {
                        return valueDataTemplate.Template;
                    }
                }
                else if (valueDataTemplate.Value is string strDestValue)
                {
                    if (strSourceValue == strDestValue)
                    {
                        return valueDataTemplate.Template;
                    }
                }
                else if (valueDataTemplate.Value.Equals(value))
                {
                    return valueDataTemplate.Template;
                }
            }
        }

        return Default ?? base.SelectTemplateCore(item, container);
    }
}
