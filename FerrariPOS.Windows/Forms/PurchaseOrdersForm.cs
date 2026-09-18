using FerrarisPOS.Data;
using FerrarisPOS.Services;

namespace FerrarisPOS.Forms;

public sealed class PurchaseOrdersForm : Form
{
    private readonly DataGridView grid = new();

    public PurchaseOrdersForm()
    {
        Text = "FerrarisPOS - Compras";
        Width = 1200;
        Height = 680;
        StartPosition = FormStartPosition.CenterParent;
        Build();
        LoadGrid();
        ThemeService.Apply(this);
    }

    private void Build()
    {
        Controls.Add(new Label
        {
            Text = "ÓRDENES DE COMPRA",
            Location = new Point(20, 18),
            AutoSize = true,
            Font = new Font("Segoe UI", 16, FontStyle.Bold)
        });

        var bNew = Btn("ARMAR PEDIDO", 20, 55, 145);
        bNew.Click += (_, _) => NewOrder();
        Controls.Add(bNew);

        var bReceive = Btn("RECIBIR MERCADERÍA", 175, 55, 170);
        bReceive.Click += (_, _) => ReceiveSelected(false);
        Controls.Add(bReceive);

        var bDifference = Btn("RECIBIDO CON DIFERENCIA", 355, 55, 190);
        bDifference.Click += (_, _) => ReceiveSelected(true);
        Controls.Add(bDifference);

        var bCancel = Btn("CANCELAR", 555, 55, 120);
        bCancel.Click += (_, _) => CancelSelected();
        Controls.Add(bCancel);

        var bRefresh = Btn("ACTUALIZAR", 685, 55, 120);
        bRefresh.Click += (_, _) => LoadGrid();
        Controls.Add(bRefresh);

        var bAccounts = Btn("CUENTAS", 815, 55, 110);
        bAccounts.Click += (_, _) =>
        {
            using var f = new SupplierAccountsForm();
            f.ShowDialog(this);
        };
        Controls.Add(bAccounts);

        var bInsights = Btn("SUGERIR COMPRAS", 935, 55, 145);
        bInsights.Click += (_, _) =>
        {
            using var f = new PurchaseInsightsForm();
            f.ShowDialog(this);
        };
        Controls.Add(bInsights);

        grid.Location = new Point(20, 105);
        grid.Size = new Size(1060, 500);
        grid.ReadOnly = true;
        grid.AllowUserToAddRows = false;
        grid.AllowUserToDeleteRows = false;
        grid.RowHeadersVisible = false;
        grid.SelectionMode = DataGridViewSelectionMode.FullRowSelect;
        grid.MultiSelect = false;
        grid.AutoGenerateColumns = false;
        grid.AutoSizeColumnsMode = DataGridViewAutoSizeColumnsMode.Fill;
        grid.DataError += (_, e) =>
        {
            e.ThrowException = false;
            e.Cancel = true;
        };

        grid.Columns.Add(new DataGridViewTextBoxColumn { Name = "ID", HeaderText = "ID", Visible = false });
        grid.Columns.Add(new DataGridViewTextBoxColumn { Name = "Orden", HeaderText = "ORDEN", FillWeight = 13 });
        grid.Columns.Add(new DataGridViewTextBoxColumn { Name = "Proveedor", HeaderText = "PROVEEDOR", FillWeight = 24 });
        grid.Columns.Add(new DataGridViewTextBoxColumn { Name = "Estado", HeaderText = "ESTADO", FillWeight = 13 });
        grid.Columns.Add(new DataGridViewTextBoxColumn { Name = "Fecha", HeaderText = "FECHA", FillWeight = 13 });
        grid.Columns.Add(new DataGridViewTextBoxColumn { Name = "Entrega", HeaderText = "ENTREGA", FillWeight = 13 });
        grid.Columns.Add(new DataGridViewTextBoxColumn { Name = "Total", HeaderText = "TOTAL", FillWeight = 12 });
        grid.Columns.Add(new DataGridViewTextBoxColumn { Name = "Recibido", HeaderText = "RECIBIDO", FillWeight = 12 });

        grid.CellDoubleClick += (_, e) =>
        {
            if (e.RowIndex >= 0)
                ReceiveSelected(false);
        };

        Controls.Add(grid);
    }

    private Button Btn(string text, int x, int y, int width) =>
        new()
        {
            Text = text,
            Location = new Point(x, y),
            Width = width,
            Height = 34
        };

    private void LoadGrid()
    {
        grid.Rows.Clear();

        using var cn = Database.Open();
        using var cmd = cn.CreateCommand();

        cmd.CommandText = @"
SELECT
    po.id,
    po.order_no,
    s.name,
    po.status,
    po.order_date,
    po.expected_date,
    ROUND(po.total,2),
    ROUND(COALESCE(po.received_total,0),2)
FROM purchase_orders po
JOIN suppliers s ON s.id=po.supplier_id
ORDER BY po.id DESC";

        using var r = cmd.ExecuteReader();

        while (r.Read())
        {
            var row = grid.Rows.Add(
                r.GetInt32(0),
                r.IsDBNull(1) ? "" : r.GetString(1),
                r.IsDBNull(2) ? "" : r.GetString(2),
                FormatStatus(r.IsDBNull(3) ? "" : r.GetString(3)),
                r.IsDBNull(4) ? "" : r.GetString(4),
                r.IsDBNull(5) ? "" : r.GetString(5),
                FormatMoney(r.GetDouble(6)),
                FormatMoney(r.GetDouble(7))
            );

            grid.Rows[row].Tag = r.GetInt32(0);
        }
    }

    private static string FormatStatus(string status) => status switch
    {
        "DRAFT" => "BORRADOR",
        "PARTIAL" => "PARCIAL",
        "RECEIVED" => "COMPLETA",
        "RECEIVED_WITH_DIFFERENCE" => "COMPLETA CON DIFERENCIA",
        "CANCELLED" => "CANCELADA",
        _ => status
    };

    private static string FormatMoney(double value) =>
        value.ToString("N2", System.Globalization.CultureInfo.CurrentCulture);

    private int SelectedId()
    {
        if (grid.CurrentRow?.Cells["ID"].Value is null)
            return 0;

        return int.TryParse(Convert.ToString(grid.CurrentRow.Cells["ID"].Value), out var id)
            ? id
            : 0;
    }

    private void NewOrder()
    {
        using var f = new PurchaseOrderForm();
        if (f.ShowDialog(this) == DialogResult.OK)
            LoadGrid();
    }

    private void ReceiveSelected(bool differenceMode)
    {
        var id = SelectedId();

        if (id == 0)
        {
            MessageBox.Show("Seleccioná una orden.", "Compras",
                MessageBoxButtons.OK, MessageBoxIcon.Warning);
            return;
        }

        using var cn = Database.Open();
        using var q = cn.CreateCommand();
        q.CommandText = "SELECT status FROM purchase_orders WHERE id=$id";
        q.Parameters.AddWithValue("$id", id);
        var status = Convert.ToString(q.ExecuteScalar()) ?? "";

        if (status == "CANCELLED")
        {
            MessageBox.Show("Esta orden está cancelada.", "Compras",
                MessageBoxButtons.OK, MessageBoxIcon.Warning);
            return;
        }

        if (status is "RECEIVED" or "RECEIVED_WITH_DIFFERENCE")
        {
            MessageBox.Show("Esta orden ya fue recibida completamente.", "Compras",
                MessageBoxButtons.OK, MessageBoxIcon.Information);
            return;
        }

        using var f = new PurchaseOrderForm(id, true, differenceMode);
        if (f.ShowDialog(this) == DialogResult.OK)
            LoadGrid();
    }

    private void CancelSelected()
    {
        var id = SelectedId();
        if (id == 0)
            return;

        using var cn = Database.Open();

        using var q = cn.CreateCommand();
        q.CommandText = "SELECT status FROM purchase_orders WHERE id=$id";
        q.Parameters.AddWithValue("$id", id);
        var status = Convert.ToString(q.ExecuteScalar()) ?? "";

        if (status is "RECEIVED" or "CANCELLED")
        {
            MessageBox.Show("Esta orden no se puede cancelar.", "Compras",
                MessageBoxButtons.OK, MessageBoxIcon.Warning);
            return;
        }

        if (MessageBox.Show(
            "¿Cancelar la orden seleccionada?",
            "Compras",
            MessageBoxButtons.YesNo,
            MessageBoxIcon.Question) != DialogResult.Yes)
            return;

        using var c = cn.CreateCommand();
        c.CommandText = @"
UPDATE purchase_orders
SET status='CANCELLED',
    updated_at=CURRENT_TIMESTAMP
WHERE id=$id";
        c.Parameters.AddWithValue("$id", id);
        c.ExecuteNonQuery();

        LoadGrid();
    }
}
