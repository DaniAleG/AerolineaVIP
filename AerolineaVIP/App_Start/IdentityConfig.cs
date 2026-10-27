using System;
using System.Security.Claims;
using System.Threading.Tasks;
using Microsoft.AspNet.Identity;
using Microsoft.AspNet.Identity.EntityFramework;
using Microsoft.AspNet.Identity.Owin;
using Microsoft.Owin;
using Microsoft.Owin.Security;
using AerolineaVIP.Models;

namespace AerolineaVIP
{
    /// <summary>
    /// Servicio de correo SIMULADO (alcance académico): no envía correos reales.
    /// El enlace de recuperación de contraseña se escribe en la ventana de
    /// Salida (Output) de Visual Studio para poder probar el flujo.
    /// </summary>
    public class EmailService : IIdentityMessageService
    {
        public Task SendAsync(IdentityMessage message)
        {
            System.Diagnostics.Debug.WriteLine("[Correo simulado] Para: " + message.Destination);
            System.Diagnostics.Debug.WriteLine("[Correo simulado] Asunto: " + message.Subject);
            System.Diagnostics.Debug.WriteLine("[Correo simulado] " + message.Body);
            return Task.FromResult(0);
        }
    }

    public class SmsService : IIdentityMessageService
    {
        public Task SendAsync(IdentityMessage message)
        {
            return Task.FromResult(0);
        }
    }

    /// <summary>Administrador de usuarios de la aplicación (ASP.NET Identity 2).</summary>
    public class ApplicationUserManager : UserManager<ApplicationUser>
    {
        public ApplicationUserManager(IUserStore<ApplicationUser> store)
            : base(store)
        {
        }

        public static ApplicationUserManager Create(IdentityFactoryOptions<ApplicationUserManager> options, IOwinContext context)
        {
            var manager = Configurar(new ApplicationUserManager(new UserStore<ApplicationUser>(context.Get<ApplicationDbContext>())));
            var dataProtectionProvider = options.DataProtectionProvider;
            if (dataProtectionProvider != null)
            {
                manager.UserTokenProvider =
                    new DataProtectorTokenProvider<ApplicationUser>(dataProtectionProvider.Create("ASP.NET Identity"))
                    {
                        TokenLifespan = TimeSpan.FromHours(2)
                    };
            }
            return manager;
        }

        /// <summary>Crea un administrador sin contexto OWIN (inicialización en Global.asax).</summary>
        public static ApplicationUserManager CrearSinOwin(ApplicationDbContext db)
        {
            return Configurar(new ApplicationUserManager(new UserStore<ApplicationUser>(db)));
        }

        private static ApplicationUserManager Configurar(ApplicationUserManager manager)
        {
            // Nombre de usuario: letras, números, punto o guion bajo; correo único.
            manager.UserValidator = new UserValidator<ApplicationUser>(manager)
            {
                AllowOnlyAlphanumericUserNames = false,
                RequireUniqueEmail = true
            };

            // Política de contraseñas del grupo: mínimo 8, mayúscula, minúscula, número y símbolo.
            manager.PasswordValidator = new PasswordValidator
            {
                RequiredLength = 8,
                RequireNonLetterOrDigit = true,
                RequireDigit = true,
                RequireLowercase = true,
                RequireUppercase = true,
            };

            // Bloqueo temporal tras 5 intentos fallidos (pantalla 2).
            manager.UserLockoutEnabledByDefault = true;
            manager.DefaultAccountLockoutTimeSpan = TimeSpan.FromMinutes(5);
            manager.MaxFailedAccessAttemptsBeforeLockout = 5;

            manager.EmailService = new EmailService();
            manager.SmsService = new SmsService();
            return manager;
        }
    }

    /// <summary>Administrador de roles de la aplicación.</summary>
    public class ApplicationRoleManager : RoleManager<IdentityRole>
    {
        public ApplicationRoleManager(IRoleStore<IdentityRole, string> store) : base(store)
        {
        }

        public static ApplicationRoleManager Create(IdentityFactoryOptions<ApplicationRoleManager> options, IOwinContext context)
        {
            return new ApplicationRoleManager(new RoleStore<IdentityRole>(context.Get<ApplicationDbContext>()));
        }
    }

    /// <summary>Administrador de inicio de sesión.</summary>
    public class ApplicationSignInManager : SignInManager<ApplicationUser, string>
    {
        public ApplicationSignInManager(ApplicationUserManager userManager, IAuthenticationManager authenticationManager)
            : base(userManager, authenticationManager)
        {
        }

        public override Task<ClaimsIdentity> CreateUserIdentityAsync(ApplicationUser user)
        {
            return user.GenerateUserIdentityAsync((ApplicationUserManager)UserManager);
        }

        public static ApplicationSignInManager Create(IdentityFactoryOptions<ApplicationSignInManager> options, IOwinContext context)
        {
            return new ApplicationSignInManager(context.GetUserManager<ApplicationUserManager>(), context.Authentication);
        }
    }
}
