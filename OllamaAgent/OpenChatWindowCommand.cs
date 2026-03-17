using Microsoft.VisualStudio.Shell;
using System;
using System.ComponentModel.Design;
using System.Threading.Tasks;
using Task = System.Threading.Tasks.Task;

namespace OllamaAgent
{
    internal sealed class OpenChatWindowCommand
    {
        public const int CommandId = 0x0101;
        public static readonly Guid CommandSet = new Guid("3915f1f5-2f21-496a-a823-f802a49149cb");

        private readonly AsyncPackage package;

        private OpenChatWindowCommand(AsyncPackage package, OleMenuCommandService commandService)
        {
            this.package = package ?? throw new ArgumentNullException(nameof(package));
            if (commandService == null) throw new ArgumentNullException(nameof(commandService));

            var menuCommandId = new CommandID(CommandSet, CommandId);
            var menuItem = new OleMenuCommand(async (s, e) => await ExecuteAsync(), menuCommandId);
            commandService.AddCommand(menuItem);
        }

        public static OpenChatWindowCommand Instance { get; private set; }

        public static async Task InitializeAsync(AsyncPackage package)
        {
            await ThreadHelper.JoinableTaskFactory.SwitchToMainThreadAsync(package.DisposalToken);
            var commandService = await package.GetServiceAsync(typeof(IMenuCommandService)) as OleMenuCommandService;
            Instance = new OpenChatWindowCommand(package, commandService);
        }

        private async Task ExecuteAsync()
        {
            await ThreadHelper.JoinableTaskFactory.SwitchToMainThreadAsync();

            var window = await package.ShowToolWindowAsync(typeof(OllamaChatToolWindow), 0, true, package.DisposalToken) as OllamaChatToolWindow;
            if (window?.Frame == null)
            {
                throw new InvalidOperationException("Cannot create Ollama Chat tool window.");
            }
        }
    }
}
