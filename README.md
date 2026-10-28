# Aerolínea VIP · Primera Entrega (Grupo 4)

Sistema web monolítico para la compra, reserva y administración de pasajes de una aerolínea VIP.
**ASP.NET Framework 4.8.1 · MVC 5 · Razor · Bootstrap 5 · jQuery · ASP.NET Identity 2 · Entity Framework 6 · SQL Server.**

Universidad de las Fuerzas Armadas ESPE · Ingeniería en Tecnologías de la Información ·
Desarrollo Web para Integración de Tecnología · Ing. Juan Fernando Galarraga Hurtado ·
Grupo 4: Melanie Camila Barrionuevo Gallegos y Daniel Alejandro Guevara Bedon.

---

## 1. Puesta en marcha

1. **Base de datos.** Abra `Database/Script_BD_AerolineaVIP.sql` en SQL Server Management Studio y ejecútelo completo (F5).
   Crea dos bases **separadas**: `AerolineaVIP` (dominio) y `AerolineaVIP_Identity` (ASP.NET Identity).
2. **Cadenas de conexión.** En `AerolineaVIP/Web.config` ambas usan `Data Source=.` (instancia predeterminada, igual que el molde).
   Si usa SQL Express o LocalDB cambie `Data Source` a `.\SQLEXPRESS` o `(localdb)\MSSQLLocalDB` en **las dos** cadenas:
   - `DefaultConnection` → `AerolineaVIP_Identity`
   - `AerolineaVIPEntities` → `AerolineaVIP`
3. **Paquetes NuGet.** Abra `AerolineaVIP.slnx` (Visual Studio 2022 17.13 o superior) o `AerolineaVIP.sln`.
   Clic derecho en la solución → *Restaurar paquetes NuGet* (son los mismos paquetes del molde: MVC 5.3, EF 6.5.2, Identity 2.2.4, OWIN 4.2.3, Bootstrap 5.3.8, jQuery 3.7.1).
4. **Ejecutar** (F5). En el primer inicio `Global.asax.cs` llama a `InicializadorIdentity.Inicializar()`, que:
   - agrega las columnas `Nombres` y `Apellidos` a `AspNetUsers` (o ejecute `Database/Script_Complemento_Identity.sql`);
   - crea por código los roles **SuperUsuario, Administrador, Counter (empleado de counter) y Cliente**;
   - crea por código los usuarios iniciales y les asigna su rol.

Si la conexión falla, el sitio muestra un aviso rojo en la parte superior con la causa.

## 2. Usuarios iniciales (creados por código, datos ficticios)

| Usuario | Contraseña | Rol | Acceso |
|---|---|---|---|
| `superusuario` | `Super.Vip2026!` | SuperUsuario | Todas las funciones, incluida la asignación de roles y la creación de cuentas con cualquier rol |
| `admin01` | `Admin.Vip2026!` | Administrador | Pantallas 17–21 y CRUD permitidos (sin asignar roles ni modificar rutas, tarifas o frecuencias) |
| `counter01` | `Counter.Vip2026!` | Counter | Asistencia: reservar y pagar en nombre de un cliente, consulta de pasajeros y reservas, check-in |
| `cliente01` … `cliente05` | `Cliente.Vip2026!` | Cliente | Sus propias reservas. Tienen los mismos Id que `Reserva.UsuarioId` del script, así que ven las reservas de demostración |

El registro público (pantalla 3) **siempre** crea cuentas con rol Cliente: el rol enviado por el navegador se ignora en el servidor salvo que quien registra sea SuperUsuario.

## 3. Datos para probar

- Vuelos de demostración: **02/11/2026 al 08/11/2026** (y la semana pasada 12–18/10 ya completada). Busque, por ejemplo, Quito → Guayaquil el martes 03/11/2026 o Quito → Galápagos el domingo 08/11/2026 (itinerario con escala).
- Pago simulado: cualquier número con formato válido se **aprueba**; un número que **termina en 0000** se **rechaza**.
  Ejemplos: Visa `4111 1111 1111 1111`, Mastercard `5555 5555 5555 4444`, American Express `3782 822463 10005`, Diners `3056 930902 5904`.
- Check-in (DP-12): abre 24 h antes y cierra 1 h antes de la salida. Para demostrarlo con los vuelos de noviembre, cambie temporalmente `Politica:HorasAperturaCheckIn` a `240` en `Web.config`.

## 4. Políticas propuestas (apartado 18.1), parametrizables

Ninguna política está aprobada; por eso se configuran en `Web.config` (`appSettings`, claves `Politica:*`):
retención de reservas pendientes (DP-07: 24 h o hasta 2 h antes de la salida), cancelación de reservas pagadas
(DP-08: hasta 48 h antes), ventana de check-in (DP-12), conexión mínima/máxima en Guayaquil (DP-10: 60/240 min),
preparación entre vuelos de una aeronave (DP-03: 60 min), regla de rechazo del pago simulado y moneda.

## 5. Estructura

```
AerolineaVIP/
├─ App_Start/        BundleConfig, RouteConfig, IdentityConfig, Startup.Auth, InicializadorIdentity (roles y usuarios)
├─ Controllers/      Home, Account, Manage, Vuelos, Reservas, Compras, CheckIn, Comprobantes, Admin
│                    + 15 controladores CRUD con scaffolding (Aeropuertos, Rutas, Frecuencias, ModelosAeronave, Asientos,
│                      Aeronaves, VuelosCrud, Itinerarios, TramosItinerario, Pasajeros, MetodosPago, MarcasTarjeta,
│                      Monedas, TiposDocumento, DatosEmisor)
├─ Models/Dominio/   Entidades de EF mapeadas al script (tablas y vistas) y contexto AerolineaVIPEntities
├─ Models/Dominio/Metadatos/  Clases parciales con Data Annotations (validación central, RN-10 / 11.1.4)
├─ Models/ViewModels/         ViewModels fuertemente tipados de cada pantalla
├─ Servicios/        Reglas de negocio: reservas, pagos simulados, comprobantes (PDF), políticas, formatos
├─ Infraestructura/  Traducción de errores SQL (triggers 50001–50052, únicos, FK), binder decimal, validación de casillas
├─ Views/            Vistas Razor con _Layout compartido; parciales _MapaAsientos, _BarraVuelo, _Pasos…
├─ Scripts/app/      jQuery: site.js, validaciones.js, asientos.js (mapa), pago.js (formulario y modal)
└─ Content/          Site.css, asientos.css, imágenes propias (logotipo y distintivos neutros de tarjetas)
Database/            Script_BD_AerolineaVIP.sql (entregado por el docente) y Script_Complemento_Identity.sql
```

**Entity Framework.** Se usa el enfoque *Database First con clases de código* (equivalente a "Code First desde base de datos existente"):
las clases reproducen exactamente las tablas, claves compuestas, columnas calculadas y vistas del script, y el inicializador es `null`,
por lo que EF nunca crea ni altera la base (los triggers, vistas y el procedimiento siguen siendo la fuente de las reglas en SQL Server).
Las validaciones se declaran en clases parciales con `[MetadataType]`, como pide el apartado 11.1.4.

## 6. Las 21 pantallas

| N.º | Pantalla | Ruta |
|---|---|---|
| 1 | Página de inicio | `/Home/Index` |
| 2 | Inicio de sesión | `/Account/Login` |
| 3 | Registro de usuarios | `/Account/Register` |
| 4 | Recuperación de acceso | `/Account/ForgotPassword` |
| 5 | Búsqueda de vuelos | `/Vuelos/Buscar` |
| 6 | Resultados y horarios | `/Vuelos/Resultados` |
| 7 | Disponibilidad de asientos | `/Vuelos/Disponibilidad/{vueloId}` |
| 8 | Selección de vuelo y asiento | `/Reservas/SeleccionAsiento/{vueloId}` |
| 9 | Datos del pasajero | `/Reservas/Pasajeros` |
| 10 | Reserva de pasajes | `/Reservas/Confirmar` |
| 11 | Compra y pago simulado | `/Compras/Pagar/{reservaId}` |
| 12 | Confirmación | `/Compras/Confirmacion/{reservaId}` |
| 13 | Consulta de reservas | `/Reservas/MisReservas` |
| 14 | Gestión y cancelación | `/Reservas/Gestionar/{reservaId}` |
| 15 | Check-in | `/CheckIn/Index` |
| 16 | Comprobantes (PDF) | `/Comprobantes/Index` |
| 17 | Panel del administrador (solo accesos) | `/Admin/Index` |
| 18 | Vuelos e itinerarios | `/Admin/Vuelos` |
| 19 | Aeronaves | `/Admin/Aeronaves` |
| 20 | Usuarios, pasajeros y reservas | `/Admin/Gestion` |
| 21 | Consultas y reportes | `/Admin/Reportes` |

Fuera del alcance de esta entrega (según las instrucciones): dashboard, auditoría, concurrencia avanzada y bloqueos, pruebas formales, despliegue, pagos reales.

## 7. GitHub

```
git init
git add .
git commit -m "Primera entrega: proyecto Aerolínea VIP"
git branch -M main
git remote add origin https://github.com/<usuario>/<repositorio>.git
git push -u origin main
```
El `.gitignore` excluye `bin`, `obj`, `packages` y `.vs`.
