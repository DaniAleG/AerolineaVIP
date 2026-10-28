using System;
using System.Linq;
using System.Threading.Tasks;
using System.Web;
using System.Web.Mvc;
using Microsoft.AspNet.Identity;
using Microsoft.AspNet.Identity.Owin;
using Microsoft.Owin.Security;
using AerolineaVIP.Models;

namespace AerolineaVIP.Controllers
{
    /// <summary>Pantallas 2 (inicio de sesión), 3 (registro) y 4 (recuperación de acceso).</summary>
    [Authorize]
    public class AccountController : Controller
    {
        private ApplicationSignInManager _signInManager;
        private ApplicationUserManager _userManager;

        public ApplicationSignInManager SignInManager
        {
            get { return _signInManager ?? HttpContext.GetOwinContext().Get<ApplicationSignInManager>(); }
            private set { _signInManager = value; }
        }

        public ApplicationUserManager UserManager
        {
            get { return _userManager ?? HttpContext.GetOwinContext().GetUserManager<ApplicationUserManager>(); }
            private set { _userManager = value; }
        }

        private IAuthenticationManager AuthenticationManager
        {
            get { return HttpContext.GetOwinContext().Authentication; }
        }

        // ----- Pantalla 2: inicio de sesión -----
        [AllowAnonymous]
        public ActionResult Login(string returnUrl, bool expirada = false)
        {
            if (User.Identity.IsAuthenticated)
            {
                // Usuario autenticado sin el rol requerido por la página de origen:
                // se informa el acceso no autorizado (evita un bucle de redirecciones).
                if (!string.IsNullOrEmpty(returnUrl))
                {
                    Response.StatusCode = 403;
                    Response.TrySkipIisCustomErrors = true;
                    ViewBag.Mensaje = "No tiene permisos para acceder a esta sección.";
                    return View("~/Views/Shared/AccesoDenegado.cshtml");
                }
                return RedirigirSegunRol(null, null);
            }
            ViewBag.ReturnUrl = returnUrl;
            if (expirada || (!string.IsNullOrEmpty(returnUrl) && Request.Cookies[".AspNet.ApplicationCookie"] != null))
                ViewBag.Advertencia = "Su sesión expiró. Inicie sesión nuevamente.";
            else if (!string.IsNullOrEmpty(returnUrl))
                ViewBag.Advertencia = "Inicie sesión para continuar.";
            return View();
        }

        [HttpPost]
        [AllowAnonymous]
        [ValidateAntiForgeryToken]
        public async Task<ActionResult> Login(LoginViewModel model, string returnUrl)
        {
            ViewBag.ReturnUrl = returnUrl;
            if (!ModelState.IsValid) return View(model);

            // Se acepta nombre de usuario o correo; el mensaje de error es genérico.
            var texto = model.UsuarioOCorreo.Trim();
            var usuario = texto.Contains("@") ? await UserManager.FindByEmailAsync(texto) : await UserManager.FindByNameAsync(texto);
            var nombreUsuario = usuario != null ? usuario.UserName : texto;

            var result = await SignInManager.PasswordSignInAsync(nombreUsuario, model.Password, model.RememberMe, shouldLockout: true);
            switch (result)
            {
                case SignInStatus.Success:
                    return RedirigirSegunRol(usuario, returnUrl);
                case SignInStatus.LockedOut:
                    return View("Lockout");
                default:
                    model.Password = null;
                    ModelState.Remove("Password");
                    ModelState.AddModelError("", "Usuario o contraseña incorrectos.");
                    return View(model);
            }
        }

        /// <summary>Redirige a ReturnUrl (solo si es local) o al área del rol.</summary>
        private ActionResult RedirigirSegunRol(ApplicationUser usuario, string returnUrl)
        {
            if (!string.IsNullOrEmpty(returnUrl) && Url.IsLocalUrl(returnUrl)) return Redirect(returnUrl);
            var id = usuario != null ? usuario.Id : User.Identity.GetUserId();
            if (UserManager.IsInRole(id, RolesApp.SuperUsuario) || UserManager.IsInRole(id, RolesApp.Administrador))
                return RedirectToAction("Index", "Admin");
            if (UserManager.IsInRole(id, RolesApp.Counter))
                return RedirectToAction("MisReservas", "Reservas");
            return RedirectToAction("MisReservas", "Reservas");
        }

        // ----- Pantalla 3: registro -----
        private bool EsSuperUsuario { get { return User.Identity.IsAuthenticated && User.IsInRole(RolesApp.SuperUsuario); } }

        private void PrepararRoles(RegisterViewModel model)
        {
            model.PuedeElegirRol = EsSuperUsuario;
            var roles = EsSuperUsuario ? RolesApp.Lista : new[] { RolesApp.Cliente };
            model.Roles = roles.Select(r => new SelectListItem { Value = r, Text = RolesApp.Descripcion(r), Selected = r == model.Rol });
            if (!EsSuperUsuario) model.Rol = RolesApp.Cliente;
        }

        [AllowAnonymous]
        public ActionResult Register()
        {
            // Un usuario autenticado que no es SuperUsuario no crea cuentas.
            if (User.Identity.IsAuthenticated && !EsSuperUsuario) return RedirectToAction("Index", "Home");
            var model = new RegisterViewModel { Rol = RolesApp.Cliente };
            PrepararRoles(model);
            return View(model);
        }

        [HttpPost]
        [AllowAnonymous]
        [ValidateAntiForgeryToken]
        public async Task<ActionResult> Register(RegisterViewModel model)
        {
            if (User.Identity.IsAuthenticated && !EsSuperUsuario) return RedirectToAction("Index", "Home");

            // Solo el SuperUsuario puede elegir rol; los demás quedan como Cliente.
            var rol = EsSuperUsuario && RolesApp.Lista.Contains(model.Rol) ? model.Rol : RolesApp.Cliente;
            model.Rol = rol;
            ModelState.Remove("Rol");

            if (ModelState.IsValid)
            {
                if (await UserManager.FindByNameAsync(model.UserName) != null)
                    ModelState.AddModelError("UserName", "El nombre de usuario ya está registrado.");
                if (await UserManager.FindByEmailAsync(model.Email) != null)
                    ModelState.AddModelError("Email", "El correo ya está registrado.");
            }

            if (ModelState.IsValid)
            {
                var user = new ApplicationUser
                {
                    UserName = model.UserName.Trim(),
                    Email = model.Email.Trim(),
                    Nombres = model.Nombres.Trim(),
                    Apellidos = model.Apellidos.Trim()
                };
                var result = await UserManager.CreateAsync(user, model.Password);
                if (result.Succeeded)
                {
                    await UserManager.AddToRoleAsync(user.Id, rol);
                    if (EsSuperUsuario)
                    {
                        TempData["Confirmacion"] = "Cuenta «" + user.UserName + "» creada con el rol " + RolesApp.Descripcion(rol) + ".";
                        return RedirectToAction("Gestion", "Admin", new { tab = "usuarios" });
                    }
                    TempData["Confirmacion"] = "Cuenta creada correctamente. Ya puede iniciar sesión.";
                    return RedirectToAction("Login");
                }
                foreach (var error in result.Errors) ModelState.AddModelError("", Traducir(error));
            }

            model.Password = model.ConfirmPassword = null;
            ModelState.Remove("Password");
            ModelState.Remove("ConfirmPassword");
            PrepararRoles(model);
            return View(model);
        }

        private static string Traducir(string error)
        {
            if (error.Contains("Passwords must")) return "La contraseña no cumple los requisitos: mínimo 8 caracteres, una mayúscula, una minúscula, un número y un símbolo.";
            if (error.Contains("is already taken")) return "El nombre de usuario o el correo ya está registrado.";
            if (error.Contains("invalid, can only contain")) return "El nombre de usuario solo admite letras, números, punto o guion bajo.";
            return error;
        }

        // ----- Pantalla 4: recuperación de acceso -----
        [AllowAnonymous]
        public ActionResult ForgotPassword()
        {
            if (User.Identity.IsAuthenticated) return RedirectToAction("Index", "Home");
            return View();
        }

        [HttpPost]
        [AllowAnonymous]
        [ValidateAntiForgeryToken]
        public async Task<ActionResult> ForgotPassword(ForgotPasswordViewModel model)
        {
            if (!ModelState.IsValid) return View(model);

            // Control de solicitudes repetidas: una por minuto por sesión.
            var ultima = Session["UltimaRecuperacion"] as DateTime?;
            if (ultima.HasValue && (DateTime.Now - ultima.Value).TotalSeconds < 60)
            {
                ViewBag.Advertencia = "Ya se envió una solicitud recientemente. Intente más tarde.";
                return View(model);
            }
            Session["UltimaRecuperacion"] = DateTime.Now;

            var texto = model.UsuarioOCorreo.Trim();
            var user = texto.Contains("@") ? await UserManager.FindByEmailAsync(texto) : await UserManager.FindByNameAsync(texto);
            if (user != null && UserManager.UserTokenProvider != null)
            {
                // El token se genera solo si la cuenta existe y nunca se muestra en la respuesta.
                var code = await UserManager.GeneratePasswordResetTokenAsync(user.Id);
                var callbackUrl = Url.Action("ResetPassword", "Account", new { code = code }, protocol: Request.Url.Scheme);
                await UserManager.SendEmailAsync(user.Id, "Recuperar acceso - Aerolínea VIP",
                    "Para restablecer su contraseña abra este enlace: " + callbackUrl);
            }

            // Mensaje neutral: no revela si la cuenta existe.
            ViewBag.Informacion = "Si los datos corresponden a una cuenta registrada, recibirá las instrucciones para recuperar el acceso.";
            ModelState.Clear();
            return View(new ForgotPasswordViewModel());
        }

        [AllowAnonymous]
        public ActionResult ResetPassword(string code)
        {
            return code == null ? View("Error") : View(new ResetPasswordViewModel { Code = code });
        }

        [HttpPost]
        [AllowAnonymous]
        [ValidateAntiForgeryToken]
        public async Task<ActionResult> ResetPassword(ResetPasswordViewModel model)
        {
            if (!ModelState.IsValid) return View(model);
            var user = await UserManager.FindByEmailAsync(model.Email);
            if (user == null) return RedirectToAction("ResetPasswordConfirmation");
            var result = await UserManager.ResetPasswordAsync(user.Id, model.Code, model.Password);
            if (result.Succeeded) return RedirectToAction("ResetPasswordConfirmation");
            foreach (var error in result.Errors) ModelState.AddModelError("", error.Contains("Invalid token") ? "El enlace no es válido o expiró." : Traducir(error));
            return View(model);
        }

        [AllowAnonymous]
        public ActionResult ResetPasswordConfirmation()
        {
            return View();
        }

        // ----- Cerrar sesión -----
        [HttpPost]
        [ValidateAntiForgeryToken]
        public ActionResult LogOff()
        {
            AuthenticationManager.SignOut(DefaultAuthenticationTypes.ApplicationCookie);
            Session.Remove("ReservaEnCurso");
            return RedirectToAction("Index", "Home");
        }

        protected override void Dispose(bool disposing)
        {
            if (disposing)
            {
                if (_userManager != null) { _userManager.Dispose(); _userManager = null; }
                if (_signInManager != null) { _signInManager.Dispose(); _signInManager = null; }
            }
            base.Dispose(disposing);
        }
    }
}
