using Microsoft.VisualStudio.Shell;
using Microsoft.VisualStudio.Shell.Interop;
using Microsoft.VisualStudio.TextManager.Interop;
using OllamaCommunicationService;
using System;
using System.ComponentModel.Design;
using System.Threading.Tasks;
using Task = System.Threading.Tasks.Task;

namespace OllamaAgent
{
    /// <summary>
    /// Command handler
    /// </summary>
    internal sealed class AskAgent
    {
        /// <summary>
        /// Command ID.
        /// </summary>
        public const int CommandId = 0x0100;

        /// <summary>
        /// Command menu group (command set GUID).
        /// </summary>
        public static readonly Guid CommandSet = new Guid("3915f1f5-2f21-496a-a823-f802a49149cb");

        /// <summary>
        /// VS Package that provides this command, not null.
        /// </summary>
        private readonly AsyncPackage package;
        private readonly OllamaManager ollamaManager;

        /// <summary>
        /// Initializes a new instance of the <see cref="AskAgent"/> class.
        /// Adds our command handlers for menu (commands must exist in the command table file)
        /// </summary>
        /// <param name="package">Owner package, not null.</param>
        /// <param name="commandService">Command service to add command to, not null.</param>
        private AskAgent(
            AsyncPackage package,
            OleMenuCommandService commandService,
            OllamaManager ollamaManager = default)
        {
            this.package = package ?? throw new ArgumentNullException(nameof(package));
            commandService = commandService ?? throw new ArgumentNullException(nameof(commandService));

            var menuCommandID = new CommandID(CommandSet, CommandId);
            var menuItemAsync = new OleMenuCommand(async (sender, e) => await ExecuteAsync(), menuCommandID);
            commandService.AddCommand(menuItemAsync);

            this.ollamaManager = ollamaManager;
        }

        /// <summary>
        /// Gets the instance of the command.
        /// </summary>
        public static AskAgent Instance { get; private set; }

        public static OllamaManager OllamaManager => Instance.ollamaManager;

        /// <summary>
        /// Initializes the singleton instance of the command.
        /// </summary>
        /// <param name="package">Owner package, not null.</param>
        public static async Task InitializeAsync(AsyncPackage package)
        {
            await ThreadHelper.JoinableTaskFactory.SwitchToMainThreadAsync(package.DisposalToken);

            OleMenuCommandService commandService = await package.GetServiceAsync(typeof(IMenuCommandService)) as OleMenuCommandService;
            Instance = new AskAgent(package, commandService, new OllamaManager(model: OllamaManager.ModelSmart));
        }

        private async Task ExecuteAsync()
        {
            await ThreadHelper.JoinableTaskFactory.SwitchToMainThreadAsync();

            const string title = "Ollama Agent";
            string message = "Agent is analyzing...\n";

            string selectedCode = await GetSelectedCodeAsync();
            message += selectedCode + "\n\n";
            if (string.IsNullOrWhiteSpace(selectedCode))
            {
                message += "No code selected.";
            }
            else
            {
                message += await ollamaManager.ExplainCodeAsync(selectedCode);
            }

            VsShellUtilities.ShowMessageBox(
                package,
                message,
                title,
                OLEMSGICON.OLEMSGICON_INFO,
                OLEMSGBUTTON.OLEMSGBUTTON_OK,
                OLEMSGDEFBUTTON.OLEMSGDEFBUTTON_FIRST);
        }

        private async Task<string> GetSelectedCodeAsync()
        {
            await ThreadHelper.JoinableTaskFactory.SwitchToMainThreadAsync();

            var textManager = await package.GetServiceAsync(typeof(SVsTextManager)) as IVsTextManager;
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
    }
}
