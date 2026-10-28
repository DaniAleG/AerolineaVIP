// CRUD de TipoDocumentoIdentidad (generado con scaffolding y adaptado).
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
    public class TiposDocumentoController : Controller
    {
        private readonly AerolineaVIPEntities db = new AerolineaVIPEntities();

        private void CargarListas(TipoDocumentoIdentidad e)
        {
            // Sin listas desplegables.
        }

        // GET: TiposDocumento
        public ActionResult Index()
        {
            ViewBag.Nota = "Catálogo usado en el registro de pasajeros (pantalla 9).";
            return View(db.TipoDocumentoIdentidad.AsNoTracking().OrderBy(t => t.Codigo).ToList());
        }

        // GET: TiposDocumento/Details
        public ActionResult Details(string id)
        {
            if (string.IsNullOrEmpty(id)) return new HttpStatusCodeResult(HttpStatusCode.BadRequest);
            var e = db.TipoDocumentoIdentidad.Find(id);
            if (e == null) return HttpNotFound();
            return View(e);
        }

        // GET: TiposDocumento/Create
        [Authorize(Roles = RolesApp.SuperUsuario)]
        public ActionResult Create()
        {
            var e = new TipoDocumentoIdentidad {  };
            CargarListas(e);
            return View(e);
        }

        // POST: TiposDocumento/Create
        [HttpPost]
        [ValidateAntiForgeryToken]
        [Authorize(Roles = RolesApp.SuperUsuario)]
        public ActionResult Create([Bind(Include = "Codigo,Descripcion")] TipoDocumentoIdentidad e)
        {
            if (ModelState.IsValid)
            {
                try
                {
                    
                    db.TipoDocumentoIdentidad.Add(e);
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

        // GET: TiposDocumento/Edit
        [Authorize(Roles = RolesApp.SuperUsuario)]
        public ActionResult Edit(string id)
        {
            if (string.IsNullOrEmpty(id)) return new HttpStatusCodeResult(HttpStatusCode.BadRequest);
            var e = db.TipoDocumentoIdentidad.Find(id);
            if (e == null) return HttpNotFound();
            CargarListas(e);
            return View(e);
        }

        // POST: TiposDocumento/Edit
        [HttpPost, ActionName("Edit")]
        [ValidateAntiForgeryToken]
        [Authorize(Roles = RolesApp.SuperUsuario)]
        public ActionResult EditPost(string id)
        {
            if (string.IsNullOrEmpty(id)) return new HttpStatusCodeResult(HttpStatusCode.BadRequest);
            var e = db.TipoDocumentoIdentidad.Find(id);
            if (e == null) return HttpNotFound();
            // Solo se actualizan los campos editables.
            if (TryUpdateModel(e, "", new[] { "Descripcion" }))
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

        // GET: TiposDocumento/Delete
        [Authorize(Roles = RolesApp.SuperUsuario)]
        public ActionResult Delete(string id)
        {
            if (string.IsNullOrEmpty(id)) return new HttpStatusCodeResult(HttpStatusCode.BadRequest);
            var e = db.TipoDocumentoIdentidad.Find(id);
            if (e == null) return HttpNotFound();
            return View(e);
        }

        // POST: TiposDocumento/Delete
        [HttpPost, ActionName("Delete")]
        [ValidateAntiForgeryToken]
        [Authorize(Roles = RolesApp.SuperUsuario)]
        public ActionResult DeleteConfirmed(string id)
        {
            var e = db.TipoDocumentoIdentidad.Find(id);
            if (e == null) return HttpNotFound();
            try
            {
                db.TipoDocumentoIdentidad.Remove(e);
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
