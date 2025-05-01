using System;
using System.Collections.Generic;
using System.Reflection;
using Avalonia.Controls;
using Avalonia.Controls.ApplicationLifetimes;
using SourceGit.ViewModels.Services;

namespace SourceGit.Services
{
    public class WindowService : IWindowService
    {
        public WindowService()
        {
            _windowCache = new Dictionary<string, Window>();
        }

        public void ShowWindow(string windowName, bool isModal = false)
        {
            // Ensure window name has the full namespace if not provided
            if (!windowName.Contains('.'))
            {
                windowName = $"SourceGit.Views.{windowName}";
            }

            try
            {
                // Use reflection to create the window instance
                var windowType = Type.GetType(windowName) ?? 
                                 Assembly.GetExecutingAssembly().GetType(windowName);
                
                if (windowType == null)
                {
                    throw new InvalidOperationException($"Window type '{windowName}' not found");
                }

                var window = Activator.CreateInstance(windowType) as Window;
                if (window == null)
                {
                    throw new InvalidOperationException($"Failed to create window instance of type '{windowName}'");
                }

                // Cache the window if we need to close it later
                _windowCache[windowName] = window;

                if (isModal)
                {
                    var mainWindow = GetMainWindow();
                    if (mainWindow != null)
                    {
                        window.ShowDialog(mainWindow);
                    }
                    else
                    {
                        window.Show();
                    }
                }
                else
                {
                    window.Show();
                }
            }
            catch (Exception ex)
            {
                // Log exception or handle it appropriately
                Console.WriteLine($"Error showing window: {ex.Message}");
            }
        }

        public void CloseWindow(string windowName)
        {
            if (!windowName.Contains('.'))
            {
                windowName = $"SourceGit.Views.{windowName}";
            }

            if (_windowCache.TryGetValue(windowName, out var window))
            {
                window.Close();
                _windowCache.Remove(windowName);
            }
        }

        private static Window GetMainWindow()
        {
            if (Avalonia.Application.Current?.ApplicationLifetime is IClassicDesktopStyleApplicationLifetime desktop)
            {
                return desktop.MainWindow;
            }
            return null;
        }

        private readonly Dictionary<string, Window> _windowCache;
    }
}
