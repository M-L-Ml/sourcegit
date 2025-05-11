using System;
using System.Collections.Generic;
using System.IO;
using System.Threading.Tasks;
using System.Windows.Input;

using Avalonia.Controls;
using Avalonia.Platform.Storage;
using Avalonia.Threading;

using CommunityToolkit.Mvvm.ComponentModel;

namespace SourceGit.ViewModels
{
    public class WorkingCopy : ObservableObject
    {
        public bool IncludeUntracked
        {
            get => _repo.IncludeUntracked;
            set
            {
                if (_repo.IncludeUntracked != value)
                {
                    _repo.IncludeUntracked = value;
                    OnPropertyChanged();
                }
            }
        }

        public bool HasRemotes
        {
            get => _hasRemotes;
            set => SetProperty(ref _hasRemotes, value);
        }

        public bool HasUnsolvedConflicts
        {
            get => _hasUnsolvedConflicts;
            set => SetProperty(ref _hasUnsolvedConflicts, value);
        }

        public InProgressContext InProgressContext
        {
            get => _inProgressContext;
            private set => SetProperty(ref _inProgressContext, value);
        }

        public bool IsStaging
        {
            get => _isStaging;
            private set => SetProperty(ref _isStaging, value);
        }

        public bool IsUnstaging
        {
            get => _isUnstaging;
            private set => SetProperty(ref _isUnstaging, value);
        }

        public bool IsCommitting
        {
            get => _isCommitting;
            private set => SetProperty(ref _isCommitting, value);
        }

        public bool EnableSignOff
        {
            get => _repo.Settings.EnableSignOffForCommit;
            set => _repo.Settings.EnableSignOffForCommit = value;
        }

        public bool UseAmend
        {
            get => _useAmend;
            set
            {
                if (SetProperty(ref _useAmend, value))
                {
                    if (value)
                    {
                        var currentBranch = _repo.CurrentBranch;
                        if (currentBranch == null)
                        {
                            App.RaiseException(_repo.FullPath, "No commits to amend!!!");
                            _useAmend = false;
                            OnPropertyChanged();
                            return;
                        }

                        CommitMessage = new Commands.QueryCommitFullMessage(_repo.FullPath, currentBranch.Head).Result();
                    }
                    else
                    {
                        CommitMessage = string.Empty;
                    }

                    Staged = GetStagedChanges();
                    VisibleStaged = GetVisibleChanges(_staged);
                    SelectedStaged = [];
                }
            }
        }

        public string Filter
        {
            get => _filter;
            set
            {
                if (SetProperty(ref _filter, value))
                {
                    if (_isLoadingData)
                        return;

                    VisibleUnstaged = GetVisibleChanges(_unstaged);
                    VisibleStaged = GetVisibleChanges(_staged);
                    SelectedUnstaged = [];
                }
            }
        }

        public List<Models.Change> Unstaged
        {
            get => _unstaged;
            private set => SetProperty(ref _unstaged, value);
        }

        public List<Models.Change> VisibleUnstaged
        {
            get => _visibleUnstaged;
            private set => SetProperty(ref _visibleUnstaged, value);
        }

        public List<Models.Change> Staged
        {
            get => _staged;
            private set => SetProperty(ref _staged, value);
        }

        public List<Models.Change> VisibleStaged
        {
            get => _visibleStaged;
            private set => SetProperty(ref _visibleStaged, value);
        }

        public List<Models.Change> SelectedUnstaged
        {
            get => _selectedUnstaged;
            set
            {
                if (SetProperty(ref _selectedUnstaged, value))
                {
                    if (value == null || value.Count == 0)
                    {
                        if (_selectedStaged == null || _selectedStaged.Count == 0)
                            SetDetail(null, true);
                    }
                    else
                    {
                        if (_selectedStaged != null && _selectedStaged.Count > 0)
                            SelectedStaged = [];

                        if (value.Count == 1)
                            SetDetail(value[0], true);
                        else
                            SetDetail(null, true);
                    }
                }
            }
        }

        public List<Models.Change> SelectedStaged
        {
            get => _selectedStaged;
            set
            {
                if (SetProperty(ref _selectedStaged, value))
                {
                    if (value == null || value.Count == 0)
                    {
                        if (_selectedUnstaged == null || _selectedUnstaged.Count == 0)
                            SetDetail(null, false);
                    }
                    else
                    {
                        if (_selectedUnstaged != null && _selectedUnstaged.Count > 0)
                            SelectedUnstaged = [];

                        if (value.Count == 1)
                            SetDetail(value[0], false);
                        else
                            SetDetail(null, false);
                    }
                }
            }
        }

        public object DetailContext
        {
            get => _detailContext;
            private set => SetProperty(ref _detailContext, value);
        }

        public string CommitMessage
        {
            get => _commitMessage;
            set => SetProperty(ref _commitMessage, value);
        }

        public WorkingCopy(Repository repo)
        {
            _repo = repo;
        }

        public void Cleanup()
        {
            _repo = null;
            _inProgressContext = null;

            _selectedUnstaged.Clear();
            OnPropertyChanged(nameof(SelectedUnstaged));

            _selectedStaged.Clear();
            OnPropertyChanged(nameof(SelectedStaged));

            _visibleUnstaged.Clear();
            OnPropertyChanged(nameof(VisibleUnstaged));

            _visibleStaged.Clear();
            OnPropertyChanged(nameof(VisibleStaged));

            _unstaged.Clear();
            OnPropertyChanged(nameof(Unstaged));

            _staged.Clear();
            OnPropertyChanged(nameof(Staged));

            _detailContext = null;
            _commitMessage = string.Empty;
        }

        public void SetData(List<Models.Change> changes)
        {
            if (!IsChanged(_cached, changes))
            {
                // Just force refresh selected changes.
                Dispatcher.UIThread.Invoke(() =>
                {
                    HasUnsolvedConflicts = _cached.Find(x => x.IsConflict) != null;

                    UpdateDetail();
                    UpdateInProgressState();
                });

                return;
            }

            _cached = changes;
            _count = _cached.Count;

            var lastSelectedUnstaged = new HashSet<string>();
            var lastSelectedStaged = new HashSet<string>();
            if (_selectedUnstaged != null && _selectedUnstaged.Count > 0)
            {
                foreach (var c in _selectedUnstaged)
                    lastSelectedUnstaged.Add(c.Path);
            }
            else if (_selectedStaged != null && _selectedStaged.Count > 0)
            {
                foreach (var c in _selectedStaged)
                    lastSelectedStaged.Add(c.Path);
            }

            var unstaged = new List<Models.Change>();
            var hasConflict = false;
            foreach (var c in changes)
            {
                if (c.WorkTree != Models.ChangeState.None)
                {
                    unstaged.Add(c);
                    hasConflict |= c.IsConflict;
                }
            }

            var visibleUnstaged = GetVisibleChanges(unstaged);
            var selectedUnstaged = new List<Models.Change>();
            foreach (var c in visibleUnstaged)
            {
                if (lastSelectedUnstaged.Contains(c.Path))
                    selectedUnstaged.Add(c);
            }

            var staged = GetStagedChanges();

            var visibleStaged = GetVisibleChanges(staged);
            var selectedStaged = new List<Models.Change>();
            foreach (var c in visibleStaged)
            {
                if (lastSelectedStaged.Contains(c.Path))
                    selectedStaged.Add(c);
            }

            Dispatcher.UIThread.Invoke(() =>
            {
                _isLoadingData = true;
                HasUnsolvedConflicts = hasConflict;
                VisibleUnstaged = visibleUnstaged;
                VisibleStaged = visibleStaged;
                Unstaged = unstaged;
                Staged = staged;
                SelectedUnstaged = selectedUnstaged;
                SelectedStaged = selectedStaged;
                _isLoadingData = false;

                UpdateDetail();
                UpdateInProgressState();
            });
        }

        public void OpenExternalMergeToolAllConflicts()
        {
            // No <file> arg, mergetool runs on all files with merge conflicts!
            UseExternalMergeTool(null);
        }

        public void OpenAssumeUnchanged()
        {
            App.ShowWindow(new AssumeUnchangedManager(_repo), true);
        }

        public void StashAll(bool autoStart)
        {
            if (!_repo.CanCreatePopup())
                return;

            if (autoStart)
                _repo.ShowAndStartPopup(new StashChanges(_repo, _cached, false));
            else
                _repo.ShowPopup(new StashChanges(_repo, _cached, false));
        }

        public void StageSelected(Models.Change next)
        {
            StageChanges(_selectedUnstaged, next);
        }

        public void StageAll()
        {
            StageChanges(_visibleUnstaged, null);
        }

        public void UnstageSelected(Models.Change next)
        {
            UnstageChanges(_selectedStaged, next);
        }

        public void UnstageAll()
        {
            UnstageChanges(_visibleStaged, null);
        }

        public void Discard(List<Models.Change> changes)
        {
            if (_repo.CanCreatePopup())
                _repo.ShowPopup(new Discard(_repo, changes));
        }

        public void ClearFilter()
        {
            Filter = string.Empty;
        }

        public async void UseTheirs(List<Models.Change> changes)
        {
            _repo.SetWatcherEnabled(false);

            var files = new List<string>();
            var needStage = new List<string>();
            var log = _repo.CreateLog("Use Theirs");

            foreach (var change in changes)
            {
                if (!change.IsConflict)
                    continue;

                if (change.WorkTree == Models.ChangeState.Deleted)
                {
                    var fullpath = Path.Combine(_repo.FullPath, change.Path);
                    if (File.Exists(fullpath))
                        File.Delete(fullpath);

                    needStage.Add(change.Path);
                }
                else
                {
                    files.Add(change.Path);
                }
            }

            if (files.Count > 0)
            {
                var succ = await Task.Run(() => new Commands.Checkout(_repo.FullPath).Use(log).UseTheirs(files));
                if (succ)
                    needStage.AddRange(files);
            }

            if (needStage.Count > 0)
                await Task.Run(() => new Commands.Add(_repo.FullPath, needStage).Use(log).Exec());

            log.Complete();
            _repo.MarkWorkingCopyDirtyManually();
            _repo.SetWatcherEnabled(true);
        }

        public async void UseMine(List<Models.Change> changes)
        {
            _repo.SetWatcherEnabled(false);

            var files = new List<string>();
            var needStage = new List<string>();
            var log = _repo.CreateLog("Use Mine");

            foreach (var change in changes)
            {
                if (!change.IsConflict)
                    continue;

                if (change.Index == Models.ChangeState.Deleted)
                {
                    var fullpath = Path.Combine(_repo.FullPath, change.Path);
                    if (File.Exists(fullpath))
                        File.Delete(fullpath);

                    needStage.Add(change.Path);
                }
                else
                {
                    files.Add(change.Path);
                }
            }

            if (files.Count > 0)
            {
                var succ = await Task.Run(() => new Commands.Checkout(_repo.FullPath).Use(log).UseMine(files));
                if (succ)
                    needStage.AddRange(files);
            }

            if (needStage.Count > 0)
                await Task.Run(() => new Commands.Add(_repo.FullPath, needStage).Use(log).Exec());

            log.Complete();
            _repo.MarkWorkingCopyDirtyManually();
            _repo.SetWatcherEnabled(true);
        }

        public async void UseExternalMergeTool(Models.Change change)
        {
            var toolType = Preferences.Instance.ExternalMergeToolType;
            var toolPath = Preferences.Instance.ExternalMergeToolPath;
            var file = change?.Path; // NOTE: With no <file> arg, mergetool runs on on every file with merge conflicts!
            await Task.Run(() => Commands.MergeTool.OpenForMerge(_repo.FullPath, toolType, toolPath, file));
        }

        public void ContinueMerge()
        {
            IsCommitting = true;

            if (_inProgressContext != null)
            {
                _repo.SetWatcherEnabled(false);
                Task.Run(() =>
                {
                    var mergeMsgFile = Path.Combine(_repo.GitDir, "MERGE_MSG");
                    if (File.Exists(mergeMsgFile) && !string.IsNullOrWhiteSpace(_commitMessage))
                        File.WriteAllText(mergeMsgFile, _commitMessage);

                    var succ = _inProgressContext.Continue();
                    Dispatcher.UIThread.Invoke(() =>
                    {
                        if (succ)
                            CommitMessage = string.Empty;

                        _repo.SetWatcherEnabled(true);
                        IsCommitting = false;
                    });
                });
            }
            else
            {
                _repo.MarkWorkingCopyDirtyManually();
                IsCommitting = false;
            }
        }

        public void SkipMerge()
        {
            IsCommitting = true;

            if (_inProgressContext != null)
            {
                _repo.SetWatcherEnabled(false);
                Task.Run(() =>
                {
                    var succ = _inProgressContext.Skip();
                    Dispatcher.UIThread.Invoke(() =>
                    {
                        if (succ)
                            CommitMessage = string.Empty;

                        _repo.SetWatcherEnabled(true);
                        IsCommitting = false;
                    });
                });
            }
            else
            {
                _repo.MarkWorkingCopyDirtyManually();
                IsCommitting = false;
            }
        }

        public void AbortMerge()
        {
            IsCommitting = true;

            if (_inProgressContext != null)
            {
                _repo.SetWatcherEnabled(false);
                Task.Run(() =>
                {
                    var succ = _inProgressContext.Abort();
                    Dispatcher.UIThread.Invoke(() =>
                    {
                        if (succ)
                            CommitMessage = string.Empty;

                        _repo.SetWatcherEnabled(true);
                        IsCommitting = false;
                    });
                });
            }
            else
            {
                _repo.MarkWorkingCopyDirtyManually();
                IsCommitting = false;
            }
        }

        public void Commit()
        {
            DoCommit(false, false, false);
        }

        public void CommitWithAutoStage()
        {
            DoCommit(true, false, false);
        }

        public void CommitWithPush()
        {
            DoCommit(false, true, false);
        }

        public ContextMenuModel CreateContextMenuModelForUnstagedChanges()
        {
            if (_selectedUnstaged == null || _selectedUnstaged.Count == 0)
                return null;

            var menu = new ContextMenuModel();
            if (_selectedUnstaged.Count == 1)
            {
                var change = _selectedUnstaged[0];
                var path = Path.GetFullPath(Path.Combine(_repo.FullPath, change.Path));

                var explore = new MenuItemModel {
                    IconKey = App.MenuIconKey("Icons.Explore"),
                    Header = App.ResText("RevealFile"),
                    IsEnabled = File.Exists(path) || Directory.Exists(path),
                    Command = new RelayCommand(() => Native.OS.OpenInFileManager(path, true))
                };
                menu.Items.Add(explore);

                var openWith = new MenuItemModel {
                    IconKey = App.MenuIconKey("Icons.OpenWith"),
                    Header = App.ResText("OpenWith"),
                    IsEnabled = File.Exists(path),
                    Command = new RelayCommand(() => Native.OS.OpenWithDefaultEditor(path))
                };
                menu.Items.Add(openWith);
                menu.Items.Add(MenuModel.Separator());

                if (change.IsConflict)
                {
                    var useTheirs = new MenuItemModel
                    {
                        IconKey = App.MenuIconKey("Icons.Incoming"),
                        Header = App.ResText("FileCM.UseTheirs"),
                        Command = new RelayCommand(() => UseTheirs(_selectedUnstaged))
                    };
                    var useMine = new MenuItemModel
                    {
                        IconKey = App.MenuIconKey("Icons.Local"),
                        Header = App.ResText("FileCM.UseMine"),
                        Command = new RelayCommand(() => UseMine(_selectedUnstaged))
                    };

                    if (_inProgressContext is CherryPickInProgress cherryPick)
                    {
                        useTheirs.Header = App.Text("FileCM.ResolveUsing", cherryPick.HeadName);
                        useMine.Header = App.Text("FileCM.ResolveUsing", _repo.CurrentBranch.Name);
                    }
                    else if (_inProgressContext is RebaseInProgress rebase)
                    {
                        useTheirs.Header = App.Text("FileCM.ResolveUsing", rebase.HeadName);
                        useMine.Header = App.Text("FileCM.ResolveUsing", rebase.BaseName);
                    }
                    else if (_inProgressContext is RevertInProgress revert)
                    {
                        useTheirs.Header = App.Text("FileCM.ResolveUsing", $"{revert.Head.SHA.AsSpan(0, 10)} (revert)");
                        useMine.Header = App.Text("FileCM.ResolveUsing", _repo.CurrentBranch.Name);
                    }
                    else if (_inProgressContext is MergeInProgress merge)
                    {
                        useTheirs.Header = App.Text("FileCM.ResolveUsing", merge.SourceName);
                        useMine.Header = App.Text("FileCM.ResolveUsing", _repo.CurrentBranch.Name);
                    }

                    menu.Items.Add(useTheirs);
                    menu.Items.Add(useMine);
                    menu.Items.Add(MenuModel.Separator());

                    var openMerger = new MenuItemModel
                    {
                        IconKey = App.MenuIconKey("Icons.OpenWith"),
                        Header = App.ResText("FileCM.OpenWithExternalMerger"),
                        Command = new RelayCommand(() => UseExternalMergeTool(change))
                    };
                    menu.Items.Add(openMerger);
                    menu.Items.Add(MenuModel.Separator());
                }
                else
                {
                    var stage = new MenuItemModel
                    {
                        IconKey = App.MenuIconKey("Icons.File.Add"),
                        Header = App.ResText("FileCM.Stage"),
                        Command = new RelayCommand(() => StageChanges(_selectedUnstaged, null))
                    };
                    menu.Items.Add(stage);

                    var discard = new MenuItemModel
                    {
                        IconKey = App.MenuIconKey("Icons.Undo"),
                        Header = App.ResText("FileCM.Discard"),
                        Command = new RelayCommand(() => Discard(_selectedUnstaged))
                    };
                    menu.Items.Add(discard);

                    var stash = new MenuItemModel
                    {
                        IconKey = App.MenuIconKey("Icons.Stashes.Add"),
                        Header = App.ResText("FileCM.Stash"),
                        Command = new RelayCommand(() =>
                        {
                            if (_repo.CanCreatePopup())
                                _repo.ShowPopup(new StashChanges(_repo, _selectedUnstaged, true));
                        })
                    };
                    menu.Items.Add(stash);

                    var patch = new MenuItemModel
                    {
                        IconKey = App.MenuIconKey("Icons.Diff"),
                        Header = App.ResText("FileCM.SaveAsPatch"),
                        Command = new AsyncRelayCommand(async () =>
                        {
                            var storageProvider = App.GetStorageProvider();
                            if (storageProvider == null)
                                return;

                            var options = new FilePickerSaveOptions();
                            options.Title = App.Text("FileCM.SaveAsPatch");
                            options.DefaultExtension = ".patch";
                            options.FileTypeChoices = [new FilePickerFileType("Patch File") { Patterns = ["*.patch"] }];

                            var storageFile = await storageProvider.SaveFilePickerAsync(options);
                            if (storageFile != null)
                            {
                                var succ = await Task.Run(() => Commands.SaveChangesAsPatch.ProcessLocalChanges(_repo.FullPath, _selectedUnstaged, true, storageFile.Path.LocalPath));
                                if (succ)
                                    App.SendNotification(_repo.FullPath, App.Text("SaveAsPatchSuccess"));
                            }
                        })
                    };
                    menu.Items.Add(patch);

                    var assumeUnchanged = new MenuItemModel
                    {
                        IconKey = App.MenuIconKey("Icons.File.Ignore"),
                        Header = App.ResText("FileCM.AssumeUnchanged"),
                        Command = new RelayCommand(() =>
                        {
                            var log = _repo.CreateLog("Assume File Unchanged");
                            new Commands.AssumeUnchanged(_repo.FullPath, change.Path, true).Use(log).Exec();
                            log.Complete();
                        })
                    };
                    assumeUnchanged.IsVisible = change.WorkTree != Models.ChangeState.Untracked;
                    menu.Items.Add(assumeUnchanged);

                    var history = new MenuItemModel
                    {
                        IconKey = App.MenuIconKey("Icons.Histories"),
                        Header = App.ResText("FileHistory"),
                        Command = new RelayCommand(() =>
                        {
                            App.ShowWindow(new FileHistories(_repo, change.Path), false);
                        })
                    };
                    menu.Items.Add(history);
                    menu.Items.Add(MenuModel.Separator());

                    var extension = Path.GetExtension(change.Path);
                    var hasExtra = false;
                    if (change.WorkTree == Models.ChangeState.Untracked)
                    {
                        var isRooted = change.Path.IndexOf('/', StringComparison.Ordinal) <= 0;
                        var addToIgnore = new MenuItemModel
                        {
                            IconKey = App.MenuIconKey("Icons.GitIgnore"),
                            Header = App.ResText("WorkingCopy.AddToGitIgnore")
                        };

                        var singleFile = new MenuItemModel
                        {
                            Header = App.ResText("WorkingCopy.AddToGitIgnore.SingleFile"),
                            Command = new RelayCommand(() =>
                            {
                                Commands.GitIgnore.Add(_repo.FullPath, change.Path);
                            })
                        };
                        addToIgnore.Items.Add(singleFile);

                        var byParentFolder = new MenuItemModel
                        {
                            Header = App.ResText("WorkingCopy.AddToGitIgnore.InSameFolder"),
                            Command = new RelayCommand(() =>
                            {
                                var dir = Path.GetDirectoryName(change.Path).Replace("\\", "/");
                                Commands.GitIgnore.Add(_repo.FullPath, dir + "/");
                            })
                        };
                        byParentFolder.IsVisible = !isRooted;
                        addToIgnore.Items.Add(byParentFolder);

                        if (!string.IsNullOrEmpty(extension))
                        {
                            var byExtension = new MenuItemModel
                            {
                                Header = App.ResText("WorkingCopy.AddToGitIgnore.Extension", extension),
                                Command = new RelayCommand(() =>
                                {
                                    Commands.GitIgnore.Add(_repo.FullPath, $"*{extension}");
                                })
                            };
                            addToIgnore.Items.Add(byExtension);

                            var byExtensionInSameFolder = new MenuItemModel
                            {
                                Header = App.ResText("WorkingCopy.AddToGitIgnore.ExtensionInSameFolder", extension),
                                Command = new RelayCommand(() =>
                                {
                                    var dir = Path.GetDirectoryName(change.Path).Replace("\\", "/");
                                    Commands.GitIgnore.Add(_repo.FullPath, $"{dir}/*{extension}");
                                })
                            };
                            byExtensionInSameFolder.IsVisible = !isRooted;
                            addToIgnore.Items.Add(byExtensionInSameFolder);
                        }

                        menu.Items.Add(addToIgnore);
                        hasExtra = true;
                    }

                    var lfsEnabled = new Commands.LFS(_repo.FullPath).IsEnabled();
                    if (lfsEnabled)
                    {
                        var lfs = new MenuItemModel
                        {
                            IconKey = App.MenuIconKey("Icons.LFS"),
                            Header = App.ResText("GitLFS")
                        };

                        var lfsTrackThisFile = new MenuItemModel
                        {
                            Header = App.ResText("GitLFS.Track", Path.GetFileName(change.Path)),
                            Command = new AsyncRelayCommand(async () =>
                            {
                                var log = _repo.CreateLog("Track LFS");
                                var succ = await Task.Run(() => new Commands.LFS(_repo.FullPath).Track(Path.GetFileName(change.Path), true, log));
                                if (succ)
                                    App.SendNotification(_repo.FullPath, $"Tracking file named {Path.GetFileName(change.Path)} successfully!");

                                log.Complete();
                            })
                        };
                        lfs.Items.Add(lfsTrackThisFile);

                        if (!string.IsNullOrEmpty(extension))
                        {
                            var lfsTrackByExtension = new MenuItemModel
                            {
                                Header = App.ResText("GitLFS.TrackByExtension", extension),
                                Command = new AsyncRelayCommand(async () =>
                                {
                                    var log = _repo.CreateLog("Track LFS");
                                    var succ = await Task.Run(() => new Commands.LFS(_repo.FullPath).Track($"*{extension}", false, log));
                                    if (succ)
                                        App.SendNotification(_repo.FullPath, $"Tracking all *{extension} files successfully!");

                                    log.Complete();
                                })
                            };
                            lfs.Items.Add(lfsTrackByExtension);
                        }

                        lfs.Items.Add(MenuModel.Separator());

                        var lfsLock = new MenuItemModel
                        {
                            IconKey = App.MenuIconKey("Icons.Lock"),
                            Header = App.ResText("GitLFS.Locks.Lock")
                        };
                        lfsLock.IsEnabled = _repo.Remotes.Count > 0;
                        if (_repo.Remotes.Count == 1)
                        {
                            lfsLock.Command = new AsyncRelayCommand(async () =>
                            {
                                var log = _repo.CreateLog("Lock LFS File");
                                var succ = await Task.Run(() => new Commands.LFS(_repo.FullPath).Lock(_repo.Remotes[0].Name, change.Path, log));
                                if (succ)
                                    App.SendNotification(_repo.FullPath, $"Lock file \"{change.Path}\" successfully!");

                                log.Complete();
                            });
                        }
                        else
                        {
                            foreach (var remote in _repo.Remotes)
                            {
                                var remoteName = remote.Name;
                                var lockRemote = new MenuItemModel
                                {
                                    Header = remoteName,
                                    Command = new AsyncRelayCommand(async () =>
                                    {
                                        var log = _repo.CreateLog("Lock LFS File");
                                        var succ = await Task.Run(() => new Commands.LFS(_repo.FullPath).Lock(remoteName, change.Path, log));
                                        if (succ)
                                            App.SendNotification(_repo.FullPath, $"Lock file \"{change.Path}\" successfully!");

                                        log.Complete();
                                    })
                                };
                                lfsLock.Items.Add(lockRemote);
                            }
                        }
                        lfs.Items.Add(lfsLock);

                        var lfsUnlock = new MenuItemModel
                        {
                            IconKey = App.MenuIconKey("Icons.Unlock"),
                            Header = App.ResText("GitLFS.Locks.Unlock")
                        };
                        lfsUnlock.IsEnabled = _repo.Remotes.Count > 0;
                        if (_repo.Remotes.Count == 1)
                        {
                            lfsUnlock.Command = new AsyncRelayCommand(async () =>
                            {
                                var log = _repo.CreateLog("Unlock LFS File");
                                var succ = await Task.Run(() => new Commands.LFS(_repo.FullPath).Unlock(_repo.Remotes[0].Name, change.Path, false, log));
                                if (succ)
                                    App.SendNotification(_repo.FullPath, $"Unlock file \"{change.Path}\" successfully!");

                                log.Complete();
                            });
                        }
                        else
                        {
                            foreach (var remote in _repo.Remotes)
                            {
                                var remoteName = remote.Name;
                                var unlockRemote = new MenuItemModel
                                {
                                    Header = remoteName,
                                    Command = new AsyncRelayCommand(async () =>
                                    {
                                        var log = _repo.CreateLog("Unlock LFS File");
                                        var succ = await Task.Run(() => new Commands.LFS(_repo.FullPath).Unlock(remoteName, change.Path, false, log));
                                        if (succ)
                                            App.SendNotification(_repo.FullPath, $"Unlock file \"{change.Path}\" successfully!");

                                        log.Complete();
                                    })
                                };
                                lfsUnlock.Items.Add(unlockRemote);
                            }
                        }
                        lfs.Items.Add(lfsUnlock);

                        menu.Items.Add(lfs);
                        hasExtra = true;
                    }

                    if (hasExtra)
                        menu.Items.Add(MenuModel.Separator());
                }

                var copy = new MenuItemModel
                {
                    IconKey = App.MenuIconKey("Icons.Copy"),
                    Header = App.ResText("CopyPath"),
                    Command = new RelayCommand(() =>
                    {
                        App.CopyText(change.Path);
                    })
                };
                menu.Items.Add(copy);

                var copyFullPath = new MenuItemModel
                {
                    IconKey = App.MenuIconKey("Icons.Copy"),
                    Header = App.ResText("CopyFullPath"),
                    Command = new RelayCommand(() =>
                    {
                        App.CopyText(Native.OS.GetAbsPath(_repo.FullPath, change.Path));
                    })
                };
                menu.Items.Add(copyFullPath);
            }
            else
            {
                var hasConflicts = false;
                var hasNonConflicts = false;
                foreach (var change in _selectedUnstaged)
                {
                    if (change.IsConflict)
                        hasConflicts = true;
                    else
                        hasNonConflicts = true;
                }

                if (hasConflicts)
                {
                    if (hasNonConflicts)
                    {
                        App.RaiseException(_repo.FullPath, "Selection contains both conflict and non-conflict changes!");
                        return null;
                    }

                    var useTheirs = new MenuItemModel
                    {
                        IconKey = App.MenuIconKey("Icons.Incoming"),
                        Header = App.ResText("FileCM.UseTheirs"),
                        Command = new RelayCommand(() => UseTheirs(_selectedUnstaged))
                    };
                    var useMine = new MenuItemModel
                    {
                        IconKey = App.MenuIconKey("Icons.Local"),
                        Header = App.ResText("FileCM.UseMine"),
                        Command = new RelayCommand(() => UseMine(_selectedUnstaged))
                    };

                    if (_inProgressContext is CherryPickInProgress cherryPick)
                    {
                        useTheirs.Header = App.Text("FileCM.ResolveUsing", cherryPick.HeadName);
                        useMine.Header = App.Text("FileCM.ResolveUsing", _repo.CurrentBranch.Name);
                    }
                    else if (_inProgressContext is RebaseInProgress rebase)
                    {
                        useTheirs.Header = App.Text("FileCM.ResolveUsing", rebase.HeadName);
                        useMine.Header = App.Text("FileCM.ResolveUsing", rebase.BaseName);
                    }
                    else if (_inProgressContext is RevertInProgress revert)
                    {
                        useTheirs.Header = App.Text("FileCM.ResolveUsing", $"{revert.Head.SHA.AsSpan(0, 10)} (revert)");
                        useMine.Header = App.Text("FileCM.ResolveUsing", _repo.CurrentBranch.Name);
                    }
                    else if (_inProgressContext is MergeInProgress merge)
                    {
                        useTheirs.Header = App.Text("FileCM.ResolveUsing", merge.SourceName);
                        useMine.Header = App.Text("FileCM.ResolveUsing", _repo.CurrentBranch.Name);
                    }

                    menu.Items.Add(useTheirs);
                    menu.Items.Add(useMine);
                    return menu;
                }

                var stage = new MenuItemModel
                {
                    IconKey = App.MenuIconKey("Icons.File.Add"),
                    Header = App.ResText("FileCM.StageMulti", _selectedUnstaged.Count),
                    Command = new RelayCommand(() => StageChanges(_selectedUnstaged, null))
                };
                menu.Items.Add(stage);

                var discard = new MenuItemModel
                {
                    IconKey = App.MenuIconKey("Icons.Undo"),
                    Header = App.ResText("FileCM.DiscardMulti", _selectedUnstaged.Count),
                    Command = new RelayCommand(() => Discard(_selectedUnstaged))
                };
                menu.Items.Add(discard);

                var stash = new MenuItemModel
                {
                    IconKey = App.MenuIconKey("Icons.Stashes.Add"),
                    Header = App.ResText("FileCM.StashMulti", _selectedUnstaged.Count),
                    Command = new RelayCommand(() =>
                    {
                        if (_repo.CanCreatePopup())
                            _repo.ShowPopup(new StashChanges(_repo, _selectedUnstaged, true));
                    })
                };
                menu.Items.Add(stash);
            }
            return menu;
        }

        public ContextMenuModel CreateContextMenuModelForStagedChanges()
        {
            if (_selectedStaged == null || _selectedStaged.Count == 0)
                return null;

            var menu = new ContextMenuModel();
            // AI menu item (if any)
            MenuItemModel ai = null;
            var services = _repo.GetPreferedOpenAIServices();
            if (services.Count > 0)
            {
                ai = new MenuItemModel
                {
                    IconKey = App.MenuIconKey("Icons.AIAssist"),
                    Header = App.ResText("ChangeCM.GenerateCommitMessage"),
                    Items = new Avalonia.Collections.AvaloniaList<MenuItemModel>()
                };
                if (services.Count == 1)
                {
                    ai.Command = new RelayCommand(() =>
                        App.ShowWindow(new AIAssistant(_repo, services[0], _selectedStaged, t => CommitMessage = t), true)
                    );
                }
                else
                {
                    foreach (var service in services)
                    {
                        var dup = service;
                        ai.Items.Add(new MenuItemModel
                        {
                            Header = service.Name,
                            Command = new RelayCommand(() =>
                                App.ShowWindow(new AIAssistant(_repo, dup, _selectedStaged, t => CommitMessage = t), true)
                            )
                        });
                    }
                }
            }

            if (_selectedStaged.Count == 1)
            {
                var change = _selectedStaged[0];
                var path = Path.GetFullPath(Path.Combine(_repo.FullPath, change.Path));

                var explore = new MenuItem();
                explore.IsEnabled = File.Exists(path) || Directory.Exists(path);
                explore.Header = App.Text("RevealFile");
                explore.Icon = App.CreateMenuIcon("Icons.Explore");
                explore.Click += (_, e) =>
                {
                    Native.OS.OpenInFileManager(path, true);
                    e.Handled = true;
                };

                var openWith = new MenuItemModel {
                    IconKey = App.MenuIconKey("Icons.OpenWith"),
                    Header = App.ResText("OpenWith"),
                    IsEnabled = File.Exists(path),
                    Command = new RelayCommand(() => Native.OS.OpenWithDefaultEditor(path))
                };

                var unstage = new MenuItemModel
                {
                    IconKey = App.MenuIconKey("Icons.File.Remove"),
                    Header = App.ResText("FileCM.Unstage"),
                    Command = new RelayCommand(() => UnstageChanges(_selectedStaged, null))
                };

                var stash = new MenuItemModel
                {
                    IconKey = App.MenuIconKey("Icons.Stashes.Add"),
                    Header = App.ResText("FileCM.Stash"),
                    Command = new RelayCommand(() =>
                    {
                        if (_repo.CanCreatePopup())
                            _repo.ShowPopup(new StashChanges(_repo, _selectedStaged, true));
                    })
                };

                var patch = new MenuItemModel
                {
                    IconKey = App.MenuIconKey("Icons.Diff"),
                    Header = App.ResText("FileCM.SaveAsPatch"),
                    Command = new RelayCommand(async () =>
                    {
                        var storageProvider = App.GetStorageProvider();
                        if (storageProvider == null)
                            return;

                        var options = new FilePickerSaveOptions();
                        options.Title = App.Text("FileCM.SaveAsPatch");
                        options.DefaultExtension = ".patch";
                        options.FileTypeChoices = [new FilePickerFileType("Patch File") { Patterns = ["*.patch"] }];

                        var storageFile = await storageProvider.SaveFilePickerAsync(options);
                        if (storageFile != null)
                        {
                            var succ = await Task.Run(() => Commands.SaveChangesAsPatch.ProcessLocalChanges(_repo.FullPath, _selectedStaged, false, storageFile.Path.LocalPath));
                            if (succ)
                                App.SendNotification(_repo.FullPath, App.Text("SaveAsPatchSuccess"));
                        }
                    })
                };

                var history = new MenuItemModel
                {
                    IconKey = App.MenuIconKey("Icons.Histories"),
                    Header = App.ResText("FileHistory"),
                    Command = new RelayCommand(() => App.ShowWindow(new FileHistories(_repo, change.Path), false))
                };

                menu.Items.Add(explore);
                menu.Items.Add(openWith);
                menu.Items.Add(MenuModel.Separator());
                menu.Items.Add(unstage);
                menu.Items.Add(stash);
                menu.Items.Add(patch);
                menu.Items.Add(MenuModel.Separator());
                menu.Items.Add(history);
                menu.Items.Add(MenuModel.Separator());

                var lfsEnabled = new Commands.LFS(_repo.FullPath).IsEnabled();
                if (lfsEnabled)
                {
                    var lfs = new MenuItemModel
                    {
                        IconKey = App.MenuIconKey("Icons.LFS"),
                        Header = App.ResText("GitLFS"),
                        Items = new Avalonia.Collections.AvaloniaList<MenuItemModel>()
                    };
                    lfs.Header = App.Text("GitLFS");
                    lfs.Icon = App.CreateMenuIcon("Icons.LFS");

                    var lfsLock = new MenuItem();
                    lfsLock.Header = App.Text("GitLFS.Locks.Lock");
                    lfsLock.Icon = App.CreateMenuIcon("Icons.Lock");
                    lfsLock.IsEnabled = _repo.Remotes.Count > 0;
                    if (_repo.Remotes.Count == 1)
                    {
                        lfsLock.Click += async (_, e) =>
                        {
                            var log = _repo.CreateLog("Lock LFS File");
                            var succ = await Task.Run(() => new Commands.LFS(_repo.FullPath).Lock(_repo.Remotes[0].Name, change.Path, log));
                            if (succ)
                                App.SendNotification(_repo.FullPath, $"Lock file \"{change.Path}\" successfully!");

                            log.Complete();
                            e.Handled = true;
                        };
                    }
                    else
                    {
                        foreach (var remote in _repo.Remotes)
                        {
                            var remoteName = remote.Name;
                            var lockRemote = new MenuItem();
                            lockRemote.Header = remoteName;
                            lockRemote.Click += async (_, e) =>
                            {
                                var log = _repo.CreateLog("Lock LFS File");
                                var succ = await Task.Run(() => new Commands.LFS(_repo.FullPath).Lock(remoteName, change.Path, log));
                                if (succ)
                                    App.SendNotification(_repo.FullPath, $"Lock file \"{change.Path}\" successfully!");

                                log.Complete();
                                e.Handled = true;
                            };
                            lfsLock.Items.Add(lockRemote);
                        }
                    }
                    lfs.Items.Add(lfsLock);

                    var lfsUnlock = new MenuItem();
                    lfsUnlock.Header = App.Text("GitLFS.Locks.Unlock");
                    lfsUnlock.Icon = App.CreateMenuIcon("Icons.Unlock");
                    lfsUnlock.IsEnabled = _repo.Remotes.Count > 0;
                    if (_repo.Remotes.Count == 1)
                    {
                        lfsUnlock.Click += async (_, e) =>
                        {
                            var log = _repo.CreateLog("Unlock LFS File");
                            var succ = await Task.Run(() => new Commands.LFS(_repo.FullPath).Unlock(_repo.Remotes[0].Name, change.Path, false, log));
                            if (succ)
                                App.SendNotification(_repo.FullPath, $"Unlock file \"{change.Path}\" successfully!");

                            log.Complete();
                            e.Handled = true;
                        };
                    }
                    else
                    {
                        foreach (var remote in _repo.Remotes)
                        {
                            var remoteName = remote.Name;
                            var unlockRemote = new MenuItem();
                            unlockRemote.Header = remoteName;
                            unlockRemote.Click += async (_, e) =>
                            {
                                var log = _repo.CreateLog("Unlock LFS File");
                                var succ = await Task.Run(() => new Commands.LFS(_repo.FullPath).Unlock(remoteName, change.Path, false, log));
                                if (succ)
                                    App.SendNotification(_repo.FullPath, $"Unlock file \"{change.Path}\" successfully!");

                                log.Complete();
                                e.Handled = true;
                            };
                            lfsUnlock.Items.Add(unlockRemote);
                        }
                    }
                    lfs.Items.Add(lfsUnlock);

                    menu.Items.Add(lfs);
                    menu.Items.Add(new MenuItem() { Header = "-" });
                }

                if (ai != null)
                {
                    menu.Items.Add(ai);
                    menu.Items.Add(new MenuItem() { Header = "-" });
                }

                var copyPath = new MenuItem();
                copyPath.Header = App.Text("CopyPath");
                copyPath.Icon = App.CreateMenuIcon("Icons.Copy");
                copyPath.Click += (_, e) =>
                {
                    App.CopyText(change.Path);
                    e.Handled = true;
                };

                var copyFullPath = new MenuItem();
                copyFullPath.Header = App.Text("CopyFullPath");
                copyFullPath.Icon = App.CreateMenuIcon("Icons.Copy");
                copyFullPath.Click += (_, e) =>
                {
                    App.CopyText(Native.OS.GetAbsPath(_repo.FullPath, change.Path));
                    e.Handled = true;
                };

                menu.Items.Add(copyPath);
                menu.Items.Add(copyFullPath);
            }
            else
            {
                var unstage = new MenuItem();
                unstage.Header = App.Text("FileCM.UnstageMulti", _selectedStaged.Count);
                unstage.Icon = App.CreateMenuIcon("Icons.File.Remove");
                unstage.Click += (_, e) =>
                {
                    UnstageChanges(_selectedStaged, null);
                    e.Handled = true;
                };

                var stash = new MenuItem();
                stash.Header = App.Text("FileCM.StashMulti", _selectedStaged.Count);
                stash.Icon = App.CreateMenuIcon("Icons.Stashes.Add");
                stash.Click += (_, e) =>
                {
                    if (_repo.CanCreatePopup())
                        _repo.ShowPopup(new StashChanges(_repo, _selectedStaged, true));

                    e.Handled = true;
                };

                var patch = new MenuItem();
                patch.Header = App.Text("FileCM.SaveAsPatch");
                patch.Icon = App.CreateMenuIcon("Icons.Diff");
                patch.Click += async (_, e) =>
                {
                    var storageProvider = App.GetStorageProvider();
                    if (storageProvider == null)
                        return;

                    var options = new FilePickerSaveOptions();
                    options.Title = App.Text("FileCM.SaveAsPatch");
                    options.DefaultExtension = ".patch";
                    options.FileTypeChoices = [new FilePickerFileType("Patch File") { Patterns = ["*.patch"] }];

                    var storageFile = await storageProvider.SaveFilePickerAsync(options);
                    if (storageFile != null)
                    {
                        var succ = await Task.Run(() => Commands.SaveChangesAsPatch.ProcessLocalChanges(_repo.FullPath, _selectedStaged, false, storageFile.Path.LocalPath));
                        if (succ)
                            App.SendNotification(_repo.FullPath, App.Text("SaveAsPatchSuccess"));
                    }

                    e.Handled = true;
                };

                menu.Items.Add(unstage);
                menu.Items.Add(stash);
                menu.Items.Add(patch);

                if (ai != null)
                {
                    menu.Items.Add(new MenuItem() { Header = "-" });
                    menu.Items.Add(ai);
                }
            }

            return menu;
        }

        public ContextMenu CreateContextMenuForCommitMessages()
        {
            var menu = new ContextMenu();

            var gitTemplate = new Commands.Config(_repo.FullPath).Get("commit.template");
            var templateCount = _repo.Settings.CommitTemplates.Count;
            if (templateCount == 0 && string.IsNullOrEmpty(gitTemplate))
            {
                menu.Items.Add(new MenuItem()
                {
                    Header = App.Text("WorkingCopy.NoCommitTemplates"),
                    Icon = App.CreateMenuIcon("Icons.Code"),
                    IsEnabled = false
                });
            }
            else
            {
                for (int i = 0; i < templateCount; i++)
                {
                    var template = _repo.Settings.CommitTemplates[i];
                    var item = new MenuItem();
                    item.Header = App.Text("WorkingCopy.UseCommitTemplate", template.Name);
                    item.Icon = App.CreateMenuIcon("Icons.Code");
                    item.Click += (_, e) =>
                    {
                        CommitMessage = template.Apply(_repo.CurrentBranch, _staged);
                        e.Handled = true;
                    };
                    menu.Items.Add(item);
                }

                if (!string.IsNullOrEmpty(gitTemplate))
                {
                    var friendlyName = gitTemplate;
                    if (!OperatingSystem.IsWindows())
                    {
                        var home = Environment.GetFolderPath(Environment.SpecialFolder.UserProfile);
                        var prefixLen = home.EndsWith('/') ? home.Length - 1 : home.Length;
                        if (gitTemplate.StartsWith(home, StringComparison.Ordinal))
                            friendlyName = $"~{gitTemplate.AsSpan(prefixLen)}";
                    }

                    var gitTemplateItem = new MenuItem();
                    gitTemplateItem.Header = App.Text("WorkingCopy.UseCommitTemplate", friendlyName);
                    gitTemplateItem.Icon = App.CreateMenuIcon("Icons.Code");
                    gitTemplateItem.Click += (_, e) =>
                    {
                        if (File.Exists(gitTemplate))
                            CommitMessage = File.ReadAllText(gitTemplate);
                        e.Handled = true;
                    };
                    menu.Items.Add(gitTemplateItem);
                }
            }

            menu.Items.Add(new MenuItem() { Header = "-" });

            var historiesCount = _repo.Settings.CommitMessages.Count;
            if (historiesCount == 0)
            {
                menu.Items.Add(new MenuItem()
                {
                    Header = App.Text("WorkingCopy.NoCommitHistories"),
                    Icon = App.CreateMenuIcon("Icons.Histories"),
                    IsEnabled = false
                });
            }
            else
            {
                for (int i = 0; i < historiesCount; i++)
                {
                    var message = _repo.Settings.CommitMessages[i].Trim().ReplaceLineEndings("\n");
                    var subjectEndIdx = message.IndexOf('\n');
                    var subject = subjectEndIdx > 0 ? message.Substring(0, subjectEndIdx) : message;
                    var item = new MenuItem();
                    item.Header = subject;
                    item.Icon = App.CreateMenuIcon("Icons.Histories");
                    item.Click += (_, e) =>
                    {
                        CommitMessage = message;
                        e.Handled = true;
                    };

                    menu.Items.Add(item);
                }
            }

            return menu;
        }

        public ContextMenu CreateContextForOpenAI()
        {
            if (_staged == null || _staged.Count == 0)
            {
                App.RaiseException(_repo.FullPath, "No files added to commit!");
                return null;
            }

            var services = _repo.GetPreferedOpenAIServices();
            if (services.Count == 0)
            {
                App.RaiseException(_repo.FullPath, "Bad configuration for OpenAI");
                return null;
            }

            if (services.Count == 1)
            {
                App.ShowWindow(new AIAssistant(_repo, services[0], _staged, t => CommitMessage = t), true);
                return null;
            }

            var menu = new ContextMenu() { Placement = PlacementMode.TopEdgeAlignedLeft };
            foreach (var service in services)
            {
                var dup = service;
                var item = new MenuItem();
                item.Header = service.Name;
                item.Click += (_, e) =>
                {
                    App.ShowWindow(new AIAssistant(_repo, dup, _staged, t => CommitMessage = t), true);
                    e.Handled = true;
                };

                menu.Items.Add(item);
            }

            return menu;
        }

        private List<Models.Change> GetVisibleChanges(List<Models.Change> changes)
        {
            if (string.IsNullOrEmpty(_filter))
                return changes;

            var visible = new List<Models.Change>();

            foreach (var c in changes)
            {
                if (c.Path.Contains(_filter, StringComparison.OrdinalIgnoreCase))
                    visible.Add(c);
            }

            return visible;
        }

        private List<Models.Change> GetStagedChanges()
        {
            if (_useAmend)
            {
                var head = new Commands.QuerySingleCommit(_repo.FullPath, "HEAD").Result();
                return new Commands.QueryStagedChangesWithAmend(_repo.FullPath, head.Parents.Count == 0 ? "4b825dc642cb6eb9a060e54bf8d69288fbee4904" : $"{head.SHA}^").Result();
            }

            var rs = new List<Models.Change>();
            foreach (var c in _cached)
            {
                if (c.Index != Models.ChangeState.None &&
                    c.Index != Models.ChangeState.Untracked)
                    rs.Add(c);
            }
            return rs;
        }

        private void UpdateDetail()
        {
            if (_selectedUnstaged.Count == 1)
                SetDetail(_selectedUnstaged[0], true);
            else if (_selectedStaged.Count == 1)
                SetDetail(_selectedStaged[0], false);
            else
                SetDetail(null, false);
        }

        private void UpdateInProgressState()
        {
            if (string.IsNullOrEmpty(_commitMessage))
            {
                var mergeMsgFile = Path.Combine(_repo.GitDir, "MERGE_MSG");
                if (File.Exists(mergeMsgFile))
                    CommitMessage = File.ReadAllText(mergeMsgFile);
            }

            if (File.Exists(Path.Combine(_repo.GitDir, "CHERRY_PICK_HEAD")))
            {
                InProgressContext = new CherryPickInProgress(_repo);
            }
            else if (Directory.Exists(Path.Combine(_repo.GitDir, "rebase-merge")) || Directory.Exists(Path.Combine(_repo.GitDir, "rebase-apply")))
            {
                var rebasing = new RebaseInProgress(_repo);
                InProgressContext = rebasing;

                if (string.IsNullOrEmpty(_commitMessage))
                {
                    var rebaseMsgFile = Path.Combine(_repo.GitDir, "rebase-merge", "message");
                    if (File.Exists(rebaseMsgFile))
                        CommitMessage = File.ReadAllText(rebaseMsgFile);
                    else if (rebasing.StoppedAt != null)
                        CommitMessage = new Commands.QueryCommitFullMessage(_repo.FullPath, rebasing.StoppedAt.SHA).Result();
                }
            }
            else if (File.Exists(Path.Combine(_repo.GitDir, "REVERT_HEAD")))
            {
                InProgressContext = new RevertInProgress(_repo);
            }
            else if (File.Exists(Path.Combine(_repo.GitDir, "MERGE_HEAD")))
            {
                InProgressContext = new MergeInProgress(_repo);
            }
            else
            {
                InProgressContext = null;
            }
        }

        private async void StageChanges(List<Models.Change> changes, Models.Change next)
        {
            var count = changes.Count;
            if (count == 0)
                return;

            // Use `_selectedUnstaged` instead of `SelectedUnstaged` to avoid UI refresh.
            _selectedUnstaged = next != null ? [next] : [];

            IsStaging = true;
            _repo.SetWatcherEnabled(false);

            var log = _repo.CreateLog("Stage");
            if (count == _unstaged.Count)
            {
                await Task.Run(() => new Commands.Add(_repo.FullPath, _repo.IncludeUntracked).Use(log).Exec());
            }
            else if (Native.OS.GitVersion >= Models.GitVersions.ADD_WITH_PATHSPECFILE)
            {
                var paths = new List<string>();
                foreach (var c in changes)
                    paths.Add(c.Path);

                var tmpFile = Path.GetTempFileName();
                File.WriteAllLines(tmpFile, paths);
                await Task.Run(() => new Commands.Add(_repo.FullPath, tmpFile).Use(log).Exec());
                File.Delete(tmpFile);
            }
            else
            {
                var paths = new List<string>();
                foreach (var c in changes)
                    paths.Add(c.Path);

                for (int i = 0; i < count; i += 10)
                {
                    var step = paths.GetRange(i, Math.Min(10, count - i));
                    await Task.Run(() => new Commands.Add(_repo.FullPath, step).Use(log).Exec());
                }
            }
            log.Complete();
            _repo.MarkWorkingCopyDirtyManually();
            _repo.SetWatcherEnabled(true);
            IsStaging = false;
        }

        private async void UnstageChanges(List<Models.Change> changes, Models.Change next)
        {
            var count = changes.Count;
            if (count == 0)
                return;

            // Use `_selectedStaged` instead of `SelectedStaged` to avoid UI refresh.
            _selectedStaged = next != null ? [next] : [];

            IsUnstaging = true;
            _repo.SetWatcherEnabled(false);

            var log = _repo.CreateLog("Unstage");
            if (_useAmend)
            {
                log.AppendLine("$ git update-index --index-info ");
                await Task.Run(() => new Commands.UnstageChangesForAmend(_repo.FullPath, changes).Exec());
            }
            else if (count == _staged.Count)
            {
                await Task.Run(() => new Commands.Reset(_repo.FullPath).Use(log).Exec());
            }
            else
            {
                for (int i = 0; i < count; i += 10)
                {
                    var step = changes.GetRange(i, Math.Min(10, count - i));
                    await Task.Run(() => new Commands.Reset(_repo.FullPath, step).Use(log).Exec());
                }
            }
            log.Complete();
            _repo.MarkWorkingCopyDirtyManually();
            _repo.SetWatcherEnabled(true);
            IsUnstaging = false;
        }

        private void SetDetail(Models.Change change, bool isUnstaged)
        {
            if (_isLoadingData)
                return;

            if (change == null)
                DetailContext = null;
            else if (change.IsConflict && isUnstaged)
                DetailContext = new Conflict(_repo, this, change);
            else
                DetailContext = new DiffContext(_repo.FullPath, new Models.DiffOption(change, isUnstaged), _detailContext as DiffContext);
        }

        private void DoCommit(bool autoStage, bool autoPush, bool allowEmpty = false, bool confirmWithFilter = false)
        {
            if (string.IsNullOrWhiteSpace(_commitMessage))
                return;

            if (!_repo.CanCreatePopup())
            {
                App.RaiseException(_repo.FullPath, "Repository has unfinished job! Please wait!");
                return;
            }

            if (!string.IsNullOrEmpty(_filter) && _staged.Count > _visibleStaged.Count && !confirmWithFilter)
            {
                var confirmMessage = App.Text("WorkingCopy.ConfirmCommitWithFilter", _staged.Count, _visibleStaged.Count, _staged.Count - _visibleStaged.Count);
                App.ShowWindow(new ConfirmCommit(confirmMessage, () => DoCommit(autoStage, autoPush, allowEmpty, true)), true);
                return;
            }

            if (!_useAmend && !allowEmpty)
            {
                if ((autoStage && _count == 0) || (!autoStage && _staged.Count == 0))
                {
                    App.ShowWindow(new ConfirmEmptyCommit(_count > 0, stageAll => DoCommit(stageAll, autoPush, true, confirmWithFilter)), true);
                    return;
                }
            }

            IsCommitting = true;
            _repo.Settings.PushCommitMessage(_commitMessage);
            _repo.SetWatcherEnabled(false);

            var log = _repo.CreateLog("Commit");
            Task.Run(() =>
            {
                var succ = true;
                if (autoStage && _unstaged.Count > 0)
                    succ = new Commands.Add(_repo.FullPath, _repo.IncludeUntracked).Use(log).Exec();

                if (succ)
                    succ = new Commands.Commit(_repo.FullPath, _commitMessage, _useAmend, _repo.Settings.EnableSignOffForCommit).Use(log).Run();

                log.Complete();

                Dispatcher.UIThread.Post(() =>
                {
                    if (succ)
                    {
                        CommitMessage = string.Empty;
                        UseAmend = false;

                        if (autoPush && _repo.Remotes.Count > 0)
                        {
                            if (_repo.CurrentBranch == null)
                            {
                                var currentBranchName = Commands.Branch.ShowCurrent(_repo.FullPath);
                                var tmp = new Models.Branch() { Name = currentBranchName };
                                _repo.ShowAndStartPopup(new Push(_repo, tmp));
                            }
                            else
                            {
                                _repo.ShowAndStartPopup(new Push(_repo, null));
                            }
                        }
                    }

                    _repo.MarkBranchesDirtyManually();
                    _repo.SetWatcherEnabled(true);
                    IsCommitting = false;
                });
            });
        }

        private bool IsChanged(List<Models.Change> old, List<Models.Change> cur)
        {
            if (old.Count != cur.Count)
                return true;

            var oldSet = new HashSet<string>();
            foreach (var c in old)
                oldSet.Add($"{c.Path}\n{c.WorkTree}\n{c.Index}");

            foreach (var c in cur)
            {
                if (!oldSet.Contains($"{c.Path}\n{c.WorkTree}\n{c.Index}"))
                    return true;
            }

            return false;
        }

        private Repository _repo = null;
        private bool _isLoadingData = false;
        private bool _isStaging = false;
        private bool _isUnstaging = false;
        private bool _isCommitting = false;
        private bool _useAmend = false;
        private bool _hasRemotes = false;
        private List<Models.Change> _cached = [];
        private List<Models.Change> _unstaged = [];
        private List<Models.Change> _visibleUnstaged = [];
        private List<Models.Change> _staged = [];
        private List<Models.Change> _visibleStaged = [];
        private List<Models.Change> _selectedUnstaged = [];
        private List<Models.Change> _selectedStaged = [];
        private int _count = 0;
        private object _detailContext = null;
        private string _filter = string.Empty;
        private string _commitMessage = string.Empty;

        private bool _hasUnsolvedConflicts = false;
        private InProgressContext _inProgressContext = null;
    }
}
