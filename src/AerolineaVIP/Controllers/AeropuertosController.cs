// CRUD de Aeropuerto (generado con scaffolding y adaptado).
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
    public class AeropuertosController : Controller
    {
        private readonly AerolineaVIPEntities db = new AerolineaVIPEntities();

        private void CargarListas(Aeropuerto e)
        {
            // Sin listas desplegables.
        }

        // GET: Aeropuertos
        public ActionResult Index()
        {
            ViewBag.Nota = "Los aeropuertos alimentan los selectores de búsqueda. Galápagos opera con un único aeropuerto activo: GPS (DP-01).";
            return View(db.Aeropuerto.AsNoTracking().OrderBy(a => a.Localidad).ToList());
        }

        // GET: Aeropuertos/Details
        public ActionResult Details(int? id)
        {
            if (!id.HasValue) return new HttpStatusCodeResult(HttpStatusCode.BadRequest);
            var e = db.Aeropuerto.Find(id.Value);
            if (e == null) return HttpNotFound();
            return View(e);
        }

        // GET: Aeropuertos/Create
        [Authorize(Roles = RolesApp.SuperUsuario)]
        public ActionResult Create()
        {
            var e = new Aeropuerto { Activo = true };
            CargarListas(e);
            return View(e);
        }

        // POST: Aeropuertos/Create
        [HttpPost]
        [ValidateAntiForgeryToken]
        [Authorize(Roles = RolesApp.SuperUsuario)]
        public ActionResult Create([Bind(Include = "CodigoIATA,Nombre,Localidad,Activo")] Aeropuerto e)
        {
            if (e.CodigoIATA != null) e.CodigoIATA = e.CodigoIATA.Trim().ToUpper();
            if (ModelState.IsValid)
            {
                try
                {
                    
                    db.Aeropuerto.Add(e);
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

        // GET: Aeropuertos/Edit
        [Authorize(Roles = RolesApp.SuperUsuario)]
        public ActionResult Edit(int? id)
        {
            if (!id.HasValue) return new HttpStatusCodeResult(HttpStatusCode.BadRequest);
            var e = db.Aeropuerto.Find(id.Value);
            if (e == null) return HttpNotFound();
            CargarListas(e);
            return View(e);
        }

        // POST: Aeropuertos/Edit
        [HttpPost, ActionName("Edit")]
        [ValidateAntiForgeryToken]
        [Authorize(Roles = RolesApp.SuperUsuario)]
        public ActionResult EditPost(int? id)
        {
            if (!id.HasValue) return new HttpStatusCodeResult(HttpStatusCode.BadRequest);
            var e = db.Aeropuerto.Find(id.Value);
            if (e == null) return HttpNotFound();
            // Solo se actualizan los campos editables.
            if (TryUpdateModel(e, "", new[] { "CodigoIATA", "Nombre", "Localidad", "Activo" }))
            {
                if (e.CodigoIATA != null) e.CodigoIATA = e.CodigoIATA.Trim().ToUpper();
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

        // GET: Aeropuertos/Delete
        [Authorize(Roles = RolesApp.SuperUsuario)]
        public ActionResult Delete(int? id)
        {
            if (!id.HasValue) return new HttpStatusCodeResult(HttpStatusCode.BadRequest);
            var e = db.Aeropuerto.Find(id.Value);
            if (e == null) return HttpNotFound();
            return View(e);
        }

        // POST: Aeropuertos/Delete
        [HttpPost, ActionName("Delete")]
        [ValidateAntiForgeryToken]
        [Authorize(Roles = RolesApp.SuperUsuario)]
        public ActionResult DeleteConfirmed(int id)
        {
            var e = db.Aeropuerto.Find(id);
            if (e == null) return HttpNotFound();
            try
            {
                db.Aeropuerto.Remove(e);
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
