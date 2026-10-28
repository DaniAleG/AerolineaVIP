using System.Collections.Generic;
using System.Linq;
using AerolineaVIP.Models;

namespace AerolineaVIP.Servicios
{
    /// <summary>Consulta usuarios de la base de Identity.</summary>
    public static class ServicioUsuarios
    {
        public class DatosUsuario
        {
            public string Id { get; set; }
            public string UserName { get; set; }
            public string Email { get; set; }
            public string NombreCompleto { get; set; }
            public string Etiqueta
            {
                get { return string.IsNullOrWhiteSpace(NombreCompleto) ? UserName : NombreCompleto + " (" + UserName + ")"; }
            }
        }

        public static Dictionary<string, DatosUsuario> Obtener(IEnumerable<string> ids)
        {
            var lista = ids.Where(i => !string.IsNullOrEmpty(i)).Distinct().ToList();
            using (var db = new ApplicationDbContext())
            {
                return db.Users.Where(u => lista.Contains(u.Id))
                    .Select(u => new DatosUsuario
                    {
                        Id = u.Id,
                        UserName = u.UserName,
                        Email = u.Email,
                        NombreCompleto = ((u.Nombres ?? "") + " " + (u.Apellidos ?? "")).Trim()
                    })
                    .ToList()
                    .ToDictionary(u => u.Id);
            }
        }

        public static DatosUsuario Obtener(string id)
        {
            DatosUsuario d;
            return Obtener(new[] { id }).TryGetValue(id ?? "", out d) ? d : null;
        }

        /// <summary>Ids de usuarios cuyo nombre de usuario, correo o nombre contiene el texto.</summary>
        public static List<string> Buscar(string texto)
        {
            using (var db = new ApplicationDbContext())
            {
                return db.Users.Where(u => u.UserName.Contains(texto) || u.Email.Contains(texto)
                                           || u.Nombres.Contains(texto) || u.Apellidos.Contains(texto))
                    .Select(u => u.Id).ToList();
            }
        }

        /// <summary>Busca un cliente por nombre de usuario o correo (asistencia de counter).</summary>
        public static ApplicationUser BuscarCliente(string usuarioOCorreo)
        {
            if (string.IsNullOrWhiteSpace(usuarioOCorreo)) return null;
            var texto = usuarioOCorreo.Trim();
            using (var db = new ApplicationDbContext())
            {
                var rolCliente = db.Roles.FirstOrDefault(r => r.Name == RolesApp.Cliente);
                if (rolCliente == null) return null;
                return db.Users.FirstOrDefault(u => (u.UserName == texto || u.Email == texto)
                                                    && u.Roles.Any(r => r.RoleId == rolCliente.Id));
            }
        }
    }
}
