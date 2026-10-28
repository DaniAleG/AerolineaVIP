using System;
using System.Collections.Generic;
using System.Data.Entity;
using System.Linq;
using System.Web.Mvc;
using AerolineaVIP.Models.ViewModels;
using AerolineaVIP.Servicios;

namespace AerolineaVIP.Controllers
{
    /// <summary>Pantallas 8, 9, 10, 13 y 14. La selección se guarda en sesión hasta confirmar.</summary>
    [Authorize]
    public class ReservasController : ControladorBase
    {
        private const string ClaveSesion = "ReservaEnCurso";

        private ReservaEnCurso Operacion
        {
            get { return Session[ClaveSesion] as ReservaEnCurso; }
            set { Session[ClaveSesion] = value; }
        }

        private static List<int> LeerVuelos(string vuelos, int? id)
        {
            var lista = new List<int>();
            foreach (var parte in (vuelos ?? "").Split(new[] { ',' }, StringSplitOptions.RemoveEmptyEntries))
            {
                int v;
                if (int.TryParse(parte.Trim(), out v) && !lista.Contains(v)) lista.Add(v);
            }
            if (lista.Count == 0 && id.HasValue) lista.Add(id.Value);
            return lista.Take(4).ToList();
        }

        private SeleccionAsientoVM ConstruirSeleccion(ReservaEnCurso op)
        {
            var vm = new SeleccionAsientoVM { Operacion = op, Pasajeros = op.Pasajeros, Tramos = new List<TramoSeleccionVM>() };
            for (int t = 0; t < op.VueloIds.Count; t++)
            {
                var info = Reservas.Info(op.VueloIds[t]);
                vm.Tramos.Add(new TramoSeleccionVM
                {
                    Indice = t,
                    Info = info,
                    Mapa = new MapaAsientosVM { VueloId = info.Vuelo.VueloId, Tramo = t, Asientos = Reservas.Mapa(info.Vuelo.VueloId), Interactivo = true }
                });
            }
            return vm;
        }

        // ----- Pantalla 8: selección de vuelo y asiento -----
        [Authorize(Roles = RolesApp.Operadores)]
        public ActionResult SeleccionAsiento(int? id, string vuelos, int? pasajeros)
        {
            Reservas.ExpirarVencidas();
            var ids = LeerVuelos(vuelos, id);
            var n = Math.Max(1, Math.Min(7, pasajeros ?? 1));
            if (!ids.Any()) return RedirectToAction("Buscar", "Vuelos");

            var infos = ids.Select(Reservas.Info).ToList();
            if (infos.Any(i => i == null))
            {
                TempData["Mensaje"] = "El vuelo seleccionado no está disponible.";
                return RedirectToAction("Buscar", "Vuelos");
            }

            // Reutiliza la selección temporal si es la misma operación.
            var op = Operacion;
            if (op == null || !op.VueloIds.SequenceEqual(ids) || op.Pasajeros != n)
            {
                op = new ReservaEnCurso { VueloIds = ids, Pasajeros = n };
                for (int p = 0; p < n; p++) op.Asientos.Add(ids.Select(x => (byte)0).ToList());
                Operacion = op;
            }

            var vm = ConstruirSeleccion(op);
            foreach (var tramo in vm.Tramos)
            {
                var motivo = Reservas.MotivoNoComercializable(tramo.Info.Vuelo, tramo.Info.TieneAeronave, tramo.Info.Disponibles, n);
                if (motivo != null)
                {
                    vm.Mensaje = tramo.Info.Vuelo.CodigoVuelo + ": " + (tramo.Info.Disponibles == 0 ? "No quedan asientos disponibles en este vuelo." : motivo);
                    vm.TipoMensaje = "warning";
                    vm.SinAsientos = true;
                }
            }
            return View(vm);
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        [Authorize(Roles = RolesApp.Operadores)]
        public ActionResult SeleccionAsiento(FormCollection form)
        {
            var op = Operacion;
            var ids = LeerVuelos(form["vuelos"], null);
            int n;
            if (op == null || !int.TryParse(form["pasajeros"], out n) || !op.VueloIds.SequenceEqual(ids) || op.Pasajeros != n)
            {
                TempData["Mensaje"] = "La selección temporal expiró. Vuelva a elegir el vuelo.";
                return RedirectToAction("Buscar", "Vuelos");
            }

            // Lee asiento_{pasajero}_{tramo} enviados por el mapa (campos ocultos).
            op.Asientos = new List<List<byte>>();
            for (int p = 0; p < op.Pasajeros; p++)
            {
                var fila = new List<byte>();
                for (int t = 0; t < op.VueloIds.Count; t++)
                {
                    byte numero;
                    fila.Add(byte.TryParse(form["asiento_" + p + "_" + t], out numero) && numero >= 1 && numero <= 7 ? numero : (byte)0);
                }
                op.Asientos.Add(fila);
            }

            // Validación en el servidor: disponibilidad real en este momento.
            Reservas.ExpirarVencidas(true);
            var errores = Reservas.ValidarSeleccion(op);
            if (errores.Any())
            {
                var vm = ConstruirSeleccion(op);
                // Se quitan solo las selecciones que dejaron de estar disponibles.
                for (int t = 0; t < op.VueloIds.Count; t++)
                {
                    var disponibles = vm.Tramos[t].Mapa.Asientos.Where(a => a.EstadoVisual == "DISPONIBLE").Select(a => a.NumeroAsiento).ToList();
                    for (int p = 0; p < op.Pasajeros; p++)
                        if (!disponibles.Contains(op.Asientos[p][t])) op.Asientos[p][t] = 0;
                }
                Operacion = op;
                foreach (var e in errores) ModelState.AddModelError("", e);
                vm.Mensaje = "Revise la selección: el mapa se actualizó con el estado real de los asientos.";
                vm.TipoMensaje = "warning";
                return View(vm);
            }

            Operacion = op;
            return RedirectToAction("Pasajeros");
        }

        // ----- Pantalla 9: datos de los pasajeros -----
        private IEnumerable<SelectListItem> TiposDocumento(string seleccionado = null)
        {
            return new SelectList(db.TipoDocumentoIdentidad.OrderBy(t => t.Descripcion).ToList(), "Codigo", "Descripcion", seleccionado);
        }

        private List<string> AsientosPorPasajero(ResumenOperacionVM resumen)
        {
            return resumen.Pasajeros.Select(p => string.Join(", ", p.Asientos)).ToList();
        }

        [Authorize(Roles = RolesApp.Operadores)]
        public ActionResult Pasajeros()
        {
            var op = Operacion;
            if (op == null || !op.AsientosCompletos)
            {
                Advertir("Seleccione un asiento para cada pasajero antes de continuar.");
                return op == null ? RedirectToAction("Buscar", "Vuelos")
                                  : RedirectToAction("SeleccionAsiento", new { vuelos = string.Join(",", op.VueloIds), pasajeros = op.Pasajeros });
            }

            var datos = op.Datos.Count == op.Pasajeros ? op.Datos : Enumerable.Range(0, op.Pasajeros).Select(i => i < op.Datos.Count ? op.Datos[i] : new PasajeroFormVM { TipoDocumentoCodigo = "CEDULA" }).ToList();
            var resumen = Reservas.Resumen(op);
            return View(new PasajerosVM
            {
                Pasajeros = datos,
                Resumen = resumen,
                TiposDocumento = TiposDocumento(),
                AsientosPorPasajero = AsientosPorPasajero(resumen)
            });
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        [Authorize(Roles = RolesApp.Operadores)]
        public ActionResult Pasajeros(PasajerosVM model)
        {
            var op = Operacion;
            if (op == null || !op.AsientosCompletos) return RedirectToAction("Buscar", "Vuelos");
            model.Pasajeros = model.Pasajeros ?? new List<PasajeroFormVM>();
            if (model.Pasajeros.Count != op.Pasajeros)
                ModelState.AddModelError("", "La cantidad de pasajeros no coincide con los asientos seleccionados.");

            // No repetir el mismo documento en la misma reserva.
            for (int i = 0; i < model.Pasajeros.Count; i++)
            {
                var actual = model.Pasajeros[i];
                if (string.IsNullOrEmpty(actual.NumeroDocumento)) continue;
                for (int j = 0; j < i; j++)
                    if (model.Pasajeros[j].TipoDocumentoCodigo == actual.TipoDocumentoCodigo
                        && string.Equals(model.Pasajeros[j].NumeroDocumento, actual.NumeroDocumento, StringComparison.OrdinalIgnoreCase))
                        ModelState.AddModelError("Pasajeros[" + i + "].NumeroDocumento", "Este documento ya se usó para el Pasajero " + (j + 1) + ".");

                // UX_RPV_PasajeroActivo: un pasajero no puede ocupar dos asientos vigentes en el mismo vuelo.
                var numero = actual.NumeroDocumento.Trim().ToUpper();
                var ocupado = db.ReservaPasajeroVuelo.Any(a => op.VueloIds.Contains(a.VueloId)
                                                              && a.Pasajero.TipoDocumentoCodigo == actual.TipoDocumentoCodigo
                                                              && a.Pasajero.NumeroDocumento == numero
                                                              && ServicioReservas.EstadosOcupan.Contains(a.EstadoAsignacionCodigo));
                if (ocupado)
                    ModelState.AddModelError("Pasajeros[" + i + "].NumeroDocumento", "Este pasajero ya tiene un asiento en este vuelo.");
            }

            if (!ModelState.IsValid)
            {
                var resumen = Reservas.Resumen(op);
                model.Resumen = resumen;
                model.TiposDocumento = TiposDocumento();
                model.AsientosPorPasajero = AsientosPorPasajero(resumen);
                return View(model);
            }

            op.Datos = model.Pasajeros;
            Operacion = op;
            Exito("Datos de " + (op.Pasajeros == 1 ? "el pasajero guardados." : "los " + op.Pasajeros + " pasajeros guardados."));
            return RedirectToAction("Confirmar");
        }

        // ----- Pantalla 10: reserva de pasajes -----
        private ConfirmarReservaVM ConstruirConfirmacion(ReservaEnCurso op)
        {
            var resumen = Reservas.Resumen(op);
            var ahora = Politicas.Ahora;
            var primera = resumen.Tramos.Min(t => t.Fecha.Add(t.Salida));
            return new ConfirmarReservaVM
            {
                Resumen = resumen,
                EsPersonal = User.IsInRole(RolesApp.Counter) || User.IsInRole(RolesApp.SuperUsuario),
                HorasRetencion = Politicas.HorasRetencionReserva,
                ExpiracionEstimada = ServicioReservas.CalcularExpiracion(ahora, primera)
            };
        }

        [Authorize(Roles = RolesApp.Operadores)]
        public ActionResult Confirmar()
        {
            var op = Operacion;
            if (op == null || !op.AsientosCompletos || !op.DatosCompletos)
            {
                Advertir("No hay una operación en curso. Busque un vuelo para comenzar.");
                return RedirectToAction("Buscar", "Vuelos");
            }
            ViewBag.Token = op.Token;
            return View(ConstruirConfirmacion(op));
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        [Authorize(Roles = RolesApp.Operadores)]
        public ActionResult Confirmar(ConfirmarReservaVM model, Guid? token)
        {
            // Un doble envío del mismo formulario no crea una segunda reserva.
            if (token.HasValue && Session["ReservaToken:" + token.Value] is int)
            {
                var existente = (int)Session["ReservaToken:" + token.Value];
                return RedirectToAction("Confirmacion", "Compras", new { id = existente });
            }

            var op = Operacion;
            if (op == null || !op.AsientosCompletos || !op.DatosCompletos)
            {
                Advertir("No hay una operación en curso. Busque un vuelo para comenzar.");
                return RedirectToAction("Buscar", "Vuelos");
            }

            // Titular de la reserva: el propio usuario o, si actúa el counter, el cliente indicado.
            var titularId = UsuarioId;
            if (User.IsInRole(RolesApp.Counter) || (User.IsInRole(RolesApp.SuperUsuario) && !string.IsNullOrWhiteSpace(model.ClienteTitular)))
            {
                var cliente = ServicioUsuarios.BuscarCliente(model.ClienteTitular);
                if (cliente == null)
                    ModelState.AddModelError("ClienteTitular", "Indique el usuario o correo de un cliente registrado (rol Cliente).");
                else
                    titularId = cliente.Id;
            }

            if (!ModelState.IsValid)
            {
                var vm = ConstruirConfirmacion(op);
                vm.ClienteTitular = model.ClienteTitular;
                ViewBag.Token = op.Token;
                return View(vm);
            }

            List<string> errores;
            var reserva = Reservas.CrearReserva(op, titularId, out errores);
            if (reserva == null)
            {
                // Si la revalidación de asientos falla se vuelve a la pantalla 8.
                if (errores.Any(e => e.Contains("asiento") || e.Contains("Asiento")))
                {
                    Error(string.Join(" ", errores));
                    return RedirectToAction("SeleccionAsiento", new { vuelos = string.Join(",", op.VueloIds), pasajeros = op.Pasajeros });
                }
                foreach (var e in errores) ModelState.AddModelError("", e);
                var vm = ConstruirConfirmacion(op);
                vm.ClienteTitular = model.ClienteTitular;
                ViewBag.Token = op.Token;
                return View(vm);
            }

            Session["ReservaToken:" + op.Token] = reserva.ReservaId;
            Operacion = null;
            Exito("Reserva " + reserva.CodigoReserva + " registrada. Estado: Pendiente de pago. Expira el "
                      + Formato.FechaHora(reserva.FechaExpiracion) + " si no se paga (DP-07).");

            if (model.Accion == "pagar")
                return RedirectToAction("Pagar", "Compras", new { id = reserva.ReservaId });
            return RedirectToAction("Confirmacion", "Compras", new { id = reserva.ReservaId });
        }

        // ----- Pantalla 13: consulta de reservas -----
        public ActionResult MisReservas(MisReservasVM filtros)
        {
            Reservas.ExpirarVencidas();
            filtros = filtros ?? new MisReservasVM();
            filtros.EsPersonal = EsPersonal;
            filtros.Pagina = Math.Max(1, filtros.Pagina);
            filtros.Estados = new SelectList(new[]
            {
                new { V = "", T = "Todos" }, new { V = "PENDIENTE", T = "Pendiente" }, new { V = "CONFIRMADA", T = "Confirmada" },
                new { V = "CANCELADA", T = "Cancelada" }, new { V = "EXPIRADA", T = "Expirada" }
            }, "V", "T", filtros.Estado);

            var consulta = db.Reserva.AsNoTracking()
                .Include(r => r.Asignaciones)
                .Include(r => r.Compras);

            // El cliente solo ve sus propias reservas.
            if (!EsPersonal)
            {
                var id = UsuarioId;
                consulta = consulta.Where(r => r.UsuarioId == id);
            }
            else if (!string.IsNullOrWhiteSpace(filtros.Buscar))
            {
                var texto = filtros.Buscar.Trim();
                var usuarios = ServicioUsuarios.Buscar(texto);
                consulta = consulta.Where(r => r.CodigoReserva == texto.ToUpper() || usuarios.Contains(r.UsuarioId)
                                               || r.Asignaciones.Any(a => a.Pasajero.NumeroDocumento == texto));
            }
            if (!string.IsNullOrEmpty(filtros.Estado))
                consulta = consulta.Where(r => r.EstadoReservaCodigo == filtros.Estado);

            var reservas = consulta.OrderByDescending(r => r.FechaCreacion).ToList();
            var vueloIds = reservas.SelectMany(r => r.Asignaciones.Select(a => a.VueloId)).Distinct().ToList();
            var vuelos = db.vw_VueloDetalle.AsNoTracking().Where(v => vueloIds.Contains(v.VueloId)).ToDictionary(v => v.VueloId);
            var usuariosDic = EsPersonal ? ServicioUsuarios.Obtener(reservas.Select(r => r.UsuarioId)) : new Dictionary<string, ServicioUsuarios.DatosUsuario>();

            var filas = reservas.Select(r =>
            {
                var tramos = r.Asignaciones.Select(a => a.VueloId).Distinct().Where(vuelos.ContainsKey).Select(v => vuelos[v]).OrderBy(v => v.SalidaProgramada).ToList();
                var compra = r.Compras.OrderByDescending(c => c.EstadoCompraCodigo == "PAGADA").ThenByDescending(c => c.FechaCompra).FirstOrDefault();
                ServicioUsuarios.DatosUsuario u;
                return new FilaReservaVM
                {
                    ReservaId = r.ReservaId,
                    CodigoReserva = r.CodigoReserva,
                    FechaViaje = tramos.Any() ? tramos.First().FechaOperacion : (DateTime?)null,
                    Ruta = TextoRuta(tramos),
                    Pasajeros = r.Asignaciones.Select(a => a.PasajeroId).Distinct().Count(),
                    EstadoReserva = r.EstadoReservaCodigo,
                    EstadoCompra = compra == null ? null : compra.EstadoCompraCodigo,
                    FechaExpiracion = r.EstadoReservaCodigo == "PENDIENTE" ? r.FechaExpiracion : null,
                    Usuario = usuariosDic.TryGetValue(r.UsuarioId, out u) ? u.Etiqueta : null
                };
            });

            if (filtros.Desde.HasValue) filas = filas.Where(f => f.FechaViaje >= filtros.Desde.Value);
            if (filtros.Hasta.HasValue) filas = filas.Where(f => f.FechaViaje <= filtros.Hasta.Value);
            var todas = filas.ToList();

            const int porPagina = 10;
            filtros.TotalPaginas = Math.Max(1, (int)Math.Ceiling(todas.Count / (double)porPagina));
            filtros.Pagina = Math.Min(filtros.Pagina, filtros.TotalPaginas);
            filtros.Filas = todas.Skip((filtros.Pagina - 1) * porPagina).Take(porPagina).ToList();
            return View(filtros);
        }

        /// <summary>"Quito → Manta", "Quito ⇄ Guayaquil" (ida y vuelta) o "Quito ⇄ Galápagos".</summary>
        public static string TextoRuta(List<Models.Dominio.vw_VueloDetalle> tramos)
        {
            if (!tramos.Any()) return "—";
            var primero = tramos.First();
            var ultimo = tramos.Last();
            if (tramos.Count > 1 && primero.Origen == ultimo.Destino)
                return primero.CiudadOrigen + " ⇄ " + tramos[tramos.Count / 2 - 1].CiudadDestino;
            return primero.CiudadOrigen + " → " + ultimo.CiudadDestino;
        }

        // ----- Pantalla 14: gestión y cancelación -----
        public ActionResult Gestionar(int? id)
        {
            if (!id.HasValue) return RedirectToAction("MisReservas");
            Reservas.ExpirarVencidas();
            var r = Reservas.CargarReserva(id.Value);
            if (r == null || !PuedeAcceder(r)) return AccesoDenegado();

            var vm = Reservas.Detalle(r);
            var titular = ServicioUsuarios.Obtener(r.UsuarioId);
            vm.Titular = titular == null ? "(usuario no registrado)" : titular.Etiqueta;
            vm.TitularCorreo = titular == null ? null : titular.Email;
            Reservas.EvaluarCancelacion(vm, EsAdministracion);
            return View(vm);
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public ActionResult Cancelar(int id, string motivo)
        {
            var r = db.Reserva.AsNoTracking().FirstOrDefault(x => x.ReservaId == id);
            if (r == null || !PuedeAcceder(r)) return AccesoDenegado();
            // El counter no cancela reservas pagadas (DP-13).
            if (motivo != null && motivo.Length > 300) motivo = motivo.Substring(0, 300);

            var error = Reservas.Cancelar(id, motivo, EsAdministracion);
            if (error == null)
                Exito("Reserva " + r.CodigoReserva + " cancelada. Los asientos volvieron a estar disponibles.");
            else
                Error(error);
            return RedirectToAction("Gestionar", new { id });
        }
    }
}
