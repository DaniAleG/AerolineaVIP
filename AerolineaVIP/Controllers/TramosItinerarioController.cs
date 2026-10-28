// -----------------------------------------------------------------------------
// Controlador CRUD generado con el patrón de scaffolding de ASP.NET MVC 5
// ("Controlador de MVC 5 con vistas que usan Entity Framework") y ajustado al
// proyecto: layout general, validación cliente/servidor, mensajes de las reglas de
// la base (triggers, CHECK, FK) y permisos por rol verificados en el servidor.
// Tabla: dbo.TramoItinerario
// -----------------------------------------------------------------------------
using System;
using System.Data.Entity;
using System.Linq;
using System.Net;
using System.Web.Mvc;
using AerolineaVIP.Infraestructura;
using AerolineaVIP.Models.Dominio;
using AerolineaVIP.Servicios;

namespace AerolineaVIP.Controllers
{
    [Authorize(Roles = RolesApp.Administracion)]
    public class TramosItinerarioController : Controller
    {
        private readonly AerolineaVIPEntities db = new AerolineaVIPEntities();

        private void CargarListas(TramoItinerario e)
        {
            ViewBag.ItinerarioId = new SelectList(db.Itinerario.OrderByDescending(i => i.Referencia).ToList(), "ItinerarioId", "Referencia", e == null ? null : (object)e.ItinerarioId);
            ViewBag.VueloId = new SelectList(db.vw_VueloDetalle.OrderByDescending(v => v.FechaOperacion).ThenBy(v => v.HoraSalida).ToList()
                .Select(v => new { v.VueloId, Texto = v.CodigoVuelo + " · " + Formato.Fecha(v.FechaOperacion) + " · " + v.Origen + " → " + v.Destino + " " + Formato.Hora(v.HoraSalida) }),
                "VueloId", "Texto", e == null ? null : (object)e.VueloId);
        }

        // GET: TramosItinerario
        public ActionResult Index()
        {
            ViewBag.Nota = "El trigger trg_TramoItinerario_Validar exige continuidad geográfica y horaria entre tramos consecutivos (RNP-05).";
            return View(db.TramoItinerario.AsNoTracking().Include(t => t.Itinerario).Include(t => t.Vuelo.Frecuencia.Ruta.AeropuertoOrigen).Include(t => t.Vuelo.Frecuencia.Ruta.AeropuertoDestino).OrderByDescending(t => t.Itinerario.Referencia).ThenBy(t => t.Orden).Take(400).ToList());
        }

        // GET: TramosItinerario/Details
        public ActionResult Details(int? id)
        {
            if (!id.HasValue) return new HttpStatusCodeResult(HttpStatusCode.BadRequest);
            var e = db.TramoItinerario.Find(id.Value);
            if (e == null) return HttpNotFound();
            return View(e);
        }

        // GET: TramosItinerario/Create
        [Authorize(Roles = RolesApp.Administracion)]
        public ActionResult Create()
        {
            var e = new TramoItinerario { Orden = 1 };
            CargarListas(e);
            return View(e);
        }

        // POST: TramosItinerario/Create
        [HttpPost]
        [ValidateAntiForgeryToken]
        [Authorize(Roles = RolesApp.Administracion)]
        public ActionResult Create([Bind(Include = "ItinerarioId,VueloId,Orden")] TramoItinerario e)
        {
            if (ModelState.IsValid)
            {
                try
                {
                    
                    db.TramoItinerario.Add(e);
                    db.SaveChanges();
                    TempData["Confirmacion"] = "Registro creado correctamente.";
                    return RedirectToAction("Index");
                }
                catch (Exception ex)
                {
                    ModelState.AddModelError("", ErroresBD.Mensaje(ex));
                }
            }
            CargarListas(e);
            return View(e);
        }

        // GET: TramosItinerario/Edit
        [Authorize(Roles = RolesApp.Administracion)]
        public ActionResult Edit(int? id)
        {
            if (!id.HasValue) return new HttpStatusCodeResult(HttpStatusCode.BadRequest);
            var e = db.TramoItinerario.Find(id.Value);
            if (e == null) return HttpNotFound();
            CargarListas(e);
            return View(e);
        }

        // POST: TramosItinerario/Edit
        [HttpPost, ActionName("Edit")]
        [ValidateAntiForgeryToken]
        [Authorize(Roles = RolesApp.Administracion)]
        public ActionResult EditPost(int? id)
        {
            if (!id.HasValue) return new HttpStatusCodeResult(HttpStatusCode.BadRequest);
            var e = db.TramoItinerario.Find(id.Value);
            if (e == null) return HttpNotFound();
            // Solo se actualizan los campos editables (equivalente a [Bind(Include = ...)]).
            if (TryUpdateModel(e, "", new[] { "ItinerarioId", "VueloId", "Orden" }))
            {
                try
                {
                    db.SaveChanges();
                    TempData["Confirmacion"] = "Cambios guardados.";
                    return RedirectToAction("Index");
                }
                catch (Exception ex)
                {
                    ModelState.AddModelError("", ErroresBD.Mensaje(ex));
                }
            }
            CargarListas(e);
            return View(e);
        }

        // GET: TramosItinerario/Delete
        [Authorize(Roles = RolesApp.Administracion)]
        public ActionResult Delete(int? id)
        {
            if (!id.HasValue) return new HttpStatusCodeResult(HttpStatusCode.BadRequest);
            var e = db.TramoItinerario.Find(id.Value);
            if (e == null) return HttpNotFound();
            return View(e);
        }

        // POST: TramosItinerario/Delete
        [HttpPost, ActionName("Delete")]
        [ValidateAntiForgeryToken]
        [Authorize(Roles = RolesApp.Administracion)]
        public ActionResult DeleteConfirmed(int id)
        {
            var e = db.TramoItinerario.Find(id);
            if (e == null) return HttpNotFound();
            try
            {
                db.TramoItinerario.Remove(e);
                db.SaveChanges();
                TempData["Confirmacion"] = "Registro eliminado.";
                return RedirectToAction("Index");
            }
            catch (Exception ex)
            {
                // Integridad referencial: no se eliminan registros con información asociada.
                db.Entry(e).State = EntityState.Unchanged;
                ViewBag.Error = ErroresBD.Mensaje(ex);
                return View("Delete", e);
            }
        }

        protected override void Dispose(bool disposing)
        {
            if (disposing) db.Dispose();
            base.Dispose(disposing);
        }
    }
}
