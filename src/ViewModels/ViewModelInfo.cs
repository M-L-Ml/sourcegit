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
    public class ViewModelInfo(List<(string, string)> setValue)
    {
        /// <summary>
        /// Data to set the value. <code>SetValue(Views.MenuItemExtension.CommandProperty, "--top-order");</code>
        /// </summary>
        public List<(string, string)> SetValue { get; } = setValue;
    }
}
