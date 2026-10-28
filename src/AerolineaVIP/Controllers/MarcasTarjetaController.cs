// CRUD de MarcaTarjeta (generado con scaffolding y adaptado).
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
    public class MarcasTarjetaController : Controller
    {
        private readonly AerolineaVIPEntities db = new AerolineaVIPEntities();

        private void CargarListas(MarcaTarjeta e)
        {
            // Sin listas desplegables.
        }

        // GET: MarcasTarjeta
        public ActionResult Index()
        {
            ViewBag.Nota = "Las cuatro marcas son una configuración académica de presentación que debe confirmarse; no implican acuerdos con emisores. Los logotipos son marcadores neutros del grupo.";
            return View(db.MarcaTarjeta.AsNoTracking().OrderBy(m => m.Nombre).ToList());
        }

        // GET: MarcasTarjeta/Details
        public ActionResult Details(string id)
        {
            if (string.IsNullOrEmpty(id)) return new HttpStatusCodeResult(HttpStatusCode.BadRequest);
            var e = db.MarcaTarjeta.Find(id);
            if (e == null) return HttpNotFound();
            return View(e);
        }

        // GET: MarcasTarjeta/Create
        [Authorize(Roles = RolesApp.SuperUsuario)]
        public ActionResult Create()
        {
            var e = new MarcaTarjeta { Activa = true, LongitudNumero = 16, LongitudCodigoSeguridad = 3, RutaLogo = "~/Content/img/tarjetas/" };
            CargarListas(e);
            return View(e);
        }

        // POST: MarcasTarjeta/Create
        [HttpPost]
        [ValidateAntiForgeryToken]
        [Authorize(Roles = RolesApp.SuperUsuario)]
        public ActionResult Create([Bind(Include = "Codigo,Nombre,LongitudNumero,LongitudCodigoSeguridad,PrefijosReferencia,RutaLogo,Activa")] MarcaTarjeta e)
        {
            if (ModelState.IsValid)
            {
                try
                {
                    
                    db.MarcaTarjeta.Add(e);
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

        // GET: MarcasTarjeta/Edit
        [Authorize(Roles = RolesApp.SuperUsuario)]
        public ActionResult Edit(string id)
        {
            if (string.IsNullOrEmpty(id)) return new HttpStatusCodeResult(HttpStatusCode.BadRequest);
            var e = db.MarcaTarjeta.Find(id);
            if (e == null) return HttpNotFound();
            CargarListas(e);
            return View(e);
        }

        // POST: MarcasTarjeta/Edit
        [HttpPost, ActionName("Edit")]
        [ValidateAntiForgeryToken]
        [Authorize(Roles = RolesApp.SuperUsuario)]
        public ActionResult EditPost(string id)
        {
            if (string.IsNullOrEmpty(id)) return new HttpStatusCodeResult(HttpStatusCode.BadRequest);
            var e = db.MarcaTarjeta.Find(id);
            if (e == null) return HttpNotFound();
            // Solo se actualizan los campos editables.
            if (TryUpdateModel(e, "", new[] { "Nombre", "LongitudNumero", "LongitudCodigoSeguridad", "PrefijosReferencia", "RutaLogo", "Activa" }))
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

        // GET: MarcasTarjeta/Delete
        [Authorize(Roles = RolesApp.SuperUsuario)]
        public ActionResult Delete(string id)
        {
            if (string.IsNullOrEmpty(id)) return new HttpStatusCodeResult(HttpStatusCode.BadRequest);
            var e = db.MarcaTarjeta.Find(id);
            if (e == null) return HttpNotFound();
            return View(e);
        }

        // POST: MarcasTarjeta/Delete
        [HttpPost, ActionName("Delete")]
        [ValidateAntiForgeryToken]
        [Authorize(Roles = RolesApp.SuperUsuario)]
        public ActionResult DeleteConfirmed(string id)
        {
            var e = db.MarcaTarjeta.Find(id);
            if (e == null) return HttpNotFound();
            try
            {
                db.MarcaTarjeta.Remove(e);
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
