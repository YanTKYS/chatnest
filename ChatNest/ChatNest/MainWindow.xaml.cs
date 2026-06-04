using System.Windows;
using System.Windows.Input;
using ChatNest.ViewModels;

namespace ChatNest
{
    public partial class MainWindow : Window
    {
        private readonly MainViewModel _vm;

        public MainWindow(string? filePath = null)
        {
            InitializeComponent();
            _vm = new MainViewModel();
            DataContext = _vm;

            _vm.Messages.CollectionChanged += (_, _) => ScrollToBottom();

            if (filePath != null)
                Loaded += (_, _) => _vm.LoadFromPath(filePath);
        }

        private void InputBox_PreviewKeyDown(object sender, KeyEventArgs e)
        {
            var mods = Keyboard.Modifiers;

            // Post: Ctrl+Enter or Shift+Enter
            if (e.Key == Key.Enter &&
                (mods == ModifierKeys.Control || mods == ModifierKeys.Shift))
            {
                if (_vm.PostCommand.CanExecute(null))
                    _vm.PostCommand.Execute(null);
                e.Handled = true;
                return;
            }

            // Speaker cycle: Ctrl+→/← or Shift+→/←
            if ((e.Key == Key.Right || e.Key == Key.Left) &&
                (mods == ModifierKeys.Control || mods == ModifierKeys.Shift))
            {
                _vm.CycleSpeaker(e.Key == Key.Right);
                e.Handled = true;
            }
        }

        private void FinishButton_Click(object sender, RoutedEventArgs e)
        {
            var dialog = new FinishDialog { Owner = this };
            dialog.ShowDialog();

            switch (dialog.SelectedAction)
            {
                case FinishAction.CopyMarkdown:
                    _vm.ExecuteMarkdownCopyWithSave();
                    break;
                case FinishAction.CopyIdeaNest:
                    _vm.ExecuteIdeaNestCopyWithSave();
                    break;
                case FinishAction.DeleteAll:
                    _vm.DeleteAllCommand.Execute(null);
                    break;
            }
        }

        private void ScrollToBottom()
        {
            Dispatcher.InvokeAsync(
                () => ChatScrollViewer.ScrollToBottom(),
                System.Windows.Threading.DispatcherPriority.Background);
        }
    }
}
