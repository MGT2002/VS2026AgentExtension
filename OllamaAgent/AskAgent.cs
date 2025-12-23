using Microsoft.VisualStudio.Shell;
using Microsoft.VisualStudio.Shell.Interop;
using Microsoft.VisualStudio.Threading;
using OllamaCommunicationService;
using System;
using System.ComponentModel.Design;
using System.Globalization;
using System.Threading;
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

        private OllamaManager ollamaManager;

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
            //var menuItem = new MenuCommand(this.Execute, menuCommandID);
            var menuItemAsync = new OleMenuCommand(async (sender, e) => await ExecuteAsync(), menuCommandID);
            commandService.AddCommand(menuItemAsync);
            this.ollamaManager = ollamaManager;
        }    

        /// <summary>
        /// Gets the instance of the command.
        /// </summary>
        public static AskAgent Instance
        {
            get;
            private set;
        }

        public static OllamaManager OllamaManager => Instance.ollamaManager;

        /// <summary>
        /// Gets the service provider from the owner package.
        /// </summary>
        private Microsoft.VisualStudio.Shell.IAsyncServiceProvider ServiceProvider
        {
            get
            {
                return this.package;
            }
        }

        /// <summary>
        /// Initializes the singleton instance of the command.
        /// </summary>
        /// <param name="package">Owner package, not null.</param>
        public static async Task InitializeAsync(AsyncPackage package)
        {
            // Switch to the main thread - the call to AddCommand in AskAgent's constructor requires
            // the UI thread.
            await ThreadHelper.JoinableTaskFactory.SwitchToMainThreadAsync(package.DisposalToken);

            OleMenuCommandService commandService = await package.GetServiceAsync(typeof(IMenuCommandService)) as OleMenuCommandService;
            Instance = new AskAgent(package, commandService, new OllamaManager());
        }

        /// <summary>
        /// This function is the callback used to execute the command when the menu item is clicked.
        /// See the constructor to see how the menu item is associated with this function using
        /// OleMenuCommandService service and MenuCommand class.
        /// </summary>
        /// <param name="sender">Event sender.</param>
        /// <param name="e">Event args.</param>
        private void Execute(object sender, EventArgs e)
        {
            ThreadHelper.ThrowIfNotOnUIThread();
            string title = "Ollama Agent";
            string message = "Agent is analyzing...\n";
            
            var task = ollamaManager.ExplainCodeAsync("int i = 12;");
            task.Wait();

            message += task.Result;

            // Show a message box to prove we were here
            VsShellUtilities.ShowMessageBox(
                this.package,
                message,
                title,
                OLEMSGICON.OLEMSGICON_INFO,
                OLEMSGBUTTON.OLEMSGBUTTON_OK,
                OLEMSGDEFBUTTON.OLEMSGDEFBUTTON_FIRST);
        }

        private async Task ExecuteAsync()
        {
            await ThreadHelper.JoinableTaskFactory.SwitchToMainThreadAsync();

            string title = "Ollama Agent";
            string message = "Agent is analyzing...\n";

            message += await ollamaManager.ExplainCodeAsync("class A { int a = 56; }");

            // Show a message box to prove we were here
            VsShellUtilities.ShowMessageBox(
                this.package,
                message,
                title,
                OLEMSGICON.OLEMSGICON_INFO,
                OLEMSGBUTTON.OLEMSGBUTTON_OK,
                OLEMSGDEFBUTTON.OLEMSGDEFBUTTON_FIRST);
        }
    }
}
