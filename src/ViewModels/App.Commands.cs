using System;
using System.Windows.Input;
using Avalonia.Controls;

namespace SourceGit.ViewModels
{
    public partial class App
    {
        public class Command : ICommand
        {
            public event EventHandler CanExecuteChanged
            {
                add { CommandManager.RequerySuggested += value; }
                remove { CommandManager.RequerySuggested -= value; }
            }

            public Command(Action<object> action)
            {
                _action = action;
            }

            public bool CanExecute(object parameter) => _action != null;
            public void Execute(object parameter) => _action?.Invoke(parameter);

            private readonly Action<object> _action;
        }

        public static bool IsCheckForUpdateCommandVisible
        {
            get
            {
#if DISABLE_UPDATE_DETECTION
                return false;
#else
                return true;
#endif
            }
        }
        
        public static readonly Command OpenPreferencesCommand = new Command(_ => 
            SourceGit.App.GetWindowService()?.ShowWindow("Preferences", false));
        public static readonly Command OpenHotkeysCommand = new Command(_ => 
            SourceGit.App.GetWindowService()?.ShowWindow("Hotkeys", false));
        public static readonly Command OpenAppDataDirCommand = new Command(_ => Native.OS.OpenInFileManager(Native.OS.DataDir));
        public static readonly Command OpenAboutCommand = new Command(_ => 
            SourceGit.App.GetWindowService()?.ShowWindow("About", false));
        public static readonly Command CheckForUpdateCommand = new Command(_ => AppDyn.Check4Update(true));
        public static readonly Command QuitCommand = new Command(_ => App.Quit(0));
        public static readonly Command CopyTextBlockCommand = new Command(p =>
        {
            var textBlock = p as TextBlock;
            if (textBlock == null)
                return;

            if (textBlock.Inlines is { Count: > 0 } inlines)
                CopyText(inlines.Text);
            else if (!string.IsNullOrEmpty(textBlock.Text))
                CopyText(textBlock.Text);
        });
    }
}
