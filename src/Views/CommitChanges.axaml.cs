using Avalonia.Controls;

namespace SourceGit.Views
{
    public partial class CommitChanges : UserControl
    {
        public CommitChanges()
        {
            InitializeComponent();
        }

        private void OnChangeContextRequested(object sender, ContextRequestedEventArgs e)
        {
            if (sender is ChangeCollectionView { SelectedChanges: { } selected } view &&
                selected.Count == 1 &&
                DataContext is ViewModels.CommitDetail vm)
            {
                var menuModel = vm.CreateChangeContextMenu(selected[0]);
                var menu = menuModel?.CreateContextMenuFromModel();
                menu?.Open(view);
            }

            e.Handled = true;
        }
    }
}
