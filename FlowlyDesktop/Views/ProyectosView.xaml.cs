using System.ComponentModel;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Data;

namespace FlowlyDesktop.Views;

public class ProyectoFila
{
    public int Id { get; set; }
    public string Nombre { get; set; } = "";
    public string Equipo { get; set; } = "";
    public string Estado { get; set; } = "";
    public string Fechas { get; set; } = "";
    public string Tareas { get; set; } = "";
    public string Descripcion { get; set; } = "";
    public ProyectoDetalle Detalle { get; set; } = null!;
}

public partial class ProyectosView : UserControl
{
    private readonly List<ProyectoFila> _filas = new();
    private ICollectionView? _vista;

    public ProyectosView()
    {
        InitializeComponent();
        Loaded += async (_, _) => await CargarAsync();
    }

    private async Task CargarAsync()
    {
        Error.Text = "";
        try
        {
            var lista = await Api.GetAsync<List<ProyectoResumen>>("/api/proyectos");
            // La lista no trae el estado ni las tareas finalizadas: se piden los detalles
            var detalles = await Task.WhenAll(lista.Select(p =>
                Api.GetAsync<ProyectoDetalle>($"/api/proyectos/{p.IdProyecto}")));

            _filas.Clear();
            foreach (var d in detalles.OrderBy(d => d.FechaInicio))
            {
                int total = d.Tareas.Count;
                int fin = d.Tareas.Count(t => t.Estado == "FINALIZADA");
                string estado = total == 0 || d.Tareas.All(t => t.Estado == "PENDIENTE") ? "Pendiente"
                              : fin == total ? "Finalizado" : "En curso";
                _filas.Add(new ProyectoFila
                {
                    Id = d.IdProyecto,
                    Nombre = d.Nombre,
                    Descripcion = d.Descripcion ?? "",
                    Equipo = d.Equipo,
                    Estado = estado,
                    Fechas = $"{d.FechaInicio:dd/MM} – {Ui.Fecha(d.FechaFin)}",
                    Tareas = $"{fin} de {total}",
                    Detalle = d
                });
            }

            KTotal.Text = _filas.Count.ToString();
            KCurso.Text = _filas.Count(f => f.Estado == "En curso").ToString();
            KPend.Text = _filas.Count(f => f.Estado == "Pendiente").ToString();
            KFin.Text = _filas.Count(f => f.Estado == "Finalizado").ToString();
            Pie.Text = $"{_filas.Count} proyectos";

            _vista = CollectionViewSource.GetDefaultView(_filas);
            _vista.Filter = Filtrar;
            Tabla.ItemsSource = _vista;
        }
        catch (ApiException ex)
        {
            Error.Text = ex.Message;
        }
    }

    private bool Filtrar(object o)
    {
        var f = (ProyectoFila)o;
        string texto = Buscar.Text.Trim();
        if (texto.Length > 0 &&
            !f.Nombre.Contains(texto, StringComparison.CurrentCultureIgnoreCase) &&
            !f.Descripcion.Contains(texto, StringComparison.CurrentCultureIgnoreCase))
            return false;

        string estado = (EstadoFiltro.SelectedItem as ComboBoxItem)?.Content as string ?? "";
        return estado == "" || estado.StartsWith("Todos") || f.Estado == estado;
    }

    private void Filtro_Changed(object sender, RoutedEventArgs e) => _vista?.Refresh();

    private void Crear_Click(object sender, RoutedEventArgs e) =>
        MainWindow.Current.Ir(new ProyectoFormView(null), "Proyectos");

    private void Ver_Click(object sender, RoutedEventArgs e) =>
        MainWindow.Current.Ir(new TareasView((int)((Button)sender).Tag), "Tareas");

    private void Editar_Click(object sender, RoutedEventArgs e)
    {
        if (Tabla.SelectedItem is not ProyectoFila f)
        {
            Ui.Info("Selecciona primero un proyecto de la tabla.");
            return;
        }
        MainWindow.Current.Ir(new ProyectoFormView(f.Detalle), "Proyectos");
    }

    private async void Eliminar_Click(object sender, RoutedEventArgs e)
    {
        if (Tabla.SelectedItem is not ProyectoFila f)
        {
            Ui.Info("Selecciona primero un proyecto de la tabla.");
            return;
        }
        if (!Ui.Confirmar($"¿Eliminar el proyecto «{f.Nombre}» con todas sus tareas?"))
            return;
        try
        {
            await Api.DeleteAsync($"/api/proyectos/{f.Id}");
            await CargarAsync();
        }
        catch (ApiException ex)
        {
            Ui.Error(ex.Message);
        }
    }
}
