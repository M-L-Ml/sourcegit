
using Avalonia.Collections;

/// <summary>
/// This is POCO  
/// and maybe better named MenuAction 
/// But temporary called MenuItem to minimize merge conflicts with upstream SourceGit (see roadmap.md)
/// </summary>
namespace SourceGit.ViewModels
{
    public class MenuItem
    {
        public string Header { get; set; } // Should be set using App.ResText
        public string IconKey { get; set; } // Should be set using App.MenuIconKey
        public System.Windows.Input.ICommand Command { get; set; }
        public bool IsEnabled { get; set; } = true;
        public bool IsSeparator { get; set; } = false;
        public object Tag { get; set; } // Optional: for attaching extra data
        public ViewModelInfo ViewToDo { get; internal set; }
    }

    public class ContextMenuModel : MenuItem
    {
        public AvaloniaList<MenuItem> Items { get; set; } = new ();
   
   
    }


}
