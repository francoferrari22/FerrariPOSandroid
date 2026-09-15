using System.Drawing;
using FerrarisPOS.Services;
using QRCoder;

namespace FerrarisPOS.Forms;

public sealed class MobilePairingForm : Form
{
    private readonly PictureBox qr = new();
    private readonly TextBox payload = new();
    private readonly TextBox code = new();

    public MobilePairingForm()
    {
        Text = "FerrariPOS Manager · Vincular teléfono";
        Width = 650; Height = 760;
        StartPosition = FormStartPosition.CenterParent;
        BackColor = Color.FromArgb(18,22,29);
        ForeColor = Color.White;
        FormBorderStyle = FormBorderStyle.FixedDialog;
        MaximizeBox = false;
        MinimizeBox = false;
        Build();
        Generate();
    }

    private void Build()
    {
        Controls.Add(new Label {
            Text="VINCULAR FERRARIPOS MANAGER",
            Location=new Point(30,25), AutoSize=true,
            Font=new Font("Segoe UI",18,FontStyle.Bold), ForeColor=Color.White
        });
        Controls.Add(new Label {
            Text="En el teléfono abrí FerrariPOS Manager → Vincular FerrariPOS y escaneá este QR.",
            Location=new Point(30,68), Size=new Size(570,45),
            Font=new Font("Segoe UI",10), ForeColor=Color.Gainsboro
        });
        qr.Location=new Point(145,125); qr.Size=new Size(360,360);
        qr.SizeMode=PictureBoxSizeMode.Zoom; qr.BackColor=Color.White;
        Controls.Add(qr);

        Controls.Add(new Label {
            Text="DATOS DE VINCULACIÓN (también podés copiarlos)",
            Location=new Point(30,510), AutoSize=true,
            Font=new Font("Segoe UI",9,FontStyle.Bold)
        });
        payload.Location=new Point(30,540); payload.Size=new Size(470,70);
        payload.Multiline=true; payload.ReadOnly=true; payload.ScrollBars=ScrollBars.Vertical;
        payload.BackColor=Color.FromArgb(30,36,45); payload.ForeColor=Color.White;
        Controls.Add(payload);

        var copy=new Button {Text="COPIAR DATOS",Location=new Point(515,540),Width=110,Height=35};
        copy.Click += (_,_) => { Clipboard.SetText(payload.Text); MessageBox.Show("Datos de vinculación copiados.","FerrariPOS Manager",MessageBoxButtons.OK,MessageBoxIcon.Information); };
        Controls.Add(copy);

        Controls.Add(new Label { Text="CÓDIGO ALTERNATIVO PARA VINCULAR", Location=new Point(30,625), AutoSize=true, Font=new Font("Segoe UI",9,FontStyle.Bold) });
        code.Location=new Point(30,650); code.Size=new Size(470,45); code.ReadOnly=true; code.Multiline=true; code.ScrollBars=ScrollBars.Horizontal;
        code.BackColor=Color.FromArgb(30,36,45); code.ForeColor=Color.White;
        Controls.Add(code);
        var copyCode=new Button {Text="COPIAR CÓDIGO",Location=new Point(515,650),Width=110,Height=38};
        copyCode.Click += (_,_) => { Clipboard.SetText(code.Text); MessageBox.Show("Código de vinculación copiado.","FerrariPOS Manager",MessageBoxButtons.OK,MessageBoxIcon.Information); };
        Controls.Add(copyCode);

        var close=new Button {Text="CERRAR",Location=new Point(485,700),Width=140,Height=38};
        close.Click += (_,_) => Close();
        Controls.Add(close);
    }

    private void Generate()
    {
        try
        {
            var text=WebDashboardServer.GetMobilePairingPayload();
            payload.Text=text;
            code.Text=WebDashboardServer.GetMobilePairingCode();
            using var generator=new QRCodeGenerator();
            using var data=generator.CreateQrCode(text, QRCodeGenerator.ECCLevel.Q);
            var png=new PngByteQRCode(data).GetGraphic(10);
            using var ms=new MemoryStream(png);
            using var tmp=Image.FromStream(ms);
            qr.Image=new Bitmap(tmp);
        }
        catch(Exception ex)
        {
            payload.Text="";
            MessageBox.Show("No se pudo generar el QR de vinculación.\n\n"+ex.Message,
                "FerrariPOS Manager",MessageBoxButtons.OK,MessageBoxIcon.Warning);
        }
    }

    protected override void Dispose(bool disposing)
    {
        if (disposing) qr.Image?.Dispose();
        base.Dispose(disposing);
    }
}
