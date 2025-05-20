using System;
using System.Collections.Generic;
using System.IO;
using System.Threading.Tasks;
using Avalonia.Platform.Storage;
using Avalonia.Threading;

using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;

namespace SourceGit.ViewModels
{
    public class StashesPage : ObservableObject, IDisposable
    {
        public List<Models.Stash> Stashes
        {
            get => _stashes;
            set
            {
                if (SetProperty(ref _stashes, value))
                    RefreshVisible();
            }
        }

        public List<Models.Stash> VisibleStashes
        {
            get => _visibleStashes;
            private set
            {
                if (SetProperty(ref _visibleStashes, value))
                    SelectedStash = null;
            }
        }

        public string SearchFilter
        {
            get => _searchFilter;
            set
            {
                if (SetProperty(ref _searchFilter, value))
                    RefreshVisible();
            }
        }

        public Models.Stash SelectedStash
        {
            get => _selectedStash;
            set
            {
                if (SetProperty(ref _selectedStash, value))
                {
                    if (value == null)
                    {
                        Changes = null;
                    }
                    else
                    {
                        Task.Run(() =>
                        {
                            var changes = null as List<Models.Change>;

                            if (Native.OS.GitVersion >= Models.GitVersions.STASH_SHOW_WITH_UNTRACKED)
                            {
                                changes = new Commands.QueryStashChanges(_repo.FullPath, value.Name).Result();
                            }
                            else
                            {
                                changes = new Commands.CompareRevisions(_repo.FullPath, $"{value.SHA}^", value.SHA).Result();
                                if (value.Parents.Count == 3)
                                {
                                    var untracked = new Commands.CompareRevisions(_repo.FullPath, "4b825dc642cb6eb9a060e54bf8d69288fbee4904", value.Parents[2]).Result();
                                    var needSort = changes.Count > 0;

                                    foreach (var c in untracked)
                                        changes.Add(c);

                                    if (needSort)
                                        changes.Sort((l, r) => string.Compare(l.Path, r.Path, StringComparison.Ordinal));
                                }
                            }

                            Dispatcher.UIThread.Invoke(() => Changes = changes);
                        });
                    }
                }
            }
        }

        public List<Models.Change> Changes
        {
            get => _changes;
            private set
            {
                if (SetProperty(ref _changes, value))
                    SelectedChange = value is { Count: > 0 } ? value[0] : null;
            }
        }

        public Models.Change SelectedChange
        {
            get => _selectedChange;
            set
            {
                if (SetProperty(ref _selectedChange, value))
                {
                    if (value == null)
                        DiffContext = null;
                    else if (value.Index == Models.ChangeState.Added && _selectedStash.Parents.Count == 3)
                        DiffContext = new DiffContext(_repo.FullPath, new Models.DiffOption("4b825dc642cb6eb9a060e54bf8d69288fbee4904", _selectedStash.Parents[2], value), _diffContext);
                    else
                        DiffContext = new DiffContext(_repo.FullPath, new Models.DiffOption(_selectedStash.Parents[0], _selectedStash.SHA, value), _diffContext);
                }
            }
        }

        public DiffContext DiffContext
        {
            get => _diffContext;
            private set => SetProperty(ref _diffContext, value);
        }

        public StashesPage(Repository repo)
        {
            _repo = repo;
        }

        public void Dispose()
        {
            _stashes?.Clear();
            _changes?.Clear();

            _repo = null;
            _selectedStash = null;
            _selectedChange = null;
            _diffContext = null;
        }

        public ContextMenuModel MakeContextMenuModel(Models.Stash stash)
        {
            if (stash == null)
                return null;

            var menu = new ContextMenuModel();

            menu.Items.Add(new MenuItemModel
            {
                Header = App.ResText("StashCM.Apply"),
                Command = new RelayCommand(() =>
                {
                    if (_repo.CanCreatePopup())
                        _repo.ShowPopup(new ApplyStash(_repo, stash));
                })
            });
            menu.Items.Add(new MenuItemModel
            {
                Header = App.ResText("StashCM.Drop"),
                Command = new RelayCommand(() =>
                {
                    if (_repo.CanCreatePopup())
                        _repo.ShowPopup(new DropStash(_repo, stash));
                })
            });
            menu.Items.Add(MenuModel.Separator());
            menu.Items.Add(new MenuItemModel
            {
                Header = App.ResText("StashCM.SaveAsPatch"),
                IconKey = App.MenuIconKey("Icons.Diff"),
                Command = new AsyncRelayCommand(async () =>
                {
                    var storageProvider = App.GetStorageProvider();
                    if (storageProvider == null)
                        return;

                    var options = new FilePickerSaveOptions();
                    options.Title = App.Text("StashCM.SaveAsPatch");
                    options.DefaultExtension = ".patch";
                    options.FileTypeChoices = [new FilePickerFileType("Patch File") { Patterns = ["*.patch"] }];

                    var storageFile = await storageProvider.SaveFilePickerAsync(options);
                    if (storageFile != null)
                    {
                        var opts = new List<Models.DiffOption>();
                        foreach (var c in _changes)
                        {
                            if (c.Index == Models.ChangeState.Added && _selectedStash.Parents.Count == 3)
                                opts.Add(new Models.DiffOption("4b825dc642cb6eb9a060e54bf8d69288fbee4904", _selectedStash.Parents[2], c));
                            else
                                opts.Add(new Models.DiffOption(_selectedStash.Parents[0], _selectedStash.SHA, c));
                        }

                        var succ = await Task.Run(() => Commands.SaveChangesAsPatch.ProcessStashChanges(_repo.FullPath, opts, storageFile.Path.LocalPath));
                        if (succ)
                            App.SendNotification(_repo.FullPath, App.Text("SaveAsPatchSuccess"));
                    }
                })
            });
            return menu;
        }

        public ContextMenuModel MakeContextMenuModelForChange(Models.Change change)
        {
            if (change == null)
                return null;

            var menu = new ContextMenuModel();

            menu.Items.Add(new MenuItemModel
            {
                Header = App.ResText("DiffWithMerger"),
                IconKey = App.MenuIconKey("Icons.OpenWith"),
                Command = new RelayCommand(() =>
                {
                    var toolType = Preferences.Instance.ExternalMergeToolType;
                    var toolPath = Preferences.Instance.ExternalMergeToolPath;
                    var opt = new Models.DiffOption($"{_selectedStash.SHA}^", _selectedStash.SHA, change);
                    Task.Run(() => Commands.MergeTool.OpenForDiff(_repo.FullPath, toolType, toolPath, opt));
                })
            });

            var fullPath = Path.Combine(_repo.FullPath, change.Path);
            menu.Items.Add(new MenuItemModel
            {
                Header = App.ResText("RevealFile"),
                IconKey = App.MenuIconKey("Icons.Explore"),
                IsEnabled = File.Exists(fullPath),
                Command = new RelayCommand(() => Native.OS.OpenInFileManager(fullPath, true))
            });
            menu.Items.Add(MenuModel.Separator());
            menu.Items.Add(new MenuItemModel
            {
                Header = App.ResText("ChangeCM.CheckoutThisRevision"),
                IconKey = App.MenuIconKey("Icons.File.Checkout"),
                Command = new RelayCommand(() =>
                {
                    var log = _repo.CreateLog($"Reset File to '{_selectedStash.SHA}'");
                    new Commands.Checkout(_repo.FullPath).Use(log).FileWithRevision(change.Path, $"{_selectedStash.SHA}");
                    log.Complete();
                })
            });
            menu.Items.Add(MenuModel.Separator());
            menu.Items.Add(new MenuItemModel
            {
                Header = App.ResText("CopyPath"),
                IconKey = App.MenuIconKey("Icons.Copy"),
                Command = new RelayCommand(() => App.CopyText(change.Path))
            });
            menu.Items.Add(new MenuItemModel
            {
                Header = App.ResText("CopyFullPath"),
                IconKey = App.MenuIconKey("Icons.Copy"),
                Command = new RelayCommand(() => App.CopyText(Native.OS.GetAbsPath(_repo.FullPath, change.Path)))
            });

            return menu;
        }

        public void Clear()
        {
            if (_repo.CanCreatePopup())
                _repo.ShowPopup(new ClearStashes(_repo));
        }

        public void ClearSearchFilter()
        {
            SearchFilter = string.Empty;
        }

        private void RefreshVisible()
        {
            if (string.IsNullOrEmpty(_searchFilter))
            {
                VisibleStashes = _stashes;
            }
            else
            {
                var visible = new List<Models.Stash>();
                foreach (var s in _stashes)
                {
                    if (s.Message.Contains(_searchFilter, StringComparison.OrdinalIgnoreCase))
                        visible.Add(s);
                }

                VisibleStashes = visible;
            }
        }

        private Repository _repo = null;
        private List<Models.Stash> _stashes = new List<Models.Stash>();
        private List<Models.Stash> _visibleStashes = new List<Models.Stash>();
        private string _searchFilter = string.Empty;
        private Models.Stash _selectedStash = null;
        private List<Models.Change> _changes = null;
        private Models.Change _selectedChange = null;
        private DiffContext _diffContext = null;
    }
}
