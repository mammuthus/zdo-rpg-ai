using ZdoRpgAi.Core;

namespace ZdoRpgAi.Server.App;

public class ServerApplication : IDisposable {
    private static readonly ILog Log = Logger.Get<ServerApplication>();

    public async Task RunAsync(CancellationToken ct) {
        Log.Info("Server started");

        try {
            await Task.Delay(Timeout.Infinite, ct);
        }
        catch (OperationCanceledException) {
            // Expected on shutdown
        }

        Log.Info("Server stopped");
    }

    public void Dispose() {
    }
}
