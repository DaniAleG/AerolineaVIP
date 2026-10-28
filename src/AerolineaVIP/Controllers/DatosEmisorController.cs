// CRUD de DatosEmisor (generado con scaffolding y adaptado).
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
    public class DatosEmisorController : Controller
    {
        private readonly AerolineaVIPEntities db = new AerolineaVIPEntities();

        private void CargarListas(DatosEmisor e)
        {
            // Sin listas desplegables.
        }

        // GET: DatosEmisor
        public ActionResult Index()
        {
            ViewBag.Nota = "Una sola fila (CK_DatosEmisor_UnaFila). La identificación y la numeración son de ejemplo hasta resolver DP-05.";
            return View(db.DatosEmisor.AsNoTracking().OrderBy(d => d.DatosEmisorId).ToList());
        }

        // GET: DatosEmisor/Details
        public ActionResult Details(byte? id)
        {
            if (!id.HasValue) return new HttpStatusCodeResult(HttpStatusCode.BadRequest);
            var e = db.DatosEmisor.Find(id.Value);
            if (e == null) return HttpNotFound();
            return View(e);
        }

        // GET: DatosEmisor/Edit
        [Authorize(Roles = RolesApp.SuperUsuario)]
        public ActionResult Edit(byte? id)
        {
            if (!id.HasValue) return new HttpStatusCodeResult(HttpStatusCode.BadRequest);
            var e = db.DatosEmisor.Find(id.Value);
            if (e == null) return HttpNotFound();
            CargarListas(e);
            return View(e);
        }

        // POST: DatosEmisor/Edit
        [HttpPost, ActionName("Edit")]
        [ValidateAntiForgeryToken]
        [Authorize(Roles = RolesApp.SuperUsuario)]
        public ActionResult EditPost(byte? id)
        {
            if (!id.HasValue) return new HttpStatusCodeResult(HttpStatusCode.BadRequest);
            var e = db.DatosEmisor.Find(id.Value);
            if (e == null) return HttpNotFound();
            // Solo se actualizan los campos editables.
            if (TryUpdateModel(e, "", new[] { "NombreComercial", "Identificacion", "Direccion", "Telefono", "Correo", "LeyendaComprobante" }))
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

        protected override void Dispose(bool disposing)
        {
            if (disposing) db.Dispose();
            base.Dispose(disposing);
        }
    }
}
