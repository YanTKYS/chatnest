using System;
using System.Windows.Input;

namespace ChatNest.ViewModels
{
    public enum FinishAction { None, CopyMarkdown, CopyIdeaNest, DeleteAll }

    public class FinishDialogViewModel
    {
        public FinishAction SelectedAction { get; private set; } = FinishAction.None;
        public event EventHandler? CloseRequested;

        public ICommand CopyMarkdownCommand { get; }
        public ICommand CopyIdeaNestCommand { get; }
        public ICommand DeleteAllCommand { get; }
        public ICommand CancelCommand { get; }

        public FinishDialogViewModel()
        {
            CopyMarkdownCommand = new RelayCommand(() => Select(FinishAction.CopyMarkdown));
            CopyIdeaNestCommand = new RelayCommand(() => Select(FinishAction.CopyIdeaNest));
            DeleteAllCommand    = new RelayCommand(() => Select(FinishAction.DeleteAll));
            CancelCommand       = new RelayCommand(() => Select(FinishAction.None));
        }

        private void Select(FinishAction action)
        {
            SelectedAction = action;
            CloseRequested?.Invoke(this, EventArgs.Empty);
        }
    }
}
