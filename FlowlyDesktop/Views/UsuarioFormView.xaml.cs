using System.Windows;
using System.Windows.Controls;

namespace FlowlyDesktop.Views;

public partial class UsuarioFormView : UserControl
{
    private readonly UsuarioItem? _editar;
    private List<RolItem> _roles = new();

    public UsuarioFormView(UsuarioItem? editar)
    {
        InitializeComponent();
        _editar = editar;

        if (editar != null)
        {
            Titulo.Text = "Editar usuario";
            Guardar.Content = "Guardar cambios";
            Nombre.Text = editar.Nombre;
            Apellidos.Text = editar.Apellidos;
            Email.Text = editar.Email;
            Tarifa.Text = editar.TarifaHora.ToString("0.##", System.Globalization.CultureInfo.InvariantCulture);
            // Al editar no se toca ni la contraseña (hay un botón aparte) ni los grupos
            PanelPassword.Visibility = Visibility.Collapsed;
            PanelGrupo.Visibility = Visibility.Collapsed;
        }

        Loaded += async (_, _) => await CargarAsync();
    }

    private async Task CargarAsync()
    {
        try
        {
            _roles = await Api.GetAsync<List<RolItem>>("/api/roles");
            Rol.ItemsSource = _roles;
            Rol.SelectedItem = _editar != null
                ? _roles.FirstOrDefault(r => r.Nombre == _editar.Rol)
                : _roles.FirstOrDefault(r => r.Nombre == Roles.Tecnico);

            var equipos = await Api.GetAsync<List<EquipoItem>>("/api/equipos");
            Grupo.ItemsSource = equipos;
        }
        catch (ApiException ex)
        {
            Error.Text = ex.Message;
        }
    }

    private void Rol_Changed(object sender, SelectionChangedEventArgs e)
    {
        if (Rol.SelectedItem is not RolItem rol) return;
        bool tecnico = rol.Nombre == Roles.Tecnico;
        RolTitulo.Text = rol.Etiqueta;
        RolTexto.Text = tecnico
            ? "Consulta sus tareas asignadas, cambia su estado y registra horas desde el móvil."
            : "Escritorio: crea proyectos y usuarios, gestiona tareas y roles y consulta informes.";
        RolLimite.Text = tecnico
            ? "No puede crear proyectos ni gestionar usuarios o roles."
            : "Móvil: consulta de proyectos y tareas en solo lectura.";
        if (_editar == null)
            PanelGrupo.Visibility = tecnico ? Visibility.Visible : Visibility.Collapsed;
    }

    private async void Guardar_Click(object sender, RoutedEventArgs e)
    {
        Error.Text = "";
        if (string.IsNullOrWhiteSpace(Nombre.Text) || string.IsNullOrWhiteSpace(Apellidos.Text))
        { Error.Text = "El nombre y los apellidos son obligatorios."; return; }
        if (string.IsNullOrWhiteSpace(Email.Text) || !Email.Text.Contains('@'))
        { Error.Text = "Escribe un correo electrónico válido."; return; }
        if (Rol.SelectedItem is not RolItem rol) { Error.Text = "Elige un rol."; return; }
        if (!Ui.TryDecimal(Tarifa.Text, out decimal tarifa) || tarifa < 0)
        { Error.Text = "La tarifa debe ser un número igual o mayor que 0."; return; }
        if (_editar == null && Password.Password.Length < 8)
        { Error.Text = "La contraseña debe tener al menos 8 caracteres."; return; }

        Guardar.IsEnabled = false;
        try
        {
            if (_editar == null)
            {
                var creado = await Api.PostAsync<CreadoResponse>("/api/usuarios", new
                {
                    nombre = Nombre.Text.Trim(),
                    apellidos = Apellidos.Text.Trim(),
                    email = Email.Text.Trim(),
                    password = Password.Password,
                    tarifaHora = tarifa,
                    idRol = rol.IdRol
                });

                if (rol.Nombre == Roles.Tecnico && Grupo.SelectedItem is EquipoItem grupo)
                {
                    try
                    {
                        await Api.PostAsync($"/api/equipos/{grupo.IdEquipo}/miembros/{creado.IdUsuario}");
                    }
                    catch (ApiException ex)
                    {
                        Ui.Error($"El usuario se ha creado, pero no se pudo añadir al grupo: {ex.Message}");
                    }
                }
            }
            else
            {
                await Api.PutAsync($"/api/usuarios/{_editar.IdUsuario}", new
                {
                    nombre = Nombre.Text.Trim(),
                    apellidos = Apellidos.Text.Trim(),
                    email = Email.Text.Trim(),
                    tarifaHora = tarifa,
                    idRol = rol.IdRol
                });
            }
            MainWindow.Current.Ir(new UsuariosView(), "Usuarios");
        }
        catch (ApiException ex)
        {
            Error.Text = ex.Message;
            Guardar.IsEnabled = true;
        }
    }

    private void Cancelar_Click(object sender, RoutedEventArgs e) =>
        MainWindow.Current.Ir(new UsuariosView(), "Usuarios");
}
