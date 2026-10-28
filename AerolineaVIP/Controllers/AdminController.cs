using System;
using System.Collections.Generic;
using System.Data.Entity;
using System.Linq;
using System.Web;
using System.Web.Mvc;
using Microsoft.AspNet.Identity;
using Microsoft.AspNet.Identity.EntityFramework;
using Microsoft.AspNet.Identity.Owin;
using AerolineaVIP.Models;
using AerolineaVIP.Models.Dominio;
using AerolineaVIP.Models.ViewModels;
using AerolineaVIP.Servicios;

namespace AerolineaVIP.Controllers
{
    /// <summary>Área administrativa: pantallas 17 a 21.</summary>
    [Authorize(Roles = RolesApp.Administracion)]
    public class AdminController : ControladorBase
    {
        private ApplicationUserManager UserManager
        {
            get { return HttpContext.GetOwinContext().GetUserManager<ApplicationUserManager>(); }
        }

        // ===================== Pantalla 17: accesos (sin dashboard) =====================
        public ActionResult Index()
        {
            return View();
        }

        /// <summary>Accesos a los CRUD generados con scaffolding (tablas administrables).</summary>
        public ActionResult Mantenimientos()
        {
            return View();
        }

        // ===================== Pantalla 18: vuelos e itinerarios =====================
        public ActionResult Vuelos(AdminVuelosVM vm)
        {
            Reservas.ExpirarVencidas();
            vm.Tab = string.IsNullOrEmpty(vm.Tab) ? "vuelos" : vm.Tab;
            if (!vm.Desde.HasValue && !vm.Hasta.HasValue)
            {
                // Por defecto: desde hoy hasta el último vuelo programado (máx. 60 días).
                vm.Desde = DateTime.Today;
                vm.Hasta = DateTime.Today.AddDays(60);
                ModelState.Clear();
            }
            CargarVuelos(vm);
            return View(vm);
        }

        private void CargarVuelos(AdminVuelosVM vm)
        {
            vm.Rutas = new SelectList(db.Ruta.Include(r => r.AeropuertoOrigen).Include(r => r.AeropuertoDestino).ToList()
                .Select(r => new { r.RutaId, T = r.AeropuertoOrigen.CodigoIATA + " → " + r.AeropuertoDestino.CodigoIATA })
                .OrderBy(r => r.T), "RutaId", "T", vm.RutaId);
            vm.Estados = new SelectList(db.EstadoVuelo.ToList().Select(e => new { e.Codigo, T = Formato.TextoEstadoVuelo(e.Codigo) }), "Codigo", "T", vm.Estado);
            vm.Vuelos = new List<FilaVueloAdminVM>();
            vm.ListaConflictos = new List<ConflictoVM>();
            vm.Frecuencias = db.Frecuencia.Include(f => f.Ruta.AeropuertoOrigen).Include(f => f.Ruta.AeropuertoDestino)
                .OrderBy(f => f.DiaSemana).ThenBy(f => f.HoraSalida).ToList();

            if (!ModelState.IsValid || !vm.Desde.HasValue || !vm.Hasta.HasValue) { vm.Itinerarios = new List<Itinerario>(); return; }

            var desde = vm.Desde.Value.Date;
            var hasta = vm.Hasta.Value.Date;
            var q = db.vw_VueloDetalle.AsNoTracking().Where(v => v.FechaOperacion >= desde && v.FechaOperacion <= hasta);
            if (vm.RutaId.HasValue) q = q.Where(v => v.RutaId == vm.RutaId.Value);
            if (!string.IsNullOrEmpty(vm.Estado)) q = q.Where(v => v.EstadoVueloCodigo == vm.Estado);
            var vuelos = q.OrderBy(v => v.FechaOperacion).ThenBy(v => v.HoraSalida).ToList();
            var ids = vuelos.Select(v => v.VueloId).ToList();
            var disp = db.vw_DisponibilidadVuelo.AsNoTracking().Where(d => ids.Contains(d.VueloId)).ToDictionary(d => d.VueloId);
            vm.Vuelos = vuelos.Select(v =>
            {
                vw_DisponibilidadVuelo d;
                disp.TryGetValue(v.VueloId, out d);
                return new FilaVueloAdminVM
                {
                    Vuelo = v,
                    Matricula = d == null ? null : d.Matricula,
                    Capacidad = d == null ? 0 : d.Capacidad,
                    Ocupados = d == null ? 0 : d.Ocupados,
                    Disponibles = d == null ? 0 : d.Disponibles
                };
            }).ToList();

            vm.Itinerarios = db.Itinerario.Include(i => i.Tramos)
                .Where(i => i.Tramos.Any(t => t.Vuelo.FechaOperacion >= desde && t.Vuelo.FechaOperacion <= hasta))
                .OrderBy(i => i.Referencia).ToList();
            var tramoIds = vm.Itinerarios.SelectMany(i => i.Tramos.Select(t => t.VueloId)).Distinct().ToList();
            vm.CodigosVuelo = db.vw_VueloDetalle.AsNoTracking().Where(v => tramoIds.Contains(v.VueloId)).ToList()
                .ToDictionary(v => v.VueloId, v => v.CodigoVuelo + " · " + v.Origen + " → " + v.Destino + " · " + Formato.Horario(v.HoraSalida, v.HoraLlegada));

            if (vm.Conflictos) vm.ListaConflictos = Conflictos(desde, hasta);

            if (vm.Editar.HasValue)
            {
                var v = db.vw_VueloDetalle.AsNoTracking().FirstOrDefault(x => x.VueloId == vm.Editar.Value);
                var vuelo = db.Vuelo.Find(vm.Editar.Value);
                if (v != null && vuelo != null)
                {
                    vm.Edicion = new EditarVueloVM
                    {
                        VueloId = v.VueloId,
                        CodigoVuelo = v.CodigoVuelo,
                        Descripcion = v.CiudadOrigen + " → " + v.CiudadDestino + " · " + Formato.FechaConDia(v.FechaOperacion) + " · " + Formato.Horario(v.HoraSalida, v.HoraLlegada),
                        AeronaveId = vuelo.AeronaveId,
                        EstadoVueloCodigo = vuelo.EstadoVueloCodigo
                    };
                    CargarListasEdicion(vm, vuelo.AeronaveId);
                }
            }
        }

        private void CargarListasEdicion(AdminVuelosVM vm, int? aeronaveActual)
        {
            var aeronaves = db.Aeronave.Where(a => a.EstadoAeronaveCodigo == "DISPONIBLE" || a.AeronaveId == aeronaveActual)
                .OrderBy(a => a.Matricula).ToList()
                .Select(a => new SelectListItem { Value = a.AeronaveId.ToString(), Text = a.Matricula + (a.EstadoAeronaveCodigo == "DISPONIBLE" ? "" : " (no disponible)") })
                .ToList();
            aeronaves.Insert(0, new SelectListItem { Value = "", Text = "Sin asignar" });
            vm.Aeronaves = aeronaves;
            vm.EstadosEdicion = new SelectList(db.EstadoVuelo.ToList().Select(e => new { e.Codigo, T = Formato.TextoEstadoVuelo(e.Codigo) }), "Codigo", "T");
        }

        /// <summary>
        /// Revisión de la programación por aeronave: superposición (RNP-02, bloqueante),
        /// tiempo de preparación (DP-03) y continuidad geográfica (DP-02), estas dos como advertencias.
        /// </summary>
        private List<ConflictoVM> Conflictos(DateTime desde, DateTime hasta)
        {
            var lista = new List<ConflictoVM>();
            var vuelos = db.vw_VueloDetalle.AsNoTracking()
                .Where(v => v.FechaOperacion >= desde && v.FechaOperacion <= hasta && v.AeronaveId != null && v.EstadoVueloCodigo != "CANCELADO")
                .ToList();
            var matriculas = db.Aeronave.ToDictionary(a => a.AeronaveId, a => a.Matricula);
            var minutos = Politicas.MinutosPreparacionAeronave;

            foreach (var grupo in vuelos.GroupBy(v => v.AeronaveId.Value))
            {
                var orden = grupo.OrderBy(v => v.SalidaProgramada).ToList();
                for (int i = 1; i < orden.Count; i++)
                {
                    var a = orden[i - 1];
                    var b = orden[i];
                    var espera = (b.SalidaProgramada - a.LlegadaProgramada).TotalMinutes;
                    var mat = matriculas[grupo.Key];
                    if (espera < 0)
                        lista.Add(new ConflictoVM { Tipo = "Superposición horaria", Aeronave = mat, Vuelo1 = a.CodigoVuelo, Vuelo2 = b.CodigoVuelo, Bloqueante = true,
                            Detalle = "La aeronave " + mat + " tiene vuelos superpuestos (" + Formato.FechaHora(a.SalidaProgramada) + " y " + Formato.FechaHora(b.SalidaProgramada) + ")." });
                    else if (espera < minutos)
                        lista.Add(new ConflictoVM { Tipo = "Preparación insuficiente (DP-03)", Aeronave = mat, Vuelo1 = a.CodigoVuelo, Vuelo2 = b.CodigoVuelo,
                            Detalle = "Solo " + espera + " min entre la llegada de " + a.CodigoVuelo + " y la salida de " + b.CodigoVuelo + " (mínimo propuesto: " + minutos + " min)." });
                    if (a.Destino != b.Origen)
                        lista.Add(new ConflictoVM { Tipo = "Continuidad geográfica (DP-02)", Aeronave = mat, Vuelo1 = a.CodigoVuelo, Vuelo2 = b.CodigoVuelo,
                            Detalle = mat + " termina " + a.CodigoVuelo + " en " + a.CiudadDestino + " (" + Formato.FechaHora(a.LlegadaProgramada) + ") y "
                                      + b.CodigoVuelo + " sale de " + b.CiudadOrigen + " (" + Formato.FechaHora(b.SalidaProgramada) + "): requiere un traslado de posicionamiento no programado." });
                }
            }
            return lista;
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public ActionResult EditarVuelo(EditarVueloVM model, DateTime? desde, DateTime? hasta)
        {
            var vuelo = db.Vuelo.Include(v => v.Asignaciones).FirstOrDefault(v => v.VueloId == model.VueloId);
            if (vuelo == null) return HttpNotFound();
            var regreso = new { Desde = (desde ?? DateTime.Today).ToString("yyyy-MM-dd"), Hasta = (hasta ?? DateTime.Today.AddDays(60)).ToString("yyyy-MM-dd"), Editar = model.VueloId };

            var activos = vuelo.Asignaciones.Count(a => ServicioReservas.EstadosOcupan.Contains(a.EstadoAsignacionCodigo));
            string error = null;
            if (!ModelState.IsValid) error = "Seleccione el estado del vuelo.";
            else if (vuelo.EstadoVueloCodigo == "COMPLETADO") error = "No se modifican vuelos COMPLETADOS (historial).";
            else if (model.EstadoVueloCodigo == "CANCELADO" && activos > 0)
                error = "El vuelo tiene " + activos + " asiento(s) reservados u ocupados. Cancele o reubique esas reservas antes de cancelar el vuelo.";
            else if (!model.AeronaveId.HasValue && activos > 0)
                error = "No se puede quitar la aeronave: el vuelo tiene " + activos + " asiento(s) vigentes (RNP-01).";

            // Conflicto operativo con mensaje claro antes de llegar al trigger (RNP-02).
            if (error == null && model.AeronaveId.HasValue && model.EstadoVueloCodigo != "CANCELADO")
            {
                var actual = db.vw_VueloDetalle.AsNoTracking().First(v => v.VueloId == vuelo.VueloId);
                var aeronaveId = model.AeronaveId.Value;
                var choque = db.vw_VueloDetalle.AsNoTracking()
                    .Where(v => v.AeronaveId == aeronaveId && v.VueloId != vuelo.VueloId && v.EstadoVueloCodigo != "CANCELADO"
                                && v.SalidaProgramada < actual.LlegadaProgramada && actual.SalidaProgramada < v.LlegadaProgramada)
                    .FirstOrDefault();
                if (choque != null)
                {
                    var mat = db.Aeronave.Where(a => a.AeronaveId == aeronaveId).Select(a => a.Matricula).First();
                    error = "Conflicto operativo: la aeronave " + mat + " ya tiene el vuelo " + choque.CodigoVuelo + " entre "
                            + Formato.Horario(choque.HoraSalida, choque.HoraLlegada) + " del " + Formato.Fecha(choque.FechaOperacion) + ". No se guardó la asignación.";
                }
            }

            if (error == null)
            {
                try
                {
                    vuelo.AeronaveId = model.AeronaveId;
                    vuelo.EstadoVueloCodigo = model.EstadoVueloCodigo;
                    db.SaveChanges();     // trg_Vuelo_Validar vuelve a comprobar en la base
                    Exito("Cambios guardados para el vuelo " + vuelo.CodigoVuelo + ".");
                    return RedirectToAction("Vuelos", new { regreso.Desde, regreso.Hasta });
                }
                catch (Exception ex)
                {
                    error = Infraestructura.ErroresBD.Mensaje(ex);
                }
            }
            Error(error);
            return RedirectToAction("Vuelos", regreso);
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public ActionResult GenerarVuelos(DateTime? desde, DateTime? hasta)
        {
            if (!desde.HasValue || !hasta.HasValue || hasta.Value < desde.Value)
            {
                Error("Indique un rango de fechas válido para generar vuelos.");
                return RedirectToAction("Vuelos");
            }
            if ((hasta.Value - desde.Value).TotalDays > 365)
            {
                Error("El rango no puede superar 366 días.");
                return RedirectToAction("Vuelos");
            }
            try
            {
                var antes = db.Vuelo.Count();
                db.Database.ExecuteSqlCommand("EXEC dbo.usp_GenerarVuelos @FechaInicio, @FechaFin",
                    new System.Data.SqlClient.SqlParameter("@FechaInicio", desde.Value.Date),
                    new System.Data.SqlClient.SqlParameter("@FechaFin", hasta.Value.Date));
                var generados = db.Vuelo.Count() - antes;
                if (generados > 0)
                    Exito("Se generaron " + generados + " vuelos (solo en los días autorizados por las frecuencias). Quedan sin aeronave asignada hasta que se asignen (RNP-01).");
                else
                    Informar("No se generaron vuelos nuevos: el rango ya estaba programado o no hay frecuencias activas en esos días.");
            }
            catch (Exception ex)
            {
                Error(Infraestructura.ErroresBD.Mensaje(ex));
            }
            return RedirectToAction("Vuelos", new { Desde = desde.Value.ToString("yyyy-MM-dd"), Hasta = hasta.Value.ToString("yyyy-MM-dd") });
        }

        // ===================== Pantalla 19: aeronaves =====================
        public ActionResult Aeronaves(int? id)
        {
            var hoy = DateTime.Today;
            var vm = new AdminAeronavesVM
            {
                Flota = db.Aeronave.Include(a => a.ModeloAeronave).Include(a => a.EstadoAeronave).OrderBy(a => a.Matricula).ToList()
                    .Select(a => new FilaAeronaveVM
                    {
                        Aeronave = a,
                        VuelosProximos = db.Vuelo.Count(v => v.AeronaveId == a.AeronaveId && v.FechaOperacion >= hoy && v.EstadoVueloCodigo != "CANCELADO")
                    }).ToList()
            };
            var seleccion = id.HasValue ? vm.Flota.FirstOrDefault(f => f.Aeronave.AeronaveId == id.Value) : null;
            if (seleccion != null)
            {
                var a = seleccion.Aeronave;
                vm.Edicion = new EditarAeronaveVM
                {
                    AeronaveId = a.AeronaveId,
                    Matricula = a.Matricula,
                    ModeloAeronaveId = a.ModeloAeronaveId,
                    Capacidad = a.ModeloAeronave.CapacidadAsientos,
                    EstadoAeronaveCodigo = a.EstadoAeronaveCodigo
                };
                CargarAeronave(vm, a);
            }
            return View(vm);
        }

        private void CargarAeronave(AdminAeronavesVM vm, Aeronave a)
        {
            var hoy = DateTime.Today;
            vm.Distribucion = new MapaAsientosVM { Asientos = Reservas.Distribucion(a.ModeloAeronaveId), Interactivo = false, MostrarEstados = false };
            vm.Asignaciones = db.vw_VueloDetalle.AsNoTracking().Where(v => v.AeronaveId == a.AeronaveId && v.FechaOperacion >= hoy)
                .OrderBy(v => v.FechaOperacion).ThenBy(v => v.HoraSalida).Take(30).ToList();
            vm.Modelos = new SelectList(db.ModeloAeronave.ToList().Select(m => new { m.ModeloAeronaveId, T = m.Fabricante + " " + m.Nombre }), "ModeloAeronaveId", "T", vm.Edicion.ModeloAeronaveId);
            vm.Estados = new SelectList(db.EstadoAeronave.ToList(), "Codigo", "Descripcion", vm.Edicion.EstadoAeronaveCodigo);
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public ActionResult Aeronaves([Bind(Prefix = "Edicion")] EditarAeronaveVM model)
        {
            var a = db.Aeronave.Include(x => x.ModeloAeronave).FirstOrDefault(x => x.AeronaveId == model.AeronaveId);
            if (a == null) return HttpNotFound();

            if (ModelState.IsValid && db.Aeronave.Any(x => x.Matricula == model.Matricula && x.AeronaveId != model.AeronaveId))
                ModelState.AddModelError("Matricula", "La matrícula ya está registrada.");
            // Cambiar el modelo alteraría la distribución de asientos de vuelos con reservas (no se alteran históricos).
            if (ModelState.IsValid && model.ModeloAeronaveId != a.ModeloAeronaveId
                && db.ReservaPasajeroVuelo.Any(r => r.Vuelo.AeronaveId == a.AeronaveId))
                ModelState.AddModelError("ModeloAeronaveId", "No se puede cambiar el modelo: la aeronave tiene asignaciones de asientos registradas.");

            if (ModelState.IsValid)
            {
                try
                {
                    var hoy = DateTime.Today;
                    var proximos = db.Vuelo.Count(v => v.AeronaveId == a.AeronaveId && v.FechaOperacion >= hoy && v.EstadoVueloCodigo != "CANCELADO");
                    var pasaANoDisponible = a.EstadoAeronaveCodigo != "NO_DISPONIBLE" && model.EstadoAeronaveCodigo == "NO_DISPONIBLE";
                    a.Matricula = model.Matricula.Trim().ToUpper();
                    a.ModeloAeronaveId = model.ModeloAeronaveId;
                    a.EstadoAeronaveCodigo = model.EstadoAeronaveCodigo;
                    db.SaveChanges();
                    Exito("Cambios guardados para la aeronave " + a.Matricula + ".");
                    if (pasaANoDisponible && proximos > 0)
                        Advertir("La aeronave tiene " + proximos + " vuelos asignados próximamente. Revise la programación (pantalla 18): no recibirá nuevas asignaciones.");
                    return RedirectToAction("Aeronaves", new { id = a.AeronaveId });
                }
                catch (Exception ex)
                {
                    ModelState.AddModelError("", Infraestructura.ErroresBD.Mensaje(ex));
                }
            }

            var vm = new AdminAeronavesVM
            {
                Flota = db.Aeronave.Include(x => x.ModeloAeronave).Include(x => x.EstadoAeronave).OrderBy(x => x.Matricula).ToList()
                    .Select(x => new FilaAeronaveVM { Aeronave = x, VuelosProximos = 0 }).ToList(),
                Edicion = model
            };
            model.Capacidad = a.ModeloAeronave.CapacidadAsientos;
            CargarAeronave(vm, a);
            return View(vm);
        }

        // ===================== Pantalla 20: usuarios, pasajeros y reservas =====================
        [OverrideAuthorization]
        [Authorize(Roles = RolesApp.Personal)]
        public ActionResult Gestion(AdminGestionVM vm)
        {
            vm.Tab = string.IsNullOrEmpty(vm.Tab) ? (EsAdministracion ? "usuarios" : "reservas") : vm.Tab;
            vm.EsSuperUsuario = User.IsInRole(RolesApp.SuperUsuario);
            vm.PuedeVerUsuarios = EsAdministracion;
            ViewBag.UsuarioActual = UsuarioId;
            // El counter solo consulta pasajeros y reservas (DP-13).
            if (vm.Tab == "usuarios" && !vm.PuedeVerUsuarios) return AccesoDenegado();

            var texto = string.IsNullOrWhiteSpace(vm.Buscar) ? null : vm.Buscar.Trim();
            if (vm.Tab == "usuarios")
            {
                var ctx = HttpContext.GetOwinContext().Get<ApplicationDbContext>();
                var roles = ctx.Roles.ToDictionary(r => r.Id, r => r.Name);
                var q = ctx.Users.Include(u => u.Roles).AsQueryable();
                if (texto != null) q = q.Where(u => u.UserName.Contains(texto) || u.Email.Contains(texto) || u.Nombres.Contains(texto) || u.Apellidos.Contains(texto));
                if (!string.IsNullOrEmpty(vm.Rol))
                {
                    var rolId = roles.Where(r => r.Value == vm.Rol).Select(r => r.Key).FirstOrDefault();
                    q = q.Where(u => u.Roles.Any(r => r.RoleId == rolId));
                }
                var ahora = DateTime.UtcNow;
                vm.Usuarios = q.OrderBy(u => u.UserName).Take(200).ToList().Select(u =>
                {
                    var nombres = u.Roles.Select(r => roles[r.RoleId]).ToList();
                    return new FilaUsuarioVM
                    {
                        Id = u.Id,
                        UserName = u.UserName,
                        Email = u.Email,
                        NombreCompleto = u.NombreCompleto,
                        Roles = nombres,
                        Bloqueado = u.LockoutEndDateUtc.HasValue && u.LockoutEndDateUtc.Value > ahora,
                        EsElevado = nombres.Contains(RolesApp.SuperUsuario) || nombres.Contains(RolesApp.Administrador)
                    };
                }).ToList();
                vm.Roles = new SelectList(new[] { new { V = "", T = "Todos" } }.Concat(RolesApp.Lista.Select(r => new { V = r, T = RolesApp.Descripcion(r) })), "V", "T", vm.Rol);
            }
            else if (vm.Tab == "pasajeros")
            {
                var q = db.Pasajero.AsNoTracking().AsQueryable();
                if (texto != null) q = q.Where(p => p.NumeroDocumento.Contains(texto) || p.Nombres.Contains(texto) || p.Apellidos.Contains(texto) || p.Correo.Contains(texto));
                vm.Pasajeros = q.OrderBy(p => p.Apellidos).ThenBy(p => p.Nombres).Take(200)
                    .Select(p => new FilaPasajeroVM { Pasajero = p, Reservas = p.Asignaciones.Select(a => a.ReservaId).Distinct().Count() }).ToList();
            }
            else
            {
                vm.Tab = "reservas";
                Reservas.ExpirarVencidas();
                var q = db.Reserva.AsNoTracking().Include(r => r.Asignaciones).Include(r => r.Compras).AsQueryable();
                if (texto != null)
                {
                    var usuarios = ServicioUsuarios.Buscar(texto);
                    var t = texto.ToUpper();
                    q = q.Where(r => r.CodigoReserva == t || usuarios.Contains(r.UsuarioId) || r.Asignaciones.Any(a => a.Pasajero.NumeroDocumento == texto));
                }
                if (!string.IsNullOrEmpty(vm.Estado)) q = q.Where(r => r.EstadoReservaCodigo == vm.Estado);
                var reservas = q.OrderByDescending(r => r.FechaCreacion).Take(200).ToList();
                var vueloIds = reservas.SelectMany(r => r.Asignaciones.Select(a => a.VueloId)).Distinct().ToList();
                var vuelos = db.vw_VueloDetalle.AsNoTracking().Where(v => vueloIds.Contains(v.VueloId)).ToDictionary(v => v.VueloId);
                var usuariosDic = ServicioUsuarios.Obtener(reservas.Select(r => r.UsuarioId));
                vm.Reservas = reservas.Select(r =>
                {
                    var tramos = r.Asignaciones.Select(a => a.VueloId).Distinct().Where(vuelos.ContainsKey).Select(x => vuelos[x]).OrderBy(x => x.SalidaProgramada).ToList();
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
                        Usuario = usuariosDic.TryGetValue(r.UsuarioId, out u) ? u.Etiqueta : "(sin usuario)"
                    };
                }).ToList();
                vm.Estados = new SelectList(new[]
                {
                    new { V = "", T = "Todos" }, new { V = "PENDIENTE", T = "Pendiente" }, new { V = "CONFIRMADA", T = "Confirmada" },
                    new { V = "CANCELADA", T = "Cancelada" }, new { V = "EXPIRADA", T = "Expirada" }
                }, "V", "T", vm.Estado);
            }
            return View(vm);
        }

        /// <summary>Asignar rol: SOLO SuperUsuario (validado en el servidor).</summary>
        [HttpPost]
        [ValidateAntiForgeryToken]
        [OverrideAuthorization]
        [Authorize(Roles = RolesApp.SuperUsuario)]
        public ActionResult AsignarRol(string id, string rol)
        {
            if (!RolesApp.Lista.Contains(rol)) { Error("Rol no válido."); return RedirectToAction("Gestion", new { tab = "usuarios" }); }
            if (id == UsuarioId && rol != RolesApp.SuperUsuario)
            {
                Error("No puede quitarse a sí mismo el rol SuperUsuario.");
                return RedirectToAction("Gestion", new { tab = "usuarios" });
            }
            var user = UserManager.FindById(id);
            if (user == null) return HttpNotFound();
            var actuales = UserManager.GetRoles(id).ToArray();
            if (actuales.Any()) UserManager.RemoveFromRoles(id, actuales);
            var r = UserManager.AddToRole(id, rol);
            UserManager.UpdateSecurityStamp(id);     // el cambio se aplica en la próxima validación de la sesión
            if (r.Succeeded) Exito("Rol de «" + user.UserName + "» actualizado a " + RolesApp.Descripcion(rol) + ".");
            else Error(string.Join(" ", r.Errors));
            return RedirectToAction("Gestion", new { tab = "usuarios" });
        }

        private bool PuedeEditarUsuario(string id)
        {
            if (User.IsInRole(RolesApp.SuperUsuario)) return true;
            // Un Administrador no edita ni bloquea cuentas con privilegios elevados.
            return !UserManager.IsInRole(id, RolesApp.SuperUsuario) && !UserManager.IsInRole(id, RolesApp.Administrador);
        }

        public ActionResult EditarUsuario(string id)
        {
            var u = UserManager.FindById(id ?? "");
            if (u == null) return HttpNotFound();
            if (!PuedeEditarUsuario(id)) return AccesoDenegado();
            return View(new EditarUsuarioVM { Id = u.Id, UserName = u.UserName, Nombres = u.Nombres, Apellidos = u.Apellidos, Email = u.Email, PhoneNumber = u.PhoneNumber });
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public ActionResult EditarUsuario(EditarUsuarioVM model)
        {
            var u = UserManager.FindById(model.Id ?? "");
            if (u == null) return HttpNotFound();
            if (!PuedeEditarUsuario(model.Id)) return AccesoDenegado();
            model.UserName = u.UserName;
            var otro = ModelState.IsValid ? UserManager.FindByEmail(model.Email.Trim()) : null;
            if (otro != null && otro.Id != u.Id) ModelState.AddModelError("Email", "El correo ya está registrado.");
            if (!ModelState.IsValid) return View(model);

            u.Nombres = model.Nombres.Trim();
            u.Apellidos = model.Apellidos.Trim();
            u.Email = model.Email.Trim();
            u.PhoneNumber = string.IsNullOrWhiteSpace(model.PhoneNumber) ? null : model.PhoneNumber.Trim();
            var r = UserManager.Update(u);
            if (!r.Succeeded)
            {
                foreach (var e in r.Errors) ModelState.AddModelError("", e);
                return View(model);
            }
            Exito("Cambios guardados.");
            return RedirectToAction("Gestion", new { tab = "usuarios" });
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public ActionResult CambiarEstadoUsuario(string id)
        {
            var u = UserManager.FindById(id ?? "");
            if (u == null) return HttpNotFound();
            if (id == UsuarioId || !PuedeEditarUsuario(id))
            {
                Error("No tiene permisos para cambiar el estado de esta cuenta.");
                return RedirectToAction("Gestion", new { tab = "usuarios" });
            }
            var bloqueado = u.LockoutEndDateUtc.HasValue && u.LockoutEndDateUtc.Value > DateTime.UtcNow;
            UserManager.SetLockoutEnabled(id, true);
            UserManager.SetLockoutEndDate(id, bloqueado ? DateTimeOffset.UtcNow.AddMinutes(-1) : DateTimeOffset.UtcNow.AddYears(100));
            UserManager.UpdateSecurityStamp(id);
            Exito("La cuenta «" + u.UserName + "» quedó " + (bloqueado ? "activa." : "bloqueada."));
            return RedirectToAction("Gestion", new { tab = "usuarios" });
        }

        // ===================== Pantalla 21: consultas y reportes =====================
        public ActionResult Reportes(ReportesVM vm)
        {
            vm.Consultas = new SelectList(new[]
            {
                new { V = "ocupacion", T = "Ocupación por vuelo" },
                new { V = "reservas", T = "Reservas por estado" },
                new { V = "compras", T = "Compras y pagos simulados" }
            }, "V", "T", vm.Consulta);
            vm.Rutas = new SelectList(db.Ruta.Include(r => r.AeropuertoOrigen).Include(r => r.AeropuertoDestino).ToList()
                .Select(r => new { r.RutaId, T = r.AeropuertoOrigen.CodigoIATA + " → " + r.AeropuertoDestino.CodigoIATA }).OrderBy(r => r.T), "RutaId", "T", vm.RutaId);
            vm.AeronavesLista = new SelectList(db.Aeronave.OrderBy(a => a.Matricula).ToList(), "AeronaveId", "Matricula", vm.AeronaveId);
            vm.Corte = Politicas.Ahora;

            var estados = vm.Consulta == "ocupacion" ? db.EstadoVuelo.Select(e => e.Codigo).ToList()
                        : vm.Consulta == "compras" ? db.EstadoCompra.Select(e => e.Codigo).ToList()
                        : db.EstadoReserva.Select(e => e.Codigo).ToList();
            vm.Estados = new SelectList(new[] { "" }.Concat(estados).Select(e => new { V = e, T = e == "" ? "Todos" : e }), "V", "T", vm.Estado);

            if (string.IsNullOrEmpty(vm.Consulta))
            {
                ModelState.Clear();
                vm.Consulta = "ocupacion";
                vm.Desde = DateTime.Today.AddDays(-30);
                vm.Hasta = DateTime.Today.AddDays(30);
                return View(vm);
            }
            if (!ModelState.IsValid) return View(vm);

            Reservas.ExpirarVencidas();
            vm.Ejecutada = true;
            var desde = vm.Desde.Value.Date;
            var hasta = vm.Hasta.Value.Date;

            if (vm.Consulta == "ocupacion")
            {
                // Ocupación desde vw_DisponibilidadVuelo: Disponibles = Capacidad − Ocupados.
                var q = from d in db.vw_DisponibilidadVuelo
                        join v in db.vw_VueloDetalle on d.VueloId equals v.VueloId
                        where d.FechaOperacion >= desde && d.FechaOperacion <= hasta
                        select new { d, v };
                if (vm.RutaId.HasValue) q = q.Where(x => x.v.RutaId == vm.RutaId.Value);
                if (vm.AeronaveId.HasValue) q = q.Where(x => x.v.AeronaveId == vm.AeronaveId.Value);
                if (!string.IsNullOrEmpty(vm.Estado)) q = q.Where(x => x.d.EstadoVueloCodigo == vm.Estado);
                vm.Ocupacion = q.OrderBy(x => x.d.FechaOperacion).ThenBy(x => x.d.HoraSalida).Select(x => x.d).ToList();
            }
            else if (vm.Consulta == "reservas")
            {
                var hastaFin = hasta.AddDays(1);
                var q = db.Reserva.Where(r => r.FechaCreacion >= desde && r.FechaCreacion < hastaFin);
                if (!string.IsNullOrEmpty(vm.Estado)) q = q.Where(r => r.EstadoReservaCodigo == vm.Estado);
                if (vm.RutaId.HasValue) q = q.Where(r => r.Asignaciones.Any(a => a.Vuelo.Frecuencia.RutaId == vm.RutaId.Value));
                if (vm.AeronaveId.HasValue) q = q.Where(r => r.Asignaciones.Any(a => a.Vuelo.AeronaveId == vm.AeronaveId.Value));
                vm.ReservasLista = q.GroupBy(r => r.EstadoReservaCodigo)
                    .Select(g => new FilaReporteReservaVM
                    {
                        Estado = g.Key,
                        Reservas = g.Count(),
                        Asientos = g.Sum(r => r.Asignaciones.Count()),
                        Importe = g.Sum(r => r.Asignaciones.Sum(a => (decimal?)a.ImporteTramo) ?? 0m)
                    }).OrderBy(f => f.Estado).ToList();
            }
            else
            {
                var hastaFin = hasta.AddDays(1);
                var q = db.Compra.Where(c => c.FechaCompra >= desde && c.FechaCompra < hastaFin);
                if (!string.IsNullOrEmpty(vm.Estado)) q = q.Where(c => c.EstadoCompraCodigo == vm.Estado);
                if (vm.RutaId.HasValue) q = q.Where(c => c.Reserva.Asignaciones.Any(a => a.Vuelo.Frecuencia.RutaId == vm.RutaId.Value));
                if (vm.AeronaveId.HasValue) q = q.Where(c => c.Reserva.Asignaciones.Any(a => a.Vuelo.AeronaveId == vm.AeronaveId.Value));
                vm.Compras = q.OrderByDescending(c => c.FechaCompra).Take(500)
                    .Select(c => new
                    {
                        c.ReservaId,
                        c.Reserva.CodigoReserva,
                        c.FechaCompra,
                        c.Importe,
                        c.MonedaCodigo,
                        c.EstadoCompraCodigo,
                        Intentos = c.Pagos.Count(),
                        Ultimo = c.Pagos.OrderByDescending(p => p.FechaPago).ThenByDescending(p => p.PagoId).Select(p => new { p.EstadoPagoCodigo, p.CodigoOperacion, Marca = p.MarcaTarjeta.Nombre, p.UltimosCuatroDigitos }).FirstOrDefault()
                    }).ToList()
                    .Select(c => new FilaReporteCompraVM
                    {
                        ReservaId = c.ReservaId,
                        CodigoReserva = c.CodigoReserva,
                        FechaCompra = c.FechaCompra,
                        Importe = c.Importe,
                        Moneda = c.MonedaCodigo,
                        EstadoCompra = c.EstadoCompraCodigo,
                        Intentos = c.Intentos,
                        UltimoPago = c.Ultimo == null ? null : c.Ultimo.EstadoPagoCodigo,
                        CodigoOperacion = c.Ultimo == null ? null : c.Ultimo.CodigoOperacion,
                        Marca = c.Ultimo == null ? null : c.Ultimo.Marca + " **** " + c.Ultimo.UltimosCuatroDigitos
                    }).ToList();
            }
            return View(vm);
        }
    }
}
