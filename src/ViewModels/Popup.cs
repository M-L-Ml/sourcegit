using System;
using System.Diagnostics.CodeAnalysis;
using System.Threading.Tasks;

using Avalonia.Threading;

using CommunityToolkit.Mvvm.ComponentModel;

namespace SourceGit.ViewModels
{
    /// <summary>
    /// Model for popups
    /// For the view and its' OK and Cancel  Buttons
    /// see <see cref="SourceGit.Views.LauncherPage.OnPopupSure"/>
    /// </summary>
    public class Popup : ObservableValidator
    {
        public bool InProgress
        {
            get => _inProgress;
            set => SetProperty(ref _inProgress, value);
        }

        public string ProgressDescription
        {
            get => _progressDescription;
            set => SetProperty(ref _progressDescription, value);
        }

        [UnconditionalSuppressMessage("AssemblyLoadTrimming", "IL2026:RequiresUnreferencedCode")]
        public bool Check()
        {
            if (HasErrors)
                return false;
            ValidateAllProperties();
            return !HasErrors;
        }

        public virtual bool CanStartDirectly()
        {
            return true;
        }

        public virtual Task<bool> Sure()
        {
            return null;
        }

        //TODO: fix MVVM violation. This should be in the View project
        protected void CallUIThread(Action action)
        {
            Dispatcher.UIThread.Invoke(action);
        }
        /// <summary>
        /// todo:rename to UseLog?
        /// </summary>
        /// <param name="log"></param>
        protected void Use(CommandLog log)
        {
            log.Register(newline => ProgressDescription = newline.Trim());
        }

        private bool _inProgress = false;
        private string _progressDescription = string.Empty;
    }
}
