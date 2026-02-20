using OllamaCommunicationService;
using System;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Input;

namespace OllamaAgent
{
    public class OllamaChatControl : UserControl
    {
        private readonly OllamaManager ollamaManager;
        private readonly TextBox transcriptTextBox;
        private readonly TextBox inputTextBox;
        private readonly Button sendButton;

        public OllamaChatControl(OllamaManager ollamaManager)
        {
            this.ollamaManager = ollamaManager ?? throw new ArgumentNullException(nameof(ollamaManager));

            var root = new Grid();
            root.RowDefinitions.Add(new RowDefinition { Height = new GridLength(1, GridUnitType.Star) });
            root.RowDefinitions.Add(new RowDefinition { Height = GridLength.Auto });
            root.RowDefinitions.Add(new RowDefinition { Height = GridLength.Auto });

            transcriptTextBox = new TextBox
            {
                IsReadOnly = true,
                AcceptsReturn = true,
                VerticalScrollBarVisibility = ScrollBarVisibility.Auto,
                HorizontalScrollBarVisibility = ScrollBarVisibility.Auto,
                TextWrapping = TextWrapping.Wrap
            };
            Grid.SetRow(transcriptTextBox, 0);

            inputTextBox = new TextBox
            {
                AcceptsReturn = true,
                Height = 90,
                VerticalScrollBarVisibility = ScrollBarVisibility.Auto,
                TextWrapping = TextWrapping.Wrap
            };
            inputTextBox.KeyDown += InputTextBox_KeyDown;
            Grid.SetRow(inputTextBox, 1);

            sendButton = new Button
            {
                Content = "Send",
                Height = 30,
                HorizontalAlignment = HorizontalAlignment.Stretch
            };
            sendButton.Click += async (s, e) => await SendCurrentInputAsync();
            Grid.SetRow(sendButton, 2);

            root.Children.Add(transcriptTextBox);
            root.Children.Add(inputTextBox);
            root.Children.Add(sendButton);

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
