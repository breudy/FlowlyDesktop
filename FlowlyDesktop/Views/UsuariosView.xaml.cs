using System.ComponentModel;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Data;

namespace FlowlyDesktop.Views;

public class UsuarioFila
{
    public UsuarioItem Usuario { get; set; } = null!;
    public string NombreCompleto => $"{Usuario.Nombre} {Usuario.Apellidos}";
    public string Email => Usuario.Email;
    public string RolEtiqueta => Usuario.Rol == Roles.Administrador ? "Administrador" : "Usuario técnico";
    public string Tarifa => $"{Ui.Euros(Usuario.TarifaHora)}/h";
    public string Grupos => Usuario.Equipos.Count == 0 ? "—" : string.Join(", ", Usuario.Equipos);
    public string Estado => Usuario.Activo ? "Activo" : "Desactivado";
}

public partial class UsuariosView : UserControl
{
    private readonly List<UsuarioFila> _filas = new();
    private ICollectionView? _vista;

    public UsuariosView()
    {
        InitializeComponent();
        Loaded += async (_, _) => await CargarAsync();
    }

    private async Task CargarAsync()
    {
        Error.Text = "";
        try
        {
            var usuarios = await Api.GetAsync<List<UsuarioItem>>("/api/usuarios");
            _filas.Clear();
            _filas.AddRange(usuarios.Select(u => new UsuarioFila { Usuario = u }));
            _vista = CollectionViewSource.GetDefaultView(_filas);
            _vista.Filter = o =>
            {
                var f = (UsuarioFila)o;
                string t = Buscar.Text.Trim();
                return t.Length == 0 ||
                       f.NombreCompleto.Contains(t, StringComparison.CurrentCultureIgnoreCase) ||
                       f.Email.Contains(t, StringComparison.CurrentCultureIgnoreCase);
            };
            Tabla.ItemsSource = _vista;
        }
        catch (ApiException ex)
        {
            Error.Text = ex.Message;
        }
    }

    private void Buscar_Changed(object sender, TextChangedEventArgs e) => _vista?.Refresh();

    private void Crear_Click(object sender, RoutedEventArgs e) =>
        MainWindow.Current.Ir(new UsuarioFormView(null), "Usuarios");

    private UsuarioFila? Seleccionado()
    {
        if (Tabla.SelectedItem is UsuarioFila f) return f;
        Ui.Info("Selecciona primero un usuario de la tabla.");
        return null;
    }

    private void Editar_Click(object sender, RoutedEventArgs e)
    {
        if (Seleccionado() is { } f)
            MainWindow.Current.Ir(new UsuarioFormView(f.Usuario), "Usuarios");
    }

    private async void Activo_Click(object sender, RoutedEventArgs e)
    {
        if (Seleccionado() is not { } f) return;
        bool nuevo = !f.Usuario.Activo;
        if (!Ui.Confirmar($"¿{(nuevo ? "Activar" : "Desactivar")} a {f.NombreCompleto}?")) return;
        try
        {
            await Api.PatchAsync($"/api/usuarios/{f.Usuario.IdUsuario}/activo", new { activo = nuevo });
            await CargarAsync();
        }
        catch (ApiException ex) { Ui.Error(ex.Message); }
    }

    private async void Password_Click(object sender, RoutedEventArgs e)
    {
        if (Seleccionado() is not { } f) return;
        string? pass = Ui.PedirPassword("Cambiar contraseña",
            $"Nueva contraseña para {f.NombreCompleto} (mínimo 8 caracteres):");
        if (pass == null) return;
        try
        {
            await Api.PatchAsync($"/api/usuarios/{f.Usuario.IdUsuario}/password", new { password = pass });
            Ui.Info("Contraseña actualizada.");
        }
        catch (ApiException ex) { Ui.Error(ex.Message); }
    }
}
