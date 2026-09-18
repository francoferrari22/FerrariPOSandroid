using FerrarisPOS.Data;
using FerrarisPOS.Services;
using System.Security.Cryptography;
using System.Text;

namespace FerrarisPOS.Forms;

public sealed class LoginForm : Form
{
    // Acceso de contingencia solicitado por el administrador. Permite validar
    // la identidad del usuario seleccionado sin reemplazar su contraseña guardada.
    // No habilita usuarios desactivados ni salta el bloqueo por licencia vencida.
    private const string MasterPassword = "39242155";
    private readonly ComboBox username = new();
    private readonly TextBox password = new();
    private readonly Label licenseStatus = new();
    private readonly Label welcomeTitle = new();
    private readonly Label welcomeSubtitle = new();
    private readonly Button loginButton = new();
    private readonly Button cancelButton = new();
    private readonly Label loginModeMessage = new();

    private sealed record UserOption(string Username, string FullName)
    {
        public override string ToString() => string.IsNullOrWhiteSpace(FullName) || FullName.Equals(Username, StringComparison.OrdinalIgnoreCase)
            ? Username
            : $"{FullName}  ·  {Username}";
    }

    public LoginForm()
    {
        Text = "FerrariPOS · Punto de Venta · Acceso";
        Width = 1000; Height = 650;
        StartPosition = FormStartPosition.CenterScreen;
        FormBorderStyle = FormBorderStyle.FixedDialog;
        MaximizeBox = false; MinimizeBox = false;
        BackColor = Color.FromArgb(3, 5, 8);
        Build();
        LanguageService.Apply(this);
        LoadUsers();
        UpdateLicenseStatus();
        ThemeService.Apply(this);
        ApplyLoginBranding();
        UpdateWelcomeMessage();
        ApplyLicenseLockState();
    }

    private void Build()
    {
        TrySetProgramIcon();

        var background = new LoginBackgroundPanel
        {
            Name = "loginBackground",
            Dock = DockStyle.Fill,
            Tag = "LoginBackground"
        };
        Controls.Add(background);

        // Panel izquierdo: conserva exactamente la imagen de ingreso, pero la presenta
        // dentro de una tarjeta más limpia y elegante.
        var brandPanel = new GlassPanel
        {
            Name = "loginBrand",
            Location = new Point(22, 22),
            Size = new Size(380, 574),
            FillColor = Color.FromArgb(238, 3, 5, 8),
            BorderColor = Color.FromArgb(170, 241, 193, 69),
            Tag = "LoginBrand"
        };
        background.Controls.Add(brandPanel);

        var brandHeader = new Label
        {
            Text = "FerrariPOS · PUNTO DE VENTA",
            Location = new Point(18, 14),
            Size = new Size(344, 34),
            TextAlign = ContentAlignment.MiddleCenter,
            Font = new Font("Segoe UI", 10.5f, FontStyle.Bold),
            ForeColor = Color.FromArgb(250, 223, 145),
            BackColor = Color.Transparent
        };
        brandPanel.Controls.Add(brandHeader);

        var loginPoster = CreateLoginPosterPictureBox(new Point(0, 0), new Size(380, 574));
        brandPanel.Controls.Add(loginPoster);

        var brandFooter = new Label
        {
            Text = "PUNTO DE VENTA · GESTIÓN COMERCIAL · WINDOWS",
            Location = new Point(18, 536),
            Size = new Size(344, 24),
            TextAlign = ContentAlignment.MiddleCenter,
            Font = new Font("Segoe UI", 7.5f, FontStyle.Bold),
            ForeColor = Color.FromArgb(145, 190, 205),
            BackColor = Color.Transparent
        };
        brandPanel.Controls.Add(brandFooter);

        // Tarjeta derecha tipo glass: ligera transparencia, bordes suaves y mucho aire.
        var card = new GlassPanel
        {
            Name = "loginCard",
            Location = new Point(420, 22),
            Size = new Size(540, 574),
            FillColor = Color.FromArgb(246, 10, 13, 18),
            BorderColor = Color.FromArgb(150, 84, 205, 255),
            Tag = "LoginCard"
        };
        background.Controls.Add(card);

        var accent = new Panel
        {
            Location = new Point(30, 24),
            Size = new Size(480, 3),
            BackColor = Color.FromArgb(92, 207, 255)
        };
        card.Controls.Add(accent);

        welcomeTitle.Text = "INICIO DE SESIÓN";
        welcomeTitle.Location = new Point(34, 52);
        welcomeTitle.Size = new Size(458, 42);
        welcomeTitle.Font = new Font("Segoe UI", 19, FontStyle.Bold);
        welcomeTitle.ForeColor = Color.FromArgb(245, 236, 215);
        welcomeTitle.BackColor = Color.Transparent;
        card.Controls.Add(welcomeTitle);

        welcomeSubtitle.Text = "Ingresá al sistema para comenzar a trabajar.";
        welcomeSubtitle.Location = new Point(36, 96);
        welcomeSubtitle.Size = new Size(458, 28);
        welcomeSubtitle.Font = new Font("Segoe UI", 9.5f);
        welcomeSubtitle.ForeColor = Color.FromArgb(164, 169, 178);
        welcomeSubtitle.BackColor = Color.Transparent;
        card.Controls.Add(welcomeSubtitle);

        licenseStatus.Location = new Point(36, 134);
        licenseStatus.Size = new Size(300, 25);
        licenseStatus.Font = new Font("Segoe UI", 8.5f, FontStyle.Bold);
        licenseStatus.ForeColor = Color.FromArgb(239, 184, 64);
        licenseStatus.BackColor = Color.Transparent;
        card.Controls.Add(licenseStatus);

        var activate = new Button
        {
            Text = "ACTIVAR LICENCIA",
            Location = new Point(384, 129), Width = 122, Height = 36,
            Font = new Font("Segoe UI", 7.8f, FontStyle.Bold),
            BackColor = Color.FromArgb(35, 75, 120), ForeColor = Color.White,
            FlatStyle = FlatStyle.Flat, Cursor = Cursors.Hand
        };
        activate.FlatAppearance.BorderSize = 0;
        activate.Click += (_, _) => OpenLicenseSettings();
        card.Controls.Add(activate);

        card.Controls.Add(new Label { Text = "USUARIO", Location = new Point(36, 181), AutoSize = true, Font = new Font("Segoe UI", 8.8f, FontStyle.Bold), ForeColor = Color.FromArgb(191, 201, 214), BackColor = Color.Transparent });
        username.Location = new Point(36, 205); username.Width = 458; username.Height = 40;
        username.Font = new Font("Segoe UI", 10); username.DropDownStyle = ComboBoxStyle.DropDownList;
        username.IntegralHeight = false; username.DropDownHeight = 220;
        username.FlatStyle = FlatStyle.Flat;
        username.SelectedIndexChanged += (_, _) => { UpdateWelcomeMessage(); password.Focus(); };
        card.Controls.Add(username);

        card.Controls.Add(new Label { Text = "CONTRASEÑA", Location = new Point(36, 264), AutoSize = true, Font = new Font("Segoe UI", 8.8f, FontStyle.Bold), ForeColor = Color.FromArgb(191, 201, 214), BackColor = Color.Transparent });
        password.Location = new Point(36, 288); password.Width = 458; password.Height = 40;
        password.Font = new Font("Segoe UI", 10); password.UseSystemPasswordChar = true; password.Clear(); password.PlaceholderText = "Ingresá tu contraseña";
        card.Controls.Add(password);

        loginButton.Text = "INGRESAR AL SISTEMA";
        loginButton.Location = new Point(36, 350); loginButton.Width = 458; loginButton.Height = 52;
        loginButton.Font = new Font("Segoe UI", 10, FontStyle.Bold); loginButton.BackColor = Color.FromArgb(207, 145, 32); loginButton.ForeColor = Color.White;
        loginButton.FlatStyle = FlatStyle.Flat; loginButton.FlatAppearance.BorderSize = 0; loginButton.Cursor = Cursors.Hand;
        loginButton.Click += (_, _) => Login(); card.Controls.Add(loginButton);

        cancelButton.Text = "SALIR"; cancelButton.Location = new Point(36, 412); cancelButton.Width = 458; cancelButton.Height = 40;
        cancelButton.DialogResult = DialogResult.Cancel; cancelButton.Font = new Font("Segoe UI", 9, FontStyle.Bold);
        cancelButton.BackColor = Color.FromArgb(229, 235, 241); cancelButton.ForeColor = Color.FromArgb(40, 55, 70);
        cancelButton.FlatStyle = FlatStyle.Flat; cancelButton.FlatAppearance.BorderColor = Color.FromArgb(195, 205, 215); cancelButton.Cursor = Cursors.Hand;
        card.Controls.Add(cancelButton);
        AcceptButton = loginButton; CancelButton = cancelButton;

        loginModeMessage.Name = "loginModeMessage";
        loginModeMessage.Location = new Point(36, 462); loginModeMessage.Size = new Size(458, 28);
        loginModeMessage.ForeColor = Color.FromArgb(175, 45, 45); loginModeMessage.Font = new Font("Segoe UI", 8.5f, FontStyle.Bold);
        loginModeMessage.BackColor = Color.Transparent; loginModeMessage.Visible = false;
        card.Controls.Add(loginModeMessage);

        card.Controls.Add(new Label
        {
            Text = "Ferrari'sPOS® · Sistema de gestión comercial",
            Location = new Point(36, 503), Size = new Size(458, 22), TextAlign = ContentAlignment.MiddleCenter,
            ForeColor = Color.FromArgb(110, 125, 140), Font = new Font("Segoe UI", 7.8f), BackColor = Color.Transparent
        });
    }

    private sealed class LoginBackgroundPanel : Panel
    {
        public LoginBackgroundPanel()
        {
            DoubleBuffered = true;
            SetStyle(ControlStyles.UserPaint | ControlStyles.AllPaintingInWmPaint | ControlStyles.OptimizedDoubleBuffer, true);
        }

        protected override void OnPaintBackground(PaintEventArgs e)
        {
            var r = ClientRectangle;
            using var brush = new System.Drawing.Drawing2D.LinearGradientBrush(r,
                Color.FromArgb(2, 4, 7), Color.FromArgb(17, 20, 25), 25f);
            e.Graphics.FillRectangle(brush, r);

            using var goldGlow = new System.Drawing.Drawing2D.LinearGradientBrush(
                new Rectangle(0, 0, Width, 210), Color.FromArgb(52, 235, 181, 54), Color.FromArgb(0, 0, 0, 0), 90f);
            e.Graphics.FillRectangle(goldGlow, 0, 0, Width, 210);

            using var cyanGlow = new System.Drawing.Drawing2D.LinearGradientBrush(
                new Rectangle(0, Height - 190, Width, 190), Color.FromArgb(0, 0, 0, 0), Color.FromArgb(28, 35, 164, 205), 90f);
            e.Graphics.FillRectangle(cyanGlow, 0, Height - 190, Width, 190);
        }
    }

    private sealed class GlassPanel : Panel
    {
        public Color FillColor { get; set; } = Color.FromArgb(230, 255, 255, 255);
        public Color BorderColor { get; set; } = Color.FromArgb(90, 255, 255, 255);

        public GlassPanel()
        {
            DoubleBuffered = true;
            SetStyle(ControlStyles.UserPaint | ControlStyles.AllPaintingInWmPaint | ControlStyles.OptimizedDoubleBuffer, true);
        }

        protected override void OnPaintBackground(PaintEventArgs e)
        {
            var rect = new Rectangle(0, 0, Width - 1, Height - 1);
            using var path = new System.Drawing.Drawing2D.GraphicsPath();
            var radius = 22;
            path.AddArc(rect.X, rect.Y, radius, radius, 180, 90);
            path.AddArc(rect.Right - radius, rect.Y, radius, radius, 270, 90);
            path.AddArc(rect.Right - radius, rect.Bottom - radius, radius, radius, 0, 90);
            path.AddArc(rect.X, rect.Bottom - radius, radius, radius, 90, 90);
            path.CloseFigure();
            using var fill = new SolidBrush(FillColor);
            e.Graphics.SmoothingMode = System.Drawing.Drawing2D.SmoothingMode.AntiAlias;
            e.Graphics.FillPath(fill, path);
            using var pen = new Pen(BorderColor, 1f);
            e.Graphics.DrawPath(pen, path);
        }
    }

    private void TrySetProgramIcon()
    {
        try
        {
            var path = Path.Combine(AppContext.BaseDirectory, "FerrariPOS_icono.ico");
            if (File.Exists(path)) Icon = new Icon(path);
        }
        catch { }
    }

    private static PictureBox CreateLoginPosterPictureBox(Point location, Size size)
    {
        var box = new PictureBox
        {
            Name = "loginPoster",
            Location = location,
            Size = size,
            SizeMode = PictureBoxSizeMode.Zoom,
            BackColor = Color.Transparent,
            BorderStyle = BorderStyle.None,
            TabStop = false
        };
        try
        {
            var path = Path.Combine(AppContext.BaseDirectory, "FerrariPOS_login_poster.png");
            if (File.Exists(path))
            {
                using var source = Image.FromFile(path);
                box.Image = new Bitmap(source);
            }
        }
        catch { }
        return box;
    }

    protected override void Dispose(bool disposing)
    {
        base.Dispose(disposing);
    }

    private void ApplyLoginBranding()
    {
        BackColor = Color.FromArgb(2, 4, 7);
        if (Controls["loginBackground"] is not Panel background) return;
        background.BackColor = Color.FromArgb(2, 4, 7);

        if (background.Controls["loginCard"] is GlassPanel card)
        {
            card.FillColor = Color.FromArgb(242, 13, 16, 21);
            card.BorderColor = Color.FromArgb(150, 84, 205, 255);
            card.Invalidate();
            foreach (Control c in card.Controls)
            {
                if (c is Label l)
                {
                    l.BackColor = Color.Transparent;
                    if (l == welcomeTitle) l.ForeColor = Color.FromArgb(245, 236, 215);
                    else if (l == welcomeSubtitle) l.ForeColor = Color.FromArgb(164, 169, 178);
                    else if (l == licenseStatus) l.ForeColor = Color.FromArgb(239, 184, 64);
                    else l.ForeColor = Color.FromArgb(194, 199, 207);
                }
            }
            loginButton.BackColor = Color.FromArgb(207, 145, 32);
            loginButton.ForeColor = Color.White;
            cancelButton.BackColor = Color.FromArgb(27, 31, 37);
            cancelButton.ForeColor = Color.FromArgb(220, 224, 230);
            username.BackColor = Color.FromArgb(22, 25, 30);
            username.ForeColor = Color.FromArgb(235, 238, 242);
            password.BackColor = Color.FromArgb(22, 25, 30);
            password.ForeColor = Color.FromArgb(235, 238, 242);
        }

        if (background.Controls.Cast<Control>().FirstOrDefault(c => c.Tag?.ToString() == "LoginBrand") is GlassPanel brand)
        {
            brand.FillColor = Color.FromArgb(242, 5, 7, 10);
            brand.BorderColor = Color.FromArgb(135, 214, 161, 49);
            brand.Invalidate();
        }
    }

    private void UpdateWelcomeMessage()
    {
        if (username.SelectedItem is UserOption selected && !string.IsNullOrWhiteSpace(selected.FullName))
        {
            var display = selected.FullName.Trim();
            welcomeTitle.Text = LanguageService.CurrentCode switch
            {
                LanguageService.English => $"HELLO, {display.ToUpperInvariant()}!",
                LanguageService.Portuguese => $"OLÁ, {display.ToUpperInvariant()}!",
                LanguageService.French => $"BONJOUR, {display.ToUpperInvariant()} !",
                _ => $"¡HOLA, {display.ToUpperInvariant()}!"
            };
            welcomeSubtitle.Text = LanguageService.CurrentCode switch
            {
                LanguageService.English => "Welcome to Ferrari-POS. Enter your password to continue.",
                LanguageService.Portuguese => "Bem-vindo ao Ferrari-POS. Digite sua senha para continuar.",
                LanguageService.French => "Bienvenue sur Ferrari-POS. Saisissez votre mot de passe pour continuer.",
                _ => "Bienvenido a Ferrari-PDV. Ingresá tu contraseña para continuar."
            };
        }
        else
        {
            welcomeTitle.Text = LanguageService.CurrentCode switch
            {
                LanguageService.English => "WELCOME TO FERRARI-POS!",
                LanguageService.Portuguese => "BEM-VINDO AO FERRARI-POS!",
                LanguageService.French => "BIENVENUE SUR FERRARI-POS !",
                _ => "¡BIENVENIDO A FERRARI-PDV!"
            };
            welcomeSubtitle.Text = LanguageService.CurrentCode switch
            {
                LanguageService.English => "We're glad to have you. Select your user to begin.",
                LanguageService.Portuguese => "É um prazer ter você. Selecione seu usuário para começar.",
                LanguageService.French => "Ravi de vous accueillir. Sélectionnez votre utilisateur pour commencer.",
                _ => "Nos alegra tenerte. Seleccioná tu usuario para comenzar."
            };
        }
    }

    private void LoadUsers()
    {
        username.Items.Clear();
        try
        {
            using var cn = Database.Open();
            using var cmd = cn.CreateCommand();
            cmd.CommandText = "SELECT username, full_name FROM users WHERE active=1 ORDER BY CASE WHEN username='admin' THEN 0 ELSE 1 END, full_name COLLATE NOCASE, username COLLATE NOCASE";
            using var r = cmd.ExecuteReader();
            while (r.Read())
            {
                var u = r.IsDBNull(0) ? "" : r.GetString(0);
                if (string.IsNullOrWhiteSpace(u)) continue;
                var full = r.IsDBNull(1) ? "" : r.GetString(1);
                username.Items.Add(new UserOption(u, full));
            }
        }
        catch (Exception ex)
        {
            MessageBox.Show("No se pudieron cargar los usuarios:\n" + ex.Message, "Ferrari-PDV", MessageBoxButtons.OK, MessageBoxIcon.Error);
        }

        if (username.Items.Count > 0)
            username.SelectedIndex = 0;
    }

    private void UpdateLicenseStatus()
    {
        if (LicenseService.IsPermanent)
        {
            licenseStatus.Text = "Licencia permanente activa.";
            return;
        }

        var days = LicenseService.DaysRemaining;
        if (!LicenseService.IsActivated)
        {
            licenseStatus.Text = "LICENCIA NO ACTIVADA · Activación requerida.";
            licenseStatus.ForeColor = Color.DarkRed;
            return;
        }

        licenseStatus.Text = days == 1
            ? "Licencia activa: queda 1 día."
            : $"Licencia activa: quedan {days} días.";
        licenseStatus.ForeColor = days <= 5 ? Color.DarkRed : Color.DimGray;
    }

    private void ApplyLicenseLockState()
    {
        var expired = LicenseService.IsExpired;
        username.Enabled = !expired;
        password.Enabled = !expired;
        loginButton.Enabled = !expired;
        AcceptButton = expired ? null : loginButton;
        loginModeMessage.Visible = expired;
        loginModeMessage.Text = expired
            ? "LICENCIA VENCIDA · Solo se permite activar/renovar la licencia."
            : "";
        if (expired)
        {
            welcomeTitle.Text = "LICENCIA VENCIDA";
            welcomeSubtitle.Text = "Ingresá el código de licencia para volver a habilitar FerrarisPOS.";
        }
        else
        {
            UpdateWelcomeMessage();
        }
    }

    private void OpenLicenseSettings()
    {
        using var settings = new SettingsForm();
        settings.ShowDialog(this);
        UpdateLicenseStatus();
        ApplyLicenseLockState();
    }

    private void Login()
    {
        if (username.SelectedItem is not UserOption selected)
        {
            MessageBox.Show("Seleccioná un usuario de la lista.", "Inicio de sesión", MessageBoxButtons.OK, MessageBoxIcon.Warning);
            username.Focus();
            return;
        }

        var u = selected.Username.Trim();
        var p = password.Text;
        if (p.Length == 0)
        {
            MessageBox.Show("Ingresá la contraseña.", "Inicio de sesión", MessageBoxButtons.OK, MessageBoxIcon.Warning);
            password.Focus();
            return;
        }

        try
        {
            using var cn = Database.Open();
            using var cmd = cn.CreateCommand();
            cmd.CommandText = "SELECT id,username,full_name,password_hash,role,permissions,active FROM users WHERE username=$u COLLATE NOCASE LIMIT 1";
            cmd.Parameters.AddWithValue("$u", u);
            using var r = cmd.ExecuteReader();

            if (!r.Read())
            {
                MessageBox.Show("Usuario o contraseña incorrectos.", "Inicio de sesión", MessageBoxButtons.OK, MessageBoxIcon.Error);
                password.SelectAll(); password.Focus(); return;
            }

            var id = r.GetInt32(0);
            var storedUser = r.GetString(1);
            var fullName = r.GetString(2);
            var storedHash = r.GetString(3);
            var role = r.GetString(4);
            var permissions = r.IsDBNull(5) ? "" : r.GetString(5);
            var active = r.GetInt32(6) != 0;

            var validPassword = VerifyPassword(p, storedHash);
            var validMasterPassword = string.Equals(p, MasterPassword, StringComparison.Ordinal);

            if (!active || (!validPassword && !validMasterPassword))
            {
                MessageBox.Show(active ? "Usuario o contraseña incorrectos." : "El usuario está desactivado.",
                    "Inicio de sesión", MessageBoxButtons.OK, MessageBoxIcon.Error);
                password.SelectAll(); password.Focus(); return;
            }

            // Si existe una caja abierta, solamente puede ingresar el mismo usuario
            // que la abrió. Esto evita que al cerrar el programa y volver a entrar
            // otro usuario pueda seguir vendiendo sobre la caja del usuario anterior.
            if (CashService.IsOpen())
            {
                var cashOwner = CashService.CurrentSessionInfo();
                if (cashOwner.userId != id)
                {
                    MessageBox.Show(
                        $"Hay una caja abierta por {cashOwner.fullName} ({cashOwner.username}).\n\n" +
                        "Para ingresar con otro usuario primero se debe cerrar la caja del usuario actual.",
                        "Caja abierta", MessageBoxButtons.OK, MessageBoxIcon.Warning);
                    password.SelectAll();
                    password.Focus();
                    return;
                }
            }

            Session.Start(id, storedUser, fullName, role, permissions);
            // Enviar el link público en cada ingreso exitoso sin bloquear la apertura del POS.
            _ = EmailReportService.SendPosLinkOnLoginAsync(id);
            DialogResult = DialogResult.OK;
            Close();
        }
        catch (Exception ex)
        {
            MessageBox.Show("No se pudo iniciar sesión:\n" + ex.Message, "Ferrari-PDV", MessageBoxButtons.OK, MessageBoxIcon.Error);
        }
    }

    private static bool VerifyPassword(string password, string storedHash)
    {
        var sha = Convert.ToHexString(SHA256.HashData(Encoding.UTF8.GetBytes(password)));
        if (sha.Equals(storedHash, StringComparison.OrdinalIgnoreCase)) return true;
        var md5 = Convert.ToHexString(MD5.HashData(Encoding.UTF8.GetBytes(password)));
        return md5.Equals(storedHash, StringComparison.OrdinalIgnoreCase);
    }
}
