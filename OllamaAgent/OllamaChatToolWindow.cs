using Microsoft.VisualStudio.Shell;
using OllamaCommunicationService;
using System;
using System.Runtime.InteropServices;

namespace OllamaAgent
{
    [Guid("d2c5c77a-4957-4f75-a8d2-f3605002b5de")]
    public class OllamaChatToolWindow : ToolWindowPane
    {
        public OllamaChatToolWindow() : base(null)
        {
            Caption = "Ollama Chat";
            Content = new OllamaChatControl(new OllamaManager(model: OllamaManager.ModelSmart));
        }
    }
}
