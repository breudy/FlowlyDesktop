namespace FlowlyDesktop;

// Modelos que devuelve la API

public record LoginResponse(string Token, DateTime Expira, int IdUsuario, string Nombre, string Rol);

public record ProyectoResumen(int IdProyecto, string Nombre, DateOnly FechaInicio, DateOnly? FechaFin,
                              string Equipo, int NumTareas);

public record TareaResumen(int IdTarea, int? IdTareaPadre, string Nombre, string Estado,
                           DateOnly? FechaLimite, decimal HorasEstimadas);

public record ProyectoDetalle(int IdProyecto, string Nombre, string? Descripcion, DateOnly FechaInicio,
                              DateOnly? FechaFin, int IdEquipo, string Equipo, string Creador,
                              List<TareaResumen> Tareas);

public record Asignado(int IdUsuario, string Nombre, decimal? HorasAsignadas);
public record TareaPrevia(int IdTarea, string Nombre, string Estado);

public record TareaDetalle(int IdTarea, int IdProyecto, string Proyecto, int? IdTareaPadre, string? TareaPadre,
                           string Nombre, string? Descripcion, string Estado, DateOnly? FechaLimite,
                           decimal HorasEstimadas, decimal? HorasRegistradas,
                           List<Asignado> Asignados, List<TareaPrevia> TareasPrevias);

public record UsuarioItem(int IdUsuario, string Nombre, string Apellidos, string Email, string Rol,
                          decimal TarifaHora, bool Activo, List<string> Equipos);

public record EquipoItem(int IdEquipo, string Nombre, string? Descripcion, int NumMiembros, int NumProyectos);
public record MiembroEquipo(int IdUsuario, string Nombre, string Email, bool Activo);
public record EquipoDetalle(int IdEquipo, string Nombre, string? Descripcion, List<MiembroEquipo> Miembros);

public record CosteFila(int IdUsuario, string Usuario, decimal? Horas, decimal TarifaHora, decimal? Coste);
public record InformeCostes(int IdProyecto, string Proyecto, decimal? TotalHoras, decimal? TotalCoste,
                            List<CosteFila> Personas);

public record InformeResumen(int IdProyecto, string Proyecto, int TotalTareas, int Pendientes, int EnCurso,
                             int Finalizadas, decimal PorcentajeFinalizado, decimal HorasEstimadas,
                             decimal? HorasRegistradas);

public record TareaCritica(int IdTarea, int IdProyecto, string Tarea, string Estado, int? TareasBloqueadas);

public record CreadoResponse(int IdUsuario);
public record CreadaTareaResponse(int IdTarea, string Nombre);

/// <summary>Rol con la etiqueta que se ve en pantalla.</summary>
public class RolItem
{
    public int IdRol { get; set; }
    public string Nombre { get; set; } = "";
    public string? Descripcion { get; set; }

    public string Etiqueta => Nombre switch
    {
        Roles.Administrador => "Administrador",
        Roles.Tecnico => "Usuario técnico",
        _ => Nombre
    };

    public override string ToString() => Etiqueta;
}

public static class Roles
{
    public const string Administrador = "ADMINISTRADOR";
    public const string Tecnico = "USUARIO_TECNICO";
}

/// <summary>Elemento de una lista con casilla (asignaciones y dependencias).</summary>
public class Opcion
{
    public int Id { get; set; }
    public string Texto { get; set; } = "";
    public bool Seleccionado { get; set; }
}
