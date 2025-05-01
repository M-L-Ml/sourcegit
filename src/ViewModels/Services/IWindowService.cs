using System;

namespace SourceGit.ViewModels.Services
{
    public interface IWindowService
    {
        void ShowWindow(string windowName, bool isModal = false);
        void CloseWindow(string windowName);
    }
}
