using System.Globalization;
using System.Windows;
using System.Windows.Controls;

namespace FlowlyDesktop;

/// <summary>Utilidades de interfaz y formato.</summary>
public static class Ui
{
    public static readonly CultureInfo Es = new("es-ES");

    public static void Error(string mensaje) =>
        MessageBox.Show(mensaje, "Flowly", MessageBoxButton.OK, MessageBoxImage.Warning);

    public static void Info(string mensaje) =>
        MessageBox.Show(mensaje, "Flowly", MessageBoxButton.OK, MessageBoxImage.Information);

    public static bool Confirmar(string mensaje) =>
        MessageBox.Show(mensaje, "Flowly", MessageBoxButton.YesNo, MessageBoxImage.Question) == MessageBoxResult.Yes;

    public static string Horas(decimal? v) => $"{(v ?? 0).ToString("0.##", Es)} h";
    public static string Euros(decimal? v) => $"{(v ?? 0).ToString("0.##", Es)} €";
    public static string Fecha(DateOnly? f) => f?.ToString("dd/MM/yyyy", Es) ?? "—";

    public static string EstadoTarea(string estado) => estado switch
    {
        "PENDIENTE" => "Pendiente",
        "EN_CURSO" => "En curso",
        "FINALIZADA" => "Finalizada",
        _ => estado
    };

    /// <summary>Convierte "4", "4,5" o "4.5" en decimal.</summary>
    public static bool TryDecimal(string? texto, out decimal valor) =>
        decimal.TryParse((texto ?? "").Trim().Replace(',', '.'),
                         NumberStyles.Number, CultureInfo.InvariantCulture, out valor);

    public static DateOnly? ADateOnly(DateTime? f) => f.HasValue ? DateOnly.FromDateTime(f.Value) : null;
    public static DateTime? ADateTime(DateOnly? f) => f?.ToDateTime(TimeOnly.MinValue);

    /// <summary>Ventana pequeña para pedir una contraseña nueva.</summary>
    public static string? PedirPassword(string titulo, string texto)
    {
        var caja = new PasswordBox { Margin = new Thickness(0, 8, 0, 14), Padding = new Thickness(8, 7, 8, 7) };
        string? resultado = null;
        var ventana = new Window
        {
            Title = titulo,
            Width = 380,
            SizeToContent = SizeToContent.Height,
            ResizeMode = ResizeMode.NoResize,
            WindowStartupLocation = WindowStartupLocation.CenterOwner,
            Owner = Application.Current.MainWindow
        };
        var ok = new Button { Content = "Guardar", Padding = new Thickness(16, 7, 16, 7), IsDefault = true,
                              Margin = new Thickness(0, 0, 8, 0) };
        var cancelar = new Button { Content = "Cancelar", Padding = new Thickness(16, 7, 16, 7), IsCancel = true };
        ok.Click += (_, _) => { resultado = caja.Password; ventana.DialogResult = true; };
        var botones = new StackPanel { Orientation = Orientation.Horizontal, HorizontalAlignment = HorizontalAlignment.Right };
        botones.Children.Add(ok);
        botones.Children.Add(cancelar);
        var panel = new StackPanel { Margin = new Thickness(20) };
        panel.Children.Add(new TextBlock { Text = texto, TextWrapping = TextWrapping.Wrap });
        panel.Children.Add(caja);
        panel.Children.Add(botones);
        ventana.Content = panel;
        caja.Focus();
        return ventana.ShowDialog() == true ? resultado : null;
    }
}
