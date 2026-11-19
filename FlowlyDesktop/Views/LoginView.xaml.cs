using System.Windows;
using System.Windows.Controls;

namespace FlowlyDesktop.Views;

public partial class LoginView : UserControl
{
    public LoginView() => InitializeComponent();

    public void Reiniciar(string? aviso)
    {
        Pass.Clear();
        Error.Text = aviso ?? "";
        Entrar.IsEnabled = true;
        Email.Focus();
    }

    private async void Entrar_Click(object sender, RoutedEventArgs e)
    {
        Error.Text = "";
        if (string.IsNullOrWhiteSpace(Email.Text) || Pass.Password.Length == 0)
        {
            Error.Text = "Escribe tu correo y tu contraseña.";
            return;
        }

        Entrar.IsEnabled = false;
        try
        {
            var login = await Api.LoginAsync(Email.Text.Trim(), Pass.Password);
            if (login.Rol != Roles.Administrador)
            {
                Api.Logout();
                Error.Text = "Esta aplicación es solo para administradores. Usa la app móvil.";
                return;
            }
            Pass.Clear();
            MainWindow.Current.MostrarShell();
        }
        catch (ApiException ex)
        {
            Error.Text = ex.Message;
        }
        finally
        {
            Entrar.IsEnabled = true;
        }
    }
}
