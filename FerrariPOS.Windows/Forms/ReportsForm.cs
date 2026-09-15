using FerrarisPOS.Data;
using FerrarisPOS.Services;
using System.Text;

namespace FerrarisPOS.Forms;

public class ReportsForm : Form
{
    private readonly DateTimePicker from = new();
    private readonly DateTimePicker to = new();
    private readonly Label summary = new();
    private readonly DataGridView grid = new();
    private readonly SafeComboBox report = new();
    private readonly FlowLayoutPanel reportTabs = new();
    private readonly Label selectedReportTitle = new();
    private readonly Label periodLabel = new();
    private readonly List<Button> tabButtons = new();

    private static readonly string[] ReportNames =
    {
        "RESUMEN GENERAL", "VENTAS", "PRODUCTOS MÁS VENDIDOS", "VENTAS POR USUARIO",
        "VENTAS POR CANAL", "CAJA", "DEVOLUCIONES", "MERMAS", "PROVEEDORES",
        "CLIENTES / DEUDAS", "STOCK BAJO", "COMPRAS"
    };

    public ReportsForm()
    {
        Text = "FerrarisPOS · Reportes";
        Width = 1380;
        Height = 850;
        MinimumSize = new Size(1050, 700);
        StartPosition = FormStartPosition.CenterParent;
        AutoScaleMode = AutoScaleMode.Font;
        BackColor = Color.FromArgb(242, 244, 247);
        Build();
        LoadData();
    }

    private void Build()
    {
        SuspendLayout();

        var header = new Panel
        {
            Dock = DockStyle.Top,
            Height = 72,
            BackColor = Color.FromArgb(31, 36, 43),
            Padding = new Padding(24, 12, 24, 10)
        };
        Controls.Add(header);

        var title = new Label
        {
            Text = "REPORTES",
            AutoSize = true,
            ForeColor = Color.White,
            Font = new Font("Segoe UI", 20, FontStyle.Bold),
            Location = new Point(24, 12)
        };
        header.Controls.Add(title);

        var subtitle = new Label
        {
            Text = "Análisis de ventas, caja, inventario y clientes",
            AutoSize = true,
            ForeColor = Color.FromArgb(190, 198, 208),
            Font = new Font("Segoe UI", 9.5f),
            Location = new Point(26, 43)
        };
        header.Controls.Add(subtitle);

        var close = new Button
        {
            Text = "✕",
            Dock = DockStyle.Right,
            Width = 44,
            FlatStyle = FlatStyle.Flat,
            ForeColor = Color.White,
            BackColor = Color.Transparent,
            Font = new Font("Segoe UI", 11, FontStyle.Bold),
            Cursor = Cursors.Hand
        };
        close.FlatAppearance.BorderSize = 0;
        close.Click += (_, _) => Close();
        header.Controls.Add(close);

        var content = new Panel { Dock = DockStyle.Fill, Padding = new Padding(20, 16, 20, 20), BackColor = Color.FromArgb(242, 244, 247) };
        Controls.Add(content);

        // Filtros: cada campo tiene su propia tarjeta para evitar superposiciones.
        var filterCard = new Panel
        {
            Dock = DockStyle.Top,
            Height = 96,
            BackColor = Color.White,
            Padding = new Padding(16),
            BorderStyle = BorderStyle.FixedSingle
        };
        content.Controls.Add(filterCard);

        AddFieldLabel(filterCard, "DESDE", 16, 10);
        ConfigureDatePicker(from, 16, 36);
        filterCard.Controls.Add(from);

        AddFieldLabel(filterCard, "HASTA", 190, 10);
        ConfigureDatePicker(to, 190, 36);
        filterCard.Controls.Add(to);

        periodLabel.Text = "PERÍODO SELECCIONADO";
        periodLabel.AutoSize = true;
        periodLabel.ForeColor = Color.FromArgb(90, 99, 110);
        periodLabel.Font = new Font("Segoe UI", 8.5f, FontStyle.Bold);
        periodLabel.Location = new Point(365, 16);
        filterCard.Controls.Add(periodLabel);

        selectedReportTitle.Text = "RESUMEN GENERAL";
        selectedReportTitle.AutoSize = true;
        selectedReportTitle.ForeColor = Color.FromArgb(31, 36, 43);
        selectedReportTitle.Font = new Font("Segoe UI", 11, FontStyle.Bold);
        selectedReportTitle.Location = new Point(365, 40);
        filterCard.Controls.Add(selectedReportTitle);

        var refresh = MakeActionButton("↻  ACTUALIZAR", 600, 30, 125);
        refresh.Click += (_, _) => LoadData();
        filterCard.Controls.Add(refresh);

        var calendar = MakeActionButton("▣  CALENDARIO", 735, 30, 125);
        calendar.Click += (_, _) =>
        {
            using var form = new SalesCalendarForm(from.Value);
            ThemeService.Apply(form);
            form.ShowDialog(this);
        };
        filterCard.Controls.Add(calendar);

        var export = MakeActionButton("⇩  EXPORTAR CSV", 870, 30, 145);
        export.Click += (_, _) => Export();
        filterCard.Controls.Add(export);

        report.Visible = false;
        report.DropDownStyle = ComboBoxStyle.DropDownList;
        report.Items.AddRange(ReportNames.Cast<object>().ToArray());
        report.SelectedIndex = 0;
        report.SelectedIndexChanged += (_, _) => { SyncSelectedTab(); LoadData(); };
        Controls.Add(report);

        // Selector tipo solapas: se adapta al ancho y permite ver claramente qué reporte está activo.
        var tabCard = new Panel
        {
            Dock = DockStyle.Top,
            Height = 126,
            BackColor = Color.White,
            Padding = new Padding(14, 10, 14, 8),
            BorderStyle = BorderStyle.FixedSingle
        };
        content.Controls.Add(tabCard);

        var tabCaption = new Label
        {
            Text = "VER REPORTE",
            AutoSize = true,
            Font = new Font("Segoe UI", 8.5f, FontStyle.Bold),
            ForeColor = Color.FromArgb(90, 99, 110),
            Location = new Point(14, 9)
        };
        tabCard.Controls.Add(tabCaption);

        reportTabs.Dock = DockStyle.None;
        reportTabs.Location = new Point(12, 31);
        reportTabs.Size = new Size(tabCard.ClientSize.Width - 24, 82);
        reportTabs.Anchor = AnchorStyles.Top | AnchorStyles.Left | AnchorStyles.Right;
        reportTabs.WrapContents = true;
        reportTabs.FlowDirection = FlowDirection.LeftToRight;
        reportTabs.AutoScroll = true;
        reportTabs.Padding = new Padding(0, 2, 0, 0);
        tabCard.Controls.Add(reportTabs);
        tabCaption.BringToFront();

        foreach (var name in ReportNames)
        {
            var button = new Button
            {
                Text = name,
                AutoSize = true,
                Height = 31,
                MinimumSize = new Size(90, 31),
                Margin = new Padding(3),
                Padding = new Padding(12, 0, 12, 0),
                FlatStyle = FlatStyle.Flat,
                Font = new Font("Segoe UI", 8.5f, FontStyle.Bold),
                Cursor = Cursors.Hand,
                Tag = name
            };
            button.FlatAppearance.BorderSize = 1;
            button.Click += (_, _) =>
            {
                report.SelectedItem = name;
                SyncSelectedTab();
            };
            tabButtons.Add(button);
            reportTabs.Controls.Add(button);
        }

        var summaryCard = new Panel
        {
            Dock = DockStyle.Top,
            Height = 104,
            BackColor = Color.White,
            Padding = new Padding(16, 12, 16, 10),
            BorderStyle = BorderStyle.FixedSingle
        };
        content.Controls.Add(summaryCard);

        summary.Dock = DockStyle.Fill;
        summary.Font = new Font("Segoe UI", 10.5f, FontStyle.Bold);
        summary.ForeColor = Color.FromArgb(45, 52, 60);
        summary.TextAlign = ContentAlignment.MiddleLeft;
        summary.AutoEllipsis = true;
        summaryCard.Controls.Add(summary);

        var gridCard = new Panel
        {
            Dock = DockStyle.Fill,
            BackColor = Color.White,
            Padding = new Padding(1),
            BorderStyle = BorderStyle.FixedSingle
        };
        content.Controls.Add(gridCard);

        ConfigureGrid();
        grid.Dock = DockStyle.Fill;
        gridCard.Controls.Add(grid);

        content.Controls.SetChildIndex(filterCard, 0);
        content.Controls.SetChildIndex(tabCard, 1);
        content.Controls.SetChildIndex(summaryCard, 2);
        content.Controls.SetChildIndex(gridCard, 3);

        from.ValueChanged += (_, _) => { periodLabel.Text = $"PERÍODO: {from.Value:dd/MM/yyyy} → {to.Value:dd/MM/yyyy}"; };
        to.ValueChanged += (_, _) => { periodLabel.Text = $"PERÍODO: {from.Value:dd/MM/yyyy} → {to.Value:dd/MM/yyyy}"; };
        periodLabel.Text = $"PERÍODO: {from.Value:dd/MM/yyyy} → {to.Value:dd/MM/yyyy}";
        SyncSelectedTab();

        ResumeLayout(true);
    }

    private static void AddFieldLabel(Control parent, string text, int x, int y)
    {
        parent.Controls.Add(new Label
        {
            Text = text,
            AutoSize = true,
            Font = new Font("Segoe UI", 8.5f, FontStyle.Bold),
            ForeColor = Color.FromArgb(90, 99, 110),
            Location = new Point(x, y)
        });
    }

    private static void ConfigureDatePicker(DateTimePicker picker, int x, int y)
    {
        picker.Location = new Point(x, y);
        picker.Size = new Size(145, 28);
        picker.Format = DateTimePickerFormat.Custom;
        picker.CustomFormat = "dd/MM/yyyy";
        picker.Value = DateTime.Today;
        picker.Font = new Font("Segoe UI", 9.5f);
    }

    private static Button MakeActionButton(string text, int x, int y, int width)
    {
        var b = new Button
        {
            Text = text,
            Location = new Point(x, y),
            Size = new Size(width, 34),
            FlatStyle = FlatStyle.Flat,
            BackColor = Color.FromArgb(239, 242, 246),
            ForeColor = Color.FromArgb(45, 52, 60),
            Font = new Font("Segoe UI", 8.5f, FontStyle.Bold),
            Cursor = Cursors.Hand
        };
        b.FlatAppearance.BorderColor = Color.FromArgb(210, 215, 221);
        b.FlatAppearance.BorderSize = 1;
        return b;
    }

    private void SyncSelectedTab()
    {
        var selected = report.SelectedItem?.ToString() ?? ReportNames[0];
        selectedReportTitle.Text = selected;
        foreach (var b in tabButtons)
        {
            var active = string.Equals(b.Tag?.ToString(), selected, StringComparison.Ordinal);
            b.BackColor = active ? Color.FromArgb(31, 36, 43) : Color.FromArgb(245, 247, 249);
            b.ForeColor = active ? Color.White : Color.FromArgb(60, 68, 77);
            b.FlatAppearance.BorderColor = active ? Color.FromArgb(31, 36, 43) : Color.FromArgb(215, 220, 226);
        }
    }

    private void ConfigureGrid()
    {
        grid.ReadOnly = true;
        grid.AutoGenerateColumns = true;
        grid.BackgroundColor = Color.White;
        grid.BorderStyle = BorderStyle.None;
        grid.CellBorderStyle = DataGridViewCellBorderStyle.SingleHorizontal;
        grid.GridColor = Color.FromArgb(232, 235, 239);
        grid.SelectionMode = DataGridViewSelectionMode.FullRowSelect;
        grid.MultiSelect = false;
        grid.AllowUserToAddRows = false;
        grid.AllowUserToDeleteRows = false;
        grid.AllowUserToResizeRows = false;
        grid.RowHeadersVisible = false;
        grid.AutoSizeRowsMode = DataGridViewAutoSizeRowsMode.None;
        grid.RowTemplate.Height = 32;
        grid.ColumnHeadersHeight = 38;
        grid.ColumnHeadersDefaultCellStyle = new DataGridViewCellStyle
        {
            BackColor = Color.FromArgb(31, 36, 43),
            ForeColor = Color.White,
            Font = new Font("Segoe UI", 9, FontStyle.Bold),
            Alignment = DataGridViewContentAlignment.MiddleLeft,
            SelectionBackColor = Color.FromArgb(31, 36, 43),
            SelectionForeColor = Color.White
        };
        grid.DefaultCellStyle = new DataGridViewCellStyle
        {
            BackColor = Color.White,
            ForeColor = Color.FromArgb(45, 52, 60),
            Font = new Font("Segoe UI", 9),
            SelectionBackColor = Color.FromArgb(224, 229, 235),
            SelectionForeColor = Color.FromArgb(25, 30, 35),
            Padding = new Padding(8, 0, 8, 0)
        };
        grid.AlternatingRowsDefaultCellStyle = new DataGridViewCellStyle
        {
            BackColor = Color.FromArgb(248, 249, 251),
            SelectionBackColor = Color.FromArgb(224, 229, 235),
            SelectionForeColor = Color.FromArgb(25, 30, 35)
        };
        grid.DataBindingComplete += (_, _) =>
        {
            foreach (DataGridViewColumn c in grid.Columns)
                c.SortMode = DataGridViewColumnSortMode.NotSortable;
            grid.AutoResizeColumns(DataGridViewAutoSizeColumnsMode.DisplayedCells);
            foreach (DataGridViewColumn c in grid.Columns)
                if (c.Width > 320) c.Width = 320;
        };
    }

    private string DateSql(DateTime d) => d.ToString("yyyy-MM-dd");

    private void LoadData()
    {
        try
        {
            using var cn = Database.Open();
            using var cmd = cn.CreateCommand();
            var f = DateSql(from.Value);
            var t = DateSql(to.Value);

            cmd.CommandText = report.Text switch
            {
                "PRODUCTOS MÁS VENDIDOS" => "SELECT si.description AS Producto,ROUND(SUM(si.quantity),3) AS Cantidad,ROUND(SUM(si.total),2) AS Venta FROM sale_items si JOIN sales s ON s.id=si.sale_id WHERE date(s.created_at,'localtime') BETWEEN $f AND $t AND s.status='COMPLETED' GROUP BY si.product_id,si.description ORDER BY Cantidad DESC",
                "VENTAS POR USUARIO" => "SELECT COALESCE(u.full_name,'Sin usuario') AS Usuario,COUNT(*) AS Tickets,ROUND(SUM(s.total),2) AS Total FROM sales s LEFT JOIN users u ON u.id=s.user_id WHERE date(s.created_at,'localtime') BETWEEN $f AND $t AND s.status='COMPLETED' GROUP BY s.user_id ORDER BY Total DESC",
                "VENTAS POR CANAL" => "SELECT COALESCE(s.sale_channel,'SALÓN') AS Canal,COUNT(*) AS Tickets,ROUND(SUM(s.total),2) AS Total FROM sales s WHERE date(s.created_at,'localtime') BETWEEN $f AND $t AND s.status='COMPLETED' GROUP BY s.sale_channel ORDER BY Total DESC",
                "CAJA" => "SELECT movement_type AS Movimiento,payment_method AS Medio,COUNT(*) AS Cantidad,ROUND(SUM(amount),2) AS Total FROM cash_movements WHERE date(created_at,'localtime') BETWEEN $f AND $t AND COALESCE(voided,0)=0 GROUP BY movement_type,payment_method ORDER BY Total DESC",
                "DEVOLUCIONES" => "SELECT s.ticket_no AS Ticket,r.created_at AS [Fecha/Hora],COALESCE(si.description,'') AS Producto,r.quantity AS Cantidad,ROUND(r.amount,2) AS Importe,COALESCE(r.reason,'') AS Motivo,COALESCE(u.full_name,u.username,'') AS Usuario FROM sale_returns r JOIN sales s ON s.id=r.sale_id LEFT JOIN sale_items si ON si.id=r.sale_item_id LEFT JOIN users u ON u.id=r.user_id WHERE date(r.created_at,'localtime') BETWEEN $f AND $t ORDER BY r.id DESC",
                "MERMAS" => "SELECT substr(w.created_at,1,16) AS Fecha,COALESCE(p.description,'') AS Producto,ROUND(w.quantity,3) AS Cantidad,COALESCE(w.reason,'') AS Motivo,COALESCE(w.notes,'') AS Observación,COALESCE(u.full_name,u.username,'') AS Responsable FROM waste_records w JOIN products p ON p.id=w.product_id LEFT JOIN users u ON u.id=w.user_id WHERE date(w.created_at,'localtime') BETWEEN $f AND $t ORDER BY w.id DESC",
                "PROVEEDORES" => "SELECT s.name AS Proveedor,COUNT(po.id) AS Ordenes,ROUND(COALESCE(SUM(po.total),0),2) AS Compras,ROUND(COALESCE(SUM(si.total-si.paid),0),2) AS Deuda FROM suppliers s LEFT JOIN purchase_orders po ON po.supplier_id=s.id AND date(po.order_date,'localtime') BETWEEN $f AND $t LEFT JOIN supplier_invoices si ON si.supplier_id=s.id GROUP BY s.id,s.name HAVING Ordenes>0 OR Compras>0 OR Deuda>0 ORDER BY Compras DESC",
                "CLIENTES / DEUDAS" => "SELECT c.name AS Cliente,COALESCE(c.document,'') AS Documento,ROUND(COALESCE(SUM(CASE WHEN ca.entry_type='SALE' THEN ca.amount WHEN ca.entry_type='PAYMENT' THEN -ca.amount ELSE 0 END),0),2) AS Deuda,ROUND(COALESCE(SUM(CASE WHEN ca.entry_type='PAYMENT' AND date(ca.created_at,'localtime') BETWEEN $f AND $t THEN ca.amount ELSE 0 END),0),2) AS AbonosPeriodo FROM customers c LEFT JOIN customer_accounts ca ON ca.customer_id=c.id GROUP BY c.id,c.name,c.document HAVING Deuda<>0 OR AbonosPeriodo<>0 ORDER BY Deuda DESC",
                "STOCK BAJO" => "SELECT description AS Producto,stock AS Stock,min_stock AS Minimo,unit AS Unidad FROM products WHERE active=1 AND uses_inventory=1 AND stock<=min_stock AND $inv=1 ORDER BY stock",
                "COMPRAS" => "SELECT po.order_no AS Orden,s.name AS Proveedor,po.status AS Estado,ROUND(po.total,2) AS Total,ROUND(po.received_total,2) AS Recibido FROM purchase_orders po JOIN suppliers s ON s.id=po.supplier_id WHERE date(po.order_date,'localtime') BETWEEN $f AND $t ORDER BY po.id DESC",
                "VENTAS" => "SELECT s.ticket_no AS Ticket,s.created_at AS Fecha,COALESCE(u.full_name,'') AS Usuario,COALESCE(c.name,'Consumidor Final') AS Cliente,COALESCE(s.sale_channel,'SALÓN') AS Canal,ROUND(s.total,2) AS Total,s.payment_method AS Medio FROM sales s LEFT JOIN users u ON u.id=s.user_id LEFT JOIN customers c ON c.id=s.customer_id WHERE date(s.created_at,'localtime') BETWEEN $f AND $t AND s.status='COMPLETED' ORDER BY s.id DESC",
                _ => "SELECT 'RESUMEN' AS Tipo, COUNT(*) AS Tickets, ROUND(COALESCE(SUM(total),0),2) AS Total FROM sales WHERE date(created_at,'localtime') BETWEEN $f AND $t AND status='COMPLETED'"
            };

            cmd.Parameters.AddWithValue("$f", f);
            cmd.Parameters.AddWithValue("$t", t);
            cmd.Parameters.AddWithValue("$inv", InventoryControlService.IsGlobalEnabled ? 1 : 0);
            using var r = cmd.ExecuteReader();
            var dt = new System.Data.DataTable();
            dt.Load(r);
            grid.DataSource = dt;

            var total = Scalar(cn, "SELECT COALESCE(SUM(total),0) FROM sales WHERE date(created_at,'localtime') BETWEEN $f AND $t AND status='COMPLETED'", f, t);
            var tickets = Scalar(cn, "SELECT COUNT(*) FROM sales WHERE date(created_at,'localtime') BETWEEN $f AND $t AND status='COMPLETED'", f, t);
            var credits = Scalar(cn, "SELECT COALESCE(SUM(amount),0) FROM customer_accounts WHERE date(created_at,'localtime') BETWEEN $f AND $t AND entry_type='SALE'", f, t);
            var payments = Scalar(cn, "SELECT COALESCE(SUM(amount),0) FROM customer_accounts WHERE date(created_at,'localtime') BETWEEN $f AND $t AND entry_type='PAYMENT'", f, t);
            var waste = Scalar(cn, "SELECT COALESCE(SUM(quantity),0) FROM waste_records WHERE date(created_at,'localtime') BETWEEN $f AND $t", f, t);
            var purchases = Scalar(cn, "SELECT COALESCE(SUM(total),0) FROM purchase_orders WHERE date(order_date,'localtime') BETWEEN $f AND $t", f, t);

            summary.Text =
                $"PERÍODO: {from.Value:dd/MM/yyyy} → {to.Value:dd/MM/yyyy}    ·    TICKETS: {tickets:N0}    ·    VENTAS: ${total:N2}    ·    PROMEDIO: {(tickets > 0 ? total / tickets : 0):N2}\n" +
                $"CRÉDITOS OTORGADOS: ${credits:N2}    ·    ABONOS DE CLIENTES: ${payments:N2}    ·    MERMA: {waste:N3}    ·    COMPRAS: ${purchases:N2}";
        }
        catch (Exception ex)
        {
            summary.Text = "No se pudo cargar el reporte: " + ex.Message;
        }
    }

    private static double Scalar(Microsoft.Data.Sqlite.SqliteConnection cn, string sql, string f, string t)
    {
        using var c = cn.CreateCommand();
        c.CommandText = sql;
        c.Parameters.AddWithValue("$f", f);
        c.Parameters.AddWithValue("$t", t);
        return Convert.ToDouble(c.ExecuteScalar() ?? 0);
    }

    private void Export()
    {
        if (grid.DataSource is not System.Data.DataTable dt) return;
        using var sfd = new SaveFileDialog { Filter = "CSV|*.csv", FileName = $"FerrarisPOS_Reporte_{DateTime.Now:yyyyMMdd_HHmm}.csv" };
        if (sfd.ShowDialog(this) != DialogResult.OK) return;
        var sb = new StringBuilder();
        sb.AppendLine(string.Join(";", dt.Columns.Cast<System.Data.DataColumn>().Select(c => c.ColumnName)));
        foreach (System.Data.DataRow row in dt.Rows)
            sb.AppendLine(string.Join(";", row.ItemArray.Select(x => x?.ToString()?.Replace(";", ",") ?? "")));
        File.WriteAllText(sfd.FileName, sb.ToString(), Encoding.UTF8);
        MessageBox.Show("Reporte exportado correctamente.", "FerrarisPOS", MessageBoxButtons.OK, MessageBoxIcon.Information);
    }
}
