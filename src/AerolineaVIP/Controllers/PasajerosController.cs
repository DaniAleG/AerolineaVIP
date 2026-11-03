// CRUD de Pasajero (generado con scaffolding y adaptado).
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
    [Authorize(Roles = RolesApp.Personal)]
    public class PasajerosController : Controller
    {
        private readonly AerolineaVIPEntities db = new AerolineaVIPEntities();

        /// <summary>El empleado de counter solo puede actualizar los datos de contacto.</summary>
        private bool SoloContacto
        {
            get { return !User.IsInRole(RolesApp.SuperUsuario) && !User.IsInRole(RolesApp.Administrador); }
        }

        private void CargarListas(Pasajero e)
        {
            ViewBag.SoloContacto = SoloContacto;
            ViewBag.TipoDocumentoCodigo = new SelectList(db.TipoDocumentoIdentidad.ToList(), "Codigo", "Descripcion", e == null ? null : e.TipoDocumentoCodigo);
        }

        // GET: Pasajeros
        public ActionResult Index()
        {
            ViewBag.Nota = "Datos personales con exposición mínima. No se eliminan pasajeros con reservas asociadas (integridad referencial).";
            return View(db.Pasajero.AsNoTracking().Include(p => p.TipoDocumentoIdentidad).OrderBy(p => p.Apellidos).Take(300).ToList());
        }

        // GET: Pasajeros/Details
        public ActionResult Details(int? id)
        {
            if (!id.HasValue) return new HttpStatusCodeResult(HttpStatusCode.BadRequest);
            var e = db.Pasajero.Find(id.Value);
            if (e == null) return HttpNotFound();
            return View(e);
        }

        // GET: Pasajeros/Create
        [Authorize(Roles = RolesApp.Administracion)]
        public ActionResult Create()
        {
            var e = new Pasajero { TipoDocumentoCodigo = "CEDULA" };
            CargarListas(e);
            return View(e);
        }

        // POST: Pasajeros/Create
        [HttpPost]
        [ValidateAntiForgeryToken]
        [Authorize(Roles = RolesApp.Administracion)]
        public ActionResult Create([Bind(Include = "TipoDocumentoCodigo,NumeroDocumento,Nombres,Apellidos,FechaNacimiento,Correo,Telefono")] Pasajero e)
        {
            if (e.NumeroDocumento != null) e.NumeroDocumento = e.NumeroDocumento.Trim().ToUpper();
            if (ModelState.IsValid)
            {
                try
                {
                    e.FechaRegistro = DateTime.Now;
                    db.Pasajero.Add(e);
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

        // GET: Pasajeros/Edit (el counter solo actualiza el contacto)
        public ActionResult Edit(int? id)
        {
            if (!id.HasValue) return new HttpStatusCodeResult(HttpStatusCode.BadRequest);
            var e = db.Pasajero.Find(id.Value);
            if (e == null) return HttpNotFound();
            CargarListas(e);
            return View(e);
        }

        // POST: Pasajeros/Edit
        [HttpPost, ActionName("Edit")]
        [ValidateAntiForgeryToken]
        public ActionResult EditPost(int? id)
        {
            if (!id.HasValue) return new HttpStatusCodeResult(HttpStatusCode.BadRequest);
            var e = db.Pasajero.Find(id.Value);
            if (e == null) return HttpNotFound();
            // Solo se actualizan los campos editables. El counter solo cambia correo y teléfono (MOD-04).
            var campos = SoloContacto
                ? new[] { "Correo", "Telefono" }
                : new[] { "TipoDocumentoCodigo", "NumeroDocumento", "Nombres", "Apellidos", "FechaNacimiento", "Correo", "Telefono" };
            if (TryUpdateModel(e, "", campos))
            {
                if (e.NumeroDocumento != null) e.NumeroDocumento = e.NumeroDocumento.Trim().ToUpper();
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

        // GET: Pasajeros/Delete
        [Authorize(Roles = RolesApp.Administracion)]
        public ActionResult Delete(int? id)
        {
            if (!id.HasValue) return new HttpStatusCodeResult(HttpStatusCode.BadRequest);
            var e = db.Pasajero.Find(id.Value);
            if (e == null) return HttpNotFound();
            return View(e);
        }

        // POST: Pasajeros/Delete
        [HttpPost, ActionName("Delete")]
        [ValidateAntiForgeryToken]
        [Authorize(Roles = RolesApp.Administracion)]
        public ActionResult DeleteConfirmed(int id)
        {
            var e = db.Pasajero.Find(id);
            if (e == null) return HttpNotFound();
            try
            {
                db.Pasajero.Remove(e);
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
