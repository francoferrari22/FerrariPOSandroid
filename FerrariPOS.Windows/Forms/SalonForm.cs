using FerrarisPOS.Services;
using System.Reflection;
using FerrarisPOS.Models;
using FerrarisPOS.Data;
using System.Drawing.Drawing2D;
using System.Drawing.Imaging;

namespace FerrarisPOS.Forms;

public sealed class SalonForm : Form
{
    private readonly MainForm main;
    private readonly SalonBackgroundPanel canvas = new();
    private Image? salonBackgroundImage;
    private bool editMode;
    private bool integrated;
    private bool locked;
    private readonly Dictionary<Control, Point> dragStart = new();
    private SafeComboBox salonSelector = new();
    private Label modeLabel = new();
    private Point decorationMenuPoint;
    private readonly System.Windows.Forms.Timer reservationTimer = new() { Interval = 60000 };
    private readonly HashSet<int> reservationWarningsShown = new();

    public SalonForm(MainForm main, bool editMode = false, bool integrated = false)
    {
        this.main = main; this.editMode = editMode; this.integrated = integrated;
        Text = "FerrarisPOS · Salón";
        Width = 1100; Height = 720;
        // Cuando el salón está integrado en MainForm NO debe imponer un ancho
        // mínimo: la columna derecha puede ser mucho más angosta que una
        // ventana independiente y el mínimo anterior hacía crecer/recortar
        // toda la pantalla.
        MinimumSize = integrated ? Size.Empty : new Size(760, 520);
        StartPosition = FormStartPosition.CenterParent;
        FormBorderStyle = integrated ? FormBorderStyle.None : FormBorderStyle.SizableToolWindow;
        TableService.EnsureTables(); locked = Database.GetSetting("salon_design_locked", "0") == "1";
        Build(); InitializeSalonBackground(); LanguageService.Apply(this); RefreshSalon(); ThemeService.Apply(this);
        EnableAntiFlicker();
        reservationTimer.Tick += (_, _) => CheckUpcomingReservations();
        reservationTimer.Start();
        FormClosed += (_, _) => reservationTimer.Stop();
    }

    private void Build()
    {
        // Encabezado compacto y estable. Los tres botones de acciones del salón
        // se fuerzan a una sola fila dentro del ancho REAL del módulo integrado.
        // No se usa desplazamiento horizontal y el contenedor nunca sobresale.
        var bar = new Panel
        {
            Dock = DockStyle.Top,
            Height = 128,
            Padding = new Padding(7),
            BackColor = integrated ? Color.FromArgb(24, 28, 35) : SystemColors.Control,
            Tag = integrated ? "SalonGlassBar" : null
        };

        bar.Controls.Add(new Label
        {
            Text = "SALÓN:", AutoSize = true,
            Font = new Font("Segoe UI", 9, FontStyle.Bold),
            Location = new Point(10, 12)
        });

        salonSelector = new SafeComboBox
        {
            Width = 220,
            Height = 32,
            DropDownStyle = ComboBoxStyle.DropDownList,
            IntegralHeight = false,
            DropDownHeight = 320,
            Location = new Point(70, 7)
        };
        LoadSalons();
        salonSelector.SelectedIndexChanged += (_, _) =>
        {
            if (salonSelector.SelectedValue is int id)
            {
                TableService.SetActiveSalon(id);
                RefreshSalon();
            }
        };
        bar.Controls.Add(salonSelector);

        modeLabel = new Label
        {
            AutoSize = true,
            Font = new Font("Segoe UI", 9, FontStyle.Bold),
            Location = new Point(310, 13)
        };
        bar.Controls.Add(modeLabel);

        // Tarjeta de acciones del salón: el botón BLOQUEAR EDICIÓN ocupa
        // toda la fila superior y los botones + SALÓN / + MESA quedan debajo.
        // Así los tres controles siempre caben dentro del ancho visible del módulo.
        actionBar = new TableLayoutPanel
        {
            ColumnCount = 2,
            RowCount = 2,
            Location = new Point(bar.Padding.Left, 48),
            Size = new Size(Math.Max(120, bar.ClientSize.Width - bar.Padding.Horizontal), 70),
            Padding = new Padding(2),
            Margin = Padding.Empty,
            BackColor = Color.Transparent,
            CellBorderStyle = TableLayoutPanelCellBorderStyle.None,
            GrowStyle = TableLayoutPanelGrowStyle.FixedSize,
            Anchor = AnchorStyles.Left | AnchorStyles.Right | AnchorStyles.Top
        };
        actionBar.ColumnStyles.Add(new ColumnStyle(SizeType.Percent, 50f));
        actionBar.ColumnStyles.Add(new ColumnStyle(SizeType.Percent, 50f));
        actionBar.RowStyles.Add(new RowStyle(SizeType.Absolute, 34f));
        actionBar.RowStyles.Add(new RowStyle(SizeType.Absolute, 34f));

        var newSalon = Button("+ SALÓN", (_, _) => CreateSalon());
        var addRect = Button("+ MESA", (_, _) =>
        {
            if (!locked)
            {
                TableService.AddTable();
                RefreshSalon();
            }
        });
        lockButton = Button(locked ? "🔒 BLOQUEAR EDICIÓN" : "🔓 MODO EDICIÓN", (_, _) => ToggleLock());
        lockButton.Tag = "SalonLockButton";

        // Distribución definitiva: BLOQUEAR ocupa exactamente el ancho
        // combinado de + SALÓN y + MESA; abajo quedan los dos botones al 50%.
        actionBar.Controls.Add(lockButton, 0, 0);
        actionBar.SetColumnSpan(lockButton, 2);
        actionBar.Controls.Add(newSalon, 0, 1);
        actionBar.Controls.Add(addRect, 1, 1);

        foreach (Control c in actionBar.Controls)
        {
            c.Dock = DockStyle.Fill;
            c.Margin = new Padding(1);
            c.MinimumSize = Size.Empty;
            c.MaximumSize = Size.Empty;
            c.Font = new Font("Segoe UI", 7f, FontStyle.Bold);
            c.Padding = new Padding(1);
        }

        bar.Controls.Add(actionBar);
        Controls.Add(bar);

        void ResizeActionBar()
        {
            if (actionBar == null || actionBar.IsDisposed) return;
            int available = Math.Max(100, bar.ClientSize.Width - bar.Padding.Horizontal);
            int x = bar.Padding.Left;
            int y = 48;
            // Nunca superar el ancho real de la barra. En modo integrado
            // esto evita que el tercer botón quede fuera de la pantalla.
            actionBar.Bounds = new Rectangle(x, y, available, 70);
        }
        bar.Resize += (_, _) => ResizeActionBar();
        ResizeActionBar();

        canvas.Dock = DockStyle.Fill;
        canvas.BackColor = Color.Transparent;
        canvas.AutoScroll = true;
        canvas.BorderStyle = BorderStyle.None;
        canvas.Tag = "SalonCanvas";
        Controls.Add(canvas);

        // Las herramientas decorativas se habilitan EXCLUSIVAMENTE en
        // Configuración > Edición de salón (ventana no integrada).
        // Nunca aparecen en el salón embebido de la pantalla principal.
        if (!integrated)
        {
            canvas.MouseUp += (_, e) =>
            {
                if (e.Button != MouseButtons.Right || !editMode || locked || !CanEdit()) return;
                decorationMenuPoint = e.Location;
                BuildDecorationContextMenu().Show(canvas, e.Location);
            };
        }

        bar.BringToFront();
        actionBar.BringToFront();
    }

    private TableLayoutPanel? actionBar;
    private Button? lockButton;

    private void LoadSalons()
    {
        var list = TableService.GetSalons();
        if (list.Count == 0)
        {
            salonSelector.DataSource = null;
            salonSelector.Items.Clear();
            return;
        }
        salonSelector.SelectedIndex = -1;
        salonSelector.DataSource = list;
        salonSelector.DisplayMember = "Name";
        salonSelector.ValueMember = "Id";
        var active = TableService.ActiveSalonId;
        var idx = list.FindIndex(x => x.Id == active);
        if (idx >= 0 && idx < list.Count) salonSelector.SelectedIndex = idx;
        else if (salonSelector.Items.Count > 0) salonSelector.SelectedIndex = 0;
    }
    private void CreateSalon(){using var f=new InputBoxForm("Nuevo salón", "Terraza"); if(f.ShowDialog(this)!=DialogResult.OK||string.IsNullOrWhiteSpace(f.Value))return; var id=TableService.AddSalon(f.Value); TableService.SetActiveSalon(id); LoadSalons(); RefreshSalon();}
    private void RenameSalon(){if(salonSelector.SelectedItem is not Salon s)return;using var f=new InputBoxForm("Renombrar salón",s.Name);if(f.ShowDialog(this)!=DialogResult.OK)return;TableService.RenameSalon(s.Id,f.Value,s.Description);LoadSalons();RefreshSalon();}
    private bool CanEdit(){if(locked){MessageBox.Show("El diseño está bloqueado. Desbloquealo para editar.","Salón",MessageBoxButtons.OK,MessageBoxIcon.Information);return false;} if(Database.GetSetting("tables_edit_enabled","1")!="1"){MessageBox.Show("La edición del salón está desactivada en F7 · Configuración.");return false;} return true;}
    private void ToggleLock(){
        if (locked)
        {
            locked = false;
            editMode = true;
            Database.SetSetting("salon_design_locked", "0");
        }
        else
        {
            locked = true;
            editMode = false;
            Database.SetSetting("salon_design_locked", "1");
        }
        RefreshSalon();
    }
    private Button Button(string text, EventHandler click)
    {
        var b = new Button
        {
            Text = text,
            Font = new Font("Segoe UI", 7.5f, FontStyle.Bold),
            BackColor = SystemColors.Control,
            ForeColor = SystemColors.ControlText,
            FlatStyle = FlatStyle.Standard,
            TabStop = true,
            AutoEllipsis = false,
            UseVisualStyleBackColor = false,
            TextAlign = ContentAlignment.MiddleCenter,
            Padding = new Padding(1),
            Margin = new Padding(2)
        };
        b.Click += click;
        return b;
    }

    private void InitializeSalonBackground()
    {
        try
        {
            var path = Path.Combine(AppContext.BaseDirectory, "Resources", "pos_background.png");
            if (!File.Exists(path)) return;
            using var source = Image.FromFile(path);
            using var opacity = CreateOpacityCopy(source, 0.49f);
            salonBackgroundImage = CreateBlurredCopy(opacity);
            canvas.BackgroundImage = salonBackgroundImage;
            canvas.BackgroundImageLayout = ImageLayout.Stretch;
        }
        catch
        {
            salonBackgroundImage?.Dispose();
            salonBackgroundImage = null;
        }
    }

    private static Bitmap CreateOpacityCopy(Image source, float opacity)
    {
        var copy = new Bitmap(source.Width, source.Height, System.Drawing.Imaging.PixelFormat.Format32bppArgb);
        using var g = Graphics.FromImage(copy);
        using var attributes = new System.Drawing.Imaging.ImageAttributes();
        attributes.SetColorMatrix(new ColorMatrix { Matrix33 = Math.Clamp(opacity, 0f, 1f) });
        g.Clear(Color.Transparent);
        g.DrawImage(source, new Rectangle(0, 0, copy.Width, copy.Height), 0, 0, source.Width, source.Height, GraphicsUnit.Pixel, attributes);
        return copy;
    }

    private static Bitmap CreateBlurredCopy(Image source)
    {
        const int factor = 6;
        int smallW = Math.Max(1, source.Width / factor);
        int smallH = Math.Max(1, source.Height / factor);
        using var small = new Bitmap(smallW, smallH, System.Drawing.Imaging.PixelFormat.Format32bppArgb);
        using (var g = Graphics.FromImage(small))
        {
            g.Clear(Color.Transparent);
            g.InterpolationMode = InterpolationMode.HighQualityBicubic;
            g.SmoothingMode = SmoothingMode.HighQuality;
            g.PixelOffsetMode = PixelOffsetMode.HighQuality;
            g.DrawImage(source, new Rectangle(0, 0, smallW, smallH));
        }
        var blurred = new Bitmap(source.Width, source.Height, System.Drawing.Imaging.PixelFormat.Format32bppArgb);
        using (var g = Graphics.FromImage(blurred))
        {
            g.Clear(Color.Transparent);
            g.InterpolationMode = InterpolationMode.HighQualityBicubic;
            g.SmoothingMode = SmoothingMode.HighQuality;
            g.PixelOffsetMode = PixelOffsetMode.HighQuality;
            g.DrawImage(small, new Rectangle(0, 0, blurred.Width, blurred.Height));
        }
        return blurred;
    }

    protected override void Dispose(bool disposing)
    {
        if (disposing) salonBackgroundImage?.Dispose();
        base.Dispose(disposing);
    }

    private void EnableAntiFlicker()
    {
        try
        {
            SetStyle(ControlStyles.OptimizedDoubleBuffer |
                     ControlStyles.AllPaintingInWmPaint |
                     ControlStyles.UserPaint, true);
            UpdateStyles();
            var prop = typeof(Control).GetProperty("DoubleBuffered",
                BindingFlags.Instance | BindingFlags.NonPublic);
            prop?.SetValue(this, true, null);
            prop?.SetValue(canvas, true, null);
        }
        catch { }
    }

    public void RefreshSalon()
    {
        foreach (Control c in canvas.Controls) c.Dispose(); canvas.Controls.Clear();
        if (Database.GetSetting("table_decorations_enabled", "1") == "1") foreach (var d in TableService.GetDecorations()) AddDecorationControl(d);
        foreach (var t in TableService.GetTables()) AddTableControl(t);
        modeLabel.Text = locked ? "🔒 " + LanguageService.Translate("DISEÑO BLOQUEADO") : editMode ? "✏ " + LanguageService.Translate("MODO EDICIÓN") : "● " + LanguageService.Translate("MODO OPERACIÓN");
        if (lockButton != null && !lockButton.IsDisposed)
            lockButton.Text = locked ? "🔒 BLOQUEAR EDICIÓN" : "🔓 MODO EDICIÓN";
        canvas.Invalidate();
        CheckUpcomingReservations();
    }

    private void CheckUpcomingReservations()
    {
        if (IsDisposed) return;
        var now = DateTime.Now;
        foreach (var table in TableService.GetTables())
        {
            var reservation = ReservationService.GetTableReservationWithinWindow(table.Id, now, 120);
            if (reservation is null || reservationWarningsShown.Contains(reservation.Id)) continue;
            if (!ReservationService.TryGetReservationDateTime(reservation, out var reservationTime)) continue;
            var minutes = Math.Max(0, (int)Math.Round((reservationTime - now).TotalMinutes));
            var timeText = reservationTime.ToString("HH:mm");
            reservationWarningsShown.Add(reservation.Id);
            BeginInvoke(new Action(() =>
            {
                if (IsDisposed) return;
                MessageBox.Show($"AVISO DE RESERVA\n\nLa mesa {table.Name} está reservada para las {timeText}.\nFaltan aproximadamente {minutes / 60} h {minutes % 60:00} min.\n\nLa mesa queda BLOQUEADA para nuevas ventas hasta que se cancele la reserva.", "Mesa reservada", MessageBoxButtons.OK, MessageBoxIcon.Warning);
            }));
        }
    }

    private ContextMenuStrip BuildDecorationContextMenu()
    {
        var menu = new ContextMenuStrip();
        menu.Items.Add(new ToolStripLabel("AGREGAR ELEMENTO AL SALÓN"));
        menu.Items.Add(new ToolStripSeparator());
        AddDecorationMenuItem(menu, "grass", "CÉSPED", "🌿", 120, 70);
        AddDecorationMenuItem(menu, "tree", "ÁRBOL", "🌳", 90, 110);
        AddDecorationMenuItem(menu, "road", "CALLE", "🛣️", 180, 60);
        AddDecorationMenuItem(menu, "plant", "PLANTAS", "🪴", 90, 80);
        AddDecorationMenuItem(menu, "structure", "ESTRUCTURA", "🏢", 130, 90);
        AddDecorationMenuItem(menu, "figure", "FIGURA", "⬛", 90, 70);
        return menu;
    }

    private void AddDecorationMenuItem(ContextMenuStrip menu, string type, string name, string icon, int width, int height)
    {
        var item = new ToolStripMenuItem($"{icon}  {name}");
        item.Click += (_, _) => AddDecorationAt(type, name, width, height);
        menu.Items.Add(item);
    }

    private void AddDecorationAt(string type, string name, int width, int height)
    {
        if (!editMode || locked || integrated || !CanEdit()) return;
        var maxX = Math.Max(0, canvas.ClientSize.Width - width - 6);
        var maxY = Math.Max(0, canvas.ClientSize.Height - height - 6);
        var x = Math.Clamp(decorationMenuPoint.X - width / 2, 0, maxX);
        var y = Math.Clamp(decorationMenuPoint.Y - height / 2, 0, maxY);
        var id = TableService.AddDecoration(type, name);
        TableService.UpdateDecoration(id, x, y, width, height);
        RefreshSalon();
    }

    private void AddDecorationToTableMenu(ContextMenuStrip menu)
    {
        if (integrated || !editMode || locked) return;
        var add = new ToolStripMenuItem("➕ AGREGAR ELEMENTO DECORATIVO");
        add.Click += (_, _) =>
        {
            decorationMenuPoint = canvas.PointToClient(Cursor.Position);
            BuildDecorationContextMenu().Show(canvas, decorationMenuPoint);
        };
        menu.Items.Add(add);
        menu.Items.Add(new ToolStripSeparator());
    }

    private void AddTableControl(RestaurantTable t)
    {
        var state = main.GetTableState(t.Id);
        var reservation = ReservationService.GetTableReservationWithinWindow(t.Id, DateTime.Now, 120);
        bool reservedToday = reservation is not null;
        Color backColor = reservedToday
            ? Color.FromArgb(205, 45, 45)
            : state.HasValue
                ? Color.FromArgb(225, 120, 35)
                : Color.FromArgb(245, 150, 55);
        string status = state.HasValue
            ? $"{t.Name}\r\n{state.Value.ticketName}\r\n${state.Value.total:N2}"
            : reservedToday
                ? $"{t.Name}\r\n{t.Capacity} personas\r\nRESERVADA {reservation!.Hour}"
                : $"{t.Name}\r\n{t.Capacity} personas\r\nLIBRE";

        var b = new Button { Text = status, Location = new Point(t.X,t.Y), Size = new Size(t.Width,t.Height), FlatStyle=FlatStyle.Flat, Font=new Font("Segoe UI",9,FontStyle.Bold), Tag=$"SalonTable:{(state.HasValue ? "occupied" : reservedToday ? "reserved" : "free")}", BackColor=backColor, Cursor=editMode&&!locked?Cursors.SizeAll:Cursors.Hand };
        b.FlatAppearance.BorderSize=1; b.FlatAppearance.BorderColor=reservedToday ? Color.Maroon : Color.SeaGreen;
        ApplyShape(b,t.Shape);
        b.Click += (_,_) =>
        {
            if (editMode) return;
            if (ReservationService.IsTableBlockedForReservation(t.Id, DateTime.Now, 120))
            {
                MessageBox.Show($"La mesa {t.Name} está bloqueada por una reserva próxima.\nNo se puede asignar ni abrir hasta cancelar la reserva.", "Mesa reservada", MessageBoxButtons.OK, MessageBoxIcon.Warning);
                return;
            }
            main.OpenTable(t.Id);
        };

        var menu=new ContextMenuStrip();
        AddDecorationToTableMenu(menu);
        // Estas acciones de operación están disponibles con clic derecho sin
        // quitar las herramientas de edición del salón.
        if(state.HasValue)
            menu.Items.Add("ABRIR / COBRAR MESA",null,(_,_)=>main.OpenTable(t.Id));
        else
            menu.Items.Add("ASIGNAR TICKET A ESTA MESA",null,(_,_)=>main.AssignActiveTicketToTable(t.Id));
        menu.Items.Add(new ToolStripSeparator());
        menu.Items.Add("EDITAR MESA",null,(_,_)=>EditTable(t,b));
        menu.Items.Add("RENOMBRAR",null,(_,_)=>RenameTable(t,b));
        menu.Items.Add("CAMBIAR TAMAÑO",null,(_,_)=>ResizeTable(t,b));
        menu.Items.Add(new ToolStripSeparator());
        menu.Items.Add("ELIMINAR MESA",null,(_,_)=>{if(!CanEdit())return;if(MessageBox.Show($"¿Eliminar {t.Name}?","Eliminar mesa",MessageBoxButtons.YesNo,MessageBoxIcon.Question)==DialogResult.Yes){TableService.DeleteTable(t.Id);RefreshSalon();}});
        b.ContextMenuStrip=menu; if(editMode&&!locked)EnableDrag(b,t); canvas.Controls.Add(b); b.BringToFront();
    }

    private static void ApplyShape(Control c,string shape){if(shape=="ROUND"){using var path=new GraphicsPath();path.AddEllipse(0,0,c.Width-1,c.Height-1);c.Region=new Region(path);}else if(shape=="OVAL"){using var path=new GraphicsPath();path.AddEllipse(0,0,c.Width-1,c.Height-1);c.Region=new Region(path);} }
    private void EditTable(RestaurantTable t,Control c){if(!CanEdit())return;using var f=new TableEditForm(t);if(f.ShowDialog(this)!=DialogResult.OK)return;TableService.UpdateTable(t.Id,f.NameValue,c.Left,c.Top,f.WidthValue,f.HeightValue,true,f.ShapeValue,f.RotationValue,f.CapacityValue,f.ColorValue);RefreshSalon();}
    private void RenameTable(RestaurantTable t, Control c){if(!CanEdit())return;using var f=new InputBoxForm("Nombre de la mesa",t.Name);if(f.ShowDialog(this)!=DialogResult.OK||string.IsNullOrWhiteSpace(f.Value))return;TableService.UpdateTable(t.Id,f.Value.Trim(),c.Left,c.Top,c.Width,c.Height,true,t.Shape,t.Rotation,t.Capacity,t.Color);RefreshSalon();}
    private void ResizeTable(RestaurantTable t, Control c){if(!CanEdit())return;using var f=new TableSizeForm(t.Width,t.Height);if(f.ShowDialog(this)!=DialogResult.OK)return;TableService.UpdateTable(t.Id,t.Name,c.Left,c.Top,f.TableWidth,f.TableHeight,true,t.Shape,t.Rotation,t.Capacity,t.Color);RefreshSalon();}
    private void AddDecorationControl(SalonDecoration d){var icon=d.Type switch{"grass"=>"🌿","tree"=>"🌳","road"=>"🛣️","plant"=>"🪴","structure"=>"🏢","figure"=>"⬛",_=>"✨"};var fontName=d.Type=="road"?"Segoe UI Emoji":"Segoe UI Emoji";var l=new Label{Text=$"{icon}\r\n{d.Name}",Location=new Point(d.X,d.Y),Size=new Size(d.Width,d.Height),TextAlign=ContentAlignment.MiddleCenter,Font=new Font(fontName,d.Type=="tree"?22:18),BackColor=Color.Transparent,Tag=d.Id,Cursor=editMode&&!locked?Cursors.SizeAll:Cursors.Default};if(editMode&&!locked)EnableDecorationDrag(l,d);canvas.Controls.Add(l);l.BringToFront();}
    private void EnableDrag(Control c,RestaurantTable t){c.MouseDown+=(_,e)=>{if(e.Button==MouseButtons.Left)dragStart[c]=e.Location;};c.MouseMove+=(_,e)=>{if(e.Button!=MouseButtons.Left||!dragStart.ContainsKey(c))return;var p=c.Location;p.Offset(e.X-dragStart[c].X,e.Y-dragStart[c].Y);c.Location=p;};c.MouseUp+=(_,e)=>{if(dragStart.Remove(c))TableService.UpdateTable(t.Id,t.Name,c.Left,c.Top,c.Width,c.Height,true,t.Shape,t.Rotation,t.Capacity,t.Color);};}
    private void EnableDecorationDrag(Control c,SalonDecoration d){c.MouseDown+=(_,e)=>{if(e.Button==MouseButtons.Left)dragStart[c]=e.Location;};c.MouseMove+=(_,e)=>{if(e.Button!=MouseButtons.Left||!dragStart.ContainsKey(c))return;var p=c.Location;p.Offset(e.X-dragStart[c].X,e.Y-dragStart[c].Y);c.Location=p;};c.MouseUp+=(_,_)=>{if(dragStart.Remove(c))TableService.UpdateDecoration(d.Id,c.Left,c.Top,c.Width,c.Height);};}
}

internal sealed class SalonBackgroundPanel : Panel
{
    public SalonBackgroundPanel()
    {
        SetStyle(ControlStyles.UserPaint | ControlStyles.AllPaintingInWmPaint | ControlStyles.OptimizedDoubleBuffer, true);
        UpdateStyles();
    }
}

internal sealed class TableEditForm : Form
{
    private readonly TextBox name=new(); private readonly SafeComboBox shape=new(); private readonly NumericUpDown width=new(),height=new(),capacity=new(),rotation=new(); private readonly TextBox color=new();
    public string NameValue=>name.Text.Trim(); public string ShapeValue=>shape.Text; public int WidthValue=>(int)width.Value; public int HeightValue=>(int)height.Value; public int CapacityValue=>(int)capacity.Value; public double RotationValue=>(double)rotation.Value; public string ColorValue=>color.Text.Trim();
    public TableEditForm(RestaurantTable t){Text="Editar mesa";Width=430;Height=410;StartPosition=FormStartPosition.CenterParent;Padding=new Padding(18);var p=new TableLayoutPanel{Dock=DockStyle.Fill,ColumnCount=2,RowCount=8};p.ColumnStyles.Add(new ColumnStyle(SizeType.Percent,35));p.ColumnStyles.Add(new ColumnStyle(SizeType.Percent,65));Add(p,"Nombre",name,t.Name,0);shape.Items.AddRange(new object[]{"RECTANGLE","ROUND","OVAL"});shape.SelectedItem=t.Shape;Add(p,"Forma",shape,null,1);Set(width,t.Width,70,500);Set(height,t.Height,50,400);Set(capacity,t.Capacity,1,30);Set(rotation,(decimal)t.Rotation,-180,180);Add(p,"Ancho",width,null,2);Add(p,"Alto",height,null,3);Add(p,"Capacidad",capacity,null,4);Add(p,"Rotación",rotation,null,5);Add(p,"Color",color,t.Color,6);var ok=new Button{Text="GUARDAR",DialogResult=DialogResult.OK,Width=100};var ca=new Button{Text="CANCELAR",DialogResult=DialogResult.Cancel,Width=100};var fl=new FlowLayoutPanel{Dock=DockStyle.Fill,FlowDirection=FlowDirection.RightToLeft};fl.Controls.Add(ca);fl.Controls.Add(ok);p.Controls.Add(fl,0,7);p.SetColumnSpan(fl,2);Controls.Add(p);AcceptButton=ok;CancelButton=ca;}
    private static void Add(TableLayoutPanel p,string l,Control c,string? val,int row){p.Controls.Add(new Label{Text=l,AutoSize=true},0,row);if(val!=null&&c is TextBox t)t.Text=val;p.Controls.Add(c,1,row);}
    private static void Set(NumericUpDown n,decimal v,decimal min,decimal max){n.Minimum=min;n.Maximum=max;n.Value=Math.Clamp(v,min,max);n.DecimalPlaces=v%1==0?0:1;}
}

internal sealed class InputBoxForm : Form
{
    public string Value => input.Text.Trim(); private readonly TextBox input = new();
    public InputBoxForm(string caption,string value){Text=caption;Width=420;Height=170;StartPosition=FormStartPosition.CenterParent;input.Text=value;input.Location=new Point(15,20);input.Width=370;Controls.Add(input);var ok=new Button{Text="ACEPTAR",DialogResult=DialogResult.OK,Location=new Point(195,70),Width=90};var c=new Button{Text="CANCELAR",DialogResult=DialogResult.Cancel,Location=new Point(295,70),Width=90};Controls.Add(ok);Controls.Add(c);AcceptButton=ok;CancelButton=c;}
}

internal sealed class TableSizeForm : Form
{
    private readonly NumericUpDown width = new(); private readonly NumericUpDown height = new(); public int TableWidth => (int)width.Value; public int TableHeight => (int)height.Value;
    public TableSizeForm(int currentWidth,int currentHeight){Text="Tamaño de la mesa";Width=360;Height=210;StartPosition=FormStartPosition.CenterParent;Padding=new Padding(15);var panel=new TableLayoutPanel{Dock=DockStyle.Fill,ColumnCount=2,RowCount=3};panel.Controls.Add(new Label{Text="Ancho",AutoSize=true},0,0);panel.Controls.Add(new Label{Text="Alto",AutoSize=true},0,1);width.Minimum=70;width.Maximum=500;width.Value=Math.Clamp(currentWidth,70,500);height.Minimum=50;height.Maximum=400;height.Value=Math.Clamp(currentHeight,50,400);panel.Controls.Add(width,1,0);panel.Controls.Add(height,1,1);var ok=new Button{Text="ACEPTAR",DialogResult=DialogResult.OK,Width=100};var cancel=new Button{Text="CANCELAR",DialogResult=DialogResult.Cancel,Width=100};var buttons=new FlowLayoutPanel{FlowDirection=FlowDirection.RightToLeft,Dock=DockStyle.Fill};buttons.Controls.Add(cancel);buttons.Controls.Add(ok);panel.Controls.Add(buttons,0,2);panel.SetColumnSpan(buttons,2);Controls.Add(panel);AcceptButton=ok;CancelButton=cancel;}
}
