using Avalonia.Controls;
using Avalonia.Input;
using Avalonia.Interactivity;
using Avalonia.VisualTree;

namespace SourceGit.Views
{
    public partial class RepositoryToolbar : UserControl
    {
        public RepositoryToolbar()
        {
            InitializeComponent();
        }

        private void OpenWithExternalTools(object sender, RoutedEventArgs e)
        {
            if (sender is Button button && DataContext is ViewModels.Repository repo)
            {
                var menuModel = repo.CreateContextMenuForExternalTools();
                var menu = menuModel?.CreateContextMenuFromModel();
                menu?.Open(button);
                e.Handled = true;
            }
        }

        private void OpenStatistics(object _, RoutedEventArgs e)
        {
            if (DataContext is ViewModels.Repository repo)
            {
                App.ShowWindow(new ViewModels.Statistics(repo.FullPath), true);
                e.Handled = true;
            }
        }

        private void OpenConfigure(object sender, RoutedEventArgs e)
        {
            if (DataContext is ViewModels.Repository repo)
            {
                App.ShowWindow(new ViewModels.RepositoryConfigure(repo), true);
                e.Handled = true;
            }
        }

        private void Fetch(object _, RoutedEventArgs e)
        {
            var launcher = this.FindAncestorOfType<Launcher>();
            if (launcher is not null && DataContext is ViewModels.Repository repo)
            {
                var startDirectly = launcher.HasKeyModifier(KeyModifiers.Control);
                launcher.ClearKeyModifier();
                repo.Fetch(startDirectly);
                e.Handled = true;
            }
        }

        private void Pull(object _, RoutedEventArgs e)
        {
            var launcher = this.FindAncestorOfType<Launcher>();
            if (launcher is not null && DataContext is ViewModels.Repository repo)
            {
                if (repo.IsBare)
                {
                    App.RaiseException(repo.FullPath, "Can't run `git pull` in bare repository!");
                    return;
                }

                var startDirectly = launcher.HasKeyModifier(KeyModifiers.Control);
                launcher.ClearKeyModifier();
                repo.Pull(startDirectly);
                e.Handled = true;
            }
        }

        private void Push(object _, RoutedEventArgs e)
        {
            var launcher = this.FindAncestorOfType<Launcher>();
            if (launcher is not null && DataContext is ViewModels.Repository repo)
            {
                var startDirectly = launcher.HasKeyModifier(KeyModifiers.Control);
                launcher.ClearKeyModifier();
                repo.Push(startDirectly);
                e.Handled = true;
            }
        }

        private void StashAll(object _, RoutedEventArgs e)
        {
            var launcher = this.FindAncestorOfType<Launcher>();
            if (launcher is not null && DataContext is ViewModels.Repository repo)
            {
                var startDirectly = launcher.HasKeyModifier(KeyModifiers.Control);
                launcher.ClearKeyModifier();
                repo.StashAll(startDirectly);
                e.Handled = true;
            }
        }

        private void OpenGitFlowMenu(object sender, RoutedEventArgs e)
        {
            if (DataContext is ViewModels.Repository repo && sender is Control control)
            {
                var menuModel = repo.CreateContextMenuForGitFlow();
                var menu = menuModel.CreateContextMenuFromModel();
                menu?.Open(control);
            }

            e.Handled = true;
        }



        private void OpenGitLFSMenu(object sender, RoutedEventArgs e)
        {
            if (DataContext is ViewModels.Repository repo && sender is Control control)
            {
                var menuModel = repo.CreateContextMenuForGitLFS();
                var menu = menuModel.CreateContextMenuFromModel();
                menu?.Open(control);
            }

            e.Handled = true;
        }

        private void StartBisect(object sender, RoutedEventArgs e)
        {
            if (DataContext is ViewModels.Repository { IsBisectCommandRunning: false } repo &&
                repo.InProgressContext == null &&
                repo.CanCreatePopup())
            {
                if (repo.LocalChangesCount > 0)
                    App.RaiseException(repo.FullPath, "You have un-committed local changes. Please discard or stash them first.");
                else
                    repo.Bisect("start");
            }

            e.Handled = true;
        }

        private void OpenCustomActionMenu(object sender, RoutedEventArgs e)
        {
            if (DataContext is ViewModels.Repository repo && sender is Control control)
            {
                var menuModel = repo.CreateContextMenuForCustomAction();
                var menu = menuModel.CreateContextMenuFromModel();
                menu?.Open(control);
            }

            e.Handled = true;
        }

        private void OpenGitLogs(object sender, RoutedEventArgs e)
        {
            if (DataContext is ViewModels.Repository repo)
            {
                App.ShowWindow(new ViewModels.ViewLogs(repo), true);
                e.Handled = true;
            }
        }
    }
}

