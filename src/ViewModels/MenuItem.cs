
using Avalonia.Collections;
using System.Collections.Generic;
using System.Linq;


namespace SourceGit.ViewModels
{
    /// <summary>
    /// This is POCO  
    /// and maybe better named MenuAction 
    /// But temporary called MenuItem to minimize merge conflicts with upstream SourceGit (see roadmap.md)
    /// </summary>  
   public class MenuItemModel
    {
        /// <summary>
        /// Should be set using App.ResText 
        /// should be displayed after using App.Text
        /// </summary>
        public StringResource Header { get; set; }
        /// <summary>
        /// Should be set using App.MenuIconKey
        /// should be displayed after using App.Icon
        /// </summary>
        public string IconKey { get; set; }
        public System.Windows.Input.ICommand Command { get; set; }
        public bool IsEnabled { get; set; } = true;
        //    public bool IsSeparator { get; set; } = false;
        public object Tag { get; set; } // Optional: for attaching extra data
        /// <summary>
        /// ToDo: implement setting the properties
        /// </summary>
        public ViewModelInfo ViewToDo { get; internal set; }
        public bool IsVisible { get; internal set; } = true;
    }
    public class MenuModel : MenuItemModel
    {
        public AvaloniaList<MenuItemModel> Items { get; set; } = new();
        public IEnumerable<MenuModel> AllSubmenus => Items.OfType<MenuModel>();

        internal static MenuItemModel Separator()
        {
            return new MenuItemModel { Header = "-" };
        }
    }
    public class ContextMenuModel : MenuModel
    {

    }


}
