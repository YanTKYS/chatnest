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
using ChatNest.Services;
using Microsoft.Win32;

namespace ChatNest.ViewModels
{
    public class MainViewModel : INotifyPropertyChanged
    {
        public const string AppVersion = "0.1.4";

        private string _inputText = string.Empty;
        private Speaker _selectedSpeaker = Speaker.自分;
        private string? _currentFilePath;
        private bool _isTopmost;

        private readonly SettingsService _settings = new();

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

        public bool IsTopmost
        {
            get => _isTopmost;
            set { _isTopmost = value; OnPropertyChanged(); }
        }

        public string WindowTitle => _currentFilePath != null
            ? $"ChatNest - {Path.GetFileName(_currentFilePath)} - ver{AppVersion}"
            : $"ChatNest - ver{AppVersion}";

        public Speaker[] Speakers { get; } = Enum.GetValues<Speaker>();

        private readonly RelayCommand _postCommand;
        private readonly RelayCommand _deleteAllCommand;

        public ICommand PostCommand => _postCommand;
        public ICommand DeleteMessageCommand { get; }
        public ICommand DeleteAllCommand => _deleteAllCommand;
        public ICommand SaveCommand { get; }
        public ICommand SaveAsCommand { get; }
        public ICommand LoadCommand { get; }
        public ICommand NewCommand { get; }

        public MainViewModel()
        {
            _postCommand    = new RelayCommand(Post, () => !string.IsNullOrWhiteSpace(InputText));
            _deleteAllCommand = new RelayCommand(DeleteAll, () => Messages.Count > 0);

            DeleteMessageCommand = new RelayCommand<Message>(DeleteMessage);
            SaveCommand   = new RelayCommand(Save);
            SaveAsCommand = new RelayCommand(SaveAs);
            LoadCommand   = new RelayCommand(Load);
            NewCommand    = new RelayCommand(New);

            Messages.CollectionChanged += OnMessagesChanged;
        }

        private void OnMessagesChanged(object? sender, NotifyCollectionChangedEventArgs e)
            => _deleteAllCommand.RaiseCanExecuteChanged();

        // ── Speaker cycle ─────────────────────────────────────────────────────

        public void CycleSpeaker(bool forward)
        {
            var speakers = Enum.GetValues<Speaker>();
            int idx = Array.IndexOf(speakers, SelectedSpeaker);
            idx = forward
                ? (idx + 1) % speakers.Length
                : (idx - 1 + speakers.Length) % speakers.Length;
            SelectedSpeaker = speakers[idx];
        }

        // ── Post / New ────────────────────────────────────────────────────────

        private void Post()
        {
            var text = InputText.Trim();
            if (string.IsNullOrEmpty(text)) return;
            Messages.Add(new Message { Speaker = SelectedSpeaker, Text = text });
            InputText = string.Empty;
        }

        private void New()
        {
            if (Messages.Count > 0 &&
                MessageBox.Show("現在のログを破棄して新規チャットを開始しますか？", "新規チャット",
                    MessageBoxButton.OKCancel, MessageBoxImage.Question) != MessageBoxResult.OK)
                return;

            Messages.Clear();
            SetCurrentFile(null);
        }

        // ── Delete ────────────────────────────────────────────────────────────

        private void DeleteMessage(Message? message)
        {
            if (message == null) return;
            if (MessageBox.Show("この発言を削除しますか？", "削除の確認",
                    MessageBoxButton.OKCancel, MessageBoxImage.Question) == MessageBoxResult.OK)
                Messages.Remove(message);
        }

        private void DeleteAll()
        {
            if (Messages.Count == 0) return;
            if (MessageBox.Show("すべての発言を削除しますか？\nこの操作は元に戻せません。", "全件削除の確認",
                    MessageBoxButton.OKCancel, MessageBoxImage.Warning) == MessageBoxResult.OK)
                Messages.Clear();
        }

        // ── 終了処理コピー (save-then-copy) ──────────────────────────────────

        public void ExecuteMarkdownCopyWithSave()
        {
            if (!SaveForCopy()) return;

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

            if (TrySetClipboard(sb.ToString().TrimEnd()))
                MessageBox.Show("Markdown形式でコピーしました。", "コピー完了",
                    MessageBoxButton.OK, MessageBoxImage.Information);
        }

        public void ExecuteIdeaNestCopyWithSave()
        {
            if (!SaveForCopy()) return;

            var sb = new StringBuilder();
            foreach (var msg in Messages)
            {
                sb.AppendLine($"【{msg.Speaker}】");
                sb.AppendLine(msg.Text);
                sb.AppendLine();
            }

            if (TrySetClipboard(sb.ToString().TrimEnd()))
                MessageBox.Show("IdeaNest用形式でコピーしました。", "コピー完了",
                    MessageBoxButton.OK, MessageBoxImage.Information);
        }

        // 保存してからコピー。保存キャンセル時は false を返す。
        private bool SaveForCopy()
        {
            if (_currentFilePath != null)
                return TryWriteToFile(_currentFilePath, updatePath: false);

            var dlg = new SaveFileDialog
            {
                Filter = "ChatNest ファイル (*.chatnest)|*.chatnest",
                DefaultExt = ".chatnest",
                FileName = $"chatnest_{DateTime.Now:yyyyMMdd_HHmm}"
            };

            if (dlg.ShowDialog() != true)
            {
                MessageBox.Show(
                    "保存がキャンセルされたため、コピーは実行しませんでした。",
                    "コピー中止",
                    MessageBoxButton.OK,
                    MessageBoxImage.Information);
                return false;
            }

            return TryWriteToFile(dlg.FileName, updatePath: true);
        }

        // ── Save / Load ───────────────────────────────────────────────────────

        private void Save()
        {
            if (_currentFilePath == null) { SaveAs(); return; }
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

        private bool TryWriteToFile(string path, bool updatePath)
        {
            try
            {
                var session = new ChatSessionData
                {
                    Version = AppVersion,
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
                {
                    SetCurrentFile(path);
                    _settings.AddRecentFile(path);
                }
                return true;
            }
            catch (Exception ex)
            {
                MessageBox.Show($"保存に失敗しました。\n{ex.Message}", "保存エラー",
                    MessageBoxButton.OK, MessageBoxImage.Error);
                return false;
            }
        }

        private void Load()
        {
            var dlg = new OpenFileDialog
            {
                Filter = "ChatNest ファイル (*.chatnest)|*.chatnest"
            };
            if (dlg.ShowDialog() == true)
                LoadFromPath(dlg.FileName);
        }

        public void LoadFromPath(string path)
        {
            try
            {
                var json = File.ReadAllText(path, Encoding.UTF8);
                var session = JsonSerializer.Deserialize<ChatSessionData>(json);
                if (session?.Messages == null) return;

                if (Messages.Count > 0 &&
                    MessageBox.Show("現在のログを破棄してファイルを読み込みますか？", "読み込みの確認",
                        MessageBoxButton.OKCancel, MessageBoxImage.Question) != MessageBoxResult.OK)
                    return;

                Messages.Clear();
                int skipped = 0;
                foreach (var data in session.Messages)
                {
                    // Migrate renamed speaker: 要約 → 結論
                    var speakerName = data.Speaker == "要約" ? "結論" : data.Speaker;
                    if (Enum.TryParse<Speaker>(speakerName, out var speaker))
                        Messages.Add(new Message { Id = data.Id, Speaker = speaker, Text = data.Text, CreatedAt = data.CreatedAt });
                    else
                        skipped++;
                }

                SetCurrentFile(path);
                _settings.AddRecentFile(path);

                if (skipped > 0)
                    MessageBox.Show(
                        $"{skipped} 件の発言を読み込めませんでした。未知の発言者が含まれているためスキップしました。",
                        "読み込み警告", MessageBoxButton.OK, MessageBoxImage.Warning);
            }
            catch (Exception ex)
            {
                MessageBox.Show($"読み込みに失敗しました。\n{ex.Message}", "エラー",
                    MessageBoxButton.OK, MessageBoxImage.Error);
            }
        }

        // ── Clipboard ────────────────────────────────────────────────────────

        private static bool TrySetClipboard(string text)
        {
            try { Clipboard.SetText(text); return true; }
            catch (Exception ex)
            {
                MessageBox.Show($"クリップボードへのコピーに失敗しました。再度お試しください。\n{ex.Message}",
                    "コピー失敗", MessageBoxButton.OK, MessageBoxImage.Warning);
                return false;
            }
        }

        // ── Helpers ───────────────────────────────────────────────────────────

        private void SetCurrentFile(string? path)
        {
            _currentFilePath = path;
            OnPropertyChanged(nameof(WindowTitle));
        }

        public event PropertyChangedEventHandler? PropertyChanged;
        private void OnPropertyChanged([CallerMemberName] string? name = null)
            => PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(name));
    }

    public class ChatSessionData
    {
        [JsonPropertyName("version")]
        public string Version { get; set; } = MainViewModel.AppVersion;

        [JsonPropertyName("messages")]
        public List<MessageData> Messages { get; set; } = new();
    }

    public class MessageData
    {
        [JsonPropertyName("id")]       public Guid Id { get; set; }
        [JsonPropertyName("speaker")]  public string Speaker { get; set; } = string.Empty;
        [JsonPropertyName("text")]     public string Text { get; set; } = string.Empty;
        [JsonPropertyName("createdAt")] public DateTimeOffset CreatedAt { get; set; }
    }
}
