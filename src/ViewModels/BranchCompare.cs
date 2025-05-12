using System;
using System.Collections.Generic;
using System.IO;
using System.Threading.Tasks;
using Avalonia.Threading;

using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;

namespace SourceGit.ViewModels
{
    public class BranchCompare : ObservableObject
    {
        public Models.Branch Base
        {
            get => _based;
            private set => SetProperty(ref _based, value);
        }

        public Models.Branch To
        {
            get => _to;
            private set => SetProperty(ref _to, value);
        }

        public Models.Commit BaseHead
        {
            get => _baseHead;
            private set => SetProperty(ref _baseHead, value);
        }

        public Models.Commit ToHead
        {
            get => _toHead;
            private set => SetProperty(ref _toHead, value);
        }

        public List<Models.Change> VisibleChanges
        {
            get => _visibleChanges;
            private set => SetProperty(ref _visibleChanges, value);
        }

        public List<Models.Change> SelectedChanges
        {
            get => _selectedChanges;
            set
            {
                if (SetProperty(ref _selectedChanges, value))
                {
                    if (value != null && value.Count == 1)
                        DiffContext = new DiffContext(_repo, new Models.DiffOption(_based.Head, _to.Head, value[0]), _diffContext);
                    else
                        DiffContext = null;
                }
            }
        }

        public string SearchFilter
        {
            get => _searchFilter;
            set
            {
                if (SetProperty(ref _searchFilter, value))
                {
                    RefreshVisible();
                }
            }
        }

        public DiffContext DiffContext
        {
            get => _diffContext;
            private set => SetProperty(ref _diffContext, value);
        }

        public BranchCompare(string repo, Models.Branch baseBranch, Models.Branch toBranch)
        {
            _repo = repo;
            _based = baseBranch;
            _to = toBranch;

            Refresh();
        }

        public void NavigateTo(string commitSHA)
        {
            var launcher = App.GetLauncer();
            if (launcher == null)
                return;

            foreach (var page in launcher.Pages)
            {
                if (page.Data is Repository repo && repo.FullPath.Equals(_repo))
                {
                    repo.NavigateToCommit(commitSHA);
                    break;
                }
            }
        }

        public void Swap()
        {
            (Base, To) = (_to, _based);
            SelectedChanges = [];

            if (_baseHead != null)
                (BaseHead, ToHead) = (_toHead, _baseHead);

            Refresh();
        }

        public void ClearSearchFilter()
        {
            SearchFilter = string.Empty;
        }

        public ContextMenuModel CreateChangeContextMenu()
        {
            if (_selectedChanges == null || _selectedChanges.Count != 1)
                return null;

            var change = _selectedChanges[0];
            var menu = new ContextMenuModel();
            var items = menu.Items;

            items.Add(new MenuItemModel
            {
                Header = App.ResText("DiffWithMerger"),
                IconKey = App.MenuIconKey("Icons.OpenWith"),
                Command = new RelayCommand(() =>
                {
                    var toolType = Preferences.Instance.ExternalMergeToolType;
                    var toolPath = Preferences.Instance.ExternalMergeToolPath;
                    var opt = new Models.DiffOption(_based.Head, _to.Head, change);
                    Task.Run(() => Commands.MergeTool.OpenForDiff(_repo, toolType, toolPath, opt));
                })
            });

            if (change.Index != Models.ChangeState.Deleted)
            {
                var full = Path.GetFullPath(Path.Combine(_repo, change.Path));
                items.Add(new MenuItemModel
                {
                    Header = App.ResText("RevealFile"),
                    IconKey = App.MenuIconKey("Icons.Explore"),
                    IsEnabled = File.Exists(full),
                    Command = new RelayCommand(() => Native.OS.OpenInFileManager(full, true))
                });
            }

            items.Add(new MenuItemModel
            {
                Header = App.ResText("CopyPath"),
                IconKey = App.MenuIconKey("Icons.Copy"),
                Command = new RelayCommand(() => App.CopyText(change.Path))
            });

            items.Add(new MenuItemModel
            {
                Header = App.ResText("CopyFullPath"),
                IconKey = App.MenuIconKey("Icons.Copy"),
                Command = new RelayCommand(() => App.CopyText(Native.OS.GetAbsPath(_repo, change.Path)))
            });

            return menu;
        }

        private void Refresh()
        {
            Task.Run(() =>
            {
                if (_baseHead == null)
                {
                    var baseHead = new Commands.QuerySingleCommit(_repo, _based.Head).Result();
                    var toHead = new Commands.QuerySingleCommit(_repo, _to.Head).Result();
                    Dispatcher.UIThread.Invoke(() =>
                    {
                        BaseHead = baseHead;
                        ToHead = toHead;
                    });
                }

                _changes = new Commands.CompareRevisions(_repo, _based.Head, _to.Head).Result();

                var visible = _changes;
                if (!string.IsNullOrWhiteSpace(_searchFilter))
                {
                    visible = new List<Models.Change>();
                    foreach (var c in _changes)
                    {
                        if (c.Path.Contains(_searchFilter, StringComparison.OrdinalIgnoreCase))
                            visible.Add(c);
                    }
                }

                Dispatcher.UIThread.Invoke(() => VisibleChanges = visible);
            });
        }

        private void RefreshVisible()
        {
            if (_changes == null)
                return;

            if (string.IsNullOrEmpty(_searchFilter))
            {
                VisibleChanges = _changes;
            }
            else
            {
                var visible = new List<Models.Change>();
                foreach (var c in _changes)
                {
                    if (c.Path.Contains(_searchFilter, StringComparison.OrdinalIgnoreCase))
                        visible.Add(c);
                }

                VisibleChanges = visible;
            }
        }

        private string _repo;
        private Models.Branch _based = null;
        private Models.Branch _to = null;
        private Models.Commit _baseHead = null;
        private Models.Commit _toHead = null;
        private List<Models.Change> _changes = null;
        private List<Models.Change> _visibleChanges = null;
        private List<Models.Change> _selectedChanges = null;
        private string _searchFilter = string.Empty;
        private DiffContext _diffContext = null;
    }
}
