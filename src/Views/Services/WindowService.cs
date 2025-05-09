using System;
using System.Collections.Generic;
using System.Reflection;
using System.Threading.Tasks;
using Avalonia.Controls;
using Avalonia.Controls.ApplicationLifetimes;
using SourceGit.ViewModels.Services;

namespace SourceGit.Views.Services
{
    /// <summary>
    /// Implements IWindowService to provide window/dialog management in the Views layer.
    /// This follows proper MVVM as the implementation resides in the Views project
    /// while the interface resides in the ViewModels project.
    /// </summary>
    public class WindowService : IWindowService
    {
        public WindowService()
        {
            _windowCache = new Dictionary<string, Window>();
            _viewTypeMapping = InitializeViewMapping();
        }

        #region IWindowService Implementation

        public void ShowWindow2(string windowKey, bool isModal = false)
        {
            ShowWindow2Internal(windowKey, null, isModal);
        }

        public void ShowWindow2(string windowKey, object viewModel, bool isModal = false)
        {
            ShowWindow2Internal(windowKey, viewModel, isModal);
        }

        public Task<TResult> ShowDialogAsync<TResult>(string dialogKey, object viewModel = null)
        {
            var window = CreateWindow(dialogKey, viewModel);
            if (window == null)
            {
                return Task.FromResult(default(TResult));
            }

            var tcs = new TaskCompletionSource<TResult>();

            window.Closed += (s, e) =>
            {
                if (window is IDialogWindow<TResult> dialogWindow)
                {
                    tcs.SetResult(dialogWindow.DialogResult);
                }
                else
                {
                    tcs.SetResult(default);
                }
            };

            var mainWindow = GetMainWindow();
            if (mainWindow != null)
            {
                window.ShowDialog(mainWindow);
            }
            else
            {
                window.Show();
                tcs.SetResult(default);
            }

            return tcs.Task;
        }

        public void CloseWindow(string windowKey)
        {
            var windowName = GetFullWindowName(windowKey);

            if (_windowCache.TryGetValue(windowName, out var window))
            {
                window.Close();
                _windowCache.Remove(windowName);
            }
        }

        #endregion

        #region Helper Methods

        private void ShowWindow2Internal(string windowKey, object viewModel, bool isModal)
        {
            try
            {
                var window = CreateWindow(windowKey, viewModel);
                if (window == null) return;

                // Cache the window if we need to close it later
                _windowCache[GetFullWindowName(windowKey)] = window;

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
                throw;
            }
        }

        private Window CreateWindow(string windowKey, object viewModel = null)
        {
            var windowName = GetFullWindowName(windowKey);
            
            // First try to resolve from our mapping dictionary
            Type windowType = null;
            if (_viewTypeMapping.TryGetValue(windowKey, out var mappedType))
            {
                windowType = mappedType;
            }
            else
            {
                // Fall back to reflection if not in mapping
                windowType = Type.GetType(windowName) ?? 
                             Assembly.GetExecutingAssembly().GetType(windowName);
            }
            
            if (windowType == null)
            {
                Console.WriteLine($"Window type '{windowName}' not found");
                return null;
            }

            var window = Activator.CreateInstance(windowType) as Window;
            if (window == null)
            {
                Console.WriteLine($"Failed to create window instance of type '{windowName}'");
                return null;
            }

            // Set DataContext if a viewModel was provided
            if (viewModel != null)
            {
                window.DataContext = viewModel;
            }

            return window;
        }

        private static Window GetMainWindow()
        {
            if (ViewModels.App.CurrentDesktopAppLifetime is { } desktop)
            {
                return desktop.MainWindow;
            }
            return null;
        }

        private static string GetFullWindowName(string windowKey)
        {
            return windowKey.Contains('.') ? windowKey : $"SourceGit.Views.{windowKey}";
        }

        // Create a static mapping of window keys to concrete window types
        // This avoids reflection and provides compile-time safety
        private static Dictionary<string, Type> InitializeViewMapping()
        {
            return new Dictionary<string, Type>(StringComparer.OrdinalIgnoreCase)
            {
                { "Preferences", typeof(Preferences) },
                { "About", typeof(About) },
                { "Hotkeys", typeof(Hotkeys) }
                // Add other view mappings as needed
            };
        }

        #endregion

        private readonly Dictionary<string, Window> _windowCache;
        private readonly Dictionary<string, Type> _viewTypeMapping;
    }

    /// <summary>
    /// Interface for dialog windows that return a result
    /// </summary>
    public interface IDialogWindow<out TResult>
    {
        TResult DialogResult { get; }
    }
}
