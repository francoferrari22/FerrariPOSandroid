using FerrarisPOS.Data;
using FerrarisPOS.Forms;
using FerrarisPOS.Services;

namespace FerrarisPOS;

internal static class Program
{
    [STAThread]
    private static void Main()
    {
        ApplicationConfiguration.Initialize();

        // FerrariPOS V73.1.35 se entrega únicamente para Windows 10/11,
        // donde está disponible el modo oscuro nativo de DWM utilizado por
        // Ferrari Dark Chrome. No se toca la lógica de negocio ni servicios.
        if (!OperatingSystem.IsWindowsVersionAtLeast(10, 0))
        {
            MessageBox.Show(
                "FerrariPOS V73.1.35 requiere Windows 10 u 11.",
                "FerrariPOS · Sistema no compatible",
                MessageBoxButtons.OK, MessageBoxIcon.Warning);
            return;
        }

        ThemeService.EnableGlobalWindows10Theme();
        Application.SetUnhandledExceptionMode(UnhandledExceptionMode.CatchException);
        Application.ThreadException += (_, e) => ShowFatalError("Error de interfaz", e.Exception);
        AppDomain.CurrentDomain.UnhandledException += (_, e) =>
        {
            if (e.ExceptionObject is Exception ex)
                ShowFatalError("Error no controlado", ex);
        };
        try
        {
            Database.Initialize();
            try { CustomerBackupService.Sync(); } catch { }
            using var webDashboard = new WebDashboardServer();
            try
            {
                webDashboard.Start();
            }
            catch (Exception dashboardEx)
            {
                // El panel web es un servicio auxiliar: si Windows tiene todos los
                // puertos candidatos ocupados, FerrariPOS debe seguir iniciando
                // normalmente y nunca mostrar un error de arranque por este motivo.
                try
                {
                    var path = Path.Combine(AppContext.BaseDirectory, "FerrarisPOS_error.log");
                    File.AppendAllText(path, $"[{DateTime.Now:yyyy-MM-dd HH:mm:ss}] AVISO PANEL WEB: {dashboardEx}\r\n\r\n");
                }
                catch { }
            }
            LanguageService.InitializeFromInstaller();
            LicenseService.Initialize();
            LicenseService.CheckOnlineStatus();
            LicenseService.StartOnlineMonitor();

            // Escape universal: una pulsación cierra la ventana activa actual.
            // La ventana principal de ventas nunca se cierra con Escape.
            Application.AddMessageFilter(new EscapeCloseMessageFilter());

            // Ciclo de sesión: si la licencia vence mientras el POS está abierto,
            // se cierra la venta y se vuelve al ingreso, que queda bloqueado hasta
            // que el administrador active/renueve la licencia.
            while (true)
            {
                using (var login = new LoginForm())
                {
                    if (login.ShowDialog() != DialogResult.OK)
                        return;
                }

                if (!LicenseService.CanRun())
                    continue;

                using var main = new MainForm();
                Application.Run(main);

                if (!LicenseService.IsExpired)
                    return;
            }
        }
        catch (Exception ex)
        {
            try
            {
                var path = Path.Combine(AppContext.BaseDirectory, "FerrarisPOS_error.log");
                File.AppendAllText(path, $"[{DateTime.Now:yyyy-MM-dd HH:mm:ss}] ERROR DE ARRANQUE\r\n{ex}\r\n\r\n");
            }
            catch { }

            MessageBox.Show(
                $"No se pudo iniciar FerrarisPOS.\n\n{UserFriendlyError(ex)}\n\nEl detalle técnico se guardó en FerrarisPOS_error.log.",
                "FerrarisPOS · Error de arranque",
                MessageBoxButtons.OK, MessageBoxIcon.Error);
        }
        finally
        {
            Session.Clear();
        }
    }

    private sealed class EscapeCloseMessageFilter : IMessageFilter
    {
        private const int WM_KEYDOWN = 0x0100;
        private const int WM_SYSKEYDOWN = 0x0104;
        private const int VK_ESCAPE = 0x1B;

        public bool PreFilterMessage(ref Message m)
        {
            if ((m.Msg != WM_KEYDOWN && m.Msg != WM_SYSKEYDOWN) || m.WParam.ToInt32() != VK_ESCAPE)
                return false;

            // Resolver el formulario real que contiene el control con foco. Esto es
            // más fiable que depender solamente de Form.ActiveForm cuando hay
            // formularios modales/embebidos.
            Form? active = Form.ActiveForm;
            var focused = Control.FromHandle(GetFocus());
            if (focused != null)
                active = focused.FindForm() ?? active;

            // Un formulario embebido (TopLevel=false), como el salón dentro de la
            // venta principal, no debe destruirse. Subimos hasta su formulario raíz.
            while (active != null && !active.TopLevel && active.Parent != null)
                active = active.Parent.FindForm() ?? active.Parent.TopLevelControl as Form;

            if (active == null || active is MainForm || active.IsDisposed || active.Disposing)
                return false;

            // Consume Escape para impedir que el control activo ejecute además su
            // propio comportamiento. Cada pulsación retrocede una ventana.
            try
            {
                if (active.Modal)
                    active.DialogResult = DialogResult.Cancel;
                else
                    active.Close();
            }
            catch (InvalidOperationException)
            {
                try { active.Close(); } catch { }
            }

            return true;
        }

        [System.Runtime.InteropServices.DllImport("user32.dll")]
        private static extern IntPtr GetFocus();
    }

    private static void ShowFatalError(string title, Exception ex)
    {
        try
        {
            var path = Path.Combine(AppContext.BaseDirectory, "FerrarisPOS_error.log");
            File.AppendAllText(path, $"[{DateTime.Now:yyyy-MM-dd HH:mm:ss}] {title}\r\n{ex}\r\n\r\n");
        }
        catch { }

        MessageBox.Show(
            $"No se pudo continuar FerrarisPOS.\n\n{UserFriendlyError(ex)}\n\nSe guardó el detalle técnico en FerrarisPOS_error.log.",
            "FerrarisPOS · Error", MessageBoxButtons.OK, MessageBoxIcon.Warning);
    }

    private static string UserFriendlyError(Exception ex)
    {
        var text = ex.ToString();
        if (text.Contains("UNIQUE constraint failed: products.barcode", StringComparison.OrdinalIgnoreCase) ||
            (ex is Microsoft.Data.Sqlite.SqliteException sqlite && sqlite.SqliteErrorCode == 19))
        {
            return "El código de barras ya está utilizado por otro registro.\n\nNo se perdió la información. Revisá el código e intentá nuevamente.";
        }

        return ex.Message;
    }
}
