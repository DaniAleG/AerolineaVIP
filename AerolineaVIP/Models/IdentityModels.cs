using System.ComponentModel.DataAnnotations;
using System.Data.Entity;
using System.Security.Claims;
using System.Threading.Tasks;
using Microsoft.AspNet.Identity;
using Microsoft.AspNet.Identity.EntityFramework;

namespace AerolineaVIP.Models
{
    /// <summary>Usuario de Identity con Nombres y Apellidos.</summary>
    public class ApplicationUser : IdentityUser
    {
        [StringLength(100)]
        public string Nombres { get; set; }

        [StringLength(100)]
        public string Apellidos { get; set; }

        public string NombreCompleto
        {
            get { return ((Nombres ?? "") + " " + (Apellidos ?? "")).Trim(); }
        }

        public async Task<ClaimsIdentity> GenerateUserIdentityAsync(UserManager<ApplicationUser> manager)
        {
            // authenticationType debe coincidir con CookieAuthenticationOptions.AuthenticationType
            var userIdentity = await manager.CreateIdentityAsync(this, DefaultAuthenticationTypes.ApplicationCookie);
            userIdentity.AddClaim(new Claim("NombreCompleto", string.IsNullOrWhiteSpace(NombreCompleto) ? UserName : NombreCompleto));
            return userIdentity;
        }
    }

    /// <summary>Contexto de Identity. Usa la cadena DefaultConnection (AerolineaVIP_Identity).</summary>
    public class ApplicationDbContext : IdentityDbContext<ApplicationUser>
    {
        static ApplicationDbContext()
        {
            // La base y sus tablas las crea el script (parte B) o InicializadorIdentity.
            Database.SetInitializer<ApplicationDbContext>(null);
        }

        public ApplicationDbContext()
            : base("DefaultConnection", throwIfV1Schema: false)
        {
        }

        public static ApplicationDbContext Create()
        {
            return new ApplicationDbContext();
        }
    }
}
