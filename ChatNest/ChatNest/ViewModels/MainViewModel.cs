using System;
using System.Collections.ObjectModel;
using System.Collections.Specialized;
using System.ComponentModel;
using System.IO;
using System.Linq;
using System.Runtime.CompilerServices;
using System.Text;
using System.Text.Json;
using System.Text.Json.Serialization;
using System.Windows;
using System.Windows.Input;
using ChatNest.Models;
using Microsoft.Win32;

namespace ChatNest.ViewModels
{
    public class MainViewModel : INotifyPropertyChanged
    {
        private string _inputText = string.Empty;
        private Speaker _selectedSpeaker = Speaker.自分;
        private string? _currentFilePath;

        public ObservableCollection<Message> Messages { get; } = new();

        public string InputText
        {
            get => _inputText;
            set
            {
                _inputText = value;
                OnPropertyChanged();
                _postCommand.RaiseCanExecuteChanged();
            }
        }

        public Speaker SelectedSpeaker
        {
            get => _selectedSpeaker;
            set { _selectedSpeaker = value; OnPropertyChanged(); }
        }

        public Speaker[] Speakers { get; } = Enum.GetValues<Speaker>();

        private readonly RelayCommand _postCommand;
        private readonly RelayCommand _deleteAllCommand;
        private readonly RelayCommand _copyMarkdownCommand;
        private readonly RelayCommand _copyIdeaNestCommand;

        public ICommand PostCommand => _postCommand;
        public ICommand DeleteMessageCommand { get; }
        public ICommand DeleteAllCommand => _deleteAllCommand;
        public ICommand CopyMarkdownCommand => _copyMarkdownCommand;
        public ICommand CopyIdeaNestCommand => _copyIdeaNestCommand;
        public ICommand SaveCommand { get; }
        public ICommand SaveAsCommand { get; }
        public ICommand LoadCommand { get; }

        public MainViewModel()
        {
            _postCommand = new RelayCommand(Post, () => !string.IsNullOrWhiteSpace(InputText));
            _deleteAllCommand = new RelayCommand(DeleteAll, () => Messages.Count > 0);
            _copyMarkdownCommand = new RelayCommand(CopyMarkdown, () => Messages.Count > 0);
            _copyIdeaNestCommand = new RelayCommand(CopyIdeaNest, () => Messages.Count > 0);

            DeleteMessageCommand = new RelayCommand<Message>(DeleteMessage);
            SaveCommand = new RelayCommand(Save);
            SaveAsCommand = new RelayCommand(SaveAs);
            LoadCommand = new RelayCommand(Load);

            Messages.CollectionChanged += OnMessagesChanged;
        }

        private void OnMessagesChanged(object? sender, NotifyCollectionChangedEventArgs e)
        {
            _deleteAllCommand.RaiseCanExecuteChanged();
            _copyMarkdownCommand.RaiseCanExecuteChanged();
            _copyIdeaNestCommand.RaiseCanExecuteChanged();
        }

        private void Post()
        {
            var text = InputText.Trim();
            if (string.IsNullOrEmpty(text)) return;

            Messages.Add(new Message
            {
                Speaker = SelectedSpeaker,
                Text = text
            });

            InputText = string.Empty;
        }

        private void DeleteMessage(Message? message)
        {
            if (message == null) return;

            var result = MessageBox.Show(
                "この発言を削除しますか？",
                "削除の確認",
                MessageBoxButton.OKCancel,
                MessageBoxImage.Question);

            if (result == MessageBoxResult.OK)
                Messages.Remove(message);
        }

        private void DeleteAll()
        {
            if (Messages.Count == 0) return;

            var result = MessageBox.Show(
                "すべての発言を削除しますか？\nこの操作は元に戻せません。",
                "全件削除の確認",
                MessageBoxButton.OKCancel,
                MessageBoxImage.Warning);

            if (result == MessageBoxResult.OK)
                Messages.Clear();
        }

        private void CopyMarkdown()
        {
            var sb = new StringBuilder();
            sb.AppendLine("# ChatNest Log");
            sb.AppendLine();

            foreach (var msg in Messages)
            {
                sb.AppendLine($"## {msg.Speaker}");
                sb.AppendLine();
                sb.AppendLine(msg.Text);
                sb.AppendLine();
            }

            TrySetClipboard(sb.ToString().TrimEnd());
        }

        private void CopyIdeaNest()
        {
            var sb = new StringBuilder();

            foreach (var msg in Messages)
            {
                sb.AppendLine($"【{msg.Speaker}】");
                sb.AppendLine(msg.Text);
                sb.AppendLine();
            }

            TrySetClipboard(sb.ToString().TrimEnd());
        }

        private static void TrySetClipboard(string text)
        {
            try
            {
                Clipboard.SetText(text);
            }
            catch (Exception ex)
            {
                MessageBox.Show(
                    $"クリップボードへのコピーに失敗しました。再度お試しください。\n{ex.Message}",
                    "コピー失敗",
                    MessageBoxButton.OK,
                    MessageBoxImage.Warning);
            }
        }

        private void Save()
        {
            if (_currentFilePath == null)
            {
                SaveAs();
                return;
            }
            TryWriteToFile(_currentFilePath, updatePath: false);
        }

        private void SaveAs()
        {
            var dlg = new SaveFileDialog
            {
                Filter = "ChatNest ファイル (*.chatnest)|*.chatnest",
                DefaultExt = ".chatnest",
                FileName = $"chatnest_{DateTime.Now:yyyyMMdd_HHmm}"
            };

            if (dlg.ShowDialog() == true)
                TryWriteToFile(dlg.FileName, updatePath: true);
        }

        private void TryWriteToFile(string path, bool updatePath)
        {
            try
            {
                var session = new ChatSessionData
                {
                    Version = "0.1.0",
                    Messages = Messages.Select(m => new MessageData
                    {
                        Id = m.Id,
                        Speaker = m.Speaker.ToString(),
                        Text = m.Text,
                        CreatedAt = m.CreatedAt
                    }).ToList()
                };

                var json = JsonSerializer.Serialize(session, new JsonSerializerOptions { WriteIndented = true });
                File.WriteAllText(path, json, Encoding.UTF8);

                if (updatePath)
                    _currentFilePath = path;
            }
            catch (Exception ex)
            {
                MessageBox.Show(
                    $"保存に失敗しました。\n{ex.Message}",
                    "保存エラー",
                    MessageBoxButton.OK,
                    MessageBoxImage.Error);
            }
        }

        private void Load()
        {
            var dlg = new OpenFileDialog
            {
                Filter = "ChatNest ファイル (*.chatnest)|*.chatnest"
            };

            if (dlg.ShowDialog() != true) return;

            try
            {
                var json = File.ReadAllText(dlg.FileName, Encoding.UTF8);
                var session = JsonSerializer.Deserialize<ChatSessionData>(json);
                if (session?.Messages == null) return;

                if (Messages.Count > 0)
                {
                    var result = MessageBox.Show(
                        "現在のログを破棄してファイルを読み込みますか？",
                        "読み込みの確認",
                        MessageBoxButton.OKCancel,
                        MessageBoxImage.Question);

                    if (result != MessageBoxResult.OK) return;
                }

                Messages.Clear();
                int skipped = 0;

                foreach (var data in session.Messages)
                {
                    if (Enum.TryParse<Speaker>(data.Speaker, out var speaker))
                    {
                        Messages.Add(new Message
                        {
                            Id = data.Id,
                            Speaker = speaker,
                            Text = data.Text,
                            CreatedAt = data.CreatedAt
                        });
                    }
                    else
                    {
                        skipped++;
                    }
                }

                _currentFilePath = dlg.FileName;

                if (skipped > 0)
                {
                    MessageBox.Show(
                        $"{skipped} 件の発言を読み込めませんでした。\n未知の発言者が含まれているため、該当メッセージをスキップしました。",
                        "読み込み警告",
                        MessageBoxButton.OK,
                        MessageBoxImage.Warning);
                }
            }
            catch (Exception ex)
            {
                MessageBox.Show($"読み込みに失敗しました。\n{ex.Message}", "エラー", MessageBoxButton.OK, MessageBoxImage.Error);
            }
        }

        public event PropertyChangedEventHandler? PropertyChanged;
        private void OnPropertyChanged([CallerMemberName] string? name = null)
            => PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(name));
    }

    public class ChatSessionData
    {
        [JsonPropertyName("version")]
        public string Version { get; set; } = "0.1.0";

        [JsonPropertyName("messages")]
        public List<MessageData> Messages { get; set; } = new();
    }

    public class MessageData
    {
        [JsonPropertyName("id")]
        public Guid Id { get; set; }

        [JsonPropertyName("speaker")]
        public string Speaker { get; set; } = string.Empty;

        [JsonPropertyName("text")]
        public string Text { get; set; } = string.Empty;

        [JsonPropertyName("createdAt")]
        public DateTimeOffset CreatedAt { get; set; }
    }
}
