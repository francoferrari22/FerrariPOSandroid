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
    private const string MasterPassword = "600613";
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
        Text = "Ferrari-PDV · Bienvenido";
        Width = 840; Height = 590;
        StartPosition = FormStartPosition.CenterScreen;
        FormBorderStyle = FormBorderStyle.FixedDialog;
        MaximizeBox = false; MinimizeBox = false;
        BackColor = Color.WhiteSmoke;
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

        // Diseño inspirado en la identidad visual del administrador de licencias:
        // azul petróleo/navy, cian, azul eléctrico y acento violeta.
        var background = new Panel
        {
            Name = "loginBackground",
            Dock = DockStyle.Fill,
            BackColor = Color.FromArgb(10, 22, 40)
        };
        Controls.Add(background);

        var brandPanel = new Panel
        {
            Location = new Point(22, 22),
            Size = new Size(305, 516),
            BackColor = Color.FromArgb(17, 38, 66),
            Tag = "LoginBrand"
        };
        background.Controls.Add(brandPanel);

        // Barra cromática superior, siguiendo el estilo del administrador de licencias.
        brandPanel.Paint += (_, e) =>
        {
            using var b1 = new SolidBrush(Color.FromArgb(0, 190, 230));
            using var b2 = new SolidBrush(Color.FromArgb(0, 110, 220));
            using var b3 = new SolidBrush(Color.FromArgb(130, 75, 220));
            var w = brandPanel.Width / 3;
            e.Graphics.FillRectangle(b1, 0, 0, w, 7);
            e.Graphics.FillRectangle(b2, w, 0, w, 7);
            e.Graphics.FillRectangle(b3, w * 2, 0, brandPanel.Width - w * 2, 7);
        };

        // La imagen vertical suministrada por el usuario reemplaza al logo del login.
        // Se usa la misma imagen, sin campos ni controles dibujados dentro de ella.
        var loginPoster = CreateLoginPosterPictureBox(new Point(0, 29), new Size(305, 458));
        brandPanel.Controls.Add(loginPoster);

        var card = new Panel
        {
            Name = "loginCard",
            Location = new Point(350, 22),
            Size = new Size(448, 516),
            BackColor = Color.FromArgb(245, 248, 252),
            Tag = "LoginCard"
        };
        background.Controls.Add(card);

        var titleBar = new Panel { Location = new Point(0, 0), Size = new Size(448, 8), BackColor = Color.FromArgb(0, 165, 225) };
        card.Controls.Add(titleBar);

        welcomeTitle.Text = "INICIO DE SESIÓN";
        welcomeTitle.Location = new Point(34, 38);
        welcomeTitle.Size = new Size(380, 38);
        welcomeTitle.Font = new Font("Segoe UI", 17, FontStyle.Bold);
        welcomeTitle.ForeColor = Color.FromArgb(20, 42, 70);
        card.Controls.Add(welcomeTitle);

        welcomeSubtitle.Text = "Ingresá al sistema para comenzar a trabajar.";
        welcomeSubtitle.Location = new Point(36, 79);
        welcomeSubtitle.Size = new Size(375, 30);
        welcomeSubtitle.Font = new Font("Segoe UI", 9.5f);
        welcomeSubtitle.ForeColor = Color.FromArgb(90, 106, 123);
        card.Controls.Add(welcomeSubtitle);

        // Separación fija para evitar que el estado de licencia se superponga
        // con el botón ACTIVAR LICENCIA en resoluciones/escala de Windows altas.
        licenseStatus.Location = new Point(36, 120);
        licenseStatus.Size = new Size(240, 24);
        licenseStatus.Font = new Font("Segoe UI", 8.5f, FontStyle.Bold);
        licenseStatus.ForeColor = Color.FromArgb(50, 100, 135);
        card.Controls.Add(licenseStatus);

        var activate = new Button
        {
            Text = "ACTIVAR LICENCIA",
            Location = new Point(290, 114), Width = 123, Height = 34,
            Font = new Font("Segoe UI", 8f, FontStyle.Bold),
            BackColor = Color.FromArgb(32, 74, 120), ForeColor = Color.White,
            FlatStyle = FlatStyle.Flat, Cursor = Cursors.Hand
        };
        activate.FlatAppearance.BorderSize = 0;
        activate.Click += (_, _) => OpenLicenseSettings();
        card.Controls.Add(activate);

        card.Controls.Add(new Label { Text = "USUARIO", Location = new Point(36, 164), AutoSize = true, Font = new Font("Segoe UI", 9, FontStyle.Bold), ForeColor = Color.FromArgb(35, 55, 75) });
        username.Location = new Point(36, 188); username.Width = 376; username.Height = 38;
        username.Font = new Font("Segoe UI", 10); username.DropDownStyle = ComboBoxStyle.DropDownList;
        username.IntegralHeight = false; username.DropDownHeight = 220;
        username.SelectedIndexChanged += (_, _) => { UpdateWelcomeMessage(); password.Focus(); };
        card.Controls.Add(username);

        card.Controls.Add(new Label { Text = "CONTRASEÑA", Location = new Point(36, 242), AutoSize = true, Font = new Font("Segoe UI", 9, FontStyle.Bold), ForeColor = Color.FromArgb(35, 55, 75) });
        password.Location = new Point(36, 266); password.Width = 376; password.Height = 38;
        password.Font = new Font("Segoe UI", 10); password.UseSystemPasswordChar = true; password.Clear(); password.PlaceholderText = "Ingresá tu contraseña";
        card.Controls.Add(password);

        loginButton.Text = "INGRESAR AL SISTEMA";
        loginButton.Location = new Point(36, 326); loginButton.Width = 376; loginButton.Height = 48;
        loginButton.Font = new Font("Segoe UI", 10, FontStyle.Bold); loginButton.BackColor = Color.FromArgb(0, 139, 215); loginButton.ForeColor = Color.White;
        loginButton.FlatStyle = FlatStyle.Flat; loginButton.FlatAppearance.BorderSize = 0; loginButton.Cursor = Cursors.Hand;
        loginButton.Click += (_, _) => Login(); card.Controls.Add(loginButton);

        cancelButton.Text = "SALIR"; cancelButton.Location = new Point(36, 382); cancelButton.Width = 376; cancelButton.Height = 40;
        cancelButton.DialogResult = DialogResult.Cancel; cancelButton.Font = new Font("Segoe UI", 9, FontStyle.Bold);
        cancelButton.BackColor = Color.FromArgb(224, 230, 238); cancelButton.ForeColor = Color.FromArgb(40, 55, 70);
        cancelButton.FlatStyle = FlatStyle.Flat; cancelButton.FlatAppearance.BorderColor = Color.FromArgb(190, 201, 214); cancelButton.Cursor = Cursors.Hand;
        card.Controls.Add(cancelButton);
        AcceptButton = loginButton; CancelButton = cancelButton;

        loginModeMessage.Name = "loginModeMessage";
        loginModeMessage.Location = new Point(36, 430); loginModeMessage.Size = new Size(376, 25);
        loginModeMessage.ForeColor = Color.FromArgb(175, 45, 45); loginModeMessage.Font = new Font("Segoe UI", 8.5f, FontStyle.Bold); loginModeMessage.Visible = false;
        card.Controls.Add(loginModeMessage);

        card.Controls.Add(new Label
        {
            Text = "Ferrari'sPOS® · Sistema de gestión comercial",
            Location = new Point(36, 466), Size = new Size(376, 22), TextAlign = ContentAlignment.MiddleCenter,
            ForeColor = Color.FromArgb(110, 125, 140), Font = new Font("Segoe UI", 8f)
        });
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

    private void ApplyLoginBranding()
    {
        BackColor = Color.FromArgb(10, 22, 40);
        if (Controls["loginBackground"] is not Panel background) return;
        background.BackColor = Color.FromArgb(10, 22, 40);
        if (background.Controls["loginCard"] is Panel card)
        {
            card.BackColor = Color.FromArgb(245, 248, 252);
            foreach (Control c in card.Controls)
            {
                if (c is Label l)
                {
                    l.BackColor = Color.Transparent;
                    if (l == welcomeTitle) l.ForeColor = Color.FromArgb(20, 42, 70);
                    else if (l == welcomeSubtitle) l.ForeColor = Color.FromArgb(90, 106, 123);
                    else if (l == licenseStatus) l.ForeColor = Color.FromArgb(50, 100, 135);
                    else l.ForeColor = Color.FromArgb(55, 70, 88);
                }
            }
            if (card.Controls.Count > 0 && card.Controls[0] is Panel titleBar)
                titleBar.BackColor = Color.FromArgb(0, 165, 225);
            loginButton.BackColor = Color.FromArgb(0, 139, 215);
            loginButton.ForeColor = Color.White;
            cancelButton.BackColor = Color.FromArgb(224, 230, 238);
            cancelButton.ForeColor = Color.FromArgb(40, 55, 70);
            username.BackColor = Color.White;
            username.ForeColor = Color.FromArgb(35, 55, 75);
            password.BackColor = Color.White;
            password.ForeColor = Color.FromArgb(35, 55, 75);
        }
        if (background.Controls["loginBackground"] is Panel) { }
        if (background.Controls.Cast<Control>().FirstOrDefault(c => c.Tag?.ToString() == "LoginBrand") is Panel brand)
        {
            brand.BackColor = Color.FromArgb(17, 38, 66);
            if (brand.Controls["loginPoster"] is PictureBox poster) poster.BringToFront();
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
