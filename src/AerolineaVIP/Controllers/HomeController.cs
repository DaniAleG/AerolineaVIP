using System;
using System.Linq;
using System.Web.Mvc;
using AerolineaVIP.Models.Dominio;
using AerolineaVIP.Models.ViewModels;
using AerolineaVIP.Servicios;

namespace AerolineaVIP.Controllers
{
    /// <summary>Pantalla 1. Página de inicio de la aerolínea.</summary>
    public class HomeController : Controller
    {
        private readonly AerolineaVIPEntities db = new AerolineaVIPEntities();

        public ActionResult Index()
        {
            var vm = new BusquedaVM { Origen = "UIO", Pasajeros = 1, Fecha = null };
            try
            {
                var servicio = new ServicioReservas(db);
                vm.Origenes = new SelectList(db.Aeropuerto.Where(a => a.Activo).OrderBy(a => a.Localidad).ToList()
                    .Select(a => new { a.CodigoIATA, Texto = a.Localidad + " (" + a.CodigoIATA + ")" }), "CodigoIATA", "Texto", vm.Origen);
                vm.Destinos = new SelectList(servicio.DestinosDesde(vm.Origen)
                    .Select(a => new { a.CodigoIATA, Texto = a.Localidad + " (" + a.CodigoIATA + ")" }), "CodigoIATA", "Texto");
                vm.DiasOperacion = servicio.PanelDiasOperacion();
            }
            catch (Exception ex)
            {
                // Sin conexión a la base de dominio: la portada se muestra igual con un aviso.
                ViewBag.ErrorBD = Infraestructura.ErroresBD.Mensaje(ex);
                vm.Origenes = Enumerable.Empty<SelectListItem>();
                vm.Destinos = Enumerable.Empty<SelectListItem>();
                vm.DiasOperacion = new System.Collections.Generic.List<RutaDiasVM>();
            }
            return View(vm);
        }

        public ActionResult About()
        {
            return View();
        }

        protected override void Dispose(bool disposing)
        {
            if (disposing) db.Dispose();
            base.Dispose(disposing);
        }
    }
}
