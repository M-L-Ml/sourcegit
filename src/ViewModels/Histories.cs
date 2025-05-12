using System;
using System.Collections;
using System.Collections.Generic;
using System.IO;
using System.Text;
using System.Threading.Tasks;

using Avalonia.Controls;
using Avalonia.Platform.Storage;

using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;

namespace SourceGit.ViewModels
{
    public class Histories : ObservableObject
    {
        public Repository Repo
        {
            get => _repo;
        }

        public bool IsLoading
        {
            get => _isLoading;
            set => SetProperty(ref _isLoading, value);
        }

        public List<Models.Commit> Commits
        {
            get => _commits;
            set
            {
                var lastSelected = AutoSelectedCommit;
                if (SetProperty(ref _commits, value))
                {
                    if (value.Count > 0 && lastSelected != null)
                        AutoSelectedCommit = value.Find(x => x.SHA == lastSelected.SHA);
                }
            }
        }

        public Models.CommitGraph Graph
        {
            get => _graph;
            set => SetProperty(ref _graph, value);
        }

        public Models.Commit AutoSelectedCommit
        {
            get => _autoSelectedCommit;
            set => SetProperty(ref _autoSelectedCommit, value);
        }

        public long NavigationId
        {
            get => _navigationId;
            private set => SetProperty(ref _navigationId, value);
        }

        public object DetailContext
        {
            get => _detailContext;
            set => SetProperty(ref _detailContext, value);
        }

        public Models.Bisect Bisect
        {
            get => _bisect;
            private set => SetProperty(ref _bisect, value);
        }

        public GridLength LeftArea
        {
            get => _leftArea;
            set => SetProperty(ref _leftArea, value);
        }

        public GridLength RightArea
        {
            get => _rightArea;
            set => SetProperty(ref _rightArea, value);
        }

        public GridLength TopArea
        {
            get => _topArea;
            set => SetProperty(ref _topArea, value);
        }

        public GridLength BottomArea
        {
            get => _bottomArea;
            set => SetProperty(ref _bottomArea, value);
        }

        public Histories(Repository repo)
        {
            _repo = repo;
        }

        public void Cleanup()
        {
            Commits = new List<Models.Commit>();

            _repo = null;
            _graph = null;
            _autoSelectedCommit = null;

            if (_detailContext is CommitDetail cd)
            {
                cd.Cleanup();
            }
            else if (_detailContext is RevisionCompare rc)
            {
                rc.Cleanup();
            }

            _detailContext = null;
        }

        public Models.BisectState UpdateBisectInfo()
        {
            var test = Path.Combine(_repo.GitDir, "BISECT_START");
            if (!File.Exists(test))
            {
                Bisect = null;
                return Models.BisectState.None;
            }

            var info = new Models.Bisect();
            var dir = Path.Combine(_repo.GitDir, "refs", "bisect");
            if (Directory.Exists(dir))
            {
                var files = new DirectoryInfo(dir).GetFiles();
                foreach (var file in files)
                {
                    if (file.Name.StartsWith("bad"))
                        info.Bads.Add(File.ReadAllText(file.FullName).Trim());
                    else if (file.Name.StartsWith("good"))
                        info.Goods.Add(File.ReadAllText(file.FullName).Trim());
                }
            }

            Bisect = info;

            if (info.Bads.Count == 0 || info.Goods.Count == 0)
                return Models.BisectState.WaitingForRange;
            else
                return Models.BisectState.Detecting;
        }

        public void NavigateTo(string commitSHA)
        {
            var commit = _commits.Find(x => x.SHA.StartsWith(commitSHA, StringComparison.Ordinal));
            if (commit == null)
            {
                AutoSelectedCommit = null;
                commit = new Commands.QuerySingleCommit(_repo.FullPath, commitSHA).Result();
            }
            else
            {
                AutoSelectedCommit = commit;
                NavigationId = _navigationId + 1;
            }

            if (commit != null)
            {
                if (_detailContext is CommitDetail detail)
                {
                    detail.Commit = commit;
                }
                else
                {
                    var commitDetail = new CommitDetail(_repo);
                    commitDetail.Commit = commit;
                    DetailContext = commitDetail;
                }
            }
            else
            {
                DetailContext = null;
            }
        }

        public void Select(IList commits)
        {
            if (commits.Count == 0)
            {
                _repo.SelectedSearchedCommit = null;
                DetailContext = null;
            }
            else if (commits.Count == 1)
            {
                var commit = (commits[0] as Models.Commit)!;
                if (_repo.SelectedSearchedCommit == null || _repo.SelectedSearchedCommit.SHA != commit.SHA)
                    _repo.SelectedSearchedCommit = _repo.SearchedCommits.Find(x => x.SHA == commit.SHA);

                AutoSelectedCommit = commit;
                NavigationId = _navigationId + 1;

                if (_detailContext is CommitDetail detail)
                {
                    detail.Commit = commit;
                }
                else
                {
                    var commitDetail = new CommitDetail(_repo);
                    commitDetail.Commit = commit;
                    DetailContext = commitDetail;
                }
            }
            else if (commits.Count == 2)
            {
                _repo.SelectedSearchedCommit = null;

                var end = commits[0] as Models.Commit;
                var start = commits[1] as Models.Commit;
                DetailContext = new RevisionCompare(_repo.FullPath, start, end);
            }
            else
            {
                _repo.SelectedSearchedCommit = null;
                DetailContext = commits.Count;
            }
        }

        public void DoubleTapped(Models.Commit commit)
        {
            if (commit == null || commit.IsCurrentHead)
                return;

            var firstRemoteBranch = null as Models.Branch;
            foreach (var d in commit.Decorators)
            {
                if (d.Type == Models.DecoratorType.LocalBranchHead)
                {
                    var b = _repo.Branches.Find(x => x.FriendlyName == d.Name);
                    if (b != null)
                    {
                        _repo.CheckoutBranch(b);
                        return;
                    }
                }
                else if (d.Type == Models.DecoratorType.RemoteBranchHead && firstRemoteBranch == null)
                {
                    firstRemoteBranch = _repo.Branches.Find(x => x.FriendlyName == d.Name);
                }
            }

            if (_repo.CanCreatePopup())
            {
                if (firstRemoteBranch != null)
                    _repo.ShowPopup(new CreateBranch(_repo, firstRemoteBranch));
                else if (!_repo.IsBare)
                    _repo.ShowPopup(new CheckoutCommit(_repo, commit));
            }
        }

        public ContextMenuModel MakeContextMenu(ListBox list)
        {
            var current = _repo.CurrentBranch;
            if (current == null || list.SelectedItems == null)
                return null;

            if (list.SelectedItems.Count > 1)
            {
                var selected = new List<Models.Commit>();
                var canCherryPick = true;
                var canMerge = true;

                foreach (var item in list.SelectedItems)
                {
                    if (item is Models.Commit c)
                    {
                        selected.Add(c);

                        if (c.IsMerged)
                        {
                            canMerge = false;
                            canCherryPick = false;
                        }
                        else if (c.Parents.Count > 1)
                        {
                            canCherryPick = false;
                        }
                    }
                }

                // Sort selected commits in order.
                selected.Sort((l, r) => _commits.IndexOf(r) - _commits.IndexOf(l));

                var multipleMenu = new ContextMenuModel();
                var items = multipleMenu.Items;

                if (!_repo.IsBare)
                {
                    if (canCherryPick)
                    {
                        items.Add(new MenuItemModel
                        {
                            Header = App.ResText("CommitCM.CherryPickMultiple"),
                            IconKey = App.MenuIconKey("Icons.CherryPick"),
                            Command = new RelayCommand(() =>
                            {
                                if (_repo.CanCreatePopup())
                                    _repo.ShowPopup(new CherryPick(_repo, selected));
                            })
                        });
                    }

                    if (canMerge)
                    {
                        items.Add(new MenuItemModel
                        {
                            Header = App.ResText("CommitCM.MergeMultiple"),
                            IconKey = App.MenuIconKey("Icons.Merge"),
                            Command = new RelayCommand(() =>
                            {
                                if (_repo.CanCreatePopup())
                                    _repo.ShowPopup(new MergeMultiple(_repo, selected));
                            })
                        });
                    }

                    if (canCherryPick || canMerge)
                        items.Add(MenuModel.Separator());
                }

                items.Add(new MenuItemModel
                {
                    Header = App.ResText("CommitCM.SaveAsPatch"),
                    IconKey = App.MenuIconKey("Icons.Diff"),
                    Command = new RelayCommand(async () =>
                    {
                        var storageProvider = App.GetStorageProvider();
                        if (storageProvider == null)
                            return;

                        var options = new FolderPickerOpenOptions { AllowMultiple = false };
                        CommandLog log = null;
                        try
                        {
                            var picker = await storageProvider.OpenFolderPickerAsync(options);
                            if (picker.Count == 1)
                            {
                                log = _repo.CreateLog("Save as Patch");
                                var succ = false;
                                for (var i = 0; i < selected.Count; i++)
                                {
                                    var saveTo = GetPatchFileName(picker[0].Path.LocalPath, selected[i], i);
                                    succ = await Task.Run(() => new Commands.FormatPatch(_repo.FullPath, selected[i].SHA, saveTo).Use(log).Exec());
                                    if (!succ)
                                        break;
                                }
                                if (succ)
                                    App.SendNotification(_repo.FullPath, App.Text("SaveAsPatchSuccess"));
                            }
                        }
                        catch (Exception exception)
                        {
                            App.RaiseException(_repo.FullPath, $"Failed to save as patch: {exception.Message}");
                        }
                        log?.Complete();
                    })
                });
                items.Add(MenuModel.Separator());

                var copyMultipleSHAs = new MenuItemModel
                {
                    Header = App.ResText("CommitCM.CopySHA"),
                    IconKey = App.MenuIconKey("Icons.Fingerprint"),
                    Command = new RelayCommand(() =>
                    {
                        var builder = new StringBuilder();
                        foreach (var c in selected)
                            builder.AppendLine(c.SHA);
                        App.CopyText(builder.ToString());
                    })
                };

                var copyMultipleInfo = new MenuItemModel
                {
                    Header = App.ResText("CommitCM.CopyInfo"),
                    IconKey = App.MenuIconKey("Icons.Info"),
                    Command = new RelayCommand(() =>
                    {
                        var builder = new StringBuilder();
                        foreach (var c in selected)
                            builder.AppendLine($"{c.SHA.Substring(0, 10)} - {c.Subject}");
                        App.CopyText(builder.ToString());
                    })
                };

                var copyMultiple = new MenuModel
                {
                    Header = App.ResText("Copy"),
                    IconKey = App.MenuIconKey("Icons.Copy")
                };
                copyMultiple.Items.Add(copyMultipleSHAs);
                copyMultiple.Items.Add(copyMultipleInfo);
                items.Add(copyMultiple);

                return multipleMenu;
            }

            var commit = (list.SelectedItem as Models.Commit)!;
            var menu = new ContextMenuModel();
            var tags = new List<Models.Tag>();

            if (commit.HasDecorators)
            {
                foreach (var d in commit.Decorators)
                {
                    if (d.Type == Models.DecoratorType.CurrentBranchHead)
                    {
                        FillCurrentBranchMenu(menu, current);
                    }
                    else if (d.Type == Models.DecoratorType.LocalBranchHead)
                    {
                        var b = _repo.Branches.Find(x => x.IsLocal && d.Name == x.Name);
                        FillOtherLocalBranchMenu(menu, b, current, commit.IsMerged);
                    }
                    else if (d.Type == Models.DecoratorType.RemoteBranchHead)
                    {
                        var b = _repo.Branches.Find(x => !x.IsLocal && d.Name == x.FriendlyName);
                        FillRemoteBranchMenu(menu, b, current, commit.IsMerged);
                    }
                    else if (d.Type == Models.DecoratorType.Tag)
                    {
                        var t = _repo.Tags.Find(x => x.Name == d.Name);
                        if (t != null)
                            tags.Add(t);
                    }
                }

                if (menu.Items.Count > 0)
                    menu.Items.Add(MenuModel.Separator());
            }

            if (tags.Count > 0)
            {
                foreach (var tag in tags)
                    FillTagMenu(menu, tag, current, commit.IsMerged);
                menu.Items.Add(MenuModel.Separator());
            }

            if (!_repo.IsBare)
            {
                if (current.Head != commit.SHA)
                {
                    var reset = new MenuItemModel {
    Header = App.ResText("CommitCM.Reset", current.Name),
    IconKey = App.MenuIconKey("Icons.Reset"),
    Command = new RelayCommand(() => {
        if (_repo.CanCreatePopup())
            _repo.ShowPopup(new Reset(_repo, current, commit));
    })
};
menu.Items.Add(reset);

                    if (commit.IsMerged)
                    {
                        var squash = new MenuItemModel {
    Header = App.ResText("CommitCM.SquashCommitsSinceThis"),
    IconKey = App.MenuIconKey("Icons.SquashIntoParent"),
    Command = new RelayCommand(() => {
        if (_repo.CanCreatePopup())
            _repo.ShowPopup(new Squash(_repo, commit, commit.SHA));
    })
};
menu.Items.Add(squash);
                    }
                }
                else
                {
                    var reword = new MenuItemModel {
    Header = App.ResText("CommitCM.Reword"),
    IconKey = App.MenuIconKey("Icons.Edit"),
    Command = new RelayCommand(() => {
        if (_repo.LocalChangesCount > 0)
        {
            App.RaiseException(_repo.FullPath, "You have local changes. Please run stash or discard first.");
            return;
        }
        if (_repo.CanCreatePopup())
            _repo.ShowPopup(new Reword(_repo, commit));
    })
};
menu.Items.Add(reword);

                    var squash = new MenuItemModel {
    Header = App.ResText("CommitCM.Squash"),
    IconKey = App.MenuIconKey("Icons.SquashIntoParent"),
    IsEnabled = commit.Parents.Count == 1,
    Command = new RelayCommand(() => {
        if (commit.Parents.Count == 1)
        {
            var parent = _commits.Find(x => x.SHA == commit.Parents[0]);
            if (parent != null && _repo.CanCreatePopup())
                _repo.ShowPopup(new Squash(_repo, parent, commit.SHA));
        }
    })
};
menu.Items.Add(squash);
                }

                if (!commit.IsMerged)
                {
                    var rebase = new MenuItemModel {
    Header = App.ResText("CommitCM.Rebase", current.Name),
    IconKey = App.MenuIconKey("Icons.Rebase"),
    Command = new RelayCommand(() => {
        if (_repo.CanCreatePopup())
            _repo.ShowPopup(new Rebase(_repo, current, commit));
    })
};
menu.Items.Add(rebase);

                    if (!commit.HasDecorators)
                    {
                        var merge = new MenuItemModel {
    Header = App.ResText("CommitCM.Merge", current.Name),
    IconKey = App.MenuIconKey("Icons.Merge"),
    Command = new RelayCommand(() => {
        if (_repo.CanCreatePopup())
            _repo.ShowPopup(new Merge(_repo, commit, current.Name));
    })
};
menu.Items.Add(merge);
                    }

                    var cherryPick = new MenuItemModel {
    Header = App.ResText("CommitCM.CherryPick"),
    IconKey = App.MenuIconKey("Icons.CherryPick"),
    Command = new RelayCommand(() => {
        if (_repo.CanCreatePopup())
        {
            if (commit.Parents.Count <= 1)
            {
                _repo.ShowPopup(new CherryPick(_repo, new List<Models.Commit> { commit }));
            }
            else
            {
                var parents = new List<Models.Commit>();
                foreach (var sha in commit.Parents)
                {
                    var parent = _commits.Find(x => x.SHA == sha);
                    if (parent == null)
                        parent = new Commands.QuerySingleCommit(_repo.FullPath, sha).Result();
                    if (parent != null)
                        parents.Add(parent);
                }
                _repo.ShowPopup(new CherryPick(_repo, commit, parents));
            }
        }
    })
};
menu.Items.Add(cherryPick);
                }
                else
                {
                    var revert = new MenuItemModel {
    Header = App.ResText("CommitCM.Revert"),
    IconKey = App.MenuIconKey("Icons.Undo"),
    Command = new RelayCommand(() => {
        if (_repo.CanCreatePopup())
            _repo.ShowPopup(new Revert(_repo, commit));
    })
};
menu.Items.Add(revert);
                }

                if (current.Head != commit.SHA)
                {
                    var checkoutCommit = new MenuItemModel {
    Header = App.ResText("CommitCM.Checkout"),
    IconKey = App.MenuIconKey("Icons.Detached"),
    Command = new RelayCommand(() => {
        if (_repo.CanCreatePopup())
            _repo.ShowPopup(new CheckoutCommit(_repo, commit));
    })
};

                    var interactiveRebase = new MenuItemModel {
    Header = App.ResText("CommitCM.InteractiveRebase", current.Name),
    IconKey = App.MenuIconKey("Icons.InteractiveRebase"),
    Command = new RelayCommand(() => {
        if (_repo.LocalChangesCount > 0)
        {
            App.RaiseException(_repo.FullPath, "You have local changes. Please run stash or discard first.");
            return;
        }
        App.ShowWindow(new InteractiveRebase(_repo, current, commit), true);
    })
};

                    menu.Items.Add(checkoutCommit);
                    menu.Items.Add(MenuModel.Separator());
                    menu.Items.Add(interactiveRebase);
                }

                menu.Items.Add(MenuModel.Separator());
            }

            if (current.Head != commit.SHA)
            {
                var compareWithHead = new MenuItemModel {
    Header = App.ResText("CommitCM.CompareWithHead"),
    IconKey = App.MenuIconKey("Icons.Compare"),
    Command = new RelayCommand(() => {
        var head = _commits.Find(x => x.SHA == current.Head);
        if (head == null)
        {
            _repo.SelectedSearchedCommit = null;
            head = new Commands.QuerySingleCommit(_repo.FullPath, current.Head).Result();
            if (head != null)
                DetailContext = new RevisionCompare(_repo.FullPath, commit, head);
        }
        else
        {
            list.SelectedItems.Add(head);
        }
    })
};
                menu.Items.Add(compareWithHead);

                if (_repo.LocalChangesCount > 0)
                {
                    var compareWithWorktree = new MenuItemModel {
    Header = App.ResText("CommitCM.CompareWithWorktree"),
    IconKey = App.MenuIconKey("Icons.Compare"),
    Command = new RelayCommand(() => {
        DetailContext = new RevisionCompare(_repo.FullPath, commit, null);
    })
};
                    menu.Items.Add(compareWithWorktree);
                }

                menu.Items.Add(MenuModel.Separator());
            }

            var createBranch = new MenuItemModel {
    IconKey = App.MenuIconKey("Icons.Branch.Add"),
    Header = App.ResText("CreateBranch"),
    Command = new RelayCommand(() => {
        if (_repo.CanCreatePopup())
            _repo.ShowPopup(new CreateBranch(_repo, commit));
    })
};
            menu.Items.Add(createBranch);

            var createTag = new MenuItemModel {
    IconKey = App.MenuIconKey("Icons.Tag.Add"),
    Header = App.ResText("CreateTag"),
    Command = new RelayCommand(() => {
        if (_repo.CanCreatePopup())
            _repo.ShowPopup(new CreateTag(_repo, commit));
    })
};
            menu.Items.Add(createTag);
            menu.Items.Add(MenuModel.Separator());

            var saveToPatch = new MenuItemModel {
    IconKey = App.MenuIconKey("Icons.Diff"),
    Header = App.ResText("CommitCM.SaveAsPatch"),
    Command = new AsyncRelayCommand(async () => {
        var storageProvider = App.GetStorageProvider();
        if (storageProvider == null)
            return;

        var options = new FolderPickerOpenOptions() { AllowMultiple = false };
        var log = null as CommandLog;
        try
        {
            var selected = await storageProvider.OpenFolderPickerAsync(options);
            if (selected.Count == 1)
            {
                log = _repo.CreateLog("Save as Patch");
                var saveTo = GetPatchFileName(selected[0].Path.LocalPath, commit);
                var succ = new Commands.FormatPatch(_repo.FullPath, commit.SHA, saveTo).Use(log).Exec();
                if (succ)
                    App.SendNotification(_repo.FullPath, App.Text("SaveAsPatchSuccess"));
            }
        }
        catch (Exception exception)
        {
            App.RaiseException(_repo.FullPath, $"Failed to save as patch: {exception.Message}");
        }
        log?.Complete();
    })
};
            menu.Items.Add(saveToPatch);

            var archive = new MenuItemModel {
    IconKey = App.MenuIconKey("Icons.Archive"),
    Header = App.ResText("Archive"),
    Command = new RelayCommand(() => {
        if (_repo.CanCreatePopup())
            _repo.ShowPopup(new Archive(_repo, commit));
    })
};
            menu.Items.Add(archive);
            menu.Items.Add(MenuModel.Separator());

            var actions = _repo.GetCustomActions(Models.CustomActionScope.Commit);
            if (actions.Count > 0)
            {
                var custom = new MenuModel {
    Header = App.ResText("CommitCM.CustomAction"),
    IconKey = App.MenuIconKey("Icons.Action")
};

                foreach (var action in actions)
                {
                    var dup = action;
                    var item = new MenuItemModel {
    IconKey = App.MenuIconKey("Icons.Action"),
    Header = dup.Name,
    Command = new RelayCommand(() => {
        if (_repo.CanCreatePopup())
            _repo.ShowAndStartPopup(new ExecuteCustomAction(_repo, dup, commit));
    })
};

                    custom.Items.Add(item);
                }

                menu.Items.Add(custom);
                menu.Items.Add(MenuModel.Separator());
            }

            var copySHA = new MenuItemModel {
    Header = App.ResText("CommitCM.CopySHA"),
    IconKey = App.MenuIconKey("Icons.Fingerprint"),
    Command = new RelayCommand(() => App.CopyText(commit.SHA))
};

            var copySubject = new MenuItemModel {
    Header = App.ResText("CommitCM.CopySubject"),
    IconKey = App.MenuIconKey("Icons.Subject"),
    Command = new RelayCommand(() => App.CopyText(commit.Subject))
};

            var copyInfo = new MenuItemModel {
    Header = App.ResText("CommitCM.CopyInfo"),
    IconKey = App.MenuIconKey("Icons.Info"),
    Command = new RelayCommand(() => App.CopyText($"{commit.SHA.Substring(0, 10)} - {commit.Subject}"))
};

            var copyAuthor = new MenuItemModel {
    Header = App.ResText("CommitCM.CopyAuthor"),
    IconKey = App.MenuIconKey("Icons.User"),
    Command = new RelayCommand(() => App.CopyText(commit.Author.ToString()))
};

            var copyCommitter = new MenuItemModel {
    Header = App.ResText("CommitCM.CopyCommitter"),
    IconKey = App.MenuIconKey("Icons.User"),
    Command = new RelayCommand(() => App.CopyText(commit.Committer.ToString()))
};

            var copy = new MenuModel {
    Header = App.ResText("Copy"),
    IconKey = App.MenuIconKey("Icons.Copy")
};
copy.Items.Add(copySHA);
copy.Items.Add(copySubject);
copy.Items.Add(copyInfo);
copy.Items.Add(copyAuthor);
copy.Items.Add(copyCommitter);
menu.Items.Add(copy);

            return menu;
        }

        private Models.FilterMode GetFilterMode(string pattern)
        {
            foreach (var filter in _repo.Settings.HistoriesFilters)
            {
                if (filter.Pattern.Equals(pattern, StringComparison.Ordinal))
                    return filter.Mode;
            }

            return Models.FilterMode.None;
        }

        private void FillBranchVisibilityMenu(MenuModel submenu, Models.Branch branch)
        {
            var visibility = new MenuModel();
            visibility.IconKey = App.MenuIconKey("Icons.Eye");
            visibility.Header = App.ResText("Repository.FilterCommits");

            var exclude = new MenuItemModel();
            exclude.IconKey = App.MenuIconKey("Icons.EyeClose");
            exclude.Header = App.ResText("Repository.FilterCommits.Exclude");
            exclude.Command = new RelayCommand(() =>
            {
                _repo.SetBranchFilterMode(branch, Models.FilterMode.Excluded, false, true);
            });

            var filterMode = GetFilterMode(branch.FullName);
            if (filterMode == Models.FilterMode.None)
            {
                var include = new MenuItemModel();
                include.IconKey = App.MenuIconKey("Icons.Filter");
                include.Header = App.Text("Repository.FilterCommits.Include");
                include.Command = new RelayCommand(() =>
                {
                    _repo.SetBranchFilterMode(branch, Models.FilterMode.Included, false, true);
                });
                visibility.Items.Add(include);
                visibility.Items.Add(exclude);
            }
            else
            {
                var unset = new MenuItemModel();
                unset.Header = App.Text("Repository.FilterCommits.Default");
                unset.Command = new RelayCommand(() =>
                {
                    _repo.SetBranchFilterMode(branch, Models.FilterMode.None, false, true);
                });
                visibility.Items.Add(exclude);
                visibility.Items.Add(unset);
            }

            submenu.Items.Add(visibility);
            submenu.Items.Add(MenuModel.Separator());
        }

        private void FillTagVisibilityMenu(MenuModel submenu, Models.Tag tag)
{
    var visibility = new MenuModel {
        IconKey = App.MenuIconKey("Icons.Eye"),
        Header = App.ResText("Repository.FilterCommits")
    };

    var exclude = new MenuItemModel {
        IconKey = App.MenuIconKey("Icons.EyeClose"),
        Header = App.ResText("Repository.FilterCommits.Exclude"),
        Command = new RelayCommand(() => _repo.SetTagFilterMode(tag, Models.FilterMode.Excluded))
    };

    var filterMode = GetFilterMode(tag.Name);
    if (filterMode == Models.FilterMode.None)
    {
        var include = new MenuItemModel {
            IconKey = App.MenuIconKey("Icons.Filter"),
            Header = App.ResText("Repository.FilterCommits.Include"),
            Command = new RelayCommand(() => _repo.SetTagFilterMode(tag, Models.FilterMode.Included))
        };
        visibility.Items.Add(include);
        visibility.Items.Add(exclude);
    }
    else
    {
        var unset = new MenuItemModel {
            Header = App.ResText("Repository.FilterCommits.Default"),
            Command = new RelayCommand(() => _repo.SetTagFilterMode(tag, Models.FilterMode.None))
        };
        visibility.Items.Add(exclude);
        visibility.Items.Add(unset);
    }

    submenu.Items.Add(visibility);
    submenu.Items.Add(MenuModel.Separator());
}

        private void FillCurrentBranchMenu(ContextMenuModel menu, Models.Branch current)
        {
            var submenu = new MenuModel();
            submenu.IconKey = App.MenuIconKey("Icons.Branch");
            submenu.Header = current.Name;

            FillBranchVisibilityMenu(submenu, current);

            if (!string.IsNullOrEmpty(current.Upstream))
            {
                var upstream = current.Upstream.Substring(13);

                var fastForward = new MenuItemModel {
    Header = App.ResText("BranchCM.FastForward", upstream),
    IconKey = App.MenuIconKey("Icons.FastForward"),
    IsEnabled = current.TrackStatus.Ahead.Count == 0,
    Command = new RelayCommand(() => {
        var b = _repo.Branches.Find(x => x.FriendlyName == upstream);
        if (b == null)
            return;
        if (_repo.CanCreatePopup())
            _repo.ShowAndStartPopup(new Merge(_repo, b, current.Name, true));
    })
};
submenu.Items.Add(fastForward);

                var pull = new MenuItemModel {
    Header = App.ResText("BranchCM.Pull", upstream),
    IconKey = App.MenuIconKey("Icons.Pull"),
    Command = new RelayCommand(() => {
        if (_repo.CanCreatePopup())
            _repo.ShowPopup(new Pull(_repo, null));
    })
};
submenu.Items.Add(pull);
            }

            var push = new MenuItemModel {
    Header = App.ResText("BranchCM.Push", current.Name),
    IconKey = App.MenuIconKey("Icons.Push"),
    IsEnabled = _repo.Remotes.Count > 0,
    Command = new RelayCommand(() => {
        if (_repo.CanCreatePopup())
            _repo.ShowPopup(new Push(_repo, current));
    })
};
submenu.Items.Add(push);

            var rename = new MenuItemModel {
    Header = App.ResText("BranchCM.Rename", current.Name),
    IconKey = App.MenuIconKey("Icons.Rename"),
    Command = new RelayCommand(() => {
        if (_repo.CanCreatePopup())
            _repo.ShowPopup(new RenameBranch(_repo, current));
    })
};
submenu.Items.Add(rename);
            submenu.Items.Add(MenuModel.Separator());

            if (!_repo.IsBare)
            {
                var detect = Commands.GitFlow.DetectType(_repo.FullPath, _repo.Branches, current.Name);
                if (detect.IsGitFlowBranch)
                {
                    var finish = new MenuItemModel {
    Header = App.ResText("BranchCM.Finish", current.Name),
    IconKey = App.MenuIconKey("Icons.GitFlow"),
    Command = new RelayCommand(() => {
        if (_repo.CanCreatePopup())
            _repo.ShowPopup(new GitFlowFinish(_repo, current, detect.Type, detect.Prefix));
    })
};
submenu.Items.Add(finish);
                    submenu.Items.Add(MenuModel.Separator());
                }
            }

            var copy = new MenuItemModel {
    Header = App.ResText("BranchCM.CopyName"),
    IconKey = App.MenuIconKey("Icons.Copy"),
    Command = new RelayCommand(() => App.CopyText(current.Name))
};
submenu.Items.Add(copy);

            menu.Items.Add(submenu);
        }

        private void FillOtherLocalBranchMenu(MenuModel menu, Models.Branch branch, Models.Branch current, bool merged)
        {
            var submenu = new MenuModel {
    IconKey = App.MenuIconKey("Icons.Branch"),
    Header = branch.Name
};

            FillBranchVisibilityMenu(submenu, branch);

            if (!_repo.IsBare)
            {
                var checkout = new MenuItemModel {
    Header = App.ResText("BranchCM.Checkout", branch.Name),
    IconKey = App.MenuIconKey("Icons.Check"),
    Command = new RelayCommand(() => _repo.CheckoutBranch(branch))
};
submenu.Items.Add(checkout);

                var merge = new MenuItemModel {
    Header = App.ResText("BranchCM.Merge", branch.Name, current.Name),
    IconKey = App.MenuIconKey("Icons.Merge"),
    IsEnabled = !merged,
    Command = new RelayCommand(() => {
        if (_repo.CanCreatePopup())
            _repo.ShowPopup(new Merge(_repo, branch, current.Name, false));
    })
};
submenu.Items.Add(merge);
            }

            var rename = new MenuItemModel {
    Header = App.ResText("BranchCM.Rename", branch.Name),
    IconKey = App.MenuIconKey("Icons.Rename"),
    Command = new RelayCommand(() => {
        if (_repo.CanCreatePopup())
            _repo.ShowPopup(new RenameBranch(_repo, branch));
    })
};
submenu.Items.Add(rename);
submenu.Items.Add(MenuModel.Separator());

            var delete = new MenuItemModel {
    Header = App.ResText("BranchCM.Delete", branch.Name),
    IconKey = App.MenuIconKey("Icons.Clear"),
    Command = new RelayCommand(() => {
        if (_repo.CanCreatePopup())
            _repo.ShowPopup(new DeleteBranch(_repo, branch));
    })
};
submenu.Items.Add(delete);
submenu.Items.Add(MenuModel.Separator());

            if (!_repo.IsBare)
            {
                var detect = Commands.GitFlow.DetectType(_repo.FullPath, _repo.Branches, branch.Name);
                if (detect.IsGitFlowBranch)
                {
                    var finish = new MenuItemModel {
    Header = App.ResText("BranchCM.Finish", branch.Name),
    IconKey = App.MenuIconKey("Icons.GitFlow"),
    Command = new RelayCommand(() => {
        if (_repo.CanCreatePopup())
            _repo.ShowPopup(new GitFlowFinish(_repo, branch, detect.Type, detect.Prefix));
    })
};
submenu.Items.Add(finish);
submenu.Items.Add(MenuModel.Separator());
                }
            }

            var copy = new MenuItemModel {
    Header = App.ResText("BranchCM.CopyName"),
    IconKey = App.MenuIconKey("Icons.Copy"),
    Command = new RelayCommand(() => App.CopyText(branch.Name))
};
submenu.Items.Add(copy);

            menu.Items.Add(submenu);
        }

        private void FillRemoteBranchMenu(MenuModel menu, Models.Branch branch, Models.Branch current, bool merged)
        {
            var name = branch.FriendlyName;

            var submenu = new MenuModel {
    IconKey = App.MenuIconKey("Icons.Branch"),
    Header = name
};

FillBranchVisibilityMenu(submenu, branch);

var checkout = new MenuItemModel {
    Header = App.ResText("BranchCM.Checkout", name),
    IconKey = App.MenuIconKey("Icons.Check"),
    Command = new RelayCommand(() => _repo.CheckoutBranch(branch))
};
submenu.Items.Add(checkout);

var merge = new MenuItemModel {
    Header = App.ResText("BranchCM.Merge", name, current.Name),
    IconKey = App.MenuIconKey("Icons.Merge"),
    IsEnabled = !merged,
    Command = new RelayCommand(() => {
        if (_repo.CanCreatePopup())
            _repo.ShowPopup(new Merge(_repo, branch, current.Name, false));
    })
};
submenu.Items.Add(merge);

var delete = new MenuItemModel {
    Header = App.ResText("BranchCM.Delete", name),
    IconKey = App.MenuIconKey("Icons.Clear"),
    Command = new RelayCommand(() => {
        if (_repo.CanCreatePopup())
            _repo.ShowPopup(new DeleteBranch(_repo, branch));
    })
};
submenu.Items.Add(delete);
submenu.Items.Add(MenuModel.Separator());

var copy = new MenuItemModel {
    Header = App.ResText("BranchCM.CopyName"),
    IconKey = App.MenuIconKey("Icons.Copy"),
    Command = new RelayCommand(() => App.CopyText(name))
};
submenu.Items.Add(copy);

            menu.Items.Add(submenu);
        }

        private void FillTagMenu(MenuModel menu, Models.Tag tag, Models.Branch current, bool merged)
        {
            var submenu = new MenuModel {
    Header = tag.Name,
    ViewToDo=new (){[ViewPropertySetting.MinWidth]=200},
    IconKey = App.MenuIconKey("Icons.Tag")
};

FillTagVisibilityMenu(submenu, tag);

var push = new MenuItemModel {
    Header = App.ResText("TagCM.Push", tag.Name),
    IconKey = App.MenuIconKey("Icons.Push"),
    IsEnabled = _repo.Remotes.Count > 0,
    Command = new RelayCommand(() => {
        if (_repo.CanCreatePopup())
            _repo.ShowPopup(new PushTag(_repo, tag));
    })
};
submenu.Items.Add(push);

if (!_repo.IsBare && !merged)
{
    var merge = new MenuItemModel {
        Header = App.ResText("TagCM.Merge", tag.Name, current.Name),
        IconKey = App.MenuIconKey("Icons.Merge"),
        Command = new RelayCommand(() => {
            if (_repo.CanCreatePopup())
                _repo.ShowPopup(new Merge(_repo, tag, current.Name));
        })
    };
    submenu.Items.Add(merge);
            }

            var delete = new MenuItemModel {
    Header = App.ResText("TagCM.Delete", tag.Name),
    IconKey = App.MenuIconKey("Icons.Clear"),
    Command = new RelayCommand(() => {
        if (_repo.CanCreatePopup())
            _repo.ShowPopup(new DeleteTag(_repo, tag));
    })
};
submenu.Items.Add(delete);
submenu.Items.Add(MenuModel.Separator());

var copy = new MenuItemModel {
    Header = App.ResText("TagCM.CopyName"),
    IconKey = App.MenuIconKey("Icons.Copy"),
    Command = new RelayCommand(() => App.CopyText(tag.Name))
};
submenu.Items.Add(copy);

            menu.Items.Add(submenu);
        }

        private string GetPatchFileName(string dir, Models.Commit commit, int index = 0)
        {
            var ignore_chars = new HashSet<char> { '/', '\\', ':', ',', '*', '?', '\"', '<', '>', '|', '`', '$', '^', '%', '[', ']', '+', '-' };
            var builder = new StringBuilder();
            builder.Append(index.ToString("D4"));
            builder.Append('-');

            var chars = commit.Subject.ToCharArray();
            var len = 0;
            foreach (var c in chars)
            {
                if (!ignore_chars.Contains(c))
                {
                    if (c == ' ' || c == '\t')
                        builder.Append('-');
                    else
                        builder.Append(c);

                    len++;

                    if (len >= 48)
                        break;
                }
            }
            builder.Append(".patch");

            return Path.Combine(dir, builder.ToString());
        }

        private Repository _repo = null;
        private bool _isLoading = true;
        private List<Models.Commit> _commits = new List<Models.Commit>();
        private Models.CommitGraph _graph = null;
        private Models.Commit _autoSelectedCommit = null;
        private long _navigationId = 0;
        private object _detailContext = null;

        private Models.Bisect _bisect = null;

        private GridLength _leftArea = new GridLength(1, GridUnitType.Star);
        private GridLength _rightArea = new GridLength(1, GridUnitType.Star);
        private GridLength _topArea = new GridLength(1, GridUnitType.Star);
        private GridLength _bottomArea = new GridLength(1, GridUnitType.Star);
    }
}
