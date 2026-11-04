# Sistema web de compra y reserva de pasajes – Aerolínea VIP
Segunda Entrega (Fase 4: MVP funcional) · Grupo 4

Universidad de las Fuerzas Armadas ESPE · Ingeniería en Tecnologías de la Información
Desarrollo Web para Integración de Tecnología · Ing. Juan Fernando Galarraga Hurtado

## Integrantes
- Melanie Camila Barrionuevo Gallegos
- Daniel Alejandro Guevara Bedon

## Tecnologías
ASP.NET Framework 4.8.1 · MVC 5 · Razor · Bootstrap 5 · jQuery · ASP.NET Identity · Entity Framework 6 · SQL Server

## Estructura del repositorio
```
README.md
.gitignore
src/        AerolineaVIP_Grupo4.slnx y proyecto AerolineaVIP (ASP.NET Framework 4.8.1 MVC)
database/   Script_BD_AerolineaVIP_Unificado.sql
docs/       Primera_Entrega_Documentacion_Grupo4.pdf
            UX-UI/    wireframes aplicados y mapa de navegación
            Gestion/  políticas del punto 18.1 (DP-01 a DP-15)
```

## Cómo ejecutar
1. Ejecutar `database/Script_BD_AerolineaVIP_Unificado.sql` en SQL Server (crea `AerolineaVIP` y `AerolineaVIP_Identity`).
2. Revisar en `src/AerolineaVIP/Web.config` las dos cadenas de conexión (usan autenticación de Windows, `Data Source=.`):
   - `AerolineaVIPEntities` → base de dominio `AerolineaVIP`
   - `DefaultConnection` → base de Identity `AerolineaVIP_Identity`

   Con SQL Express cambiar `Data Source=.` por `.\SQLEXPRESS` en las dos.
3. Abrir `src/AerolineaVIP_Grupo4.slnx` en Visual Studio, restaurar los paquetes NuGet y ejecutar (F5).
   Si aparece el error `roslyn\csc.exe`, cerrar y volver a abrir la solución y usar Compilar → Recompilar solución.
4. Al iniciar, `Global.asax.cs` llama a `InicializadorIdentity` y crea por código los roles y los usuarios iniciales con su rol.

## Usuarios iniciales
- `superusuario` (SuperUsuario)
- `admin01` (Administrador)
- `counter01` (Counter)
- `cliente01` a `cliente05` (Cliente)

Las contraseñas de demostración no se publican aquí.

## Módulos del sistema (Fase 4)
| Módulo | Dónde está |
|---|---|
| MOD-01 Seguridad e Identity | `AccountController`, `ManageController`, `App_Start/InicializadorIdentity.cs` (roles y usuarios por código) |
| MOD-02 Itinerarios | `VuelosController`: búsqueda por origen, destino y fecha; resultados con escala (Quito–Galápagos vía Guayaquil) |
| MOD-03 Flota y disponibilidad | `Vuelos/Disponibilidad`, `Admin/Aeronaves`, mapa de asientos verde/rojo/morado; una aeronave no puede tener vuelos con horarios cruzados |
| MOD-04 Pasajeros | `Reservas/Pasajeros`, CRUD `Pasajeros` (el counter solo actualiza correo y teléfono) |
| MOD-05 Reservas | `ReservasController`: asiento → pasajeros → confirmación, sin sobreventa (revalidación en el servidor + índice único en SQL Server) |
| MOD-06 Compras y pagos | `ComprasController`: pago simulado con modal de confirmación |
| MOD-07 Check-in | `CheckInController`: ventana DP-12, solo asientos pagados y documento del pasajero |
| MOD-08 Comprobantes | `ComprobantesController`: vista previa, impresión y PDF |
| MOD-09 Counter | `CounterController`: búsqueda de reservas, vuelos del día, cambio de asiento, cobro y check-in asistido |
| MOD-10 Administración y reportes | `AdminController` + CRUD: vuelos, aeronaves, usuarios; reportes exportables a Excel (CSV) |

## Flujo principal
Buscar vuelo → elegir asiento → datos de pasajeros → confirmar reserva → pagar (modal) → check-in → comprobantes.

## Pantallas
Las 21 interfaces de los wireframes están en `src/AerolineaVIP/Views` (una carpeta por controlador) con el layout compartido `Views/Shared/_Layout.cshtml`. Los mantenimientos de las tablas se generaron con scaffolding.

## Estado
Versión `v2.0-segunda-entrega` · 4 de noviembre de 2026 (anterior: `v1.0-primera-entrega`) · Responsables: Melanie Camila Barrionuevo Gallegos y Daniel Alejandro Guevara Bedon · Revisada mediante pull requests.