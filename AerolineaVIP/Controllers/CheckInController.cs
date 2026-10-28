using System;
using System.Collections.Generic;
using System.Data.Entity;
using System.Linq;
using System.Web.Mvc;
using AerolineaVIP.Models.Dominio;
using AerolineaVIP.Models.ViewModels;
using AerolineaVIP.Servicios;

namespace AerolineaVIP.Controllers
{
    /// <summary>Pantalla 15: check-in web (horario según DP-12).</summary>
    [Authorize(Roles = RolesApp.Todos)]
    public class CheckInController : ControladorBase
    {
        public ActionResult Index(string codigo, string documento)
        {
            var vm = new CheckInVM
            {
                CodigoReserva = codigo,
                NumeroDocumento = documento,
                HorasApertura = Politicas.HorasAperturaCheckIn,
                HorasCierre = Politicas.HorasCierreCheckIn
            };
            if (!string.IsNullOrEmpty(codigo) && !string.IsNullOrEmpty(documento))
            {
                ModelState.Clear();
                TryValidateModel(vm);
                if (ModelState.IsValid) Buscar(vm);
            }
            return View(vm);
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public ActionResult Index(CheckInVM model, string accion)
        {
            model.HorasApertura = Politicas.HorasAperturaCheckIn;
            model.HorasCierre = Politicas.HorasCierreCheckIn;
            if (!ModelState.IsValid) return View(model);

            var reserva = Buscar(model);
            if (reserva == null || accion != "confirmar") return View(model);

            if (model.Seleccionados == null || !model.Seleccionados.Any())
            {
                model.Mensaje = "Seleccione al menos un pasajero.";
                model.TipoMensaje = "warning";
                return View(model);
            }

            // Revalidación en el servidor de cada pasajero seleccionado.
            var elegibles = model.Filas.Where(f => f.Elegible && model.Seleccionados.Contains(f.AsignacionId)).ToList();
            if (elegibles.Count != model.Seleccionados.Distinct().Count())
            {
                model.Mensaje = "Uno o más pasajeros seleccionados no son elegibles para el check-in.";
                model.TipoMensaje = "danger";
                return View(model);
            }

            try
            {
                foreach (var f in elegibles)
                    db.CheckIn.Add(new CheckIn { AsignacionId = f.AsignacionId, FechaRegistro = Politicas.Ahora, EstadoCheckInCodigo = "COMPLETADO" });
                db.SaveChanges();
                Exito("Check-in completado para " + string.Join(", ", elegibles.Select(e => e.Pasajero + " en " + e.CodigoVuelo)) + ".");
                return RedirectToAction("Index", new { codigo = model.CodigoReserva.ToUpper(), documento = model.NumeroDocumento });
            }
            catch (Exception ex)
            {
                model.Mensaje = Infraestructura.ErroresBD.Mensaje(ex);
                model.TipoMensaje = "danger";
                Buscar(model);
                return View(model);
            }
        }

        /// <summary>Localiza la reserva y calcula la elegibilidad de cada pasajero y tramo.</summary>
        private Reserva Buscar(CheckInVM vm)
        {
            vm.Buscado = true;
            var codigo = vm.CodigoReserva.Trim().ToUpper();
            var documento = vm.NumeroDocumento.Trim().ToUpper();
            var r = db.Reserva
                .Include(x => x.Asignaciones.Select(a => a.Pasajero))
                .Include(x => x.Asignaciones.Select(a => a.CheckIns))
                .FirstOrDefault(x => x.CodigoReserva == codigo);

            // Mensaje neutral si no existe, el documento no pertenece a la reserva o no es del usuario.
            if (r == null || !r.Asignaciones.Any(a => a.Pasajero.NumeroDocumento.ToUpper() == documento) || !PuedeAcceder(r))
            {
                vm.Filas = new List<FilaCheckInVM>();
                vm.Mensaje = "No se encontró una reserva con esos datos.";
                vm.TipoMensaje = "info";
                return null;
            }

            var ahora = Politicas.Ahora;
            var ids = r.Asignaciones.Select(a => a.VueloId).Distinct().ToList();
            var vuelos = db.vw_VueloDetalle.AsNoTracking().Where(v => ids.Contains(v.VueloId)).ToDictionary(v => v.VueloId);
            var codigos = Reservas.CodigosAsiento(ids);

            vm.Filas = r.Asignaciones
                .Where(a => a.EstadoAsignacionCodigo != "LIBERADA")
                .OrderBy(a => vuelos[a.VueloId].SalidaProgramada).ThenBy(a => a.NumeroAsiento)
                .Select(a =>
                {
                    var v = vuelos[a.VueloId];
                    var check = a.CheckIns.OrderByDescending(c => c.FechaRegistro).FirstOrDefault();
                    var completado = a.CheckIns.Any(c => c.EstadoCheckInCodigo == "COMPLETADO");
                    var apertura = v.SalidaProgramada.AddHours(-Politicas.HorasAperturaCheckIn);
                    var cierre = v.SalidaProgramada.AddHours(-Politicas.HorasCierreCheckIn);
                    string elegibilidad;
                    bool elegible = false;
                    if (completado) elegibilidad = "Check-in ya completado";
                    else if (a.EstadoAsignacionCodigo != "CONFIRMADA") elegibilidad = "No elegible: solo reservas pagadas";
                    else if (v.EstadoVueloCodigo == "CANCELADO") elegibilidad = "Vuelo cancelado";
                    else if (ahora < apertura) elegibilidad = "Aún no habilitado (abre el " + Formato.FechaHora(apertura) + ")";
                    else if (ahora > cierre) elegibilidad = "Cerrado (cerró el " + Formato.FechaHora(cierre) + ")";
                    else { elegibilidad = "Elegible"; elegible = true; }

                    string asiento;
                    return new FilaCheckInVM
                    {
                        AsignacionId = a.AsignacionId,
                        Pasajero = a.Pasajero.NombreCompleto,
                        Documento = ServicioReservas.Documento(a.Pasajero.TipoDocumentoCodigo, a.Pasajero.NumeroDocumento),
                        CodigoVuelo = v.CodigoVuelo,
                        Ruta = v.CiudadOrigen + " → " + v.CiudadDestino,
                        Salida = v.SalidaProgramada,
                        Asiento = codigos.TryGetValue(a.VueloId + "|" + a.NumeroAsiento, out asiento) ? asiento : a.NumeroAsiento.ToString(),
                        Elegible = elegible,
                        Elegibilidad = elegibilidad,
                        EstadoCheckIn = check == null ? null : check.EstadoCheckInCodigo,
                        FechaCheckIn = check == null ? (DateTime?)null : check.FechaRegistro
                    };
                }).ToList();

            if (r.EstadoReservaCodigo != "CONFIRMADA" && vm.Mensaje == null)
            {
                vm.Mensaje = "El check-in solo puede completarse para reservas pagadas.";
                vm.TipoMensaje = "danger";
            }
            else if (vm.Filas.Any() && !vm.Filas.Any(f => f.Elegible) && vm.Mensaje == null
                     && vm.Filas.Any(f => f.Elegibilidad.StartsWith("Aún no")))
            {
                vm.Mensaje = "El check-in para este vuelo aún no está habilitado (DP-12: abre " + Politicas.HorasAperturaCheckIn
                             + " h antes de la salida y cierra " + Politicas.HorasCierreCheckIn + " h antes).";
                vm.TipoMensaje = "info";
            }
            return r;
        }
    }
}
