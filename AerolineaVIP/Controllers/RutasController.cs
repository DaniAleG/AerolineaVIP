// -----------------------------------------------------------------------------
// Controlador CRUD generado con el patrón de scaffolding de ASP.NET MVC 5
// ("Controlador de MVC 5 con vistas que usan Entity Framework") y ajustado al
// proyecto: layout general, validación cliente/servidor, mensajes de las reglas de
// la base (triggers, CHECK, FK) y permisos por rol verificados en el servidor.
// Tabla: dbo.Ruta
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
    public class RutasController : Controller
    {
        private readonly AerolineaVIPEntities db = new AerolineaVIPEntities();

        private void CargarListas(Ruta e)
        {
            var aeropuertos = db.Aeropuerto.OrderBy(a => a.Localidad).ToList()
                .Select(a => new { a.AeropuertoId, Texto = a.Localidad + " (" + a.CodigoIATA + ")" }).ToList();
            ViewBag.AeropuertoOrigenId = new SelectList(aeropuertos, "AeropuertoId", "Texto", e == null ? null : (object)e.AeropuertoOrigenId);
            ViewBag.AeropuertoDestinoId = new SelectList(aeropuertos, "AeropuertoId", "Texto", e == null ? null : (object)e.AeropuertoDestinoId);
        }

        // GET: Rutas
        public ActionResult Index()
        {
            ViewBag.Nota = "TarifaReferencial es un valor de EJEMPLO por pasajero y tramo (DP-04). Solo el SuperUsuario modifica rutas y tarifas.";
            return View(db.Ruta.AsNoTracking().Include(r => r.AeropuertoOrigen).Include(r => r.AeropuertoDestino).OrderBy(r => r.AeropuertoOrigen.Localidad).ToList());
        }

        // GET: Rutas/Details
        public ActionResult Details(int? id)
        {
            if (!id.HasValue) return new HttpStatusCodeResult(HttpStatusCode.BadRequest);
            var e = db.Ruta.Find(id.Value);
            if (e == null) return HttpNotFound();
            return View(e);
        }

        // GET: Rutas/Create
        [Authorize(Roles = RolesApp.SuperUsuario)]
        public ActionResult Create()
        {
            var e = new Ruta { Activa = true };
            CargarListas(e);
            return View(e);
        }

        // POST: Rutas/Create
        [HttpPost]
        [ValidateAntiForgeryToken]
        [Authorize(Roles = RolesApp.SuperUsuario)]
        public ActionResult Create([Bind(Include = "AeropuertoOrigenId,AeropuertoDestinoId,TarifaReferencial,Activa")] Ruta e)
        {
            if (ModelState.IsValid)
            {
                try
                {
                    
                    db.Ruta.Add(e);
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

        // GET: Rutas/Edit
        [Authorize(Roles = RolesApp.SuperUsuario)]
        public ActionResult Edit(int? id)
        {
            if (!id.HasValue) return new HttpStatusCodeResult(HttpStatusCode.BadRequest);
            var e = db.Ruta.Find(id.Value);
            if (e == null) return HttpNotFound();
            CargarListas(e);
            return View(e);
        }

        // POST: Rutas/Edit
        [HttpPost, ActionName("Edit")]
        [ValidateAntiForgeryToken]
        [Authorize(Roles = RolesApp.SuperUsuario)]
        public ActionResult EditPost(int? id)
        {
            if (!id.HasValue) return new HttpStatusCodeResult(HttpStatusCode.BadRequest);
            var e = db.Ruta.Find(id.Value);
            if (e == null) return HttpNotFound();
            // Solo se actualizan los campos editables (equivalente a [Bind(Include = ...)]).
            if (TryUpdateModel(e, "", new[] { "AeropuertoOrigenId", "AeropuertoDestinoId", "TarifaReferencial", "Activa" }))
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

        // GET: Rutas/Delete
        [Authorize(Roles = RolesApp.SuperUsuario)]
        public ActionResult Delete(int? id)
        {
            if (!id.HasValue) return new HttpStatusCodeResult(HttpStatusCode.BadRequest);
            var e = db.Ruta.Find(id.Value);
            if (e == null) return HttpNotFound();
            return View(e);
        }

        // POST: Rutas/Delete
        [HttpPost, ActionName("Delete")]
        [ValidateAntiForgeryToken]
        [Authorize(Roles = RolesApp.SuperUsuario)]
        public ActionResult DeleteConfirmed(int id)
        {
            var e = db.Ruta.Find(id);
            if (e == null) return HttpNotFound();
            try
            {
                db.Ruta.Remove(e);
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
