using OllamaCommunicationService;
using Microsoft.VisualStudio.Shell;
using Microsoft.VisualStudio.TextManager.Interop;
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
        private readonly ComboBox qualityComboBox;
        private readonly TextBox transcriptTextBox;
        private readonly TextBox inputTextBox;
        private readonly Button sendButton;
        private readonly Button explainButton;
        private readonly Button cancelButton;
        private readonly Button clearButton;
        private CancellationTokenSource activeRequestCts;
        private readonly Style buttonStyle;

        public OllamaChatControl(OllamaManager ollamaManager)
        {
            this.ollamaManager = ollamaManager ?? throw new ArgumentNullException(nameof(ollamaManager));

            var root = new Grid
            {
                Background = new LinearGradientBrush(
                    Color.FromRgb(16, 24, 32),
                    Color.FromRgb(28, 44, 56),
                    90)
            };
            root.RowDefinitions.Add(new RowDefinition { Height = GridLength.Auto });
            root.RowDefinitions.Add(new RowDefinition { Height = new GridLength(1, GridUnitType.Star) });
            root.RowDefinitions.Add(new RowDefinition { Height = GridLength.Auto });
            root.RowDefinitions.Add(new RowDefinition { Height = GridLength.Auto });

            buttonStyle = new Style(typeof(Button));
            buttonStyle.Setters.Add(new Setter(BackgroundProperty, Brushes.Black));
            buttonStyle.Setters.Add(new Setter(ForegroundProperty, Brushes.White));
            buttonStyle.Setters.Add(new Setter(BorderBrushProperty, Brushes.Transparent));
            var disabledTrigger = new Trigger { Property = UIElement.IsEnabledProperty, Value = false };
            disabledTrigger.Setters.Add(new Setter(BackgroundProperty, new SolidColorBrush(Color.FromRgb(120, 120, 120))));
            disabledTrigger.Setters.Add(new Setter(ForegroundProperty, Brushes.White));
            buttonStyle.Triggers.Add(disabledTrigger);

            var modelPanel = new StackPanel
            {
                Orientation = Orientation.Vertical,
                Margin = new Thickness(0, 0, 0, 6)
            };

            var modelRow = new StackPanel
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
            modelRow.Children.Add(modelLabel);
            modelRow.Children.Add(modelComboBox);

            var qualityRow = new StackPanel
            {
                Orientation = Orientation.Horizontal
            };
            var qualityLabel = new TextBlock
            {
                Text = "Quality:",
                VerticalAlignment = VerticalAlignment.Center,
                Margin = new Thickness(0, 0, 6, 0),
                FontWeight = FontWeights.SemiBold,
                Foreground = Brushes.White
            };
            qualityComboBox = new ComboBox
            {
                Width = 140,
                ItemsSource = new List<string>
                {
                    nameof(ResponseQuality.VeryShort),
                    nameof(ResponseQuality.Brief),
                    nameof(ResponseQuality.Detailed)
                },
                SelectedIndex = 2
            };
            qualityRow.Children.Add(qualityLabel);
            qualityRow.Children.Add(qualityComboBox);
            modelPanel.Children.Add(modelRow);
            modelPanel.Children.Add(qualityRow);
            Grid.SetRow(modelPanel, 0);

            transcriptTextBox = new TextBox
            {
                IsReadOnly = true,
                AcceptsReturn = true,
                VerticalScrollBarVisibility = ScrollBarVisibility.Auto,
                HorizontalScrollBarVisibility = ScrollBarVisibility.Auto,
                TextWrapping = TextWrapping.Wrap,
                Background = new SolidColorBrush(Color.FromRgb(12, 18, 24)),
                Foreground = new SolidColorBrush(Color.FromRgb(226, 235, 241)),
                BorderBrush = new SolidColorBrush(Color.FromRgb(75, 96, 112)),
                BorderThickness = new Thickness(1),
                Padding = new Thickness(10)
            };
            Grid.SetRow(transcriptTextBox, 1);

            inputTextBox = new TextBox
            {
                AcceptsReturn = true,
                Height = 90,
                VerticalScrollBarVisibility = ScrollBarVisibility.Auto,
                TextWrapping = TextWrapping.Wrap,
                Background = new SolidColorBrush(Color.FromRgb(20, 30, 40)),
                Foreground = new SolidColorBrush(Color.FromRgb(239, 245, 249)),
                BorderBrush = new SolidColorBrush(Color.FromRgb(87, 113, 132)),
                BorderThickness = new Thickness(1),
                Padding = new Thickness(8)
            };
            inputTextBox.KeyDown += InputTextBox_KeyDown;
            Grid.SetRow(inputTextBox, 2);

            sendButton = new Button
            {
                Content = "Send",
                Height = 30,
                HorizontalAlignment = HorizontalAlignment.Stretch,
                Style = buttonStyle
            };
            sendButton.Click += (s, e) => _ = QueueSendAsync();

            explainButton = new Button
            {
                Content = "Explain Code",
                Height = 30,
                HorizontalAlignment = HorizontalAlignment.Stretch,
                Style = buttonStyle
            };
            explainButton.Click += (s, e) => _ = QueueExplainAsync();

            cancelButton = new Button
            {
                Content = "Cancel",
                Height = 30,
                HorizontalAlignment = HorizontalAlignment.Stretch,
                IsEnabled = false,
                Style = buttonStyle
            };
            cancelButton.Click += (s, e) => activeRequestCts?.Cancel();

            clearButton = new Button
            {
                Content = "Clear",
                Height = 30,
                HorizontalAlignment = HorizontalAlignment.Stretch,
                Style = buttonStyle
            };
            clearButton.Click += (s, e) => transcriptTextBox.Clear();

            var actionPanel = new Grid();
            actionPanel.ColumnDefinitions.Add(new ColumnDefinition { Width = new GridLength(1, GridUnitType.Star) });
            actionPanel.ColumnDefinitions.Add(new ColumnDefinition { Width = new GridLength(8) });
            actionPanel.ColumnDefinitions.Add(new ColumnDefinition { Width = new GridLength(1, GridUnitType.Star) });
            actionPanel.ColumnDefinitions.Add(new ColumnDefinition { Width = new GridLength(8) });
            actionPanel.ColumnDefinitions.Add(new ColumnDefinition { Width = new GridLength(1, GridUnitType.Star) });
            actionPanel.ColumnDefinitions.Add(new ColumnDefinition { Width = new GridLength(8) });
            actionPanel.ColumnDefinitions.Add(new ColumnDefinition { Width = new GridLength(1, GridUnitType.Star) });
            Grid.SetColumn(sendButton, 0);
            Grid.SetColumn(explainButton, 2);
            Grid.SetColumn(cancelButton, 4);
            Grid.SetColumn(clearButton, 6);
            actionPanel.Children.Add(sendButton);
            actionPanel.Children.Add(explainButton);
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

        private Task QueueExplainAsync()
        {
            return ThreadHelper.JoinableTaskFactory.RunAsync(() => ExplainCurrentInputAsync()).Task;
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
            return SendToOllamaAsync(prompt, GetSelectedResponseQuality());
        }

        private Task ExplainCurrentInputAsync()
        {
            string code = null;
            Dispatcher.Invoke(() => { code = GetSelectedCodeFromEditor(); });
            if (string.IsNullOrWhiteSpace(code))
            {
                AppendMessage("System", "No code selected.");
                return Task.CompletedTask;
            }

            AppendMessage("You", code);
            return ExplainCodeStreamAsync(code, GetSelectedResponseQuality());
        }

        private Task SendToOllamaAsync(string prompt, ResponseQuality responseQuality)
        {
            SetBusyUi(isBusy: true);
            activeRequestCts = new CancellationTokenSource();

            AppendMessageHeader("Ollama");

            var streamTask = ollamaManager.StreamPromptAsync(
                    prompt,
                    chunk => Dispatcher.InvokeAsync(() => AppendResponseChunk(chunk)).Task,
                    responseQuality,
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

        private Task ExplainCodeStreamAsync(string code, ResponseQuality responseQuality)
        {
            SetBusyUi(isBusy: true);
            activeRequestCts = new CancellationTokenSource();
            AppendMessageHeader("Ollama");

            var streamTask = ollamaManager.StreamExplainCodeAsync(
                code,
                chunk => Dispatcher.InvokeAsync(() => AppendResponseChunk(chunk)).Task,
                responseQuality,
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

        private ResponseQuality GetSelectedResponseQuality()
        {
            var selected = qualityComboBox.SelectedItem as string;
            switch (selected)
            {
                case nameof(ResponseQuality.VeryShort):
                    return ResponseQuality.VeryShort;
                case nameof(ResponseQuality.Brief):
                    return ResponseQuality.Brief;
                case nameof(ResponseQuality.Detailed):
                    return ResponseQuality.Detailed;
                default:
                    return ResponseQuality.Detailed;
            }
        }

        private string GetSelectedCodeFromEditor()
        {
            ThreadHelper.ThrowIfNotOnUIThread();

            var textManager = Package.GetGlobalService(typeof(SVsTextManager)) as IVsTextManager;
            if (textManager == null)
            {
                return null;
            }

            if (textManager.GetActiveView(1, null, out IVsTextView activeView) != 0 || activeView == null)
            {
                return null;
            }

            if (activeView.GetSelection(out int startLine, out int startColumn, out int endLine, out int endColumn) != 0)
            {
                return null;
            }

            if (startLine > endLine)
            {
                var t = startLine;
                startLine = endLine;
                endLine = t;
                t = startColumn;
                startColumn = endColumn;
                endColumn = t;
            }

            if (startLine == endLine && startColumn == endColumn)
            {
                return null;
            }

            if (activeView.GetBuffer(out IVsTextLines lines) != 0 || lines == null)
            {
                return null;
            }

            if (lines.GetLineText(startLine, startColumn, endLine, endColumn, out string selectedText) != 0)
            {
                return null;
            }

            return selectedText;
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
            qualityComboBox.IsEnabled = !isBusy;
            inputTextBox.IsEnabled = !isBusy;
            explainButton.IsEnabled = !isBusy;
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
