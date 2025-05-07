using System;
using System.Diagnostics;
using System.Windows.Input;
using Avalonia.Controls;
using Avalonia.Utilities;

namespace SourceGit.ViewModels
{
    public partial class App
    {
        //TODO : maybe use CommunityToolkit.Mvvm.Input; RelayCommand?
        //         ReactiveCommand (ReactiveUI)
        // Avalonia works well with ReactiveUI, which provides ReactiveCommand:
        public class Command : ICommand
        {
            //TODO: is it used ? cannot find usage in the code.//?CommunityToolkit.Mvvm.Input.WeakEventHandlerManager
            // maybe make something like <code> add { CommandManager.RequerySuggested += value; } </code> ?
            //    add { Debug.Assert(false, "Used?"); }
            //         remove {Debug.Assert(false, "Used?");  }
            public event EventHandler CanExecuteChanged
            {
                add { }
                remove { }
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
           ShowWindow("Preferences", false));

        private static dynamic GetWindowService()
        {
            return AppDyn.GetWindowService();
        }
        public static void ShowWindow(object data, bool showAsDialog)
        { 
            AppDyn.ShowWindowI(data, showAsDialog);
        }
        public static readonly Command OpenHotkeysCommand = new Command(_ =>
                        App.ShowWindow("Hotkeys", false));
        public static readonly Command OpenAppDataDirCommand = new Command(_ => Native.OS.OpenInFileManager(Native.OS.DataDir));
        public static readonly Command OpenAboutCommand = new Command(_ =>
            App.ShowWindow("About", false));
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
