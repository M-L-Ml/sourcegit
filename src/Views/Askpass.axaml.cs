using System;
using Avalonia.Interactivity;

namespace SourceGit.Views
{
    public partial class Askpass : ChromelessWindow
    {
        public Askpass()
        {
            InitializeComponent();
        }

        public void SetTxtDescription(string text)
        {
            TxtDescription.Text = text;
        }

        private void CloseWindow(object _1, RoutedEventArgs _2)
        {
            Console.Out.WriteLine("No passphrase entered.");
            ViewModels.App.Quit(-1);
        }

        private void EnterPassword(object _1, RoutedEventArgs _2)
        {
            var passphrase = TxtPassphrase.Text ?? string.Empty;
            Console.Out.Write($"{passphrase}\n");
            ViewModels.App.Quit(0);
        }
    }
}
