// CRUD de ModeloAeronave (generado con scaffolding y adaptado).
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
    public class ModelosAeronaveController : Controller
    {
        private readonly AerolineaVIPEntities db = new AerolineaVIPEntities();

        private void CargarListas(ModeloAeronave e)
        {
            // Sin listas desplegables.
        }

        // GET: ModelosAeronave
        public ActionResult Index()
        {
            ViewBag.Nota = "La capacidad máxima es de siete asientos por vuelo (RN-03); la base lo garantiza con CK_ModeloAeronave_Capacidad.";
            return View(db.ModeloAeronave.AsNoTracking().OrderBy(m => m.Nombre).ToList());
        }

        // GET: ModelosAeronave/Details
        public ActionResult Details(int? id)
        {
            if (!id.HasValue) return new HttpStatusCodeResult(HttpStatusCode.BadRequest);
            var e = db.ModeloAeronave.Find(id.Value);
            if (e == null) return HttpNotFound();
            return View(e);
        }

        // GET: ModelosAeronave/Create
        [Authorize(Roles = RolesApp.SuperUsuario)]
        public ActionResult Create()
        {
            var e = new ModeloAeronave { CapacidadAsientos = 7 };
            CargarListas(e);
            return View(e);
        }

        // POST: ModelosAeronave/Create
        [HttpPost]
        [ValidateAntiForgeryToken]
        [Authorize(Roles = RolesApp.SuperUsuario)]
        public ActionResult Create([Bind(Include = "Fabricante,Nombre,CapacidadAsientos")] ModeloAeronave e)
        {
            if (ModelState.IsValid)
            {
                try
                {
                    
                    db.ModeloAeronave.Add(e);
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

        // GET: ModelosAeronave/Edit
        [Authorize(Roles = RolesApp.SuperUsuario)]
        public ActionResult Edit(int? id)
        {
            if (!id.HasValue) return new HttpStatusCodeResult(HttpStatusCode.BadRequest);
            var e = db.ModeloAeronave.Find(id.Value);
            if (e == null) return HttpNotFound();
            CargarListas(e);
            return View(e);
        }

        // POST: ModelosAeronave/Edit
        [HttpPost, ActionName("Edit")]
        [ValidateAntiForgeryToken]
        [Authorize(Roles = RolesApp.SuperUsuario)]
        public ActionResult EditPost(int? id)
        {
            if (!id.HasValue) return new HttpStatusCodeResult(HttpStatusCode.BadRequest);
            var e = db.ModeloAeronave.Find(id.Value);
            if (e == null) return HttpNotFound();
            // Solo se actualizan los campos editables.
            if (TryUpdateModel(e, "", new[] { "Fabricante", "Nombre", "CapacidadAsientos" }))
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

        // GET: ModelosAeronave/Delete
        [Authorize(Roles = RolesApp.SuperUsuario)]
        public ActionResult Delete(int? id)
        {
            if (!id.HasValue) return new HttpStatusCodeResult(HttpStatusCode.BadRequest);
            var e = db.ModeloAeronave.Find(id.Value);
            if (e == null) return HttpNotFound();
            return View(e);
        }

        // POST: ModelosAeronave/Delete
        [HttpPost, ActionName("Delete")]
        [ValidateAntiForgeryToken]
        [Authorize(Roles = RolesApp.SuperUsuario)]
        public ActionResult DeleteConfirmed(int id)
        {
            var e = db.ModeloAeronave.Find(id);
            if (e == null) return HttpNotFound();
            try
            {
                db.ModeloAeronave.Remove(e);
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
