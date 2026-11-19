using System.Windows;
using System.Windows.Controls;
using FlowlyDesktop.Views;

namespace FlowlyDesktop;

public partial class MainWindow : Window
{
    public static MainWindow Current { get; private set; } = null!;

    public MainWindow()
    {
        InitializeComponent();
        Current = this;
        // Si la API dice que el token ya no vale, volvemos al login
        Api.SesionCaducada += () => Dispatcher.Invoke(() => MostrarLogin("Tu sesión ha caducado. Vuelve a iniciar sesión."));
        MostrarLogin();
    }

    public void MostrarLogin(string? aviso = null)
    {
        ShellGrid.Visibility = Visibility.Collapsed;
        Host.Content = null;
        Login.Visibility = Visibility.Visible;
        Login.Reiniciar(aviso);
    }

    public void MostrarShell()
    {
        Login.Visibility = Visibility.Collapsed;
        ShellGrid.Visibility = Visibility.Visible;
        UsuarioTxt.Text = $"{Api.Sesion!.Nombre} · Administrador";
        Ir(new ProyectosView(), "Proyectos");
    }

    /// <summary>Muestra una pantalla y marca la opcion del menu.</summary>
    public void Ir(UserControl vista, string seccion)
    {
        Host.Content = vista;
        NavProyectos.IsChecked = seccion == "Proyectos";
        NavUsuarios.IsChecked = seccion == "Usuarios";
        NavTareas.IsChecked = seccion == "Tareas";
        NavRoles.IsChecked = seccion == "Roles";
        NavInformes.IsChecked = seccion == "Informes";
    }

    private void Nav_Click(object sender, RoutedEventArgs e)
    {
        switch ((string)((RadioButton)sender).Tag)
        {
            case "Proyectos": Ir(new ProyectosView(), "Proyectos"); break;
            case "Usuarios": Ir(new UsuariosView(), "Usuarios"); break;
            case "Tareas": Ir(new TareasView(null), "Tareas"); break;
            case "Roles": Ir(new RolesView(), "Roles"); break;
            case "Informes": Ir(new InformesView(), "Informes"); break;
        }
    }

    private void CerrarSesion_Click(object sender, RoutedEventArgs e)
    {
        Api.Logout();
        MostrarLogin();
    }
}
