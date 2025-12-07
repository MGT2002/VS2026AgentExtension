using Microsoft.VisualStudio.Shell;
using Microsoft.VisualStudio.Shell.Interop;
using System;
using System.ComponentModel.Design;
using System.Runtime.InteropServices;
using System.Threading;
using Task = System.Threading.Tasks.Task;

[PackageRegistration(UseManagedResourcesOnly = true, AllowsBackgroundLoading = true)]
[ProvideMenuResource("Menus.ctmenu", 1)]
[Guid("a1b2c3d4-1111-2222-3333-444455556666")]
public sealed class OllamaAgentPackage : AsyncPackage
{
    protected override async Task InitializeAsync(CancellationToken ct, IProgress<ServiceProgressData> _)
    {
        await JoinableTaskFactory.SwitchToMainThreadAsync(ct);
        
        var cmd = new OleMenuCommand((s, e) =>
            VsShellUtilities.ShowMessageBox(this, "Ollama Agent ready – VS 2026", "Ollama",
                OLEMSGICON.OLEMSGICON_INFO, OLEMSGBUTTON.OLEMSGBUTTON_OK, OLEMSGDEFBUTTON.OLEMSGDEFBUTTON_FIRST),
            new CommandID(new Guid("a1b2c3d4-1111-2222-3333-444455556666"), 0x0100));

        var menuCommandService = await GetServiceAsync(typeof(IMenuCommandService)) as IMenuCommandService;
        menuCommandService?.AddCommand(cmd);
    }
}