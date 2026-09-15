using FerrarisPOS.Models;
using FerrarisPOS.Services;
using System.Globalization;

namespace FerrarisPOS.Forms;

public class CustomersForm : Form
{
    private readonly DataGridView grid = new();
    private readonly Label accountInfo = new();
    private TextBox name = new(), doc = new(), phone = new(), email = new(), address = new(), limit = new();
    private int id;
    private readonly ContextMenuStrip debtMenu = new();

    public CustomersForm()
    {
        Text = "FerrarisPOS - Clientes y Cuentas Corrientes";
        Width = 1400; Height = 780; MinimumSize = new Size(1120, 680);
        AutoScroll = true;
        BackColor = Color.Gainsboro;
        Build();
        LoadGrid();
        Activated += (_, _) => LoadGrid();
    }

    private TextBox F(string label, int y, string hint)
    {
        Controls.Add(new Label
        {
            Text = label, Location = new Point(20, y + 5), AutoSize = true,
            Font = new Font("Segoe UI", 9, FontStyle.Bold)
        });
        var t = new TextBox
        {
            Location = new Point(210, y), Width = 300,
            BackColor = Color.White, PlaceholderText = hint
        };
        Controls.Add(t);
        return t;
    }

    private void Build()
    {
        Controls.Add(new Label
        {
            Text = "CLIENTES / CUENTAS CORRIENTES",
            Location = new Point(20, 15), AutoSize = true,
            Font = new Font("Segoe UI", 16, FontStyle.Bold)
        });

        debtMenu.Items.Add("Ver estado y detalle de deuda", null, (_, _) => ShowDebtDetails());
        debtMenu.Items.Add("Imprimir estado de deuda", null, (_, _) => PrintDebtDetails());

        name = F("NOMBRE / RAZÓN SOCIAL", 55, "Nombre del cliente");
        doc = F("DOCUMENTO / CUIT", 100, "DNI/CUIT");
        phone = F("TELÉFONO", 145, "Teléfono");
        email = F("EMAIL", 190, "Correo");
        address = F("DIRECCIÓN", 235, "Domicilio");
        limit = F("LÍMITE DE CRÉDITO", 280, "0 = crédito infinito");

        // Barra de acciones inferior: el botón de estado de deuda queda separado del
        // título para evitar la superposición visual que se producía en F2.
        var save = new Button { Text = "GUARDAR", Location = new Point(20, 365), Width = 145, Height = 40 };
        save.Click += (_, _) => Save(); Controls.Add(save);

        var n = new Button { Text = "NUEVO", Location = new Point(180, 365), Width = 145, Height = 40 };
        n.Click += (_, _) => Clear(); Controls.Add(n);

        var debtDetails = new Button
        {
            Text = "ESTADO / DETALLE",
            Location = new Point(340, 365), Width = 145, Height = 40,
            Font = new Font("Segoe UI", 8.5f, FontStyle.Bold)
        };
        debtDetails.Click += (_, _) => ShowDebtDetails();
        Controls.Add(debtDetails);

        var pay = new Button { Text = "ABONAR DEUDA", Location = new Point(20, 420), Width = 145, Height = 40 };
        pay.Click += (_, _) => RegisterPayment(); Controls.Add(pay);

        var del = new Button { Text = "DESACTIVAR", Location = new Point(180, 420), Width = 145, Height = 40 };
        del.Click += (_, _) => Delete(); Controls.Add(del);

        var export = new Button { Text = "EXPORTAR DEUDAS A EXCEL", Location = new Point(340, 420), Width = 145, Height = 40, Font = new Font("Segoe UI", 8.0f, FontStyle.Bold) };
        export.Click += (_, _) => ExportDebtDetails(); Controls.Add(export);

        accountInfo.Location = new Point(20, 485);
        accountInfo.Size = new Size(480, 120);
        accountInfo.Font = new Font("Segoe UI", 11, FontStyle.Bold);
        accountInfo.ForeColor = Color.Navy;
        Controls.Add(accountInfo);

        grid.Location = new Point(520, 30);
        grid.Size = new Size(840, 680);
        grid.Anchor = AnchorStyles.Top | AnchorStyles.Bottom | AnchorStyles.Left | AnchorStyles.Right;
        grid.ReadOnly = true;
        grid.AutoGenerateColumns = false;
        grid.AllowUserToAddRows = false;
        grid.BackgroundColor = Color.White;
        grid.BorderStyle = BorderStyle.FixedSingle;
        grid.SelectionMode = DataGridViewSelectionMode.FullRowSelect;
        grid.MultiSelect = false;
        grid.RowHeadersVisible = false;
        grid.Font = new Font("Segoe UI", 10);
        grid.ColumnHeadersHeight = 42;
        grid.RowTemplate.Height = 36;
        grid.EnableHeadersVisualStyles = false;
        grid.ColumnHeadersDefaultCellStyle = new DataGridViewCellStyle
        {
            BackColor = Color.LightSteelBlue,
            ForeColor = Color.Black,
            Font = new Font("Segoe UI", 9, FontStyle.Bold),
            Alignment = DataGridViewContentAlignment.MiddleCenter
        };
        grid.SelectionChanged += Selected;
        grid.CellDoubleClick += (_, e) => { if (e.RowIndex >= 0) ShowDebtDetails(); };
        grid.KeyDown += (_, e) =>
        {
            if (e.KeyCode == Keys.Enter)
            {
                e.Handled = true;
                e.SuppressKeyPress = true;
                ShowDebtDetails();
            }
        };
        grid.ContextMenuStrip = debtMenu;

        grid.Columns.Add(new DataGridViewTextBoxColumn
        {
            Name = "Name", HeaderText = "CLIENTE", DataPropertyName = "Name", Width = 230
        });
        grid.Columns.Add(new DataGridViewTextBoxColumn
        {
            Name = "Limit", HeaderText = "LÍMITE", DataPropertyName = "CreditLimit", Width = 115,
            DefaultCellStyle = new DataGridViewCellStyle { Format = "$ #,##0.00", Alignment = DataGridViewContentAlignment.MiddleRight }
        });
        grid.Columns.Add(new DataGridViewTextBoxColumn
        {
            Name = "Balance", HeaderText = "DEUDA ACTUAL", DataPropertyName = "BalanceText", Width = 125,
            DefaultCellStyle = new DataGridViewCellStyle { Alignment = DataGridViewContentAlignment.MiddleRight }
        });
        grid.Columns.Add(new DataGridViewTextBoxColumn
        {
            Name = "LastCredit", HeaderText = "ÚLTIMO CRÉDITO", DataPropertyName = "LastCreditText", Width = 130,
            DefaultCellStyle = new DataGridViewCellStyle { Alignment = DataGridViewContentAlignment.MiddleRight }
        });
        grid.Columns.Add(new DataGridViewTextBoxColumn
        {
            Name = "Available", HeaderText = "CRÉDITO DISPONIBLE", DataPropertyName = "AvailableText", Width = 155,
            DefaultCellStyle = new DataGridViewCellStyle { Alignment = DataGridViewContentAlignment.MiddleRight }
        });
        Controls.Add(grid);

        Controls.Add(new Label
        {
            Text = "IMPORTANTE: DEUDA ACTUAL, ÚLTIMO CRÉDITO y CRÉDITO DISPONIBLE se leen directamente de la cuenta corriente guardada en SQLite.",
            Location = new Point(20, 620), Size = new Size(480, 70),
            ForeColor = Color.DimGray
        });
    }

    private void LoadGrid()
    {
        var rows = CustomerService.AccountSummaries();
        grid.DataSource = rows;

        if (id > 0)
        {
            var index = rows.FindIndex(x => x.Customer.Id == id);
            if (index >= 0 && index < grid.Rows.Count)
                grid.Rows[index].Selected = true;
        }
    }

    private void Selected(object? s, EventArgs e)
    {
        if (grid.CurrentRow?.DataBoundItem is CustomerAccountSummary row)
        {
            var c = row.Customer;
            id = c.Id;
            name.Text = c.Name;
            doc.Text = c.Document;
            phone.Text = c.Phone;
            email.Text = c.Email;
            address.Text = c.Address;
            limit.Text = c.CreditLimit.ToString("N2");

            accountInfo.Text =
                $"CLIENTE: {c.Name}\n" +
                $"DEUDA ACTUAL: {row.BalanceText}\n" +
                $"ÚLTIMO CRÉDITO: {row.LastCreditText}\n" +
                $"CRÉDITO DISPONIBLE: {row.AvailableText}";
        }
    }

    private void Clear()
    {
        id = 0;
        name.Clear(); doc.Clear(); phone.Clear(); email.Clear(); address.Clear(); limit.Clear();
        accountInfo.Text = "";
        name.Focus();
    }

    private static double Num(TextBox t) =>
        double.TryParse(t.Text.Replace(',', '.'), NumberStyles.Any,
            CultureInfo.InvariantCulture, out var v) ? v : 0;

    private void Save()
    {
        if (string.IsNullOrWhiteSpace(name.Text))
        {
            MessageBox.Show("Ingresá el nombre del cliente.");
            return;
        }

        try
        {
            var value = Num(limit);
            if (value < 0)
            {
                MessageBox.Show("El límite no puede ser negativo.");
                return;
            }

            CustomerService.Save(new Customer(
                id, name.Text.Trim(), doc.Text.Trim(), phone.Text.Trim(),
                email.Text.Trim(), address.Text.Trim(), value, true));

            MessageBox.Show(value == 0
                ? "Cliente guardado con crédito INFINITO."
                : "Cliente guardado correctamente.");

            LoadGrid();
            Clear();
        }
        catch (Exception ex)
        {
            MessageBox.Show(ex.Message);
        }
    }

    private void RegisterPayment()
    {
        // Nunca confiar solamente en el id que quedó del último SelectionChanged:
        // al refrescar/filtrar la grilla WinForms puede mover CurrentRow al primer cliente.
        var selected = grid.CurrentRow?.DataBoundItem as CustomerAccountSummary;
        var selectedId = selected?.Customer.Id ?? id;
        if (selectedId <= 1)
        {
            MessageBox.Show("Seleccioná un cliente distinto de Público General.");
            return;
        }

        id = selectedId;
        var balance = CustomerService.Balance(selectedId);
        if (balance <= 0)
        {
            MessageBox.Show("Este cliente no tiene deuda pendiente.");
            return;
        }

        using var f = new CustomerPaymentForm(balance);
        if (f.ShowDialog(this) != DialogResult.OK) return;

        try
        {
            CustomerService.RegisterPayment(selectedId, f.Payments, Session.UserId, $"Pago de cuenta corriente - {selected?.Customer.Name ?? "cliente"}");
            MessageBox.Show(
                $"Pago registrado correctamente.\nTotal abonado: ${f.Payments.Sum(x => x.Amount):N2}",
                "Cuenta corriente", MessageBoxButtons.OK, MessageBoxIcon.Information);
            LoadGrid();
            SelectCustomerRow(selectedId);
        }
        catch (Exception ex)
        {
            MessageBox.Show(ex.Message, "No se pudo registrar el pago",
                MessageBoxButtons.OK, MessageBoxIcon.Error);
        }
    }

    private void SelectCustomerRow(int customerId)
    {
        for (int i = 0; i < grid.Rows.Count; i++)
        {
            if (grid.Rows[i].DataBoundItem is CustomerAccountSummary row && row.Customer.Id == customerId)
            {
                grid.ClearSelection();
                grid.CurrentCell = grid.Rows[i].Cells[0];
                grid.Rows[i].Selected = true;
                id = customerId;
                Selected(null, EventArgs.Empty);
                return;
            }
        }
    }


    private CustomerAccountSummary? GetSelectedCustomer()
        => grid.CurrentRow?.DataBoundItem as CustomerAccountSummary;

    private void ShowDebtDetails()
    {
        var selected = GetSelectedCustomer();
        if (selected == null || selected.Customer.Id <= 1)
        {
            MessageBox.Show("Seleccioná un cliente guardado para ver su estado de cuenta.",
                "Estado de deuda", MessageBoxButtons.OK, MessageBoxIcon.Information);
            return;
        }

        using var form = new CustomerDebtDetailsForm(selected.Customer, selected.Balance);
        form.ShowDialog(this);
    }

    private void PrintDebtDetails()
    {
        var selected = GetSelectedCustomer();
        if (selected == null || selected.Customer.Id <= 1)
        {
            MessageBox.Show("Seleccioná un cliente guardado para imprimir su estado de deuda.",
                "Estado de deuda", MessageBoxButtons.OK, MessageBoxIcon.Information);
            return;
        }

        using var form = new CustomerDebtDetailsForm(selected.Customer, selected.Balance);
        form.PrintState();
    }

    private void ExportDebtDetails()
    {
        try
        {
            var path = ExcelExportService.ExportCustomerDebtDetails();
            if (!string.IsNullOrWhiteSpace(path))
            {
                MessageBox.Show(
                    $"Estado de deuda exportado correctamente.\n\nArchivo:\n{path}",
                    "Exportar deudas", MessageBoxButtons.OK, MessageBoxIcon.Information);
            }
        }
        catch (Exception ex)
        {
            MessageBox.Show(
                "No se pudo exportar el estado de deuda a Excel:\n\n" + ex.Message,
                "Exportar deudas", MessageBoxButtons.OK, MessageBoxIcon.Error);
        }
    }

    private void Delete()
    {
        if (id <= 1) return;
        if (MessageBox.Show("¿Desactivar cliente?", "Confirmar",
            MessageBoxButtons.YesNo) == DialogResult.Yes)
        {
            CustomerService.Delete(id);
            LoadGrid();
            Clear();
        }
    }
}


internal sealed class CustomerDebtDetailsForm : Form
{
    private readonly Customer customer;
    private readonly double currentBalance;
    private readonly DataGridView grid = new();
    private readonly Label summary = new();
    private List<CustomerDebtDetail> details = new();

    public CustomerDebtDetailsForm(Customer customer, double currentBalance)
    {
        this.customer = customer;
        this.currentBalance = currentBalance;
        Text = $"Estado de cuenta - {customer.Name}";
        Width = 1120; Height = 720;
        StartPosition = FormStartPosition.CenterParent;
        BackColor = Color.Gainsboro;
        Build();
        LoadDetails();
    }

    private void Build()
    {
        Controls.Add(new Label
        {
            Text = "ESTADO Y DETALLE DE CUENTA CORRIENTE",
            Location = new Point(20, 15), AutoSize = true,
            Font = new Font("Segoe UI", 16, FontStyle.Bold)
        });
        Controls.Add(new Label
        {
            Text = $"Cliente: {customer.Name}",
            Location = new Point(20, 55), AutoSize = true,
            Font = new Font("Segoe UI", 11, FontStyle.Bold)
        });
        summary.Location = new Point(20, 82);
        summary.Size = new Size(1050, 55);
        summary.Font = new Font("Segoe UI", 10, FontStyle.Bold);
        summary.ForeColor = Color.Navy;
        Controls.Add(summary);

        grid.Location = new Point(20, 150);
        grid.Size = new Size(1050, 440);
        grid.ReadOnly = true; grid.AllowUserToAddRows = false;
        grid.AutoGenerateColumns = false;
        grid.SelectionMode = DataGridViewSelectionMode.FullRowSelect;
        grid.MultiSelect = false; grid.RowHeadersVisible = false;
        grid.BackgroundColor = Color.White;
        grid.BorderStyle = BorderStyle.FixedSingle;
        grid.Font = new Font("Segoe UI", 9);
        grid.RowTemplate.Height = 52; grid.ColumnHeadersHeight = 40;
        grid.EnableHeadersVisualStyles = false;
        grid.ColumnHeadersDefaultCellStyle = new DataGridViewCellStyle
        {
            BackColor = Color.LightSteelBlue, ForeColor = Color.Black,
            Font = new Font("Segoe UI", 9, FontStyle.Bold),
            Alignment = DataGridViewContentAlignment.MiddleCenter
        };

        AddTextColumn("Date", "FECHA / HORA", "DateTimeText", 145);
        AddTextColumn("Type", "TIPO", "TypeText", 125);
        AddTextColumn("Ref", "REFERENCIA", "ReferenceText", 115);
        AddTextColumn("Concept", "MOTIVO / CONCEPTO", "Concept", 220);
        AddTextColumn("Payment", "MEDIO", "PaymentMethod", 100);
        AddTextColumn("Amount", "IMPORTE", "AmountText", 115);
        AddTextColumn("Products", "PRODUCTOS", "Products", 215);
        Controls.Add(grid);

        var print = new Button
        {
            Text = "IMPRIMIR ESTADO DE DEUDA",
            Location = new Point(20, 610), Width = 245, Height = 45
        };
        print.Click += (_, _) => PrintState();
        Controls.Add(print);

        var close = new Button
        {
            Text = "CERRAR", Location = new Point(280, 610),
            Width = 150, Height = 45, DialogResult = DialogResult.Cancel
        };
        Controls.Add(close);
        CancelButton = close;
    }

    private void AddTextColumn(string name, string header, string property, int width)
    {
        grid.Columns.Add(new DataGridViewTextBoxColumn
        {
            Name = name, HeaderText = header, DataPropertyName = property, Width = width,
            DefaultCellStyle = new DataGridViewCellStyle
            {
                Alignment = name == "Amount"
                    ? DataGridViewContentAlignment.MiddleRight
                    : DataGridViewContentAlignment.MiddleLeft,
                WrapMode = DataGridViewTriState.True
            }
        });
    }

    private void LoadDetails()
    {
        details = CustomerService.DebtDetails(customer.Id);
        grid.DataSource = details;
        var credits = details.Where(x => x.IsSale).Sum(x => x.Amount);
        var payments = details.Where(x => !x.IsSale).Sum(x => x.Amount);
        summary.Text =
            $"DEUDA ACTUAL: $ {currentBalance:N2} | CRÉDITOS: $ {credits:N2} | PAGOS: $ {payments:N2} | MOVIMIENTOS: {details.Count}";
    }

    public void PrintState()
    {
        var printDoc = new System.Drawing.Printing.PrintDocument();
        printDoc.DocumentName = $"Estado de deuda - {customer.Name}";
        PrintConfigurationService.ApplyTo(printDoc);

        var printRows = details.Count > 0 ? details : CustomerService.DebtDetails(customer.Id);
        var lines = new List<string>();

        foreach (var d in printRows)
        {
            lines.Add($"{d.DateTimeText} | {d.TypeText} | {d.ReferenceText} | {d.Concept} | {d.AmountText}");
            if (!string.IsNullOrWhiteSpace(d.Products))
            {
                foreach (var product in d.Products.Split(
                    new[] { Environment.NewLine },
                    StringSplitOptions.RemoveEmptyEntries))
                    lines.Add("    • " + product);
            }
            lines.Add("────────────────────────────────────────────────────────────────────────");
        }

        var lineIndex = 0;
        printDoc.PrintPage += (_, e) =>
        {
            using var titleFont = new Font("Segoe UI", 15, FontStyle.Bold);
            using var headFont = new Font("Segoe UI", 10, FontStyle.Bold);
            using var bodyFont = new Font("Segoe UI", 8);
            using var smallFont = new Font("Segoe UI", 7);

            float x = e.MarginBounds.Left;
            float y = e.MarginBounds.Top;

            e.Graphics.DrawString("FERRARI'S PUNTO DE VENTA", titleFont, Brushes.Black, x, y);
            y += 30;
            e.Graphics.DrawString("ESTADO DE CUENTA CORRIENTE", headFont, Brushes.Black, x, y);
            y += 22;
            e.Graphics.DrawString($"Cliente: {customer.Name}", bodyFont, Brushes.Black, x, y);
            y += 18;
            e.Graphics.DrawString(
                $"Documento: {customer.Document}    Teléfono: {customer.Phone}",
                smallFont, Brushes.Black, x, y);
            y += 18;
            e.Graphics.DrawString($"Deuda actual: $ {currentBalance:N2}", headFont, Brushes.Black, x, y);
            y += 28;

            const float lineHeight = 16;
            while (lineIndex < lines.Count && y <= e.MarginBounds.Bottom - lineHeight)
            {
                e.Graphics.DrawString(lines[lineIndex], smallFont, Brushes.Black, x, y);
                y += lineHeight;
                lineIndex++;
            }

            if (lineIndex < lines.Count)
            {
                e.HasMorePages = true;
                return;
            }

            e.Graphics.DrawString(
                $"Generado: {DateTime.Now:dd/MM/yyyy HH:mm}",
                smallFont, Brushes.Black, x, Math.Min(y + 10, e.MarginBounds.Bottom - 10));
            e.HasMorePages = false;
        };

        if (PrintConfigurationService.PreviewEnabled)
        {
            using var preview = new PrintPreviewDialog
            {
                Document = printDoc,
                Width = 1000,
                Height = 700
            };
            preview.ShowDialog(this);
        }
        else
        {
            try
            {
                printDoc.Print();
            }
            catch (Exception ex)
            {
                MessageBox.Show(
                    "No se pudo imprimir el estado de deuda:\n\n" + ex.Message,
                    "Impresión", MessageBoxButtons.OK, MessageBoxIcon.Error);
            }
        }
        printDoc.Dispose();
    }
}

internal sealed class CustomerPaymentForm : Form
{
    private readonly double balance;
    private readonly SafeComboBox method = new();
    private NumericUpDown amount = new(), cash = new(), card = new(), transfer = new(), mercadoPago = new();
    private readonly Label info = new(), amountLabel = new();

    public List<PaymentLine> Payments { get; private set; } = new();

    public CustomerPaymentForm(double balance)
    {
        this.balance = balance;
        Text = "Abonar cuenta corriente";
        Width = 600; Height = 500;
        MinimumSize = new Size(600, 500);
        Build();
        UpdateView();
    }

    private NumericUpDown MoneyBox(int x, int y, int width = 250)
    {
        var box = new NumericUpDown
        {
            Location = new Point(x, y),
            Width = width,
            DecimalPlaces = 2,
            Maximum = (decimal)balance,
            Minimum = 0,
            ThousandsSeparator = true
        };
        box.ValueChanged += (_, _) => UpdateView();
        Controls.Add(box);
        return box;
    }

    private void Build()
    {
        Controls.Add(new Label
        {
            Text = $"DEUDA PENDIENTE: ${balance:N2}",
            Location = new Point(20, 20), AutoSize = true,
            Font = new Font("Segoe UI", 16, FontStyle.Bold)
        });

        Controls.Add(new Label
        {
            Text = "FORMA DE PAGO", Location = new Point(20, 75),
            AutoSize = true, Font = new Font("Segoe UI", 9, FontStyle.Bold)
        });

        method.Location = new Point(20, 100);
        method.Width = 540;
        method.DropDownStyle = ComboBoxStyle.DropDownList;
        method.Items.AddRange(new object[] { "EFECTIVO", "MERCADO PAGO", "TARJETA", "TRANSFERENCIA", "MIXTO" });
        if (method.Items.Count > 0) method.SelectedIndex = 0;
        method.SelectedIndexChanged += (_, _) => UpdateView();
        Controls.Add(method);

        amountLabel.Text = "IMPORTE";
        amountLabel.Location = new Point(20, 150);
        amountLabel.AutoSize = true;
        Controls.Add(amountLabel);
        amount = MoneyBox(20, 175, 540);
        amount.Value = (decimal)balance;

        Controls.Add(new Label { Text = "EFECTIVO", Location = new Point(20, 150), AutoSize = true });
        cash = MoneyBox(20, 175, 250);
        Controls.Add(new Label { Text = "MERCADO PAGO", Location = new Point(290, 150), AutoSize = true });
        mercadoPago = MoneyBox(290, 175, 270);

        Controls.Add(new Label { Text = "TARJETA", Location = new Point(20, 230), AutoSize = true });
        card = MoneyBox(20, 255, 250);
        Controls.Add(new Label { Text = "TRANSFERENCIA", Location = new Point(290, 230), AutoSize = true });
        transfer = MoneyBox(290, 255, 270);

        info.Location = new Point(20, 315);
        info.Size = new Size(540, 55);
        info.Font = new Font("Segoe UI", 9, FontStyle.Bold);
        info.ForeColor = Color.DimGray;
        Controls.Add(info);

        var ok = new Button
        {
            Text = "REGISTRAR PAGO",
            Location = new Point(20, 385), Width = 250, Height = 50
        };
        ok.Click += (_, _) => Accept();
        Controls.Add(ok);

        var cancel = new Button
        {
            Text = "CANCELAR",
            Location = new Point(290, 385), Width = 270, Height = 50,
            DialogResult = DialogResult.Cancel
        };
        Controls.Add(cancel);
        CancelButton = cancel;
    }

    private void SetMixedVisibility(bool mixed)
    {
        // Labels y cajas se distinguen por su posición; las cajas se ocultan cuando no corresponde.
        cash.Visible = mixed;
        mercadoPago.Visible = mixed;
        card.Visible = mixed;
        transfer.Visible = mixed;

        foreach (Control c in Controls)
        {
            if (c is Label l && (l.Text is "EFECTIVO" or "MERCADO PAGO" or "TARJETA" or "TRANSFERENCIA"))
                l.Visible = mixed;
        }
    }

    private void UpdateView()
    {
        bool mixed = method.Text == "MIXTO";
        amount.Visible = !mixed;
        amountLabel.Visible = !mixed;
        SetMixedVisibility(mixed);

        if (!mixed)
        {
            info.Text = method.Text is "TARJETA" or "TRANSFERENCIA"
                ? "Este pago se registra en la cuenta del cliente y también suma a la CAJA PARALELA DE MERCADO PAGO."
                : method.Text == "MERCADO PAGO"
                    ? "Este pago suma directamente a la CAJA PARALELA DE MERCADO PAGO."
                    : "Este pago de efectivo suma a la caja física del turno.";
        }
        else
        {
            var total = cash.Value + mercadoPago.Value + card.Value + transfer.Value;
            info.Text = $"TOTAL: ${total:N2} · EFECTIVO: ${cash.Value:N2} · DIGITAL A MP: ${(mercadoPago.Value + card.Value + transfer.Value):N2}";
        }
    }

    private void Accept()
    {
        Payments = new List<PaymentLine>();

        if (method.Text == "MIXTO")
        {
            var total = cash.Value + mercadoPago.Value + card.Value + transfer.Value;
            if (total <= 0 || total > (decimal)balance + 0.01m)
            {
                MessageBox.Show($"El pago mixto debe ser mayor a cero y no superar la deuda.\nDeuda: ${balance:N2}");
                return;
            }

            if (cash.Value > 0) Payments.Add(new PaymentLine("EFECTIVO", (double)cash.Value, ""));
            if (mercadoPago.Value > 0) Payments.Add(new PaymentLine("MERCADO PAGO", (double)mercadoPago.Value, ""));
            if (card.Value > 0) Payments.Add(new PaymentLine("TARJETA", (double)card.Value, ""));
            if (transfer.Value > 0) Payments.Add(new PaymentLine("TRANSFERENCIA", (double)transfer.Value, ""));
        }
        else
        {
            if (amount.Value <= 0)
            {
                MessageBox.Show("Ingresá un importe.");
                return;
            }

            Payments.Add(new PaymentLine(method.Text, (double)amount.Value, ""));
        }

        DialogResult = DialogResult.OK;
        Close();
    }
}
