using System;
using System.Collections.Generic;
using System.Data.Entity;
using System.Linq;
using System.Web.Mvc;
using AerolineaVIP.Models.ViewModels;
using AerolineaVIP.Servicios;

namespace AerolineaVIP.Controllers
{
    /// <summary>MOD-09: atención en counter. Busca reservas y muestra los vuelos de hoy y mañana.</summary>
    [Authorize(Roles = RolesApp.Personal)]
    public class CounterController : ControladorBase
    {
        public ActionResult Index(CounterViewModels vm)
        {
            vm = vm ?? new CounterViewModels();
            Reservas.ExpirarVencidas();

            // Vuelos de hoy y mañana con su ocupación, para atender consultas rápidas.
            var hoy = DateTime.Today;
            var manana = hoy.AddDays(1);
            vm.VuelosProximos = db.vw_DisponibilidadVuelo.AsNoTracking()
                .Where(v => v.FechaOperacion >= hoy && v.FechaOperacion <= manana)
                .OrderBy(v => v.FechaOperacion).ThenBy(v => v.HoraSalida)
                .ToList();

            if (string.IsNullOrWhiteSpace(vm.Buscar))
            {
                ModelState.Clear();
                return View(vm);
            }
            if (!ModelState.IsValid) return View(vm);

            vm.Buscado = true;
            var texto = vm.Buscar.Trim();
            var textoMayus = texto.ToUpper();
            var usuarios = ServicioUsuarios.Buscar(texto);

            // Busca por código RV, documento o nombre del pasajero, o por el cliente titular.
            var reservas = db.Reserva.AsNoTracking()
                .Include(r => r.Asignaciones.Select(a => a.Pasajero))
                .Include(r => r.Compras)
                .Where(r => r.CodigoReserva == textoMayus
                            || usuarios.Contains(r.UsuarioId)
                            || r.Asignaciones.Any(a => a.Pasajero.NumeroDocumento == textoMayus
                                                       || a.Pasajero.Nombres.Contains(texto)
                                                       || a.Pasajero.Apellidos.Contains(texto)))
                .OrderByDescending(r => r.FechaCreacion)
                .Take(50)
                .ToList();

            var vueloIds = reservas.SelectMany(r => r.Asignaciones.Select(a => a.VueloId)).Distinct().ToList();
            var vuelos = db.vw_VueloDetalle.AsNoTracking().Where(v => vueloIds.Contains(v.VueloId)).ToDictionary(v => v.VueloId);
            var titulares = ServicioUsuarios.Obtener(reservas.Select(r => r.UsuarioId));

            vm.Reservas = reservas.Select(r =>
            {
                var tramos = r.Asignaciones.Select(a => a.VueloId).Distinct().Where(vuelos.ContainsKey)
                              .Select(v => vuelos[v]).OrderBy(v => v.SalidaProgramada).ToList();
                var compra = r.Compras.OrderByDescending(c => c.EstadoCompraCodigo == "PAGADA").ThenByDescending(c => c.FechaCompra).FirstOrDefault();
                ServicioUsuarios.DatosUsuario u;
                return new FilaReservaVM
                {
                    ReservaId = r.ReservaId,
                    CodigoReserva = r.CodigoReserva,
                    FechaViaje = tramos.Any() ? tramos.First().FechaOperacion : (DateTime?)null,
                    Ruta = ReservasController.TextoRuta(tramos),
                    Pasajeros = r.Asignaciones.Select(a => a.PasajeroId).Distinct().Count(),
                    EstadoReserva = r.EstadoReservaCodigo,
                    EstadoCompra = compra == null ? null : compra.EstadoCompraCodigo,
                    FechaExpiracion = r.EstadoReservaCodigo == "PENDIENTE" ? r.FechaExpiracion : null,
                    Usuario = titulares.TryGetValue(r.UsuarioId, out u) ? u.Etiqueta : "(usuario no registrado)"
                };
            }).ToList();

            return View(vm);
        }
    }
}