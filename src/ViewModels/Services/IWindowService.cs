using System;
using System.Threading.Tasks;

namespace SourceGit.ViewModels.Services
{
    /// <summary>
    /// Service interface for managing window/dialog operations, defined in ViewModels layer
    /// but implemented in UI layer to respect MVVM separation.
    /// </summary>
    public interface IWindowService
    {
        /// <summary>
        /// Shows a window by its logical name/key
        /// </summary>
        /// <param name="windowKey">Key identifying which window to show (e.g. "Preferences", "About")</param>
        /// <param name="isModal">Whether to show as modal dialog</param>
        void ShowWindow(string windowKey, bool isModal = false);
        
        /// <summary>
        /// Shows a window by its logical name and with a ViewModel
        /// </summary>
        /// <param name="windowKey">Key identifying which window to show</param>
        /// <param name="viewModel">ViewModel to set as DataContext</param>
        /// <param name="isModal">Whether to show as modal dialog</param>
        void ShowWindow(string windowKey, object viewModel, bool isModal = false);
        
        /// <summary>
        /// Shows a dialog and returns a result asynchronously
        /// </summary>
        /// <typeparam name="TResult">Type of result from dialog</typeparam>
        /// <param name="dialogKey">Key identifying which dialog to show</param>
        /// <param name="viewModel">Optional ViewModel to set as DataContext</param>
        /// <returns>Result from dialog</returns>
        Task<TResult> ShowDialogAsync<TResult>(string dialogKey, object viewModel = null);
        
        /// <summary>
        /// Closes a window by its logical name/key
        /// </summary>
        /// <param name="windowKey">Key identifying which window to close</param>
        void CloseWindow(string windowKey);
    }
}
