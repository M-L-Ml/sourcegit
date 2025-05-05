using Avalonia;
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

        private void SelectGPGExecutable(object _, RoutedEventArgs e)
        {
            ViewModel.SelectGPGExecutableCommand.ExecuteAsync(null);
            e.Handled = true;
        }

        private void SelectShellOrTerminal(object _, RoutedEventArgs e)
        {
            ViewModel.SelectShellOrTerminalCommand.ExecuteAsync(null);
            e.Handled = true;
        }

        private void SelectExternalMergeTool(object _, RoutedEventArgs e)
        {
            ViewModel.SelectExternalMergeToolCommand.ExecuteAsync(null);
            e.Handled = true;
        }

        private void OnUseNativeWindowFrameChanged(object sender, RoutedEventArgs e)
        {
            ViewModel.UseNativeWindowFrameChangedCommand.Execute(sender);
            e.Handled = true;
        }

        private void OnGitInstallPathChanged(object sender, TextChangedEventArgs e)
        {
            ViewModel.UpdateGitVersion();
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
