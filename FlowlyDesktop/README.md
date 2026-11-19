# Flowly · aplicación de escritorio (administrador)

WPF · .NET 8 · C#. Habla con la API ASP.NET Core de Flowly (módulo 0486).

## Cómo arrancarla
1. Crea la base de datos con `flowly_basedatos.sql` y el usuario de la API con `flowly_api_usuario.sql`.
2. Arranca la API (perfil **http**, `http://localhost:5079`).
3. Abre `FlowlyDesktop.csproj` en Visual Studio 2022 (carga de trabajo «Desarrollo de escritorio con .NET») y pulsa F5.
4. Entra con `elena.ruiz@flowly.local` / `Admin2026!`.

Si la API está en otra dirección, define la variable de entorno `FLOWLY_API_URL`
(por ejemplo `https://localhost:7298`) antes de abrir Visual Studio.

## Pantallas (wireframes Escritorio 01–07)
| Wireframe | Vista | Qué hace |
|---|---|---|
| 01 Login | `LoginView` | Inicia sesión; rechaza a quien no sea administrador |
| 02 Dashboard | `ProyectosView` | Indicadores, búsqueda, filtro por estado, ver / editar / eliminar |
| 04 Crear proyecto | `ProyectoFormView` | Crear y editar proyectos |
| 03 Crear usuario | `UsuariosView` + `UsuarioFormView` | Listar, crear, editar, activar/desactivar, cambiar contraseña |
| 05 Gestión de tareas | `TareasView` | Crear/editar/borrar tareas y subtareas, asignar, dependencias, horas |
| 06 Roles | `RolesView` | Cambiar el rol de varios usuarios y guardar |
| 07 Informes | `InformesView` | Horas, costes, estado de tareas y tareas críticas |

## Estructura
- `Services/Api.cs`: cliente HTTP, token JWT y errores (`ApiException` con el `mensaje` de la API).
- `Services/Models.cs`: modelos del JSON de la API.
- `Services/Ui.cs`: avisos, formatos y ventana de contraseña.
- `Views/`: una pantalla por archivo (`.xaml` + `.xaml.cs`).

## Diferencias con el wireframe (por lo que ofrece la API)
- Crear usuario: en vez de «enviar invitación por correo» se escribe una contraseña inicial (la API no envía correos). Se añadió la tarifa/hora porque la API la exige. «Proyecto inicial» no existe en la API.
- Informes: sin selector de periodo (la API no filtra por fechas) y sin «coste estimado» por usuario.
- Login: sin «mantener sesión» ni «he olvidado mi contraseña».
- Dashboard: sin paginación (la tabla tiene scroll); el estado del proyecto se calcula a partir de sus tareas.
