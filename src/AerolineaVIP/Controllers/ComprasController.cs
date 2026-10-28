using System;
using System.Linq;
using System.Web.Mvc;
using AerolineaVIP.Models.Dominio;
using AerolineaVIP.Models.ViewModels;
using AerolineaVIP.Servicios;

namespace AerolineaVIP.Controllers
{
    /// <summary>Pantallas 11 (compra y pago simulado) y 12 (confirmación).</summary>
    [Authorize]
    public class ComprasController : ControladorBase
    {
        private DetalleReservaVM DetalleCompleto(Reserva r)
        {
            var vm = Reservas.Detalle(r);
            var titular = ServicioUsuarios.Obtener(r.UsuarioId);
            vm.Titular = titular == null ? "(usuario no registrado)" : titular.Etiqueta;
            vm.TitularCorreo = titular == null ? null : titular.Email;
            return vm;
        }

        private void PrepararPago(PagoVM vm, Reserva r)
        {
            vm.Detalle = DetalleCompleto(r);
            vm.Metodos = db.MetodoPago.Where(m => m.Activo).OrderBy(m => m.Nombre).ToList();
            vm.Marcas = db.MarcaTarjeta.Where(m => m.Activa).OrderBy(m => m.Nombre).ToList();
        }

        /// <summary>Solo el titular, el counter o el SuperUsuario (en nombre del cliente) pagan.</summary>
        private bool PuedePagar(Reserva r)
        {
            return r != null && (r.UsuarioId == UsuarioId || User.IsInRole(RolesApp.Counter) || User.IsInRole(RolesApp.SuperUsuario));
        }

        // ----- Pantalla 11 -----
        public ActionResult Pagar(int? id)
        {
            if (!id.HasValue) return RedirectToAction("MisReservas", "Reservas");
            Reservas.ExpirarVencidas(true);
            var r = Reservas.CargarReserva(id.Value);
            if (!PuedePagar(r)) return AccesoDenegado();

            if (r.EstadoReservaCodigo != "PENDIENTE")
            {
                Informar(r.EstadoReservaCodigo == "CONFIRMADA"
                    ? "La reserva " + r.CodigoReserva + " ya está pagada."
                    : "La reserva " + r.CodigoReserva + " está " + Formato.TextoEstadoReserva(r.EstadoReservaCodigo).ToLower() + " y no puede pagarse.");
                return RedirectToAction("MisReservas", "Reservas");
            }

            var vm = new PagoVM
            {
                ReservaId = r.ReservaId,
                Token = Guid.NewGuid(),
                MetodoPagoCodigo = "TARJETA_CREDITO"
            };
            PrepararPago(vm, r);
            vm.Mensaje = "Pago pendiente: la compra aún no está confirmada como pagada.";
            vm.TipoMensaje = "info";
            return View(vm);
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public ActionResult Pagar(PagoVM model)
        {
            Reservas.ExpirarVencidas(true);
            var r = Reservas.CargarReserva(model.ReservaId);
            if (!PuedePagar(r)) return AccesoDenegado();

            if (r.EstadoReservaCodigo == "CONFIRMADA")
            {
                Informar("La compra ya estaba pagada; no se registró un cobro duplicado.");
                return RedirectToAction("Confirmacion", new { id = r.ReservaId });
            }
            if (r.EstadoReservaCodigo != "PENDIENTE")
            {
                Error("La reserva está " + Formato.TextoEstadoReserva(r.EstadoReservaCodigo).ToLower() + " y ya no puede pagarse.");
                return RedirectToAction("Gestionar", "Reservas", new { id = r.ReservaId });
            }

            // Vuelos aún comercializables (no cancelados ni en cierre de venta).
            foreach (var vueloId in r.Asignaciones.Select(a => a.VueloId).Distinct())
            {
                var v = Reservas.Vuelo(vueloId);
                if (v == null || !ServicioReservas.EstadosComercializables.Contains(v.EstadoVueloCodigo) || v.SalidaProgramada <= Politicas.Ahora)
                    ModelState.AddModelError("", "El vuelo " + (v == null ? "" : v.CodigoVuelo) + " ya no admite compras.");
            }

            var metodo = db.MetodoPago.FirstOrDefault(m => m.Codigo == model.MetodoPagoCodigo && m.Activo);
            if (metodo == null) ModelState.AddModelError("MetodoPagoCodigo", "Seleccione un método de pago válido.");
            var marca = db.MarcaTarjeta.FirstOrDefault(m => m.Codigo == model.MarcaTarjetaCodigo && m.Activa);
            if (ModelState.IsValid)
            {
                var pagos = new ServicioPagos(db);
                foreach (var e in pagos.ValidarTarjeta(model, marca)) ModelState.AddModelError(e.Key, e.Value);
            }

            if (!ModelState.IsValid)
            {
                // Por seguridad se vacían el número y el código de seguridad.
                model.NumeroTarjeta = null;
                model.CodigoSeguridad = null;
                ModelState.Remove("NumeroTarjeta");
                ModelState.Remove("CodigoSeguridad");
                PrepararPago(model, r);
                return View(model);
            }

            var resultado = new ServicioPagos(db).Procesar(model, r);
            if (resultado.Aprobado)
            {
                if (resultado.Duplicado) Informar(resultado.Mensaje); else Exito(resultado.Mensaje);
                return RedirectToAction("Confirmacion", new { id = r.ReservaId });
            }
            if (resultado.CodigoOperacion != null)
            {
                // Rechazo comercial de la simulación: pago registrado como RECHAZADO.
                Error(resultado.Mensaje);
                return RedirectToAction("Confirmacion", new { id = r.ReservaId });
            }

            ModelState.AddModelError("", resultado.Mensaje);
            model.NumeroTarjeta = null;
            model.CodigoSeguridad = null;
            r = Reservas.CargarReserva(model.ReservaId);
            PrepararPago(model, r);
            return View(model);
        }

        // ----- Pantalla 12 -----
        public ActionResult Confirmacion(int? id)
        {
            if (!id.HasValue) return RedirectToAction("MisReservas", "Reservas");
            var r = Reservas.CargarReserva(id.Value);
            if (r == null)
            {
                ViewBag.Mensaje = "No se encontró la operación solicitada.";
                return View("~/Views/Shared/AccesoDenegado.cshtml");
            }
            if (!PuedeAcceder(r)) return AccesoDenegado();
            return View(DetalleCompleto(r));
        }
    }
}
