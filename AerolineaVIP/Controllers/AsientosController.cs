// CRUD de Asiento (generado con scaffolding y adaptado).
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
    public class AsientosController : Controller
    {
        private readonly AerolineaVIPEntities db = new AerolineaVIPEntities();

        private void CargarListas(Asiento e)
        {
            ViewBag.ModeloAeronaveId = new SelectList(db.ModeloAeronave.ToList().Select(m => new { m.ModeloAeronaveId, Texto = m.Fabricante + " " + m.Nombre }), "ModeloAeronaveId", "Texto", e == null ? null : (object)e.ModeloAeronaveId);
            ViewBag.Lado = new SelectList(new[] { new { V = "D", T = "D · Derecho" }, new { V = "I", T = "I · Izquierdo" } }, "V", "T", e == null ? null : e.Lado);
        }

        // GET: Asientos
        public ActionResult Index()
        {
            ViewBag.Nota = "Nomenclatura DP-11: fila (1–4) + lado (D/I). No existe 1I (zona de la puerta). Clave compuesta (modelo, número).";
            return View(db.Asiento.AsNoTracking().Include(s => s.ModeloAeronave).OrderBy(s => s.NumeroAsiento).ToList());
        }

        // GET: Asientos/Details
        public ActionResult Details(int? modeloAeronaveId, byte? numeroAsiento)
        {
            if (!modeloAeronaveId.HasValue || !numeroAsiento.HasValue) return new HttpStatusCodeResult(HttpStatusCode.BadRequest);
            var e = db.Asiento.Find(modeloAeronaveId.Value, numeroAsiento.Value);
            if (e == null) return HttpNotFound();
            return View(e);
        }

        // GET: Asientos/Create
        [Authorize(Roles = RolesApp.SuperUsuario)]
        public ActionResult Create()
        {
            var e = new Asiento {  };
            CargarListas(e);
            return View(e);
        }

        // POST: Asientos/Create
        [HttpPost]
        [ValidateAntiForgeryToken]
        [Authorize(Roles = RolesApp.SuperUsuario)]
        public ActionResult Create([Bind(Include = "ModeloAeronaveId,NumeroAsiento,Codigo,Fila,Lado,Ubicacion")] Asiento e)
        {
            if (e.Codigo != null) e.Codigo = e.Codigo.Trim().ToUpper();
            if (ModelState.IsValid)
            {
                try
                {
                    
                    db.Asiento.Add(e);
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

        // GET: Asientos/Edit
        [Authorize(Roles = RolesApp.SuperUsuario)]
        public ActionResult Edit(int? modeloAeronaveId, byte? numeroAsiento)
        {
            if (!modeloAeronaveId.HasValue || !numeroAsiento.HasValue) return new HttpStatusCodeResult(HttpStatusCode.BadRequest);
            var e = db.Asiento.Find(modeloAeronaveId.Value, numeroAsiento.Value);
            if (e == null) return HttpNotFound();
            CargarListas(e);
            return View(e);
        }

        // POST: Asientos/Edit
        [HttpPost, ActionName("Edit")]
        [ValidateAntiForgeryToken]
        [Authorize(Roles = RolesApp.SuperUsuario)]
        public ActionResult EditPost(int? modeloAeronaveId, byte? numeroAsiento)
        {
            if (!modeloAeronaveId.HasValue || !numeroAsiento.HasValue) return new HttpStatusCodeResult(HttpStatusCode.BadRequest);
            var e = db.Asiento.Find(modeloAeronaveId.Value, numeroAsiento.Value);
            if (e == null) return HttpNotFound();
            // Solo se actualizan los campos editables.
            if (TryUpdateModel(e, "", new[] { "Codigo", "Fila", "Lado", "Ubicacion" }))
            {
                if (e.Codigo != null) e.Codigo = e.Codigo.Trim().ToUpper();
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

        // GET: Asientos/Delete
        [Authorize(Roles = RolesApp.SuperUsuario)]
        public ActionResult Delete(int? modeloAeronaveId, byte? numeroAsiento)
        {
            if (!modeloAeronaveId.HasValue || !numeroAsiento.HasValue) return new HttpStatusCodeResult(HttpStatusCode.BadRequest);
            var e = db.Asiento.Find(modeloAeronaveId.Value, numeroAsiento.Value);
            if (e == null) return HttpNotFound();
            return View(e);
        }

        // POST: Asientos/Delete
        [HttpPost, ActionName("Delete")]
        [ValidateAntiForgeryToken]
        [Authorize(Roles = RolesApp.SuperUsuario)]
        public ActionResult DeleteConfirmed(int modeloAeronaveId, byte numeroAsiento)
        {
            var e = db.Asiento.Find(modeloAeronaveId, numeroAsiento);
            if (e == null) return HttpNotFound();
            try
            {
                db.Asiento.Remove(e);
                db.SaveChanges();
                TempData["Confirmacion"] = "Registro eliminado.";
                return RedirectToAction("Index");
            }
            catch (Exception ex)
            {
                // No se elimina si tiene registros relacionados.
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
