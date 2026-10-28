using System.Linq;
using System.Threading.Tasks;
using System.Web;
using System.Web.Mvc;
using Microsoft.AspNet.Identity;
using Microsoft.AspNet.Identity.Owin;
using AerolineaVIP.Models;

namespace AerolineaVIP.Controllers
{
    /// <summary>Perfil del usuario autenticado (cuenta de Identity).</summary>
    [Authorize]
    public class ManageController : Controller
    {
        private ApplicationUserManager UserManager
        {
            get { return HttpContext.GetOwinContext().GetUserManager<ApplicationUserManager>(); }
        }

        private ApplicationSignInManager SignInManager
        {
            get { return HttpContext.GetOwinContext().Get<ApplicationSignInManager>(); }
        }

        public async Task<ActionResult> Index()
        {
            var user = await UserManager.FindByIdAsync(User.Identity.GetUserId());
            if (user == null) return HttpNotFound();
            return View(new PerfilViewModel
            {
                UserName = user.UserName,
                Nombres = user.Nombres,
                Apellidos = user.Apellidos,
                Email = user.Email,
                PhoneNumber = user.PhoneNumber,
                Roles = (await UserManager.GetRolesAsync(user.Id)).Select(RolesApp.Descripcion).ToList()
            });
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<ActionResult> Index(PerfilViewModel model)
        {
            var user = await UserManager.FindByIdAsync(User.Identity.GetUserId());
            if (user == null) return HttpNotFound();
            model.UserName = user.UserName;
            model.Roles = (await UserManager.GetRolesAsync(user.Id)).Select(RolesApp.Descripcion).ToList();
            if (!ModelState.IsValid) return View(model);

            var otro = await UserManager.FindByEmailAsync(model.Email.Trim());
            if (otro != null && otro.Id != user.Id)
            {
                ModelState.AddModelError("Email", "El correo ya está registrado.");
                return View(model);
            }

            user.Nombres = model.Nombres.Trim();
            user.Apellidos = model.Apellidos.Trim();
            user.Email = model.Email.Trim();
            user.PhoneNumber = string.IsNullOrWhiteSpace(model.PhoneNumber) ? null : model.PhoneNumber.Trim();
            var r = await UserManager.UpdateAsync(user);
            if (!r.Succeeded)
            {
                foreach (var e in r.Errors) ModelState.AddModelError("", e);
                return View(model);
            }
            await SignInManager.SignInAsync(user, isPersistent: false, rememberBrowser: false);
            TempData["Confirmacion"] = "Cambios guardados.";
            return RedirectToAction("Index");
        }

        public ActionResult ChangePassword()
        {
            return View();
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<ActionResult> ChangePassword(ChangePasswordViewModel model)
        {
            if (!ModelState.IsValid) return View(model);
            var result = await UserManager.ChangePasswordAsync(User.Identity.GetUserId(), model.OldPassword, model.NewPassword);
            if (result.Succeeded)
            {
                var user = await UserManager.FindByIdAsync(User.Identity.GetUserId());
                if (user != null) await SignInManager.SignInAsync(user, isPersistent: false, rememberBrowser: false);
                TempData["Confirmacion"] = "La contraseña se cambió correctamente.";
                return RedirectToAction("Index");
            }
            foreach (var error in result.Errors)
                ModelState.AddModelError("", error.Contains("Incorrect password") ? "La contraseña actual no es correcta." : error);
            return View(model);
        }
    }
}
