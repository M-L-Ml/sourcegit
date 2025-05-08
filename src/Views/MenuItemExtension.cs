using System.Linq;
using Avalonia;
using Avalonia.Controls;
using Avalonia.Data;

namespace SourceGit.Views
{
    public class MenuItemExtension : AvaloniaObject
    {
        public static readonly AttachedProperty<string> CommandProperty =
            AvaloniaProperty.RegisterAttached<MenuItemExtension, MenuItem, string>("Command", string.Empty, false, BindingMode.OneWay);
    }

    public static class MenuItemModelExtension
    {

        public static MenuItem CreateMenuItemFromModel(this ViewModels.MenuItemModel x)
        {
            return new MenuItem() { Header = App.Text(x.Header), DataContext = x, Command = x.Command,
                Icon = App.CreateMenuIcon(x.IconKey), IsEnabled = x.IsEnabled };
        }
        public static ContextMenu CreateContextMenuFromModel(this ViewModels.ContextMenuModel menuModel)
        {
            var menu = new ContextMenu() { DataContext = menuModel };
            foreach (var item in menuModel.Items.Select(x => x.CreateMenuItemFromModel()))
                menu.Items.Add(item);
            return menu;
        }
    }
}
