using System.Windows.Forms;

namespace FerrarisPOS.Forms;

/// <summary>
/// Label con repintado doble para evitar destellos al cambiar rápidamente el importe.
/// No altera su apariencia ni su tamaño.
/// </summary>
public sealed class FlickerFreeLabel : Label
{
    public FlickerFreeLabel()
    {
        SetStyle(ControlStyles.OptimizedDoubleBuffer |
                  ControlStyles.AllPaintingInWmPaint |
                  ControlStyles.UserPaint, true);
        DoubleBuffered = true;
    }
}
