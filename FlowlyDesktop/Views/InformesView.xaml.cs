using System.Windows;
using System.Windows.Controls;

namespace FlowlyDesktop.Views;

public class BarraFila
{
    public string Nombre { get; set; } = "";
    public string Valor { get; set; } = "";
    public double Numero { get; set; }
    public double Maximo { get; set; } = 1;
}

public class CosteFilaVista
{
    public string Usuario { get; set; } = "";
    public string Horas { get; set; } = "";
    public string Tarifa { get; set; } = "";
    public string Coste { get; set; } = "";
}

public class CriticaFila
{
    public string Tarea { get; set; } = "";
    public string Estado { get; set; } = "";
    public string Bloquea { get; set; } = "";
}

public partial class InformesView : UserControl
{
    private InformeCostes? _costes;
    private bool _cargando;

    public InformesView()
    {
        InitializeComponent();
        Loaded += async (_, _) => await CargarProyectosAsync();
    }

    private async Task CargarProyectosAsync()
    {
        try
        {
            var proyectos = await Api.GetAsync<List<ProyectoResumen>>("/api/proyectos");
            ProyectoCombo.ItemsSource = proyectos;
            if (proyectos.Count == 0)
            {
                Error.Text = "Todavía no hay proyectos para mostrar.";
                return;
            }
            ProyectoCombo.SelectedIndex = 0;   // dispara la carga del informe
        }
        catch (ApiException ex)
        {
            Error.Text = ex.Message;
        }
    }

    private async void Proyecto_Changed(object sender, SelectionChangedEventArgs e)
    {
        if (ProyectoCombo.SelectedItem is not ProyectoResumen p) return;
        Error.Text = "";
        try
        {
            var tCostes = Api.GetAsync<InformeCostes>($"/api/informes/proyecto/{p.IdProyecto}/costes");
            var tResumen = Api.GetAsync<InformeResumen>($"/api/informes/proyecto/{p.IdProyecto}/resumen");
            var tCriticas = Api.GetAsync<List<TareaCritica>>($"/api/informes/tareas-criticas?idProyecto={p.IdProyecto}");
            await Task.WhenAll(tCostes, tResumen, tCriticas);

            _costes = tCostes.Result;
            var r = tResumen.Result;

            Subtitulo.Text = $"{p.Nombre} · Seguimiento de horas, costes y estado de las tareas.";
            KHoras.Text = $"{Ui.Horas(r.HorasRegistradas)} / {Ui.Horas(r.HorasEstimadas)}";
            KTareas.Text = $"{r.Finalizadas} de {r.TotalTareas} · {r.PorcentajeFinalizado.ToString("0.#", Ui.Es)} %";
            KCoste.Text = Ui.Euros(_costes.TotalCoste);

            BarrasEstado.ItemsSource = new List<BarraFila>
            {
                Estado("Finalizadas", r.Finalizadas, r.TotalTareas),
                Estado("En curso", r.EnCurso, r.TotalTareas),
                Estado("Pendientes", r.Pendientes, r.TotalTareas)
            };

            TablaCriticas.ItemsSource = tCriticas.Result.Select(c => new CriticaFila
            {
                Tarea = c.Tarea,
                Estado = Ui.EstadoTarea(c.Estado),
                Bloquea = $"{c.TareasBloqueadas ?? 0} tarea(s)"
            }).ToList();
            CriticasVacio.Text = tCriticas.Result.Count == 0 ? "No hay tareas críticas en este proyecto." : "";

            // Lista de usuarios para el filtro
            _cargando = true;
            var nombres = new List<string> { "Todos" };
            nombres.AddRange(_costes.Personas.Select(x => x.Usuario));
            UsuarioCombo.ItemsSource = nombres;
            UsuarioCombo.SelectedIndex = 0;
            _cargando = false;

            PintarPersonas();
        }
        catch (ApiException ex)
        {
            Error.Text = ex.Message;
        }
    }

    private static BarraFila Estado(string nombre, int valor, int total) => new()
    {
        Nombre = nombre,
        Valor = $"{valor} tareas",
        Numero = valor,
        Maximo = Math.Max(total, 1)
    };

    private void Aplicar_Click(object sender, RoutedEventArgs e) => PintarPersonas();

    /// <summary>Dibuja la grafica y la tabla de horas/costes segun el usuario elegido.</summary>
    private void PintarPersonas()
    {
        if (_costes == null || _cargando) return;
        string filtro = UsuarioCombo.SelectedItem as string ?? "Todos";
        var personas = _costes.Personas.Where(p => filtro == "Todos" || p.Usuario == filtro).ToList();

        double max = Math.Max(1, personas.Select(p => (double)(p.Horas ?? 0)).DefaultIfEmpty(0).Max());
        BarrasHoras.ItemsSource = personas.Select(p => new BarraFila
        {
            Nombre = p.Usuario,
            Valor = Ui.Horas(p.Horas),
            Numero = (double)(p.Horas ?? 0),
            Maximo = max
        }).ToList();
        BarrasHorasVacio.Text = personas.Count == 0 ? "Todavía no hay horas registradas." : "";

        var filas = personas.Select(p => new CosteFilaVista
        {
            Usuario = p.Usuario,
            Horas = Ui.Horas(p.Horas),
            Tarifa = $"{Ui.Euros(p.TarifaHora)}/h",
            Coste = Ui.Euros(p.Coste)
        }).ToList();
        filas.Add(new CosteFilaVista
        {
            Usuario = "Total",
            Horas = Ui.Horas(personas.Sum(p => p.Horas ?? 0)),
            Tarifa = "",
            Coste = Ui.Euros(personas.Sum(p => p.Coste ?? 0))
        });
        TablaCostes.ItemsSource = filas;
    }
}
