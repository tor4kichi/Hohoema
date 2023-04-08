#nullable enable
using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using Windows.UI.Xaml;
using Windows.UI.Xaml.Controls;
using Windows.UI.Xaml.Data;
using Windows.UI.Xaml.Markup;

namespace Hohoema.Views.TemplateSelector;

public class DataTemplateCollection : Collection<Windows.UI.Xaml.DataTemplate> { }

[ContentProperty(Name = "Templates")]
public class TypeBasedTemplateSelector : DataTemplateSelector
{
    Dictionary<Type, Windows.UI.Xaml.DataTemplate> _typeToTemplateMap;

    public Windows.UI.Xaml.DataTemplate NullTemplate { get; set; }
    public Windows.UI.Xaml.DataTemplate DefaultTemplate { get; set; }
    public DataTemplateCollection Templates { get; set; } = new DataTemplateCollection();

    static Dictionary<Type, Windows.UI.Xaml.DataTemplate> MakeTypeToTemplateMap(DataTemplateCollection templateCollection)
    {
        var map = new Dictionary<Type, Windows.UI.Xaml.DataTemplate>();
        foreach (var template in templateCollection)
        {
            var targetType = DataTemplate.GetTargetType(template);
            if (targetType == null)
            {
                throw new Infra.HohoemaException("TargetTypeProperty must be set your Type. (restrict only TypeBasedTemplateSelector insided DateTemplate)");
            }

            if (map.ContainsKey(targetType))
            {
                throw new Infra.HohoemaException("Duplicate TargetTypeProperty present Type's in DataTemplate. Ensure indivisual Type to set DataTemplate with TargetType.");
            }

            map.Add(targetType, template);
        }

        return map;
    }


    protected override Windows.UI.Xaml.DataTemplate SelectTemplateCore(object item)
    {
        if (item == null)
        {
            return NullTemplate ?? DefaultTemplate ?? base.SelectTemplateCore(item);
        }

        // setup _typeToTemplateMap 
        if (_typeToTemplateMap == null)
        {
            _typeToTemplateMap = MakeTypeToTemplateMap(Templates);
        }

        // Find template
        var type = item.GetType();
        if (_typeToTemplateMap.TryGetValue(type, out var template))
        {
            return template;
        }

        // Find template with subclass
        foreach (var destType in _typeToTemplateMap.Keys)
        {
            if (destType.IsAssignableFrom(type))
            {
                if (_typeToTemplateMap.TryGetValue(destType, out template))
                {
                    return template;
                }
            }
        }

        return DefaultTemplate ?? base.SelectTemplateCore(item);
    }

    protected override Windows.UI.Xaml.DataTemplate SelectTemplateCore(object item, DependencyObject container)
    {
        return SelectTemplateCore(item);
    }
}



public partial class DataTemplate : DependencyObject
{
    public static readonly DependencyProperty TargetTypeProperty =
       DependencyProperty.RegisterAttached(
           "TargetType",
           typeof(Type),
           typeof(DataTemplate),
           new PropertyMetadata(default(Type))
       );

    public static void SetTargetType(DependencyObject element, Type value)
    {
        element.SetValue(TargetTypeProperty, value);
    }
    public static Type GetTargetType(DependencyObject element)
    {
        return (Type)element.GetValue(TargetTypeProperty);
    }
}

public class StringToTypeConverter : IValueConverter
{
    readonly static Dictionary<string, Type> _map = new();
    public object Convert(object value, Type targetType, object parameter, string language)
    {
        var text = value as string;
        
        if (text is null) { throw new ArgumentNullException(); }
        
        if (_map.TryGetValue(text, out var type)) 
        {
            return type; 
        }
        else
        {
            type = Type.GetType(text);
            _map.Add(text, type);
            return type;
        }
    }

    public object ConvertBack(object value, Type targetType, object parameter, string language)
    {
        var type = value as Type;
        return type != null ? type.FullName : null;
    }
}
