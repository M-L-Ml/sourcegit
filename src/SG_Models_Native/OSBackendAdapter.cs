using System;
using Avalonia;
using Avalonia.Controls;
// Original interfaces from Models1 project
using Sausa;
using SourceGit.Models;
using static SourceGit.Native.OS;

using ShellOrTerminalModel = SourceGit.Models.ShellOrTerminal;

namespace SourceGit.Native
{
    /// <summary>
    /// Adapter class to bridge between the platform-independent interfaces and OS.IBackend implementations
    /// </summary>
    internal class OSBackendAdapter : IBackend
    {
        private readonly IOSPlatform _platform;

        public OSBackendAdapter(IOSPlatform platform)
        {
            _platform = platform ?? throw new ArgumentNullException(nameof(platform));
        }

        // Implementation of OS.IBackend methods using the platform-agnostic interfaces
        
        public void SetupApp(AppBuilder builder)
        {
            _platform.SetupApp(builder);
        }

        public void SetupWindow(Window window)
        {
            _platform.SetupWindow(window);
        }

        public string FindGitExecutable()
        {
            return _platform.FindGitExecutable();
        }

        public string FindTerminal(ShellOrTerminalModel shell)
        {
            // Create a Sausa.ShellOrTerminal with the same parameters
         //   var adaptedShell = new ShellOrTerminalModel(shell.Type, shell.Name, shell.Exec);


            return _platform.FindTerminal(shell);
       
            // Cast to specific platform implementations based on platform type

            //    if (_platform is Windows windows)
            //{
            //    return windows.FindTerminal(adaptedShell);
            //}
            //else if (_platform is MacOS macOS)
            //{
            //    return macOS.FindTerminal(adaptedShell);
            //}
            //else if (_platform is Linux linux)
            //{
            //    return linux.FindTerminal(adaptedShell);
            //}
            
            //return string.Empty;
        }

        public ExternalToolsFinder2 FindExternalTools()
        {
            // Convert between the two ExternalToolsFinder types
            var platformFinder = _platform.FindExternalTools();
           // var result = new ExternalToolsFinder2();
            
            // Implementation of conversion logic would go here
            // This would typically involve copying over the tools from platformFinder to result
            
            return (ExternalToolsFinder2)platformFinder;
        }

        public void OpenTerminal(string workdir)
        {
            _platform.OpenTerminal(workdir);
        }

        public void OpenInFileManager(string path, bool select)
        {
            _platform.OpenInFileManager(path, select);

            //if (_platform is Windows windows)
            //{
            //    windows.OpenInFileManager(path, select);
            //}
            //else if (_platform is MacOS macOS)
            //{
            //    macOS.OpenInFileManager(path, select);
            //}
            //else if (_platform is Linux linux)
            //{
            //    linux.OpenInFileManager(path, select);
            //}
        }

        public void OpenBrowser(string url)
        {
            _platform.OpenBrowser(url);
        }

        public void OpenWithDefaultEditor(string file)
        {
            _platform.OpenWithDefaultEditor(file);

            //if (_platform is Windows windows)
            //{
            //    windows.OpenWithDefaultEditor(file);
            //}
            //else if (_platform is MacOS macOS)
            //{
            //    macOS.OpenWithDefaultEditor(file);
            //}
            //else if (_platform is Linux linux)
            //{
            //    linux.OpenWithDefaultEditor(file);
            //}
        }
    }
}
