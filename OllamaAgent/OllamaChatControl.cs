using OllamaCommunicationService;
using System;
using System.Collections.Generic;
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
        private readonly Button clearButton;

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
            sendButton.Click += async (s, e) => await SendCurrentInputAsync();

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
            Grid.SetColumn(sendButton, 0);
            Grid.SetColumn(clearButton, 2);
            actionPanel.Children.Add(sendButton);
            actionPanel.Children.Add(clearButton);
            Grid.SetRow(actionPanel, 3);

            root.Children.Add(modelPanel);
            root.Children.Add(transcriptTextBox);
            root.Children.Add(inputTextBox);
            root.Children.Add(actionPanel);

            Content = root;
        }

        private async Task SendCurrentInputAsync()
        {
            var prompt = inputTextBox.Text;
            if (string.IsNullOrWhiteSpace(prompt))
            {
                return;
            }

            inputTextBox.Clear();
            AppendMessage("You", prompt);
            await SendToOllamaAsync(prompt);
        }

        private async Task SendToOllamaAsync(string prompt)
        {
            sendButton.IsEnabled = false;
            clearButton.IsEnabled = false;

            try
            {
                var response = await ollamaManager.ExplainCodeAsync(prompt, ResponseQuality.Detailed);
                AppendMessage("Ollama", response);
            }
            catch (Exception ex)
            {
                AppendMessage("Error", ex.Message);
            }
            finally
            {
                sendButton.IsEnabled = true;
                clearButton.IsEnabled = true;
            }
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

        private async void InputTextBox_KeyDown(object sender, KeyEventArgs e)
        {
            if (e.Key == Key.Enter && (Keyboard.Modifiers & ModifierKeys.Control) == ModifierKeys.Control)
            {
                e.Handled = true;
                await SendCurrentInputAsync();
            }
        }
    }
}
