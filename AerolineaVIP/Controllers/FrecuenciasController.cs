// -----------------------------------------------------------------------------
// Controlador CRUD generado con el patrón de scaffolding de ASP.NET MVC 5
// ("Controlador de MVC 5 con vistas que usan Entity Framework") y ajustado al
// proyecto: layout general, validación cliente/servidor, mensajes de las reglas de
// la base (triggers, CHECK, FK) y permisos por rol verificados en el servidor.
// Tabla: dbo.Frecuencia
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
    public class FrecuenciasController : Controller
    {
        private readonly AerolineaVIPEntities db = new AerolineaVIPEntities();

        private void CargarListas(Frecuencia e)
        {
            ViewBag.RutaId = new SelectList(db.Ruta.Include(r => r.AeropuertoOrigen).Include(r => r.AeropuertoDestino).ToList()
                .Select(r => new { r.RutaId, Texto = r.AeropuertoOrigen.Localidad + " → " + r.AeropuertoDestino.Localidad }), "RutaId", "Texto", e == null ? null : (object)e.RutaId);
            ViewBag.DiaSemana = new SelectList(Enumerable.Range(1, 7).Select(d => new { Valor = d, Texto = d + " · " + Formato.NombreDia(d) }), "Valor", "Texto", e == null ? null : (object)(int)e.DiaSemana);
        }

        // GET: Frecuencias
        public ActionResult Index()
        {
            ViewBag.Nota = "Las frecuencias reproducen los itinerarios confirmados (RN-04 a RN-06). Modificarlas cambia días u horarios: solo el SuperUsuario puede hacerlo.";
            return View(db.Frecuencia.AsNoTracking().Include(f => f.Ruta.AeropuertoOrigen).Include(f => f.Ruta.AeropuertoDestino).OrderBy(f => f.DiaSemana).ToList());
        }

        // GET: Frecuencias/Details
        public ActionResult Details(int? id)
        {
            if (!id.HasValue) return new HttpStatusCodeResult(HttpStatusCode.BadRequest);
            var e = db.Frecuencia.Find(id.Value);
            if (e == null) return HttpNotFound();
            return View(e);
        }

        // GET: Frecuencias/Create
        [Authorize(Roles = RolesApp.SuperUsuario)]
        public ActionResult Create()
        {
            var e = new Frecuencia { Activa = true };
            CargarListas(e);
            return View(e);
        }

        // POST: Frecuencias/Create
        [HttpPost]
        [ValidateAntiForgeryToken]
        [Authorize(Roles = RolesApp.SuperUsuario)]
        public ActionResult Create([Bind(Include = "RutaId,DiaSemana,HoraSalida,HoraLlegada,Activa")] Frecuencia e)
        {
            if (ModelState.IsValid)
            {
                try
                {
                    
                    db.Frecuencia.Add(e);
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

        // GET: Frecuencias/Edit
        [Authorize(Roles = RolesApp.SuperUsuario)]
        public ActionResult Edit(int? id)
        {
            if (!id.HasValue) return new HttpStatusCodeResult(HttpStatusCode.BadRequest);
            var e = db.Frecuencia.Find(id.Value);
            if (e == null) return HttpNotFound();
            CargarListas(e);
            return View(e);
        }

        // POST: Frecuencias/Edit
        [HttpPost, ActionName("Edit")]
        [ValidateAntiForgeryToken]
        [Authorize(Roles = RolesApp.SuperUsuario)]
        public ActionResult EditPost(int? id)
        {
            if (!id.HasValue) return new HttpStatusCodeResult(HttpStatusCode.BadRequest);
            var e = db.Frecuencia.Find(id.Value);
            if (e == null) return HttpNotFound();
            // Solo se actualizan los campos editables (equivalente a [Bind(Include = ...)]).
            if (TryUpdateModel(e, "", new[] { "RutaId", "DiaSemana", "HoraSalida", "HoraLlegada", "Activa" }))
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

        // GET: Frecuencias/Delete
        [Authorize(Roles = RolesApp.SuperUsuario)]
        public ActionResult Delete(int? id)
        {
            if (!id.HasValue) return new HttpStatusCodeResult(HttpStatusCode.BadRequest);
            var e = db.Frecuencia.Find(id.Value);
            if (e == null) return HttpNotFound();
            return View(e);
        }

        // POST: Frecuencias/Delete
        [HttpPost, ActionName("Delete")]
        [ValidateAntiForgeryToken]
        [Authorize(Roles = RolesApp.SuperUsuario)]
        public ActionResult DeleteConfirmed(int id)
        {
            var e = db.Frecuencia.Find(id);
            if (e == null) return HttpNotFound();
            try
            {
                db.Frecuencia.Remove(e);
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
