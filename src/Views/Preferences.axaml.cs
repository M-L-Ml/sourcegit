using Avalonia.Controls;
using Avalonia.Interactivity;

namespace SourceGit.Views
{
    public partial class Preferences : ChromelessWindow
    {
        public ViewModels.Preferences ViewModel => (ViewModels.Preferences)DataContext;

        public Preferences()
        {
            DataContext = ViewModels.Preferences.Instance;
            InitializeComponent();
            
            // Load git configuration after initialization
            ViewModel.LoadGitConfig();
        }
        
        protected override void OnClosing(WindowClosingEventArgs e)
        {
            base.OnClosing(e);

            if (Design.IsDesignMode)
                return;

            // Save git configuration on window closing
            ViewModel.SaveGitConfig();
        }

        private async void SelectThemeOverrideFile(object _, RoutedEventArgs e)
        {
            await ViewModel.SelectThemeOverrideFileCommand.ExecuteAsync(null);
            e.Handled = true;
        }

        private async void SelectGitExecutable(object _, RoutedEventArgs e)
        {
            await ViewModel.SelectGitExecutableCommand.ExecuteAsync(null);
            e.Handled = true;
        }

        private async void SelectDefaultCloneDir(object _, RoutedEventArgs e)
        {
            await ViewModel.SelectDefaultCloneDirCommand.ExecuteAsync(null);
            e.Handled = true;
        }

        private async void SelectGPGExecutable(object _, RoutedEventArgs e)
        {
            await ViewModel.SelectGPGExecutableCommand.ExecuteAsync(null);
            e.Handled = true;
        }

        private async void SelectShellOrTerminal(object _, RoutedEventArgs e)
        {
            await ViewModel.SelectShellOrTerminalCommand.ExecuteAsync(null);
            e.Handled = true;
        }

        private async void SelectExternalMergeTool(object _, RoutedEventArgs e)
        {
            await ViewModel.SelectExternalMergeToolCommand.ExecuteAsync(null);
            e.Handled = true;
        }

        // TODO: refactor. See UseSystemWindowFrame and UseNativeWindowFrame binding property
        private void OnUseNativeWindowFrameChanged(object sender, RoutedEventArgs e)
        {
            if (sender is CheckBox box)
            {
                ViewModel.UseSystemWindowFrame = box.IsChecked == true;
                App.ShowWindow(new ConfirmRestart(), true);
            }
            e.Handled = true;
        }

        private void OnAddOpenAIService(object sender, RoutedEventArgs e)
        {
            ViewModel.AddOpenAIServiceCommand.Execute(null);
            e.Handled = true;
        }

        private void OnRemoveSelectedOpenAIService(object sender, RoutedEventArgs e)
        {
            ViewModel.RemoveSelectedOpenAIServiceCommand.Execute(null);
            e.Handled = true;
        }

        private void OnAddCustomAction(object sender, RoutedEventArgs e)
        {
            ViewModel.AddCustomActionCommand.Execute(null);
            e.Handled = true;
        }

        private void SelectExecutableForCustomAction(object sender, RoutedEventArgs e)
        {
            ViewModel.SelectExecutableForCustomActionCommand.ExecuteAsync(sender);
            e.Handled = true;
        }

        private void OnRemoveSelectedCustomAction(object sender, RoutedEventArgs e)
        {
            ViewModel.RemoveSelectedCustomActionCommand.Execute(null);
            e.Handled = true;
        }
    }
}
