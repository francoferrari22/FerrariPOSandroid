using System.Diagnostics;
using System.Net.NetworkInformation;
using System.Net.Sockets;

namespace FerrarisPOS.Services;

/// <summary>
/// Recuperación de red no interactiva para FerrariPOS.
/// Primero prueba la resolución normal. Si falla, intenta vaciar caché y,
/// solo cuando la aplicación está elevada, repara DNS de adaptadores activos.
/// El instalador ya realiza esta operación con privilegios administrativos.
/// </summary>
internal static class NetworkRecovery
{
    public static async Task TryRecoverAsync(CancellationToken token)
    {
        try
        {
            if (await ResolvesCloudflareAsync(token))
                return;

            await RunAsync("ipconfig.exe", "/flushdns", token);

            if (await ResolvesCloudflareAsync(token))
                return;

            // En instalaciones donde el Setup ya dejó DNS correcto, esto no hace nada.
            // Si FerrariPOS fue copiado manualmente y está elevado, intenta reparar.
            if (IsElevated())
                await RepairActiveAdaptersAsync(token);
        }
        catch { }
    }

    private static async Task<bool> ResolvesCloudflareAsync(CancellationToken token)
    {
        try
        {
            using var client = new HttpClient { Timeout = TimeSpan.FromSeconds(4) };
            using var request = new HttpRequestMessage(HttpMethod.Head, "https://api.trycloudflare.com/");
            using var response = await client.SendAsync(request, HttpCompletionOption.ResponseHeadersRead, token);
            return response.StatusCode != System.Net.HttpStatusCode.ServiceUnavailable;
        }
        catch
        {
            return false;
        }
    }

    private static async Task RepairActiveAdaptersAsync(CancellationToken token)
    {
        foreach (var ni in NetworkInterface.GetAllNetworkInterfaces())
        {
            if (token.IsCancellationRequested) return;
            if (ni.OperationalStatus != OperationalStatus.Up ||
                ni.NetworkInterfaceType == NetworkInterfaceType.Loopback)
                continue;

            try
            {
                var props = ni.GetIPProperties();
                if (!props.GatewayAddresses.Any(g => g.Address.AddressFamily == AddressFamily.InterNetwork))
                    continue;

                var args = $"-Command \"Set-DnsClientServerAddress -InterfaceIndex {ni.GetIPProperties().GetIPv4Properties()?.Index ?? -1} -ServerAddresses @('1.1.1.1','8.8.8.8','2606:4700:4700::1111','2001:4860:4860::8888'); ipconfig /flushdns\"";
                await RunAsync("powershell.exe", args, token);
            }
            catch { }
        }
    }

    private static bool IsElevated()
    {
        try
        {
            using var identity = System.Security.Principal.WindowsIdentity.GetCurrent();
            return new System.Security.Principal.WindowsPrincipal(identity)
                .IsInRole(System.Security.Principal.WindowsBuiltInRole.Administrator);
        }
        catch { return false; }
    }

    private static async Task RunAsync(string file, string args, CancellationToken token)
    {
        using var p = new Process
        {
            StartInfo = new ProcessStartInfo
            {
                FileName = file,
                Arguments = args,
                UseShellExecute = false,
                CreateNoWindow = true,
                RedirectStandardOutput = true,
                RedirectStandardError = true
            }
        };
        p.Start();
        await p.WaitForExitAsync(token);
    }
}
