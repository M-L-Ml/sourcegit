using System;
using System.Diagnostics;
using Avalonia.Controls;
using Avalonia.Controls.Presenters;
using Avalonia.Input;
using Avalonia.Interactivity;

namespace SourceGit.Views
{
    public partial class LauncherPage : UserControl
    {
        public LauncherPage()
        {
            InitializeComponent();
        }

        public ViewModels.LauncherPage? LauncherPageModel => DataContext as ViewModels.LauncherPage;
        /// <summary>
        /// it's OK button
        /// </summary>
        /// <param name="_"></param>
        /// <param name="e"></param>
        private void OnPopupSure(object _, RoutedEventArgs e)
        {
            Debug.Assert(LauncherPageModel != null);
            LauncherPageModel?.ProcessPopup();

            e.Handled = true;
        }

        private void OnPopupCancel(object _, RoutedEventArgs e)
        {
            Debug.Assert(LauncherPageModel != null);
            LauncherPageModel?.CancelPopup();

            e.Handled = true;
        }

        private void OnMaskClicked(object sender, PointerPressedEventArgs e)
        {
            OnPopupCancel(sender, e);
        }

        private void OnCopyNotification(object sender, RoutedEventArgs e)
        {
            if (sender is Button { DataContext: Models.Notification notice })
                App.CopyText(notice.Message);

            e.Handled = true;
        }

        private void OnDismissNotification(object sender, RoutedEventArgs e)
        {
            if (sender is Button { DataContext: Models.Notification notice } &&
                DataContext is ViewModels.LauncherPage page)
                page.Notifications.Remove(notice);

            e.Handled = true;
        }

        private void OnPopupDataContextChanged(object sender, EventArgs e)
        {
            if (sender is ContentPresenter presenter)
            {
                if (presenter.DataContext == null || presenter.DataContext is not ViewModels.Popup)
                {
                    presenter.Content = null;
                    return;
                }

                var dataTypeName = presenter.DataContext.GetType().FullName;
                if (string.IsNullOrEmpty(dataTypeName))
                {
                    presenter.Content = null;
                    return;
                }

                var viewTypeName = dataTypeName.Replace(".ViewModels.", ".Views.");
                var viewType = Type.GetType(viewTypeName);
                if (viewType == null)
                {
                    presenter.Content = null;
                    return;
                }

                var view = Activator.CreateInstance(viewType);
                presenter.Content = view;
            }
        }
    }
}
