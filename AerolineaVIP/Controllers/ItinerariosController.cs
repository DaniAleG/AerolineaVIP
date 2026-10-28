// -----------------------------------------------------------------------------
// Controlador CRUD generado con el patrón de scaffolding de ASP.NET MVC 5
// ("Controlador de MVC 5 con vistas que usan Entity Framework") y ajustado al
// proyecto: layout general, validación cliente/servidor, mensajes de las reglas de
// la base (triggers, CHECK, FK) y permisos por rol verificados en el servidor.
// Tabla: dbo.Itinerario
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
    public class ItinerariosController : Controller
    {
        private readonly AerolineaVIPEntities db = new AerolineaVIPEntities();

        private void CargarListas(Itinerario e)
        {
            ViewBag.EstadoItinerarioCodigo = new SelectList(db.EstadoItinerario.ToList(), "Codigo", "Descripcion", e == null ? null : e.EstadoItinerarioCodigo);
        }

        // GET: Itinerarios
        public ActionResult Index()
        {
            ViewBag.Nota = "Un itinerario agrupa tramos ordenados (por ejemplo, Quito – Galápagos con escala en Guayaquil).";
            return View(db.Itinerario.AsNoTracking().Include(i => i.EstadoItinerario).OrderByDescending(i => i.Referencia).ToList());
        }

        // GET: Itinerarios/Details
        public ActionResult Details(int? id)
        {
            if (!id.HasValue) return new HttpStatusCodeResult(HttpStatusCode.BadRequest);
            var e = db.Itinerario.Find(id.Value);
            if (e == null) return HttpNotFound();
            return View(e);
        }

        // GET: Itinerarios/Create
        [Authorize(Roles = RolesApp.Administracion)]
        public ActionResult Create()
        {
            var e = new Itinerario { EstadoItinerarioCodigo = "ACTIVO" };
            CargarListas(e);
            return View(e);
        }

        // POST: Itinerarios/Create
        [HttpPost]
        [ValidateAntiForgeryToken]
        [Authorize(Roles = RolesApp.Administracion)]
        public ActionResult Create([Bind(Include = "Referencia,Nombre,EstadoItinerarioCodigo")] Itinerario e)
        {
            if (e.Referencia != null) e.Referencia = e.Referencia.Trim().ToUpper();
            if (ModelState.IsValid)
            {
                try
                {
                    
                    db.Itinerario.Add(e);
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

        // GET: Itinerarios/Edit
        [Authorize(Roles = RolesApp.Administracion)]
        public ActionResult Edit(int? id)
        {
            if (!id.HasValue) return new HttpStatusCodeResult(HttpStatusCode.BadRequest);
            var e = db.Itinerario.Find(id.Value);
            if (e == null) return HttpNotFound();
            CargarListas(e);
            return View(e);
        }

        // POST: Itinerarios/Edit
        [HttpPost, ActionName("Edit")]
        [ValidateAntiForgeryToken]
        [Authorize(Roles = RolesApp.Administracion)]
        public ActionResult EditPost(int? id)
        {
            if (!id.HasValue) return new HttpStatusCodeResult(HttpStatusCode.BadRequest);
            var e = db.Itinerario.Find(id.Value);
            if (e == null) return HttpNotFound();
            // Solo se actualizan los campos editables (equivalente a [Bind(Include = ...)]).
            if (TryUpdateModel(e, "", new[] { "Referencia", "Nombre", "EstadoItinerarioCodigo" }))
            {
                if (e.Referencia != null) e.Referencia = e.Referencia.Trim().ToUpper();
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

        // GET: Itinerarios/Delete
        [Authorize(Roles = RolesApp.SuperUsuario)]
        public ActionResult Delete(int? id)
        {
            if (!id.HasValue) return new HttpStatusCodeResult(HttpStatusCode.BadRequest);
            var e = db.Itinerario.Find(id.Value);
            if (e == null) return HttpNotFound();
            return View(e);
        }

        // POST: Itinerarios/Delete
        [HttpPost, ActionName("Delete")]
        [ValidateAntiForgeryToken]
        [Authorize(Roles = RolesApp.SuperUsuario)]
        public ActionResult DeleteConfirmed(int id)
        {
            var e = db.Itinerario.Find(id);
            if (e == null) return HttpNotFound();
            try
            {
                db.Itinerario.Remove(e);
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
