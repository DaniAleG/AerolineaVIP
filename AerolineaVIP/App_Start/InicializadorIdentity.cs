// -----------------------------------------------------------------------------
// Inicialización por CÓDIGO de ASP.NET Identity (entregables 2 a 6 del ZIP):
//   - creación de los roles,
//   - creación del superusuario, del administrador y de los usuarios iniciales,
//   - asignación de roles a cada usuario.
// Se invoca desde Global.asax.cs (Application_Start). Es idempotente: si los roles
// o usuarios ya existen no se duplican.
// Los clientes de demostración usan los mismos Id que Script_BD_AerolineaVIP.sql
// asigna a Reserva.UsuarioId (7c1e4a20-000X-...), para que sus reservas aparezcan
// en "Mis reservas" y en los comprobantes.
// -----------------------------------------------------------------------------
using System;
using System.Data.Entity.Infrastructure;
using System.Linq;
using Microsoft.AspNet.Identity;
using Microsoft.AspNet.Identity.EntityFramework;
using AerolineaVIP.Models;

namespace AerolineaVIP
{
    /// <summary>Nombres de los roles del sistema.</summary>
    public static class RolesApp
    {
        public const string SuperUsuario = "SuperUsuario";
        public const string Administrador = "Administrador";
        public const string Counter = "Counter";          // Empleado de counter
        public const string Cliente = "Cliente";

        // Combinaciones usadas en [Authorize(Roles = ...)]
        public const string Administracion = SuperUsuario + "," + Administrador;
        public const string Personal = SuperUsuario + "," + Administrador + "," + Counter;
        public const string Operadores = SuperUsuario + "," + Counter + "," + Cliente;   // pueden crear reservas
        public const string Todos = SuperUsuario + "," + Administrador + "," + Counter + "," + Cliente;

        public static readonly string[] Lista = { SuperUsuario, Administrador, Counter, Cliente };

        public static string Descripcion(string rol)
        {
            switch (rol)
            {
                case SuperUsuario: return "SuperUsuario";
                case Administrador: return "Administrador";
                case Counter: return "Empleado de counter";
                case Cliente: return "Cliente";
                default: return rol;
            }
        }
    }

    public static class InicializadorIdentity
    {
        /// <summary>Mensaje del último error de inicialización (se muestra en el layout).</summary>
        public static string UltimoError { get; private set; }

        private class UsuarioInicial
        {
            public string Id, UserName, Email, Password, Nombres, Apellidos, Rol;
        }

        // Credenciales de demostración (ficticias). Cámbielas antes de cualquier uso real.
        private static readonly UsuarioInicial[] Usuarios =
        {
            new UsuarioInicial { UserName = "superusuario", Email = "superusuario@aerolineavip.test", Password = "Super.Vip2026!",
                                 Nombres = "Super", Apellidos = "Usuario", Rol = RolesApp.SuperUsuario },
            new UsuarioInicial { UserName = "admin01", Email = "admin01@aerolineavip.test", Password = "Admin.Vip2026!",
                                 Nombres = "Administrador", Apellidos = "General", Rol = RolesApp.Administrador },
            new UsuarioInicial { UserName = "counter01", Email = "counter01@aerolineavip.test", Password = "Counter.Vip2026!",
                                 Nombres = "Empleado", Apellidos = "Counter Quito", Rol = RolesApp.Counter },

            // Clientes enlazados con las reservas de demostración del script (R01 a R12).
            new UsuarioInicial { Id = "7c1e4a20-0001-4b6e-9a01-000000000001", UserName = "cliente01", Email = "mariana.cevallos@ejemplo.com",
                                 Password = "Cliente.Vip2026!", Nombres = "Mariana Isabel", Apellidos = "Cevallos Ortiz", Rol = RolesApp.Cliente },
            new UsuarioInicial { Id = "7c1e4a20-0002-4b6e-9a01-000000000002", UserName = "cliente02", Email = "carlos.andrade@ejemplo.com",
                                 Password = "Cliente.Vip2026!", Nombres = "Carlos Eduardo", Apellidos = "Andrade Villacís", Rol = RolesApp.Cliente },
            new UsuarioInicial { Id = "7c1e4a20-0003-4b6e-9a01-000000000003", UserName = "cliente03", Email = "diego.zambrano@ejemplo.com",
                                 Password = "Cliente.Vip2026!", Nombres = "Diego Sebastián", Apellidos = "Zambrano Loor", Rol = RolesApp.Cliente },
            new UsuarioInicial { Id = "7c1e4a20-0004-4b6e-9a01-000000000004", UserName = "cliente04", Email = "gabriela.herrera@ejemplo.com",
                                 Password = "Cliente.Vip2026!", Nombres = "Gabriela Fernanda", Apellidos = "Herrera Castillo", Rol = RolesApp.Cliente },
            new UsuarioInicial { Id = "7c1e4a20-0005-4b6e-9a01-000000000005", UserName = "cliente05", Email = "luis.mendoza@ejemplo.com",
                                 Password = "Cliente.Vip2026!", Nombres = "Luis Alberto", Apellidos = "Mendoza Vera", Rol = RolesApp.Cliente },
        };

        public static void Inicializar()
        {
            try
            {
                using (var db = new ApplicationDbContext())
                {
                    PrepararBase(db);
                    CrearRoles(db);
                    CrearUsuarios(db);
                }
                UltimoError = null;
            }
            catch (Exception ex)
            {
                var interna = ex;
                while (interna.InnerException != null) interna = interna.InnerException;
                UltimoError = "No se pudo inicializar ASP.NET Identity (base AerolineaVIP_Identity): " + interna.Message
                              + " Revise la cadena DefaultConnection en Web.config y ejecute Script_BD_AerolineaVIP.sql.";
                System.Diagnostics.Trace.TraceError(ex.ToString());
            }
        }

        /// <summary>
        /// Garantiza que existan la base, las tablas de Identity y las columnas
        /// Nombres/Apellidos que amplían AspNetUsers.
        /// </summary>
        private static void PrepararBase(ApplicationDbContext db)
        {
            if (!db.Database.Exists())
            {
                db.Database.Create();   // crea la base con el modelo completo (incluye Nombres y Apellidos)
                return;
            }

            var existeTabla = db.Database.SqlQuery<int>(
                "SELECT CASE WHEN OBJECT_ID(N'dbo.AspNetUsers') IS NULL THEN 0 ELSE 1 END").Single();
            if (existeTabla == 0)
            {
                var script = ((IObjectContextAdapter)db).ObjectContext.CreateDatabaseScript();
                db.Database.ExecuteSqlCommand(script);
                return;
            }

            db.Database.ExecuteSqlCommand(@"
IF COL_LENGTH(N'dbo.AspNetUsers', N'Nombres') IS NULL
    ALTER TABLE dbo.AspNetUsers ADD Nombres NVARCHAR(100) NULL;
IF COL_LENGTH(N'dbo.AspNetUsers', N'Apellidos') IS NULL
    ALTER TABLE dbo.AspNetUsers ADD Apellidos NVARCHAR(100) NULL;");
        }

        private static void CrearRoles(ApplicationDbContext db)
        {
            var roleManager = new RoleManager<IdentityRole>(new RoleStore<IdentityRole>(db));
            foreach (var rol in RolesApp.Lista)
            {
                if (!roleManager.RoleExists(rol))
                {
                    var r = roleManager.Create(new IdentityRole(rol));
                    if (!r.Succeeded) throw new InvalidOperationException("No se pudo crear el rol " + rol + ": " + string.Join(" ", r.Errors));
                }
            }
        }

        private static void CrearUsuarios(ApplicationDbContext db)
        {
            var userManager = ApplicationUserManager.CrearSinOwin(db);
            foreach (var u in Usuarios)
            {
                var usuario = userManager.FindByName(u.UserName);
                if (usuario == null)
                {
                    usuario = new ApplicationUser
                    {
                        UserName = u.UserName,
                        Email = u.Email,
                        EmailConfirmed = true,
                        Nombres = u.Nombres,
                        Apellidos = u.Apellidos
                    };
                    if (!string.IsNullOrEmpty(u.Id)) usuario.Id = u.Id;

                    var r = userManager.Create(usuario, u.Password);
                    if (!r.Succeeded)
                        throw new InvalidOperationException("No se pudo crear el usuario " + u.UserName + ": " + string.Join(" ", r.Errors));
                }

                if (!userManager.IsInRole(usuario.Id, u.Rol))
                {
                    var r = userManager.AddToRole(usuario.Id, u.Rol);
                    if (!r.Succeeded)
                        throw new InvalidOperationException("No se pudo asignar el rol " + u.Rol + " a " + u.UserName + ": " + string.Join(" ", r.Errors));
                }
            }
        }
    }
}
