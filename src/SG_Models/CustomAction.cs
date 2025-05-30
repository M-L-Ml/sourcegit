﻿using CommunityToolkit.Mvvm.ComponentModel;

namespace SourceGit.Models
{
    public enum CustomActionScope
    {
        Repository,
        Commit,
        Branch,
    }
    //TODO: ObservableObject derived classes is considered ViewModel. So either move this to viewmodel or split and make the pure model to leave here.
    public class CustomAction : ObservableObject
    {
        public string Name
        {
            get => _name;
            set => SetProperty(ref _name, value);
        }

        public CustomActionScope Scope
        {
            get => _scope;
            set => SetProperty(ref _scope, value);
        }

        public string Executable
        {
            get => _executable;
            set => SetProperty(ref _executable, value);
        }

        public string Arguments
        {
            get => _arguments;
            set => SetProperty(ref _arguments, value);
        }

        public bool WaitForExit
        {
            get => _waitForExit;
            set => SetProperty(ref _waitForExit, value);
        }

        private string _name = string.Empty;
        private CustomActionScope _scope = CustomActionScope.Repository;
        private string _executable = string.Empty;
        private string _arguments = string.Empty;
        private bool _waitForExit = true;
    }
}
