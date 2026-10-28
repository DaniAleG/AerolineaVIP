// -----------------------------------------------------------------------------
// Controlador CRUD generado con el patrón de scaffolding de ASP.NET MVC 5
// ("Controlador de MVC 5 con vistas que usan Entity Framework") y ajustado al
// proyecto: layout general, validación cliente/servidor, mensajes de las reglas de
// la base (triggers, CHECK, FK) y permisos por rol verificados en el servidor.
// Tabla: dbo.Vuelo
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
    public class VuelosCrudController : Controller
    {
        private readonly AerolineaVIPEntities db = new AerolineaVIPEntities();

        private void CargarListas(Vuelo e)
        {
            ViewBag.FrecuenciaId = new SelectList(db.Frecuencia.Include(f => f.Ruta.AeropuertoOrigen).Include(f => f.Ruta.AeropuertoDestino)
                .OrderBy(f => f.DiaSemana).ThenBy(f => f.HoraSalida).ToList()
                .Select(f => new { f.FrecuenciaId, Texto = Formato.NombreDia(f.DiaSemana) + " · " + f.Ruta.AeropuertoOrigen.CodigoIATA + " → " + f.Ruta.AeropuertoDestino.CodigoIATA + " · " + Formato.Horario(f.HoraSalida, f.HoraLlegada) }),
                "FrecuenciaId", "Texto", e == null ? null : (object)e.FrecuenciaId);
            ViewBag.AeronaveId = new SelectList(db.Aeronave.OrderBy(a => a.Matricula).ToList(), "AeronaveId", "Matricula", e == null ? null : e.AeronaveId);
            ViewBag.EstadoVueloCodigo = new SelectList(db.EstadoVuelo.ToList(), "Codigo", "Descripcion", e == null ? null : e.EstadoVueloCodigo);
        }

        // GET: VuelosCrud
        public ActionResult Index()
        {
            ViewBag.Nota = "El trigger trg_Vuelo_Validar comprueba que la fecha coincida con el día de la frecuencia, que la aeronave esté disponible y que no haya superposición horaria (RNP-02). La programación diaria se gestiona en la pantalla 18.";
            return View(db.Vuelo.AsNoTracking().Include(v => v.Frecuencia.Ruta.AeropuertoOrigen).Include(v => v.Frecuencia.Ruta.AeropuertoDestino).Include(v => v.Aeronave).Include(v => v.EstadoVuelo).OrderByDescending(v => v.FechaOperacion).Take(400).ToList());
        }

        // GET: VuelosCrud/Details
        public ActionResult Details(int? id)
        {
            if (!id.HasValue) return new HttpStatusCodeResult(HttpStatusCode.BadRequest);
            var e = db.Vuelo.Find(id.Value);
            if (e == null) return HttpNotFound();
            return View(e);
        }

        // GET: VuelosCrud/Create
        [Authorize(Roles = RolesApp.Administracion)]
        public ActionResult Create()
        {
            var e = new Vuelo { EstadoVueloCodigo = "PROGRAMADO", FechaOperacion = DateTime.Today };
            CargarListas(e);
            return View(e);
        }

        // POST: VuelosCrud/Create
        [HttpPost]
        [ValidateAntiForgeryToken]
        [Authorize(Roles = RolesApp.Administracion)]
        public ActionResult Create([Bind(Include = "FrecuenciaId,FechaOperacion,AeronaveId,EstadoVueloCodigo")] Vuelo e)
        {
            if (ModelState.IsValid)
            {
                try
                {
                    
                    db.Vuelo.Add(e);
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

        // GET: VuelosCrud/Edit
        [Authorize(Roles = RolesApp.Administracion)]
        public ActionResult Edit(int? id)
        {
            if (!id.HasValue) return new HttpStatusCodeResult(HttpStatusCode.BadRequest);
            var e = db.Vuelo.Find(id.Value);
            if (e == null) return HttpNotFound();
            CargarListas(e);
            return View(e);
        }

        // POST: VuelosCrud/Edit
        [HttpPost, ActionName("Edit")]
        [ValidateAntiForgeryToken]
        [Authorize(Roles = RolesApp.Administracion)]
        public ActionResult EditPost(int? id)
        {
            if (!id.HasValue) return new HttpStatusCodeResult(HttpStatusCode.BadRequest);
            var e = db.Vuelo.Find(id.Value);
            if (e == null) return HttpNotFound();
            // Solo se actualizan los campos editables (equivalente a [Bind(Include = ...)]).
            if (TryUpdateModel(e, "", new[] { "AeronaveId", "EstadoVueloCodigo" }))
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

        // GET: VuelosCrud/Delete
        [Authorize(Roles = RolesApp.SuperUsuario)]
        public ActionResult Delete(int? id)
        {
            if (!id.HasValue) return new HttpStatusCodeResult(HttpStatusCode.BadRequest);
            var e = db.Vuelo.Find(id.Value);
            if (e == null) return HttpNotFound();
            return View(e);
        }

        // POST: VuelosCrud/Delete
        [HttpPost, ActionName("Delete")]
        [ValidateAntiForgeryToken]
        [Authorize(Roles = RolesApp.SuperUsuario)]
        public ActionResult DeleteConfirmed(int id)
        {
            var e = db.Vuelo.Find(id);
            if (e == null) return HttpNotFound();
            try
            {
                db.Vuelo.Remove(e);
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
