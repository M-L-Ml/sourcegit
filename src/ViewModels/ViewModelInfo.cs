using System;
using System.Collections.Generic;

namespace SourceGit.ViewModels
{
 
// Usage
//var viewModel = new ViewModelInfo(new()
//{
//    ("Key1", "Value1"),
//    ("Key2", "Value2")
//});
    public class ViewModelInfo0(List<(string, string)> setValue)
    {
        /// <summary>
        /// Data to set the value. <code>SetValue(Views.MenuItemExtension.CommandProperty, "--top-order");</code>
        /// </summary>
        public List<(string, string)> SetValue { get; } = setValue;
    }
    public enum ViewPropertySetting
    {
        icon_Fill,
        Placement,
        Views_MenuItemExtension_CommandProperty,
        InputGesture,
        MinWidth
    }

    /// <summary>
    /// see <see cref="SourceGit.ViewModels.MenuItemModelExtension.SetViewSettingsFromModel"/>
    public class ViewModelInfo : Dictionary<ViewPropertySetting, object>
    {
    };
    public class MyDictionary
{
    private readonly Dictionary<ViewPropertySetting, object> _internalDictionary = new();

 

    public object this[ViewPropertySetting key]
    {
        get => _internalDictionary[key];
        set => _internalDictionary[key] = value;
    }

    // You may want to expose other dictionary methods as needed
    public void Add(ViewPropertySetting key, object value)
    {
        _internalDictionary.Add(key, value);
    }
}

}
