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
            if (e.Key == Key.Enter && Keyboard.Modifiers == ModifierKeys.Control)
            {
                if (_vm.PostCommand.CanExecute(null))
                    _vm.PostCommand.Execute(null);
                e.Handled = true;
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
