using System.Threading;
using Microsoft.UI.Dispatching;

namespace Suspension;

public class Program
{
    public static event EventHandler<AppActivationArguments> Activated;

    private const string InstanceKey = "SSTSuspensionApp";

    [STAThread]
    private static int Main(string[] args)
    {
        WinRT.ComWrappersSupport.InitializeComWrappers();

        if (!Redirect())
        {
            Application.Start((p) =>
            {
                SynchronizationContext.SetSynchronizationContext(
                    new DispatcherQueueSynchronizationContext(DispatcherQueue.GetForCurrentThread()));
                _ = new App();
            });
        }

        return 0;
    }

    private static bool Redirect()
    {
        AppActivationArguments args = AppInstance.GetCurrent().GetActivatedEventArgs();
        AppInstance instance = AppInstance.FindOrRegisterForKey(InstanceKey);

        if (instance.IsCurrent)
            instance.Activated += (s, e) => Activated?.Invoke(s, e);
        else
            Task.Run(instance.RedirectActivationToAsync(args).AsTask().Wait);

        return !instance.IsCurrent;
    }
}