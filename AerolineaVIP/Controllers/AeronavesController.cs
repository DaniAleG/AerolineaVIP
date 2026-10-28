// CRUD de Aeronave (generado con scaffolding y adaptado).
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
    public class AeronavesController : Controller
    {
        private readonly AerolineaVIPEntities db = new AerolineaVIPEntities();

        private void CargarListas(Aeronave e)
        {
            ViewBag.ModeloAeronaveId = new SelectList(db.ModeloAeronave.ToList().Select(m => new { m.ModeloAeronaveId, Texto = m.Fabricante + " " + m.Nombre }), "ModeloAeronaveId", "Texto", e == null ? null : (object)e.ModeloAeronaveId);
            ViewBag.EstadoAeronaveCodigo = new SelectList(db.EstadoAeronave.ToList(), "Codigo", "Descripcion", e == null ? null : e.EstadoAeronaveCodigo);
        }

        // GET: Aeronaves
        public ActionResult Index()
        {
            ViewBag.Nota = "RN-01: la flota inicial consta de tres jets; registrar aeronaves adicionales requiere aprobación (solo SuperUsuario). El Administrador puede editar la matrícula y el estado operativo.";
            return View(db.Aeronave.AsNoTracking().Include(a => a.ModeloAeronave).Include(a => a.EstadoAeronave).OrderBy(a => a.Matricula).ToList());
        }

        // GET: Aeronaves/Details
        public ActionResult Details(int? id)
        {
            if (!id.HasValue) return new HttpStatusCodeResult(HttpStatusCode.BadRequest);
            var e = db.Aeronave.Find(id.Value);
            if (e == null) return HttpNotFound();
            return View(e);
        }

        // GET: Aeronaves/Create
        [Authorize(Roles = RolesApp.SuperUsuario)]
        public ActionResult Create()
        {
            var e = new Aeronave { EstadoAeronaveCodigo = "DISPONIBLE" };
            CargarListas(e);
            return View(e);
        }

        // POST: Aeronaves/Create
        [HttpPost]
        [ValidateAntiForgeryToken]
        [Authorize(Roles = RolesApp.SuperUsuario)]
        public ActionResult Create([Bind(Include = "Matricula,ModeloAeronaveId,EstadoAeronaveCodigo")] Aeronave e)
        {
            if (e.Matricula != null) e.Matricula = e.Matricula.Trim().ToUpper();
            if (ModelState.IsValid)
            {
                try
                {
                    
                    db.Aeronave.Add(e);
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

        // GET: Aeronaves/Edit
        [Authorize(Roles = RolesApp.Administracion)]
        public ActionResult Edit(int? id)
        {
            if (!id.HasValue) return new HttpStatusCodeResult(HttpStatusCode.BadRequest);
            var e = db.Aeronave.Find(id.Value);
            if (e == null) return HttpNotFound();
            CargarListas(e);
            return View(e);
        }

        // POST: Aeronaves/Edit
        [HttpPost, ActionName("Edit")]
        [ValidateAntiForgeryToken]
        [Authorize(Roles = RolesApp.Administracion)]
        public ActionResult EditPost(int? id)
        {
            if (!id.HasValue) return new HttpStatusCodeResult(HttpStatusCode.BadRequest);
            var e = db.Aeronave.Find(id.Value);
            if (e == null) return HttpNotFound();
            // Solo se actualizan los campos editables.
            if (TryUpdateModel(e, "", new[] { "Matricula", "ModeloAeronaveId", "EstadoAeronaveCodigo" }))
            {
                if (e.Matricula != null) e.Matricula = e.Matricula.Trim().ToUpper();
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

        // GET: Aeronaves/Delete
        [Authorize(Roles = RolesApp.SuperUsuario)]
        public ActionResult Delete(int? id)
        {
            if (!id.HasValue) return new HttpStatusCodeResult(HttpStatusCode.BadRequest);
            var e = db.Aeronave.Find(id.Value);
            if (e == null) return HttpNotFound();
            return View(e);
        }

        // POST: Aeronaves/Delete
        [HttpPost, ActionName("Delete")]
        [ValidateAntiForgeryToken]
        [Authorize(Roles = RolesApp.SuperUsuario)]
        public ActionResult DeleteConfirmed(int id)
        {
            var e = db.Aeronave.Find(id);
            if (e == null) return HttpNotFound();
            try
            {
                db.Aeronave.Remove(e);
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
