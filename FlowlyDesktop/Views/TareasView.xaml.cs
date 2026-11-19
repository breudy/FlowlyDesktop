using System.Windows;
using System.Windows.Controls;

namespace FlowlyDesktop.Views;

public partial class TareasView : UserControl
{
    private readonly int? _idProyectoInicial;
    private ProyectoDetalle? _proyecto;
    private EquipoDetalle? _equipo;
    private TareaDetalle? _actual;       // tarea que se esta editando (null = tarea nueva)
    private bool _cargando;

    public class Elemento { public string Valor { get; set; } = ""; public string Texto { get; set; } = ""; public int? Id { get; set; } }

    public TareasView(int? idProyecto)
    {
        InitializeComponent();
        _idProyectoInicial = idProyecto;
        Loaded += async (_, _) => await CargarProyectosAsync();
    }

    // ---------- Carga ----------
    private async Task CargarProyectosAsync()
    {
        try
        {
            var proyectos = await Api.GetAsync<List<ProyectoResumen>>("/api/proyectos");
            _cargando = true;
            ProyectoCombo.ItemsSource = proyectos;
            _cargando = false;
            if (proyectos.Count == 0)
            {
                ErrorGeneral.Text = "Todavía no hay proyectos. Crea uno primero en la sección Proyectos.";
                return;
            }
            ProyectoCombo.SelectedItem = proyectos.FirstOrDefault(p => p.IdProyecto == _idProyectoInicial) ?? proyectos[0];
        }
        catch (ApiException ex)
        {
            ErrorGeneral.Text = ex.Message;
        }
    }

    private async void Proyecto_Changed(object sender, SelectionChangedEventArgs e)
    {
        if (_cargando || ProyectoCombo.SelectedItem is not ProyectoResumen p) return;
        await CargarProyectoAsync(p.IdProyecto);
    }

    private async Task CargarProyectoAsync(int idProyecto, int? seleccionar = null)
    {
        ErrorGeneral.Text = "";
        try
        {
            _proyecto = await Api.GetAsync<ProyectoDetalle>($"/api/proyectos/{idProyecto}");
            _equipo = await Api.GetAsync<EquipoDetalle>($"/api/equipos/{_proyecto.IdEquipo}");

            Subtitulo.Text = $"Proyecto: {_proyecto.Nombre} · {Ui.Fecha(_proyecto.FechaInicio)}–{Ui.Fecha(_proyecto.FechaFin)}" +
                             $" · {_proyecto.Tareas.Count} tareas existentes";

            _cargando = true;
            ListaTareas.ItemsSource = _proyecto.Tareas
                .Select(t => new Elemento
                {
                    Id = t.IdTarea,
                    Texto = $"{(t.IdTareaPadre != null ? "   ↳ " : "")}{t.Nombre}  ·  {Ui.EstadoTarea(t.Estado)}"
                }).ToList();
            ListaVacia.Visibility = _proyecto.Tareas.Count == 0 ? Visibility.Visible : Visibility.Collapsed;
            _cargando = false;

            Contenido.Visibility = Visibility.Visible;

            if (seleccionar != null)
            {
                var item = ((List<Elemento>)ListaTareas.ItemsSource).FirstOrDefault(i => i.Id == seleccionar);
                if (item != null) { ListaTareas.SelectedItem = item; return; }
            }
            Nueva();
        }
        catch (ApiException ex)
        {
            ErrorGeneral.Text = ex.Message;
        }
    }

    // ---------- Formulario ----------
    private void Nueva_Click(object sender, RoutedEventArgs e) => Nueva();

    private void Nueva()
    {
        if (_proyecto == null) return;
        _actual = null;
        _cargando = true;
        ListaTareas.SelectedItem = null;
        _cargando = false;
        PrepararFormulario(null);
    }

    private async void Tarea_Selected(object sender, SelectionChangedEventArgs e)
    {
        if (_cargando || ListaTareas.SelectedItem is not Elemento el || el.Id == null) return;
        try
        {
            _actual = await Api.GetAsync<TareaDetalle>($"/api/tareas/{el.Id}");
            PrepararFormulario(_actual);
        }
        catch (ApiException ex)
        {
            Error.Text = ex.Message;
        }
    }

    /// <summary>Rellena el formulario (con la tarea o en blanco para una nueva).</summary>
    private void PrepararFormulario(TareaDetalle? t)
    {
        Error.Text = "";
        if (_proyecto == null || _equipo == null) return;

        TituloForm.Text = t == null ? "Crear tarea" : "Editar tarea";
        Guardar.Content = t == null ? "Guardar tarea" : "Guardar cambios";
        Eliminar.Visibility = t == null ? Visibility.Collapsed : Visibility.Visible;
        PanelEstado.Visibility = t == null ? Visibility.Collapsed : Visibility.Visible;
        PanelPadre.Visibility = t == null ? Visibility.Visible : Visibility.Collapsed;

        Nombre.Text = t?.Nombre ?? "";
        Descripcion.Text = t?.Descripcion ?? "";
        Horas.Text = (t?.HorasEstimadas ?? 0).ToString("0.##", System.Globalization.CultureInfo.InvariantCulture);
        FechaLimite.SelectedDate = Ui.ADateTime(t?.FechaLimite);

        // Subtarea de: solo tareas principales del proyecto
        var padres = new List<Elemento> { new() { Id = null, Texto = "— Ninguna (tarea principal) —" } };
        padres.AddRange(_proyecto.Tareas.Where(x => x.IdTareaPadre == null)
            .Select(x => new Elemento { Id = x.IdTarea, Texto = x.Nombre }));
        Padre.ItemsSource = padres;
        Padre.SelectedIndex = 0;

        // Estado
        var estados = new[] { "PENDIENTE", "EN_CURSO", "FINALIZADA" }
            .Select(s => new Elemento { Valor = s, Texto = Ui.EstadoTarea(s) }).ToList();
        Estado.ItemsSource = estados;
        Estado.SelectedItem = estados.FirstOrDefault(s => s.Valor == (t?.Estado ?? "PENDIENTE"));

        // Usuarios: miembros activos del grupo del proyecto
        var asignados = t?.Asignados.Select(a => a.IdUsuario).ToHashSet() ?? new HashSet<int>();
        var opcionesUsuarios = _equipo.Miembros.Where(m => m.Activo || asignados.Contains(m.IdUsuario))
            .Select(m => new Opcion
            {
                Id = m.IdUsuario,
                Texto = $"{m.Nombre} · Usuario técnico",
                Seleccionado = asignados.Contains(m.IdUsuario)
            }).ToList();
        ListaUsuarios.ItemsSource = opcionesUsuarios;
        UsuariosInfo.Text = opcionesUsuarios.Count == 0
            ? $"El grupo «{_equipo.Nombre}» no tiene miembros. Añade usuarios técnicos al grupo."
            : $"Miembros del grupo «{_equipo.Nombre}».";

        // Dependencias: el resto de tareas del proyecto
        var previas = t?.TareasPrevias.Select(p => p.IdTarea).ToHashSet() ?? new HashSet<int>();
        ListaPrevias.ItemsSource = _proyecto.Tareas.Where(x => x.IdTarea != t?.IdTarea)
            .Select(x => new Opcion
            {
                Id = x.IdTarea,
                Texto = $"{x.Nombre} ({Ui.EstadoTarea(x.Estado)})",
                Seleccionado = previas.Contains(x.IdTarea)
            }).ToList();

        // Resumen de horas
        decimal total = _proyecto.Tareas.Sum(x => x.HorasEstimadas);
        HorasInfo.Text = t == null
            ? $"Estado inicial: Pendiente · Estimación actual del proyecto: {Ui.Horas(total)}."
            : $"Horas registradas: {Ui.Horas(t.HorasRegistradas)} · Estimación total del proyecto: {Ui.Horas(total)}.";
    }

    // ---------- Guardar ----------
    private async void Guardar_Click(object sender, RoutedEventArgs e)
    {
        Error.Text = "";
        if (_proyecto == null) return;
        if (string.IsNullOrWhiteSpace(Nombre.Text)) { Error.Text = "El nombre de la tarea es obligatorio."; return; }
        if (string.IsNullOrWhiteSpace(Descripcion.Text)) { Error.Text = "La descripción es obligatoria."; return; }
        if (!Ui.TryDecimal(Horas.Text, out decimal horas) || horas < 0)
        { Error.Text = "Las horas estimadas deben ser un número igual o mayor que 0."; return; }

        var usuarios = ((List<Opcion>)ListaUsuarios.ItemsSource).Where(o => o.Seleccionado).Select(o => o.Id).ToHashSet();
        var previas = ((List<Opcion>)ListaPrevias.ItemsSource).Where(o => o.Seleccionado).Select(o => o.Id).ToHashSet();
        var avisos = new List<string>();

        Guardar.IsEnabled = false;
        try
        {
            int idTarea;
            if (_actual == null)
            {
                var padre = (Padre.SelectedItem as Elemento)?.Id;
                var creada = await Api.PostAsync<CreadaTareaResponse>("/api/tareas", new
                {
                    idProyecto = _proyecto.IdProyecto,
                    idTareaPadre = padre,
                    nombre = Nombre.Text.Trim(),
                    descripcion = Descripcion.Text.Trim(),
                    fechaLimite = Ui.ADateOnly(FechaLimite.SelectedDate),
                    horasEstimadas = horas
                });
                idTarea = creada.IdTarea;
                await SincronizarAsync(idTarea, new HashSet<int>(), usuarios, new HashSet<int>(), previas, avisos);
            }
            else
            {
                idTarea = _actual.IdTarea;
                await Api.PutAsync($"/api/tareas/{idTarea}", new
                {
                    nombre = Nombre.Text.Trim(),
                    descripcion = Descripcion.Text.Trim(),
                    fechaLimite = Ui.ADateOnly(FechaLimite.SelectedDate),
                    horasEstimadas = horas,
                    estado = (Estado.SelectedItem as Elemento)?.Valor ?? _actual.Estado
                });
                await SincronizarAsync(idTarea,
                    _actual.Asignados.Select(a => a.IdUsuario).ToHashSet(), usuarios,
                    _actual.TareasPrevias.Select(p => p.IdTarea).ToHashSet(), previas, avisos);
            }

            if (avisos.Count > 0)
                Ui.Error("La tarea se ha guardado, pero:\n\n• " + string.Join("\n• ", avisos));

            await CargarProyectoAsync(_proyecto.IdProyecto, idTarea);
        }
        catch (ApiException ex)
        {
            Error.Text = ex.Message;
        }
        finally
        {
            Guardar.IsEnabled = true;
        }
    }

    /// <summary>Aplica las diferencias de asignaciones y dependencias; los fallos se acumulan en avisos.</summary>
    private static async Task SincronizarAsync(int idTarea,
        HashSet<int> usuariosAntes, HashSet<int> usuariosAhora,
        HashSet<int> previasAntes, HashSet<int> previasAhora, List<string> avisos)
    {
        foreach (int u in usuariosAhora.Except(usuariosAntes))
            await Intentar(() => Api.PostAsync($"/api/tareas/{idTarea}/asignaciones", new { idUsuario = u }), avisos);
        foreach (int u in usuariosAntes.Except(usuariosAhora))
            await Intentar(() => Api.DeleteAsync($"/api/tareas/{idTarea}/asignaciones/{u}"), avisos);
        foreach (int p in previasAhora.Except(previasAntes))
            await Intentar(() => Api.PostAsync($"/api/tareas/{idTarea}/dependencias", new { idTareaPrevia = p }), avisos);
        foreach (int p in previasAntes.Except(previasAhora))
            await Intentar(() => Api.DeleteAsync($"/api/tareas/{idTarea}/dependencias/{p}"), avisos);
    }

    private static async Task Intentar(Func<Task> accion, List<string> avisos)
    {
        try { await accion(); }
        catch (ApiException ex) { avisos.Add(ex.Message); }
    }

    private async void Eliminar_Click(object sender, RoutedEventArgs e)
    {
        if (_actual == null || _proyecto == null) return;
        if (!Ui.Confirmar($"¿Eliminar la tarea «{_actual.Nombre}»?")) return;
        try
        {
            await Api.DeleteAsync($"/api/tareas/{_actual.IdTarea}");
            await CargarProyectoAsync(_proyecto.IdProyecto);
        }
        catch (ApiException ex)
        {
            Error.Text = ex.Message;
        }
    }
}
