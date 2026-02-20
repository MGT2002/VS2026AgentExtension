using OllamaCommunicationService;
using Microsoft.VisualStudio.Shell;
using System;
using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Input;
using System.Windows.Media;

namespace OllamaAgent
{
    public class OllamaChatControl : UserControl
    {
        private readonly OllamaManager ollamaManager;
        private readonly ComboBox modelComboBox;
        private readonly TextBox transcriptTextBox;
        private readonly TextBox inputTextBox;
        private readonly Button sendButton;
        private readonly Button cancelButton;
        private readonly Button clearButton;
        private CancellationTokenSource activeRequestCts;

        public OllamaChatControl(OllamaManager ollamaManager)
        {
            this.ollamaManager = ollamaManager ?? throw new ArgumentNullException(nameof(ollamaManager));

            var root = new Grid();
            root.RowDefinitions.Add(new RowDefinition { Height = GridLength.Auto });
            root.RowDefinitions.Add(new RowDefinition { Height = new GridLength(1, GridUnitType.Star) });
            root.RowDefinitions.Add(new RowDefinition { Height = GridLength.Auto });
            root.RowDefinitions.Add(new RowDefinition { Height = GridLength.Auto });

            var modelPanel = new StackPanel
            {
                Orientation = Orientation.Horizontal,
                Margin = new Thickness(0, 0, 0, 6)
            };
            var modelLabel = new TextBlock
            {
                Text = "Model:",
                VerticalAlignment = VerticalAlignment.Center,
                Margin = new Thickness(0, 0, 6, 0),
                FontWeight = FontWeights.SemiBold,
                Foreground = Brushes.White
            };
            modelComboBox = new ComboBox
            {
                Width = 220,
                ItemsSource = new List<string>
                {
                    OllamaManager.ModelSmart,
                    OllamaManager.ModelWeak,
                    OllamaManager.ModelMedium
                }
            };
            modelComboBox.SelectedItem = string.IsNullOrWhiteSpace(this.ollamaManager.Model)
                ? OllamaManager.ModelSmart
                : this.ollamaManager.Model;
            modelComboBox.SelectionChanged += ModelComboBox_SelectionChanged;
            modelPanel.Children.Add(modelLabel);
            modelPanel.Children.Add(modelComboBox);
            Grid.SetRow(modelPanel, 0);

            transcriptTextBox = new TextBox
            {
                IsReadOnly = true,
                AcceptsReturn = true,
                VerticalScrollBarVisibility = ScrollBarVisibility.Auto,
                HorizontalScrollBarVisibility = ScrollBarVisibility.Auto,
                TextWrapping = TextWrapping.Wrap
            };
            Grid.SetRow(transcriptTextBox, 1);

            inputTextBox = new TextBox
            {
                AcceptsReturn = true,
                Height = 90,
                VerticalScrollBarVisibility = ScrollBarVisibility.Auto,
                TextWrapping = TextWrapping.Wrap
            };
            inputTextBox.KeyDown += InputTextBox_KeyDown;
            Grid.SetRow(inputTextBox, 2);

            sendButton = new Button
            {
                Content = "Send",
                Height = 30,
                HorizontalAlignment = HorizontalAlignment.Stretch
            };
            sendButton.Click += (s, e) => _ = QueueSendAsync();

            cancelButton = new Button
            {
                Content = "Cancel",
                Height = 30,
                HorizontalAlignment = HorizontalAlignment.Stretch,
                IsEnabled = false
            };
            cancelButton.Click += (s, e) => activeRequestCts?.Cancel();

            clearButton = new Button
            {
                Content = "Clear",
                Height = 30,
                HorizontalAlignment = HorizontalAlignment.Stretch
            };
            clearButton.Click += (s, e) => transcriptTextBox.Clear();

            var actionPanel = new Grid();
            actionPanel.ColumnDefinitions.Add(new ColumnDefinition { Width = new GridLength(1, GridUnitType.Star) });
            actionPanel.ColumnDefinitions.Add(new ColumnDefinition { Width = new GridLength(8) });
            actionPanel.ColumnDefinitions.Add(new ColumnDefinition { Width = new GridLength(1, GridUnitType.Star) });
            actionPanel.ColumnDefinitions.Add(new ColumnDefinition { Width = new GridLength(8) });
            actionPanel.ColumnDefinitions.Add(new ColumnDefinition { Width = new GridLength(1, GridUnitType.Star) });
            Grid.SetColumn(sendButton, 0);
            Grid.SetColumn(cancelButton, 2);
            Grid.SetColumn(clearButton, 4);
            actionPanel.Children.Add(sendButton);
            actionPanel.Children.Add(cancelButton);
            actionPanel.Children.Add(clearButton);
            Grid.SetRow(actionPanel, 3);

            root.Children.Add(modelPanel);
            root.Children.Add(transcriptTextBox);
            root.Children.Add(inputTextBox);
            root.Children.Add(actionPanel);

            Content = root;
        }

        private Task QueueSendAsync()
        {
            return ThreadHelper.JoinableTaskFactory.RunAsync(() => SendCurrentInputAsync()).Task;
        }

        private Task SendCurrentInputAsync()
        {
            var prompt = inputTextBox.Text;
            if (string.IsNullOrWhiteSpace(prompt))
            {
                return Task.CompletedTask;
            }

            inputTextBox.Clear();
            AppendMessage("You", prompt);
            return SendToOllamaAsync(prompt);
        }

        private Task SendToOllamaAsync(string prompt)
        {
            SetBusyUi(isBusy: true);
            activeRequestCts = new CancellationTokenSource();

            AppendMessageHeader("Ollama");

            var streamTask = ollamaManager.StreamPromptAsync(
                    prompt,
                    chunk => Dispatcher.InvokeAsync(() => AppendResponseChunk(chunk)).Task,
                    activeRequestCts.Token);

            return streamTask.ContinueWith(t =>
            {
                Dispatcher.Invoke(() =>
                {
                    if (t.IsCanceled || IsCancellation(t.Exception))
                    {
                        AppendResponseChunk("\r\n[Cancelled]");
                    }
                    else if (t.Exception != null)
                    {
                        AppendResponseChunk("\r\n[Error] " + t.Exception.GetBaseException().Message);
                    }

                    EndResponseMessage();
                    if (activeRequestCts != null)
                    {
                        activeRequestCts.Dispose();
                        activeRequestCts = null;
                    }

                    SetBusyUi(isBusy: false);
                });
            }, TaskScheduler.Default);
        }

        private void ModelComboBox_SelectionChanged(object sender, SelectionChangedEventArgs e)
        {
            if (modelComboBox.SelectedItem is string selectedModel && !string.IsNullOrWhiteSpace(selectedModel))
            {
                ollamaManager.Model = selectedModel;
                AppendMessage("System", "Model set to " + selectedModel);
            }
        }

        private void AppendMessage(string author, string text)
        {
            transcriptTextBox.AppendText(author + ":\r\n" + text + "\r\n\r\n");
            transcriptTextBox.ScrollToEnd();
        }

        private void AppendMessageHeader(string author)
        {
            transcriptTextBox.AppendText(author + ":\r\n");
            transcriptTextBox.ScrollToEnd();
        }

        private void AppendResponseChunk(string chunk)
        {
            transcriptTextBox.AppendText(chunk);
            transcriptTextBox.ScrollToEnd();
        }

        private void EndResponseMessage()
        {
            transcriptTextBox.AppendText("\r\n\r\n");
            transcriptTextBox.ScrollToEnd();
        }

        private void SetBusyUi(bool isBusy)
        {
            sendButton.IsEnabled = !isBusy;
            cancelButton.IsEnabled = isBusy;
            clearButton.IsEnabled = !isBusy;
            modelComboBox.IsEnabled = !isBusy;
            inputTextBox.IsEnabled = !isBusy;
        }

        private void InputTextBox_KeyDown(object sender, KeyEventArgs e)
        {
            if (e.Key == Key.Enter && (Keyboard.Modifiers & ModifierKeys.Control) == ModifierKeys.Control)
            {
                e.Handled = true;
                _ = QueueSendAsync();
            }
        }

        private static bool IsCancellation(AggregateException exception)
        {
            if (exception == null)
            {
                return false;
            }

            var flattened = exception.Flatten();
            foreach (var inner in flattened.InnerExceptions)
            {
                if (inner is OperationCanceledException)
                {
                    return true;
                }
            }

            return false;
        }
    }
}
