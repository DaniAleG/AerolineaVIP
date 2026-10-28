# Sistema web de compra y reserva de pasajes – Aerolínea VIP
Primera Entrega · Grupo 4

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
4. Al iniciar, `Global.asax.cs` llama a `InicializadorIdentity` y crea por código los roles y los usuarios iniciales con su rol.

## Usuarios iniciales
- `superusuario` (SuperUsuario)
- `admin01` (Administrador)
- `counter01` (Counter)
- `cliente01` a `cliente05` (Cliente)

Las contraseñas de demostración no se publican aquí.

## Pantallas
Las 21 interfaces de los wireframes están en `src/AerolineaVIP/Views` (una carpeta por controlador) con el layout compartido `Views/Shared/_Layout.cshtml`. Los mantenimientos de las tablas se generaron con scaffolding.

## Estado
Versión `v1.0-primera-entrega` · 28 de octubre de 2026 · Responsables: Melanie Camila Barrionuevo Gallegos y Daniel Alejandro Guevara Bedon · Revisada mediante pull requests.
