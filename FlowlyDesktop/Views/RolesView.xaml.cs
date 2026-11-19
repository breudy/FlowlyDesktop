using System.ComponentModel;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Data;

namespace FlowlyDesktop.Views;

public class FilaRol
{
    public UsuarioItem Usuario { get; set; } = null!;
    public List<RolItem> Roles { get; set; } = new();
    public RolItem NuevoRol { get; set; } = null!;

    public string Nombre => $"{Usuario.Nombre} {Usuario.Apellidos}";
    public string Email => Usuario.Email;
    public string RolActualEtiqueta => Roles.FirstOrDefault(r => r.Nombre == Usuario.Rol)?.Etiqueta ?? Usuario.Rol;
    public bool Cambiado => NuevoRol.Nombre != Usuario.Rol;
}

public partial class RolesView : UserControl
{
    private readonly List<FilaRol> _filas = new();
    private List<RolItem> _roles = new();
    private ICollectionView? _vista;

    public RolesView()
    {
        InitializeComponent();
        Loaded += async (_, _) => await CargarAsync();
    }

    private async Task CargarAsync()
    {
        Error.Text = "";
        try
        {
            _roles = await Api.GetAsync<List<RolItem>>("/api/roles");
            var usuarios = await Api.GetAsync<List<UsuarioItem>>("/api/usuarios");

            var opcionesFiltro = new List<RolItem> { new() { IdRol = 0, Nombre = "Todos los roles" } };
            opcionesFiltro.AddRange(_roles);
            RolFiltro.ItemsSource = opcionesFiltro;
            RolFiltro.SelectedIndex = 0;

            _filas.Clear();
            foreach (var u in usuarios)
                _filas.Add(new FilaRol
                {
                    Usuario = u,
                    Roles = _roles,
                    NuevoRol = _roles.First(r => r.Nombre == u.Rol)
                });

            _vista = CollectionViewSource.GetDefaultView(_filas);
            _vista.Filter = o =>
            {
                var f = (FilaRol)o;
                string t = Buscar.Text.Trim();
                if (t.Length > 0 &&
                    !f.Nombre.Contains(t, StringComparison.CurrentCultureIgnoreCase) &&
                    !f.Email.Contains(t, StringComparison.CurrentCultureIgnoreCase))
                    return false;
                return RolFiltro.SelectedItem is not RolItem r || r.IdRol == 0 || f.Usuario.Rol == r.Nombre;
            };
            Tabla.ItemsSource = _vista;
        }
        catch (ApiException ex)
        {
            Error.Text = ex.Message;
        }
    }

    private void Filtro_Changed(object sender, RoutedEventArgs e) => _vista?.Refresh();

    private async void Guardar_Click(object sender, RoutedEventArgs e)
    {
        var cambios = _filas.Where(f => f.Cambiado).ToList();
        if (cambios.Count == 0) { Ui.Info("No hay cambios que guardar."); return; }

        var fallos = new List<string>();
        foreach (var f in cambios)
        {
            try
            {
                await Api.PutAsync($"/api/usuarios/{f.Usuario.IdUsuario}", new
                {
                    nombre = f.Usuario.Nombre,
                    apellidos = f.Usuario.Apellidos,
                    email = f.Usuario.Email,
                    tarifaHora = f.Usuario.TarifaHora,
                    idRol = f.NuevoRol.IdRol
                });
            }
            catch (ApiException ex)
            {
                fallos.Add($"{f.Nombre}: {ex.Message}");
            }
        }

        if (fallos.Count > 0)
            Ui.Error("No se pudieron guardar todos los cambios:\n\n• " + string.Join("\n• ", fallos));
        else
            Ui.Info("Roles actualizados.");
        await CargarAsync();
    }

    private async void Descartar_Click(object sender, RoutedEventArgs e) => await CargarAsync();
}
