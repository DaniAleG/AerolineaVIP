using System.Web.Mvc;
using Microsoft.AspNet.Identity;
using AerolineaVIP.Models.Dominio;
using AerolineaVIP.Servicios;

namespace AerolineaVIP.Controllers
{
    /// <summary>
    /// Base de los controladores de dominio: contexto de EF por solicitud y comprobación
    /// de permisos efectivos en el SERVIDOR (RNP-09), nunca solo ocultando botones.
    /// </summary>
    public abstract class ControladorBase : Controller
    {
        protected readonly AerolineaVIPEntities db = new AerolineaVIPEntities();

        private ServicioReservas servicioReservas;
        protected ServicioReservas Reservas { get { return servicioReservas ?? (servicioReservas = new ServicioReservas(db)); } }

        protected string UsuarioId { get { return User.Identity.GetUserId(); } }

        /// <summary>SuperUsuario o Administrador.</summary>
        protected bool EsAdministracion
        {
            get { return User.IsInRole(RolesApp.SuperUsuario) || User.IsInRole(RolesApp.Administrador); }
        }

        /// <summary>Personal autorizado: SuperUsuario, Administrador o Empleado de counter.</summary>
        protected bool EsPersonal
        {
            get { return EsAdministracion || User.IsInRole(RolesApp.Counter); }
        }

        /// <summary>El titular ve sus reservas; el personal autorizado puede ver cualquiera.</summary>
        protected bool PuedeAcceder(Reserva r)
        {
            return r != null && (r.UsuarioId == UsuarioId || EsPersonal);
        }

        /// <summary>Respuesta de acceso no autorizado sin revelar si la operación existe.</summary>
        protected ActionResult AccesoDenegado()
        {
            Response.StatusCode = 403;
            Response.TrySkipIisCustomErrors = true;
            return View("~/Views/Shared/AccesoDenegado.cshtml");
        }

        protected void Exito(string mensaje) { TempData["Confirmacion"] = mensaje; }
        protected void Error(string mensaje) { TempData["Error"] = mensaje; }
        protected void Informar(string mensaje) { TempData["Informacion"] = mensaje; }
        protected void Advertir(string mensaje) { TempData["Advertencia"] = mensaje; }

        protected override void Dispose(bool disposing)
        {
            if (disposing) db.Dispose();
            base.Dispose(disposing);
        }
    }
}
