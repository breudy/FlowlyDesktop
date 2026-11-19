using System.Windows;
using System.Windows.Controls;

namespace FlowlyDesktop.Views;

public partial class ProyectoFormView : UserControl
{
    private readonly ProyectoDetalle? _editar;

    public ProyectoFormView(ProyectoDetalle? editar)
    {
        InitializeComponent();
        _editar = editar;
        Responsable.Text = $"Responsable: {Api.Sesion?.Nombre} · Administrador";

        if (editar != null)
        {
            Titulo.Text = "Editar proyecto";
            Guardar.Content = "Guardar cambios";
            Nombre.Text = editar.Nombre;
            Descripcion.Text = editar.Descripcion ?? "";
            FechaInicio.SelectedDate = Ui.ADateTime(editar.FechaInicio);
            FechaFin.SelectedDate = Ui.ADateTime(editar.FechaFin);
        }

        Loaded += async (_, _) => await CargarEquiposAsync();
    }

    private async Task CargarEquiposAsync()
    {
        try
        {
            var equipos = await Api.GetAsync<List<EquipoItem>>("/api/equipos");
            Equipo.ItemsSource = equipos;
            if (_editar != null)
                Equipo.SelectedItem = equipos.FirstOrDefault(e => e.IdEquipo == _editar.IdEquipo);
            if (equipos.Count == 0)
                Error.Text = "No hay ningún grupo creado todavía. Crea un equipo en la API antes de crear proyectos.";
        }
        catch (ApiException ex)
        {
            Error.Text = ex.Message;
        }
    }

    private async void Guardar_Click(object sender, RoutedEventArgs e)
    {
        Error.Text = "";
        if (string.IsNullOrWhiteSpace(Nombre.Text)) { Error.Text = "El nombre es obligatorio."; return; }
        if (string.IsNullOrWhiteSpace(Descripcion.Text)) { Error.Text = "La descripción es obligatoria."; return; }
        if (FechaInicio.SelectedDate == null || FechaFin.SelectedDate == null)
        { Error.Text = "Indica la fecha de inicio y la de fin."; return; }
        if (FechaFin.SelectedDate < FechaInicio.SelectedDate)
        { Error.Text = "La fecha de fin no puede ser anterior a la de inicio."; return; }
        if (Equipo.SelectedItem is not EquipoItem equipo) { Error.Text = "Elige un grupo / ciclo."; return; }

        var cuerpo = new
        {
            nombre = Nombre.Text.Trim(),
            descripcion = Descripcion.Text.Trim(),
            fechaInicio = Ui.ADateOnly(FechaInicio.SelectedDate),
            fechaFin = Ui.ADateOnly(FechaFin.SelectedDate),
            idEquipo = equipo.IdEquipo
        };

        Guardar.IsEnabled = false;
        try
        {
            if (_editar == null)
                await Api.PostAsync("/api/proyectos", cuerpo);
            else
                await Api.PutAsync($"/api/proyectos/{_editar.IdProyecto}", cuerpo);
            MainWindow.Current.Ir(new ProyectosView(), "Proyectos");
        }
        catch (ApiException ex)
        {
            Error.Text = ex.Message;
            Guardar.IsEnabled = true;
        }
    }

    private void Cancelar_Click(object sender, RoutedEventArgs e) =>
        MainWindow.Current.Ir(new ProyectosView(), "Proyectos");
}
