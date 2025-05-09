using System;
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
            var menu = new MenuItem()
            {
                Header = x.Header.Text(),
                DataContext = x,
                Command = x.Command,
                Icon = App.CreateMenuIcon(x.IconKey),
                IsEnabled = x.IsEnabled
            };
            if (x is ViewModels.MenuModel y)
                y.CreateSubItemsFromModel(menu);
            return menu;
        }
        public static MenuItem CreateMenuFromModel(this ViewModels.MenuModel menuModel)
             => menuModel.CreateMenuItemFromModel();

        public static MenuDeriv CreateMenuFromModelInternal<MenuDeriv>(this ViewModels.MenuModel menuModel)
            where MenuDeriv : ItemsControl, new()
        {
            var menu = new MenuDeriv() { DataContext = menuModel };
            CreateSubItemsFromModel(menuModel, menu);
            return menu;
        }
        public static void CreateSubItemsFromModel<MenuDeriv>(this ViewModels.MenuModel menuModel, MenuDeriv menu)
              where MenuDeriv : ItemsControl, new()
        {
            foreach (var item
                    in menuModel.Items.Select(x =>
                        {
                            return x.CreateMenuItemFromModel();
                        }))
                menu.Items.Add(item);
        }

        public static ContextMenu CreateContextMenuFromModel(this ViewModels.ContextMenuModel menuModel)
        {
            var menu = menuModel.CreateMenuFromModelInternal<ContextMenu>();
            // Implement assigning menuModel.ViewToDo
            foreach (var (propName, value) in menuModel.ViewToDo.SetValue)
            {
                if (propName == "Placement" && Enum.TryParse(value.ToString(), out PlacementMode placementMode))
                {
                    menu.Placement = placementMode;
                }
            }
            return menu;
        }
    }
}
