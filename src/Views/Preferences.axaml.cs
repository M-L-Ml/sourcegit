using System;
using System.Collections.Generic;

using Avalonia;
using Avalonia.Controls;
using Avalonia.Interactivity;
using Avalonia.Platform.Storage;

namespace SourceGit.Views
{
    public partial class Preferences : ChromelessWindow
    {
        public Preferences()
        {
            DataContext = ViewModels.Preferences.Instance;
            InitializeComponent();
            
            // Load git configuration after initialization
            ViewModels.Preferences.Instance.LoadGitConfig();
        }
        
        protected override void OnClosing(WindowClosingEventArgs e)
        {
            base.OnClosing(e);

            if (Design.IsDesignMode)
                return;

            // Save git configuration on window closing
            ViewModels.Preferences.Instance.SaveGitConfig();
        }

        private async void SelectThemeOverrideFile(object _, RoutedEventArgs e)
        {
            var options = new FilePickerOpenOptions()
            {
                FileTypeFilter = [new FilePickerFileType("Theme Overrides File") { Patterns = ["*.json"] }],
                AllowMultiple = false,
            };

            var selected = await StorageProvider.OpenFilePickerAsync(options);
            if (selected.Count == 1)
            {
                ViewModels.Preferences.Instance.ThemeOverrides = selected[0].Path.LocalPath;
            }

            e.Handled = true;
        }

        private async void SelectGitExecutable(object _, RoutedEventArgs e)
        {
            var pattern = OperatingSystem.IsWindows() ? "git.exe" : "git";
            var options = new FilePickerOpenOptions()
            {
                FileTypeFilter = [new FilePickerFileType("Git Executable") { Patterns = [pattern] }],
                AllowMultiple = false,
            };

            var selected = await StorageProvider.OpenFilePickerAsync(options);
            if (selected.Count == 1)
            {
                ViewModels.Preferences.Instance.GitInstallPath = selected[0].Path.LocalPath;
                ViewModels.Preferences.Instance.UpdateGitVersion();
            }

            e.Handled = true;
        }

        private async void SelectDefaultCloneDir(object _, RoutedEventArgs e)
        {
            var options = new FolderPickerOpenOptions() { AllowMultiple = false };
            try
            {
                var selected = await StorageProvider.OpenFolderPickerAsync(options);
                if (selected.Count == 1)
                {
                    ViewModels.Preferences.Instance.GitDefaultCloneDir = selected[0].Path.LocalPath;
                }
            }
            catch (Exception ex)
            {
                App.RaiseException(string.Empty, $"Failed to select default clone directory: {ex.Message}");
            }

            e.Handled = true;
        }

        private async void SelectGPGExecutable(object _, RoutedEventArgs e)
        {
            var pref = ViewModels.Preferences.Instance;
            if (pref.GPGFormat == null)
                return;
                
            var patterns = new List<string>();
            if (OperatingSystem.IsWindows())
                patterns.Add($"{pref.GPGFormat.Program}.exe");
            else
                patterns.Add(pref.GPGFormat.Program);

            var options = new FilePickerOpenOptions()
            {
                FileTypeFilter = [new FilePickerFileType("GPG Program") { Patterns = patterns }],
                AllowMultiple = false,
            };

            var selected = await StorageProvider.OpenFilePickerAsync(options);
            if (selected.Count == 1)
            {
                pref.GPGExecutableFile = selected[0].Path.LocalPath;
            }

            e.Handled = true;
        }

        private async void SelectShellOrTerminal(object _, RoutedEventArgs e)
        {
            var type = ViewModels.Preferences.Instance.ShellOrTerminal;
            if (type == -1)
                return;

            var shell = Models.ShellOrTerminal.Supported[type];
            var options = new FilePickerOpenOptions()
            {
                FileTypeFilter = [new FilePickerFileType(shell.Name) { Patterns = [shell.Exec] }],
                AllowMultiple = false,
            };

            var selected = await StorageProvider.OpenFilePickerAsync(options);
            if (selected.Count == 1)
            {
                ViewModels.Preferences.Instance.ShellOrTerminalPath = selected[0].Path.LocalPath;
            }

            e.Handled = true;
        }

        private async void SelectExternalMergeTool(object _, RoutedEventArgs e)
        {
            var type = ViewModels.Preferences.Instance.ExternalMergeToolType;
            if (type < 0 || type >= Models.ExternalMerger.Supported.Count)
            {
                ViewModels.Preferences.Instance.ExternalMergeToolType = 0;
                e.Handled = true;
                return;
            }

            var tool = Models.ExternalMerger.Supported[type];
            var options = new FilePickerOpenOptions()
            {
                FileTypeFilter = [new FilePickerFileType(tool.Name) { Patterns = tool.GetPatterns() }],
                AllowMultiple = false,
            };

            var selected = await StorageProvider.OpenFilePickerAsync(options);
            if (selected.Count == 1)
            {
                ViewModels.Preferences.Instance.ExternalMergeToolPath = selected[0].Path.LocalPath;
            }

            e.Handled = true;
        }

        private void OnUseNativeWindowFrameChanged(object sender, RoutedEventArgs e)
        {
            if (sender is CheckBox box)
            {
                ViewModels.Preferences.Instance.UseSystemWindowFrame = box.IsChecked == true;
                App.ShowWindow(new ConfirmRestart(), true);
            }

            e.Handled = true;
        }

        private void OnGitInstallPathChanged(object sender, TextChangedEventArgs e)
        {
            ViewModels.Preferences.Instance.UpdateGitVersion();
        }

        private void OnAddOpenAIService(object sender, RoutedEventArgs e)
        {
            var service = new Models.OpenAIService() { Name = "Unnamed Service" };
            ViewModels.Preferences.Instance.OpenAIServices.Add(service);
            ViewModels.Preferences.Instance.SelectedOpenAIService = service;

            e.Handled = true;
        }

        private void OnRemoveSelectedOpenAIService(object sender, RoutedEventArgs e)
        {
            if (ViewModels.Preferences.Instance.SelectedOpenAIService == null)
                return;

            ViewModels.Preferences.Instance.OpenAIServices.Remove(ViewModels.Preferences.Instance.SelectedOpenAIService);
            ViewModels.Preferences.Instance.SelectedOpenAIService = null;
            e.Handled = true;
        }

        private void OnAddCustomAction(object sender, RoutedEventArgs e)
        {
            var action = new Models.CustomAction() { Name = "Unnamed Action (Global)" };
            ViewModels.Preferences.Instance.CustomActions.Add(action);
            ViewModels.Preferences.Instance.SelectedCustomAction = action;

            e.Handled = true;
        }

        private async void SelectExecutableForCustomAction(object sender, RoutedEventArgs e)
        {
            var options = new FilePickerOpenOptions()
            {
                FileTypeFilter = [new FilePickerFileType("Executable file(script)") { Patterns = ["*.*"] }],
                AllowMultiple = false,
            };

            var selected = await StorageProvider.OpenFilePickerAsync(options);
            if (selected.Count == 1 && sender is Button { DataContext: Models.CustomAction action })
                action.Executable = selected[0].Path.LocalPath;

            e.Handled = true;
        }

        private void OnRemoveSelectedCustomAction(object sender, RoutedEventArgs e)
        {
            if (ViewModels.Preferences.Instance.SelectedCustomAction == null)
                return;

            ViewModels.Preferences.Instance.CustomActions.Remove(ViewModels.Preferences.Instance.SelectedCustomAction);
            ViewModels.Preferences.Instance.SelectedCustomAction = null;
            e.Handled = true;
        }
    }
}
