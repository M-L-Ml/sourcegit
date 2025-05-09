
using Avalonia.Collections;
using System.Collections.Generic;
using System.Linq;

/// <summary>
/// This is POCO  
/// and maybe better named MenuAction 
/// But temporary called MenuItem to minimize merge conflicts with upstream SourceGit (see roadmap.md)
/// </summary>
namespace SourceGit.ViewModels
{
    public class MenuItemModel
    {
        /// <summary>
        /// Should be set using App.ResText 
        /// should be displayed after using App.Text
        /// </summary>
        public string Header { get; set; } 
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
    }
    public class MenuModel : MenuItemModel
    {
        public AvaloniaList<MenuItemModel> Items { get; set; } = new ();
        public IEnumerable<MenuModel> AllSubmenus => Items.OfType<MenuModel>();
   
    }
    public class ContextMenuModel : MenuModel
    {

    }


}
