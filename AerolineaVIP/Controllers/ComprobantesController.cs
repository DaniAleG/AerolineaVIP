using System.Linq;
using System.Web.Mvc;
using AerolineaVIP.Models.ViewModels;
using AerolineaVIP.Servicios;

namespace AerolineaVIP.Controllers
{
    /// <summary>
    /// Pantalla 16. Comprobantes ACADÉMICOS: listado, vista previa e impresión, y PDF
    /// generado en la aplicación con los datos persistidos (vw_ComprobanteDetalle).
    /// </summary>
    [Authorize(Roles = RolesApp.Todos)]
    public class ComprobantesController : ControladorBase
    {
        public ActionResult Index(ComprobantesVM filtros)
        {
            filtros = filtros ?? new ComprobantesVM();
            filtros.EsPersonal = EsPersonal;
            filtros.Tipos = new SelectList(new[]
            {
                new { V = "", T = "Todos" },
                new { V = "CONFIRMACION", T = "Confirmación" },
                new { V = "FACTURA", T = "Comprobante de venta académico" },
                new { V = "NOTA_CREDITO", T = "Nota de crédito académica" }
            }, "V", "T", filtros.Tipo);

            var q = db.vw_ComprobanteDetalle.AsNoTracking().AsQueryable();
            if (!EsPersonal)
            {
                var id = UsuarioId;
                q = q.Where(c => c.UsuarioId == id);     // solo comprobantes propios
            }
            if (ModelState.IsValid)
            {
                if (filtros.Desde.HasValue) { var d = filtros.Desde.Value.Date; q = q.Where(c => c.FechaEmision >= d); }
                if (filtros.Hasta.HasValue) { var h = filtros.Hasta.Value.Date.AddDays(1); q = q.Where(c => c.FechaEmision < h); }
                if (!string.IsNullOrEmpty(filtros.Tipo)) q = q.Where(c => c.TipoComprobanteCodigo == filtros.Tipo);
                if (!string.IsNullOrWhiteSpace(filtros.Buscar))
                {
                    var t = filtros.Buscar.Trim().ToUpper();
                    q = q.Where(c => c.CodigoReserva == t || c.NumeroComprobante == t);
                }
            }
            filtros.Lista = q.OrderByDescending(c => c.FechaEmision).ThenBy(c => c.NumeroComprobante).Take(200).ToList();

            if (filtros.Id.HasValue)
            {
                var permitido = filtros.Lista.Any(c => c.ComprobanteId == filtros.Id.Value)
                                || EsPersonal
                                || db.vw_ComprobanteDetalle.Any(c => c.ComprobanteId == filtros.Id.Value && c.UsuarioId == UsuarioId);
                if (permitido) filtros.Seleccionado = new ServicioComprobantes(db).Documento(filtros.Id.Value);
            }
            return View(filtros);
        }

        /// <summary>Descarga el comprobante en PDF (solo titular o personal autorizado).</summary>
        public ActionResult Pdf(int id)
        {
            var enc = db.vw_ComprobanteDetalle.AsNoTracking().FirstOrDefault(c => c.ComprobanteId == id);
            if (enc == null || (!EsPersonal && enc.UsuarioId != UsuarioId)) return AccesoDenegado();

            var servicio = new ServicioComprobantes(db);
            var documento = servicio.Documento(id);
            var bytes = servicio.Pdf(documento);
            return File(bytes, "application/pdf", "Comprobante_" + enc.NumeroComprobante + ".pdf");
        }
    }
}
