// Lógica de vuelos y reservas.
using System;
using System.Collections.Generic;
using System.Data.Entity;
using System.Data.SqlClient;
using System.Linq;
using AerolineaVIP.Infraestructura;
using AerolineaVIP.Models.Dominio;
using AerolineaVIP.Models.ViewModels;

namespace AerolineaVIP.Servicios
{
    public class ServicioReservas
    {
        public static readonly string[] EstadosComercializables = { "PROGRAMADO", "CONFIRMADO" };
        public static readonly string[] EstadosOcupan = { "PENDIENTE", "CONFIRMADA" };

        private readonly AerolineaVIPEntities db;

        public ServicioReservas(AerolineaVIPEntities db)
        {
            this.db = db;
        }

        // Expira las reservas pendientes vencidas y libera sus asientos (DP-07).
        private static DateTime ultimaExpiracion = DateTime.MinValue;
        private static readonly object candado = new object();

        public void ExpirarVencidas(bool forzar = false)
        {
            lock (candado)
            {
                if (!forzar && (DateTime.Now - ultimaExpiracion).TotalSeconds < 30) return;
                ultimaExpiracion = DateTime.Now;
            }

            db.Database.ExecuteSqlCommand(@"
SET NOCOUNT ON;
DECLARE @Vencidas TABLE (ReservaId INT PRIMARY KEY);
INSERT INTO @Vencidas (ReservaId)
SELECT ReservaId FROM dbo.Reserva
 WHERE EstadoReservaCodigo = 'PENDIENTE' AND FechaExpiracion IS NOT NULL AND FechaExpiracion <= @ahora;
UPDATE r SET r.EstadoReservaCodigo = 'EXPIRADA'
  FROM dbo.Reserva r JOIN @Vencidas v ON v.ReservaId = r.ReservaId;
UPDATE a SET a.EstadoAsignacionCodigo = 'LIBERADA'
  FROM dbo.ReservaPasajeroVuelo a JOIN @Vencidas v ON v.ReservaId = a.ReservaId
 WHERE a.EstadoAsignacionCodigo = 'PENDIENTE';
UPDATE c SET c.EstadoCompraCodigo = 'ANULADA'
  FROM dbo.Compra c JOIN @Vencidas v ON v.ReservaId = c.ReservaId
 WHERE c.EstadoCompraCodigo = 'PENDIENTE';",
                new SqlParameter("@ahora", Politicas.Ahora));
        }

        // ----- Consulta de vuelos -----
        public vw_VueloDetalle Vuelo(int vueloId)
        {
            return db.vw_VueloDetalle.AsNoTracking().FirstOrDefault(v => v.VueloId == vueloId);
        }

        public VueloInfoVM Info(int vueloId)
        {
            var v = Vuelo(vueloId);
            if (v == null) return null;

            var aeronave = db.Vuelo.Where(x => x.VueloId == vueloId)
                .Select(x => new
                {
                    Matricula = x.Aeronave.Matricula,
                    Fabricante = x.Aeronave.ModeloAeronave.Fabricante,
                    Modelo = x.Aeronave.ModeloAeronave.Nombre,
                    Capacidad = (int?)x.Aeronave.ModeloAeronave.CapacidadAsientos
                }).First();

            var estados = db.vw_EstadoAsientoVuelo.Where(e => e.VueloId == vueloId)
                .GroupBy(e => e.EstadoVisual)
                .Select(g => new { Estado = g.Key, Cantidad = g.Count() })
                .ToList();

            var info = new VueloInfoVM
            {
                Vuelo = v,
                Matricula = aeronave.Matricula,
                Modelo = aeronave.Matricula == null ? null : aeronave.Fabricante + " " + aeronave.Modelo,
                Capacidad = aeronave.Capacidad ?? 0,
                Disponibles = estados.Where(e => e.Estado == "DISPONIBLE").Sum(e => e.Cantidad),
                Reservados = estados.Where(e => e.Estado == "RESERVADO").Sum(e => e.Cantidad),
                Ocupados = estados.Where(e => e.Estado == "OCUPADO").Sum(e => e.Cantidad)
            };
            return info;
        }

        /// <summary>Mapa de asientos de un vuelo con el estado persistido (vw_EstadoAsientoVuelo).</summary>
        public List<AsientoVM> Mapa(int vueloId)
        {
            return db.vw_EstadoAsientoVuelo.AsNoTracking()
                .Where(e => e.VueloId == vueloId)
                .OrderBy(e => e.NumeroAsiento)
                .Select(e => new AsientoVM
                {
                    NumeroAsiento = e.NumeroAsiento,
                    Codigo = e.CodigoAsiento,
                    Fila = e.Fila,
                    Lado = e.Lado,
                    Ubicacion = e.Ubicacion,
                    EstadoVisual = e.EstadoVisual
                }).ToList();
        }

        /// <summary>Distribución de cabina de un modelo (sin estados), para la pantalla 19.</summary>
        public List<AsientoVM> Distribucion(int modeloAeronaveId)
        {
            return db.Asiento.AsNoTracking()
                .Where(s => s.ModeloAeronaveId == modeloAeronaveId)
                .OrderBy(s => s.NumeroAsiento)
                .Select(s => new AsientoVM
                {
                    NumeroAsiento = s.NumeroAsiento,
                    Codigo = s.Codigo,
                    Fila = s.Fila,
                    Lado = s.Lado,
                    Ubicacion = s.Ubicacion,
                    EstadoVisual = "DISPONIBLE"
                }).ToList();
        }

        /// <summary>Devuelve null si el vuelo se puede vender, o el motivo si no.</summary>
        public string MotivoNoComercializable(vw_VueloDetalle v, bool tieneAeronave, int disponibles, int pasajeros)
        {
            if (v.EstadoVueloCodigo == "CANCELADO") return "Vuelo cancelado: se muestra solo como información.";
            if (!EstadosComercializables.Contains(v.EstadoVueloCodigo)) return "El vuelo no se encuentra en un estado que permita reservas.";
            if (!tieneAeronave) return "Este vuelo aún no tiene aeronave asignada; sus asientos no pueden reservarse (RNP-01).";
            if (v.SalidaProgramada <= Politicas.Ahora.AddHours(Politicas.HorasCierreVentaAntesSalida))
                return "Venta cerrada: la salida es en menos de " + Politicas.HorasCierreVentaAntesSalida + " horas.";
            if (disponibles <= 0) return "Vuelo sin asientos disponibles.";
            if (disponibles < pasajeros) return "Solo quedan " + disponibles + " asientos; no alcanza para " + pasajeros + " pasajeros.";
            return null;
        }

        private Dictionary<int, vw_DisponibilidadVuelo> Disponibilidad(IEnumerable<int> ids)
        {
            var lista = ids.Distinct().ToList();
            return db.vw_DisponibilidadVuelo.AsNoTracking().Where(d => lista.Contains(d.VueloId)).ToDictionary(d => d.VueloId);
        }

        private VueloResultadoVM Resultado(vw_VueloDetalle v, Dictionary<int, vw_DisponibilidadVuelo> disp, int pasajeros)
        {
            vw_DisponibilidadVuelo d;
            disp.TryGetValue(v.VueloId, out d);
            var tieneAeronave = d != null && !string.IsNullOrEmpty(d.Matricula);
            var r = new VueloResultadoVM
            {
                Vuelo = v,
                Matricula = d == null ? null : d.Matricula,
                Capacidad = d == null ? 0 : d.Capacidad,
                Disponibles = d == null ? 0 : d.Disponibles
            };
            r.Motivo = MotivoNoComercializable(v, tieneAeronave, r.Disponibles, pasajeros);
            r.Seleccionable = r.Motivo == null;
            return r;
        }

        /// <summary>Pantalla 6: vuelos directos y con escala.</summary>
        public ResultadosVM Buscar(BusquedaVM b)
        {
            ExpirarVencidas();
            var fecha = b.Fecha.Value.Date;
            var pasajeros = b.Pasajeros ?? 1;

            var aeropuertos = db.Aeropuerto.AsNoTracking().ToList();
            var res = new ResultadosVM
            {
                Busqueda = b,
                CiudadOrigen = aeropuertos.Where(a => a.CodigoIATA == b.Origen).Select(a => a.Localidad).FirstOrDefault() ?? b.Origen,
                CiudadDestino = aeropuertos.Where(a => a.CodigoIATA == b.Destino).Select(a => a.Localidad).FirstOrDefault() ?? b.Destino,
                Directos = new List<VueloResultadoVM>(),
                ConEscala = new List<ItinerarioResultadoVM>(),
                ProximasFechas = new List<DateTime>()
            };

            var delDia = db.vw_VueloDetalle.AsNoTracking().Where(v => v.FechaOperacion == fecha).ToList();
            var disp = Disponibilidad(delDia.Select(v => v.VueloId));

            foreach (var v in delDia.Where(v => v.Origen == b.Origen && v.Destino == b.Destino).OrderBy(v => v.HoraSalida))
                res.Directos.Add(Resultado(v, disp, pasajeros));

            foreach (var par in Conexiones(delDia, b.Origen, b.Destino))
            {
                res.ConEscala.Add(new ItinerarioResultadoVM
                {
                    Tramos = new List<VueloResultadoVM> { Resultado(par.Item1, disp, pasajeros), Resultado(par.Item2, disp, pasajeros) },
                    MinutosConexion = (int)(par.Item2.SalidaProgramada - par.Item1.LlegadaProgramada).TotalMinutes
                });
            }

            if (!res.HayResultados)
            {
                var dias = DiasDeOperacion(b.Origen, b.Destino);
                res.MensajeInfo = dias.Count == 0
                    ? "No existe una ruta entre el origen y el destino seleccionados."
                    : "La ruta " + res.CiudadOrigen + "–" + res.CiudadDestino + " opera " + ListaDias(dias) + ". No hay vuelos programados el "
                      + Formato.FechaConDia(fecha) + ". Elija otra fecha.";
                res.ProximasFechas = ProximasFechas(b.Origen, b.Destino);
            }
            return res;
        }

        /// <summary>Pares de vuelos con una escala que cumplen la conexión mínima y máxima (DP-10).</summary>
        private IEnumerable<Tuple<vw_VueloDetalle, vw_VueloDetalle>> Conexiones(List<vw_VueloDetalle> delDia, string origen, string destino)
        {
            var min = Politicas.MinutosMinimosConexion;
            var max = Politicas.MinutosMaximosConexion;
            return from t1 in delDia
                   where t1.Origen == origen && t1.Destino != destino
                   from t2 in delDia
                   where t2.Origen == t1.Destino && t2.Destino == destino
                   let espera = (t2.SalidaProgramada - t1.LlegadaProgramada).TotalMinutes
                   where espera >= min && espera <= max
                   orderby t1.SalidaProgramada
                   select Tuple.Create(t1, t2);
        }

        /// <summary>Días (1–7) en que la ruta opera, directa o con una escala válida.</summary>
        public List<int> DiasDeOperacion(string origen, string destino)
        {
            var frec = db.Frecuencia.AsNoTracking().Where(f => f.Activa && f.Ruta.Activa)
                .Select(f => new
                {
                    f.DiaSemana,
                    f.HoraSalida,
                    f.HoraLlegada,
                    Origen = f.Ruta.AeropuertoOrigen.CodigoIATA,
                    Destino = f.Ruta.AeropuertoDestino.CodigoIATA
                }).ToList();

            var dias = frec.Where(f => f.Origen == origen && f.Destino == destino).Select(f => (int)f.DiaSemana).ToList();
            var min = Politicas.MinutosMinimosConexion;
            var max = Politicas.MinutosMaximosConexion;
            dias.AddRange(from a in frec
                          where a.Origen == origen && a.Destino != destino
                          from c in frec
                          where c.DiaSemana == a.DiaSemana && c.Origen == a.Destino && c.Destino == destino
                          let espera = (c.HoraSalida - a.HoraLlegada).TotalMinutes
                          where espera >= min && espera <= max
                          select (int)a.DiaSemana);
            return dias.Distinct().OrderBy(d => d).ToList();
        }

        public static string ListaDias(List<int> dias)
        {
            var nombres = dias.Select(d => Formato.NombreDia(d).ToLower()).ToList();
            if (nombres.Count <= 1) return string.Join("", nombres);
            return string.Join(", ", nombres.Take(nombres.Count - 1)) + " y " + nombres.Last();
        }

        /// <summary>Próximas fechas (hasta 6) con vuelos programados para la ruta.</summary>
        public List<DateTime> ProximasFechas(string origen, string destino)
        {
            var hoy = DateTime.Today;
            var hasta = hoy.AddDays(90);
            var futuros = db.vw_VueloDetalle.AsNoTracking()
                .Where(v => v.FechaOperacion >= hoy && v.FechaOperacion <= hasta
                            && (v.EstadoVueloCodigo == "PROGRAMADO" || v.EstadoVueloCodigo == "CONFIRMADO"))
                .ToList();
            var fechas = futuros.Where(v => v.Origen == origen && v.Destino == destino).Select(v => v.FechaOperacion).ToList();
            foreach (var grupo in futuros.GroupBy(v => v.FechaOperacion))
                if (Conexiones(grupo.ToList(), origen, destino).Any()) fechas.Add(grupo.Key);
            return fechas.Distinct().OrderBy(f => f).Take(6).ToList();
        }

        /// <summary>Destinos posibles desde un origen.</summary>
        public List<Aeropuerto> DestinosDesde(string origen)
        {
            var codigos = new HashSet<string>();
            foreach (var a in db.Aeropuerto.AsNoTracking().Where(x => x.Activo).ToList())
                if (a.CodigoIATA != origen && DiasDeOperacion(origen, a.CodigoIATA).Any()) codigos.Add(a.CodigoIATA);
            return db.Aeropuerto.AsNoTracking().Where(a => codigos.Contains(a.CodigoIATA)).OrderBy(a => a.Localidad).ToList();
        }

        /// <summary>Panel informativo de días de operación (pantallas 1 y 5).</summary>
        public List<RutaDiasVM> PanelDiasOperacion()
        {
            var frec = db.Frecuencia.AsNoTracking().Where(f => f.Activa && f.Ruta.Activa)
                .Select(f => new
                {
                    f.DiaSemana,
                    Origen = f.Ruta.AeropuertoOrigen.Localidad,
                    Destino = f.Ruta.AeropuertoDestino.Localidad,
                    CodO = f.Ruta.AeropuertoOrigen.CodigoIATA,
                    CodD = f.Ruta.AeropuertoDestino.CodigoIATA
                }).ToList();

            var lista = new List<RutaDiasVM>();
            // Rutas directas agrupadas sin importar el sentido (Quito–Guayaquil, Quito–Manta...).
            var grupos = frec.GroupBy(f => string.CompareOrdinal(f.CodO, f.CodD) < 0 ? f.CodO + "|" + f.CodD : f.CodD + "|" + f.CodO);
            foreach (var g in grupos)
            {
                var primero = g.First();
                var a = string.CompareOrdinal(primero.CodO, primero.CodD) < 0 ? primero.Origen : primero.Destino;
                var c = string.CompareOrdinal(primero.CodO, primero.CodD) < 0 ? primero.Destino : primero.Origen;
                if (c == "Quito") { var t = a; a = c; c = t; }
                var dias = g.Select(x => (int)x.DiaSemana).Distinct().OrderBy(d => d).ToList();
                lista.Add(new RutaDiasVM
                {
                    Nombre = a + "–" + c,
                    Dias = string.Join(" · ", dias.Select(Formato.NombreDiaCorto)),
                    Nota = dias.Contains(7) && dias.Count > 1 ? "Domingo: tramo del itinerario a Galápagos" : null
                });
            }

            // Itinerario con escala (Quito–Galápagos).
            foreach (var esc in new[] { new { O = "UIO", D = "GPS" } })
            {
                var dias = DiasDeOperacion(esc.O, esc.D);
                if (dias.Any() && !frec.Any(f => f.CodO == esc.O && f.CodD == esc.D))
                    lista.Add(new RutaDiasVM
                    {
                        Nombre = "Quito–Galápagos",
                        Dias = string.Join(" · ", dias.Select(Formato.NombreDiaCorto)),
                        Nota = "Con escala en Guayaquil"
                    });
            }
            return lista.OrderBy(l => l.Nombre).ToList();
        }

        // ----- Selección de asientos y creación de la reserva -----

        /// <summary>Vuelve a validar la selección de asientos. Devuelve la lista de errores.</summary>
        public List<string> ValidarSeleccion(ReservaEnCurso op)
        {
            var errores = new List<string>();
            if (op.VueloIds.Count == 0 || op.Pasajeros < 1 || op.Pasajeros > 7)
            {
                errores.Add("La operación no tiene vuelos o pasajeros válidos.");
                return errores;
            }
            if (!op.AsientosCompletos)
            {
                errores.Add("Falta asignar asiento a uno o más pasajeros en algún tramo.");
                return errores;
            }

            for (int t = 0; t < op.VueloIds.Count; t++)
            {
                var vueloId = op.VueloIds[t];
                var info = Info(vueloId);
                if (info == null) { errores.Add("El vuelo seleccionado no existe."); continue; }

                var motivo = MotivoNoComercializable(info.Vuelo, info.TieneAeronave, info.Disponibles, op.Pasajeros);
                if (motivo != null) { errores.Add(info.Vuelo.CodigoVuelo + ": " + motivo); continue; }

                var mapa = Mapa(vueloId).ToDictionary(a => a.NumeroAsiento);
                var usados = new HashSet<byte>();
                for (int p = 0; p < op.Pasajeros; p++)
                {
                    var numero = op.Asientos[p][t];
                    AsientoVM asiento;
                    if (!mapa.TryGetValue(numero, out asiento))
                        errores.Add("El asiento seleccionado para el Pasajero " + (p + 1) + " no existe en la aeronave del vuelo " + info.Vuelo.CodigoVuelo + ".");
                    else if (asiento.EstadoVisual != "DISPONIBLE")
                        errores.Add("El asiento " + asiento.Codigo + " del vuelo " + info.Vuelo.CodigoVuelo + " acaba de ser " + asiento.TextoEstado.ToLower() + ". Seleccione otro.");
                    else if (!usados.Add(numero))
                        errores.Add("El asiento " + asiento.Codigo + " está asignado a dos pasajeros en el tramo " + (t + 1) + ".");
                }
            }
            return errores;
        }

        /// <summary>Resumen de la operación en curso (pantallas 9 y 10).</summary>
        public ResumenOperacionVM Resumen(ReservaEnCurso op)
        {
            var r = new ResumenOperacionVM { Moneda = Politicas.Moneda };
            var codigos = new List<Dictionary<byte, string>>();
            for (int t = 0; t < op.VueloIds.Count; t++)
            {
                var v = Vuelo(op.VueloIds[t]);
                if (v == null) continue;
                r.Tramos.Add(new TramoResumenVM
                {
                    Orden = t + 1,
                    VueloId = v.VueloId,
                    CodigoVuelo = v.CodigoVuelo,
                    CiudadOrigen = v.CiudadOrigen,
                    CiudadDestino = v.CiudadDestino,
                    Origen = v.Origen,
                    Destino = v.Destino,
                    Fecha = v.FechaOperacion,
                    Salida = v.HoraSalida,
                    Llegada = v.HoraLlegada,
                    Tarifa = v.TarifaReferencial
                });
                codigos.Add(Mapa(v.VueloId).ToDictionary(a => a.NumeroAsiento, a => a.Codigo));
            }

            for (int p = 0; p < op.Pasajeros; p++)
            {
                var datos = p < op.Datos.Count ? op.Datos[p] : null;
                var pr = new PasajeroResumenVM
                {
                    Nombre = datos != null ? datos.NombreCompleto : "Pasajero " + (p + 1),
                    Documento = datos != null ? Documento(datos.TipoDocumentoCodigo, datos.NumeroDocumento) : "—",
                    Importe = r.Tramos.Sum(t => t.Tarifa)
                };
                for (int t = 0; t < r.Tramos.Count; t++)
                {
                    byte numero = p < op.Asientos.Count && t < op.Asientos[p].Count ? op.Asientos[p][t] : (byte)0;
                    string codigo;
                    pr.Asientos.Add(numero > 0 && codigos[t].TryGetValue(numero, out codigo) ? codigo : "—");
                }
                r.Pasajeros.Add(pr);
            }
            return r;
        }

        public static string Documento(string tipo, string numero)
        {
            return (tipo == "CEDULA" ? "Cédula " : tipo == "PASAPORTE" ? "Pasaporte " : (tipo + " ")) + numero;
        }

        /// <summary>Fecha de expiración de una reserva pendiente según DP-07.</summary>
        public static DateTime CalcularExpiracion(DateTime creacion, DateTime primeraSalida)
        {
            var porRetencion = creacion.AddHours(Politicas.HorasRetencionReserva);
            var porSalida = primeraSalida.AddHours(-Politicas.HorasCierreVentaAntesSalida);
            return porRetencion < porSalida ? porRetencion : porSalida;
        }

        /// <summary>Guarda la reserva y sus asientos en una transacción.</summary>
        public Reserva CrearReserva(ReservaEnCurso op, string usuarioId, out List<string> errores)
        {
            ExpirarVencidas(true);
            errores = ValidarSeleccion(op);
            if (errores.Any()) return null;

            if (!op.DatosCompletos)
            {
                errores.Add("Faltan los datos de uno o más pasajeros.");
                return null;
            }
            var duplicados = op.Datos.GroupBy(d => d.TipoDocumentoCodigo + "|" + d.NumeroDocumento.ToUpper()).Where(g => g.Count() > 1).ToList();
            if (duplicados.Any())
            {
                errores.Add("No se puede repetir el mismo documento en una reserva.");
                return null;
            }

            var ahora = Politicas.Ahora;
            var vuelos = op.VueloIds.Select(Vuelo).ToList();
            var primeraSalida = vuelos.Min(v => v.SalidaProgramada);

            using (var tx = db.Database.BeginTransaction())
            {
                try
                {
                    var reserva = new Reserva
                    {
                        UsuarioId = usuarioId,
                        FechaCreacion = ahora,
                        FechaExpiracion = CalcularExpiracion(ahora, primeraSalida),
                        EstadoReservaCodigo = "PENDIENTE"
                    };
                    db.Reserva.Add(reserva);

                    for (int p = 0; p < op.Pasajeros; p++)
                    {
                        var pasajero = ObtenerOCrearPasajero(op.Datos[p], ahora);
                        for (int t = 0; t < vuelos.Count; t++)
                        {
                            var vueloId = vuelos[t].VueloId;
                            if (pasajero.PasajeroId > 0 && db.ReservaPasajeroVuelo.Any(a => a.PasajeroId == pasajero.PasajeroId
                                    && a.VueloId == vueloId && EstadosOcupan.Contains(a.EstadoAsignacionCodigo)))
                            {
                                errores.Add(pasajero.NombreCompleto + " ya tiene un asiento vigente en el vuelo " + vuelos[t].CodigoVuelo + ".");
                                continue;
                            }
                            db.ReservaPasajeroVuelo.Add(new ReservaPasajeroVuelo
                            {
                                Reserva = reserva,
                                Pasajero = pasajero,
                                VueloId = vueloId,
                                NumeroAsiento = op.Asientos[p][t],
                                ImporteTramo = vuelos[t].TarifaReferencial,
                                EstadoAsignacionCodigo = "PENDIENTE",
                                FechaAsignacion = ahora
                            });
                        }
                    }

                    if (errores.Any())
                    {
                        tx.Rollback();
                        return null;
                    }

                    db.SaveChanges();
                    tx.Commit();
                    return reserva;
                }
                catch (Exception ex)
                {
                    tx.Rollback();
                    errores.Add(ErroresBD.Mensaje(ex));
                    return null;
                }
            }
        }

        /// <summary>Reutiliza el pasajero si ya existe (TipoDocumento + NumeroDocumento) o lo crea.</summary>
        private Pasajero ObtenerOCrearPasajero(PasajeroFormVM d, DateTime ahora)
        {
            var numero = d.NumeroDocumento.Trim().ToUpper();
            var existente = db.Pasajero.FirstOrDefault(p => p.TipoDocumentoCodigo == d.TipoDocumentoCodigo && p.NumeroDocumento == numero);
            if (existente != null)
            {
                // Solo se completan datos de contacto vacíos; la identidad registrada no se sobrescribe.
                if (string.IsNullOrEmpty(existente.Correo) && !string.IsNullOrWhiteSpace(d.Correo)) existente.Correo = d.Correo.Trim();
                if (string.IsNullOrEmpty(existente.Telefono) && !string.IsNullOrWhiteSpace(d.Telefono)) existente.Telefono = d.Telefono.Trim();
                if (!existente.FechaNacimiento.HasValue && d.FechaNacimiento.HasValue) existente.FechaNacimiento = d.FechaNacimiento;
                return existente;
            }

            var nuevo = new Pasajero
            {
                TipoDocumentoCodigo = d.TipoDocumentoCodigo,
                NumeroDocumento = numero,
                Nombres = d.Nombres.Trim(),
                Apellidos = d.Apellidos.Trim(),
                FechaNacimiento = d.FechaNacimiento,
                Correo = string.IsNullOrWhiteSpace(d.Correo) ? null : d.Correo.Trim(),
                Telefono = string.IsNullOrWhiteSpace(d.Telefono) ? null : d.Telefono.Trim(),
                FechaRegistro = ahora
            };
            db.Pasajero.Add(nuevo);
            return nuevo;
        }

        // ----- Detalle de una reserva -----
        public Reserva CargarReserva(int reservaId)
        {
            return db.Reserva
                .Include(r => r.Asignaciones.Select(a => a.Pasajero))
                .Include(r => r.Asignaciones.Select(a => a.CheckIns))
                .Include(r => r.Compras.Select(c => c.Pagos.Select(p => p.MarcaTarjeta)))
                .Include(r => r.Compras.Select(c => c.Pagos.Select(p => p.MetodoPago)))
                .Include(r => r.Compras.Select(c => c.Comprobantes))
                .FirstOrDefault(r => r.ReservaId == reservaId);
        }

        /// <summary>Códigos de asiento (1D, 2I...) por vuelo y número.</summary>
        public Dictionary<string, string> CodigosAsiento(IEnumerable<int> vueloIds)
        {
            var ids = vueloIds.Distinct().ToList();
            var modelos = db.Vuelo.Where(v => ids.Contains(v.VueloId))
                .Select(v => new { v.VueloId, Modelo = (int?)v.Aeronave.ModeloAeronaveId }).ToList();
            var porDefecto = db.ModeloAeronave.Select(m => (int?)m.ModeloAeronaveId).FirstOrDefault();
            var asientos = db.Asiento.AsNoTracking().ToList();
            var dic = new Dictionary<string, string>();
            foreach (var v in modelos)
            {
                var modelo = v.Modelo ?? porDefecto;
                foreach (var s in asientos.Where(s => s.ModeloAeronaveId == modelo))
                    dic[v.VueloId + "|" + s.NumeroAsiento] = s.Codigo;
            }
            return dic;
        }

        public DetalleReservaVM Detalle(Reserva r)
        {
            var vueloIds = r.Asignaciones.Select(a => a.VueloId).Distinct().ToList();
            var vuelos = db.vw_VueloDetalle.AsNoTracking().Where(v => vueloIds.Contains(v.VueloId)).ToDictionary(v => v.VueloId);
            var codigos = CodigosAsiento(vueloIds);
            var orden = vuelos.Values.OrderBy(v => v.SalidaProgramada).Select(v => v.VueloId).ToList();

            var compra = r.Compras.OrderByDescending(c => c.EstadoCompraCodigo == "PAGADA")
                                  .ThenByDescending(c => c.EstadoCompraCodigo == "PENDIENTE")
                                  .ThenByDescending(c => c.FechaCompra).FirstOrDefault();
            var vm = new DetalleReservaVM
            {
                Reserva = r,
                Compra = compra,
                UltimoPago = compra == null ? null : compra.Pagos.OrderByDescending(p => p.FechaPago).ThenByDescending(p => p.PagoId).FirstOrDefault(),
                Comprobantes = r.Compras.SelectMany(c => c.Comprobantes).OrderBy(c => c.FechaEmision).ToList(),
                Lineas = r.Asignaciones
                    .OrderBy(a => orden.IndexOf(a.VueloId)).ThenBy(a => a.NumeroAsiento)
                    .Select(a =>
                    {
                        var v = vuelos[a.VueloId];
                        string codigo;
                        var check = a.CheckIns.OrderByDescending(c => c.FechaRegistro).FirstOrDefault();
                        return new LineaReservaVM
                        {
                            AsignacionId = a.AsignacionId,
                            Orden = orden.IndexOf(a.VueloId) + 1,
                            CodigoVuelo = v.CodigoVuelo,
                            Ruta = v.CiudadOrigen + " → " + v.CiudadDestino,
                            Fecha = v.FechaOperacion,
                            Salida = v.HoraSalida,
                            Llegada = v.HoraLlegada,
                            SalidaProgramada = v.SalidaProgramada,
                            Pasajero = a.Pasajero.NombreCompleto,
                            Documento = Documento(a.Pasajero.TipoDocumentoCodigo, a.Pasajero.NumeroDocumento),
                            Asiento = codigos.TryGetValue(a.VueloId + "|" + a.NumeroAsiento, out codigo) ? codigo : "N.º " + a.NumeroAsiento,
                            Importe = a.ImporteTramo,
                            EstadoAsignacion = a.EstadoAsignacionCodigo,
                            CheckIn = check == null ? null : check.EstadoCheckInCodigo
                        };
                    }).ToList()
            };
            return vm;
        }

        // ----- Cancelación (DP-08) -----

        /// <summary>Indica si la reserva se puede cancelar.</summary>
        public void EvaluarCancelacion(DetalleReservaVM vm, bool esAdministracion)
        {
            var r = vm.Reserva;
            var ahora = Politicas.Ahora;
            var horas = Politicas.HorasMinimasCancelacionPagada;
            var vigentes = vm.Lineas.Where(l => EstadosOcupan.Contains(l.EstadoAsignacion)).ToList();
            var primeraSalida = vigentes.Any() ? vigentes.Min(l => l.SalidaProgramada) : (DateTime?)null;

            vm.CondicionesCancelacion = r.EstadoReservaCodigo == "CONFIRMADA"
                ? "Política de cancelación (DP-08): una reserva pagada puede cancelarse en línea hasta " + horas
                  + " horas antes de la primera salida y siempre que ningún pasajero haya hecho check-in. La cancelación libera los asientos, anula la compra, "
                  + "registra el reverso SIMULADO del pago y emite una nota de crédito académica. Dentro de las " + horas
                  + " horas solo el personal administrativo puede cancelar (excepción autorizada). No se aplican penalizaciones; el reembolso es simulado."
                : "Política de cancelación (DP-08): una reserva pendiente de pago puede cancelarse sin costo mientras no haya expirado. Los asientos vuelven a estar disponibles de inmediato.";

            if (r.EstadoReservaCodigo == "CANCELADA" || r.EstadoReservaCodigo == "EXPIRADA")
            {
                vm.MotivoNoCancelar = "La reserva ya está " + Formato.TextoEstadoReserva(r.EstadoReservaCodigo).ToLower() + ".";
                return;
            }
            if (!primeraSalida.HasValue)
            {
                vm.MotivoNoCancelar = "La reserva no tiene asientos vigentes.";
                return;
            }
            if (primeraSalida.Value <= ahora)
            {
                vm.MotivoNoCancelar = "El viaje ya inició o se realizó; no puede cancelarse.";
                return;
            }
            if (vm.Lineas.Any(l => l.CheckIn == "COMPLETADO"))
            {
                vm.MotivoNoCancelar = "Uno o más pasajeros ya realizaron el check-in; la cancelación debe gestionarse en el counter.";
                return;
            }
            if (r.EstadoReservaCodigo == "CONFIRMADA")
            {
                vm.RequiereMotivo = true;
                if ((primeraSalida.Value - ahora).TotalHours < horas && !esAdministracion)
                {
                    vm.MotivoNoCancelar = "Faltan menos de " + horas + " horas para la salida: la cancelación de una reserva pagada solo puede autorizarla el personal administrativo.";
                    return;
                }
            }
            vm.PuedeCancelar = true;
        }

        /// <summary>Ejecuta la cancelación autorizada en una sola transacción.</summary>
        public string Cancelar(int reservaId, string motivo, bool esAdministracion)
        {
            ExpirarVencidas(true);
            var r = CargarReserva(reservaId);
            if (r == null) return "No se encontró la operación solicitada.";
            var vm = Detalle(r);
            EvaluarCancelacion(vm, esAdministracion);
            if (!vm.PuedeCancelar) return vm.MotivoNoCancelar;
            if (vm.RequiereMotivo && (string.IsNullOrWhiteSpace(motivo) || motivo.Trim().Length < 10))
                return "Indique el motivo de la cancelación (10 a 300 caracteres).";

            var ahora = Politicas.Ahora;
            using (var tx = db.Database.BeginTransaction())
            {
                try
                {
                    var estabaPagada = r.EstadoReservaCodigo == "CONFIRMADA";
                    r.EstadoReservaCodigo = "CANCELADA";
                    foreach (var a in r.Asignaciones.Where(a => EstadosOcupan.Contains(a.EstadoAsignacionCodigo)))
                        a.EstadoAsignacionCodigo = "CANCELADA";

                    foreach (var c in r.Compras.Where(c => c.EstadoCompraCodigo == "PENDIENTE" || c.EstadoCompraCodigo == "PAGADA").ToList())
                    {
                        var pagada = c.EstadoCompraCodigo == "PAGADA";
                        c.EstadoCompraCodigo = "ANULADA";
                        if (!pagada) continue;

                        foreach (var p in c.Pagos.Where(p => p.EstadoPagoCodigo == "APROBADO"))
                        {
                            p.EstadoPagoCodigo = "REVERSADO";
                            var texto = "Reverso simulado por cancelación. Motivo: " + (motivo ?? "").Trim();
                            p.MensajeResultado = texto.Length > 200 ? texto.Substring(0, 200) : texto;
                        }
                        foreach (var cb in c.Comprobantes.Where(x => x.TipoComprobanteCodigo == "CONFIRMACION" && x.EstadoComprobanteCodigo == "EMITIDO"))
                            cb.EstadoComprobanteCodigo = "ANULADO";

                        db.Comprobante.Add(new Comprobante
                        {
                            CompraId = c.CompraId,
                            TipoComprobanteCodigo = "NOTA_CREDITO",
                            NumeroComprobante = ServicioPagos.SiguienteNumero(db, "NOTA_CREDITO"),
                            FechaEmision = ahora,
                            EstadoComprobanteCodigo = "EMITIDO"
                        });
                    }

                    db.SaveChanges();
                    tx.Commit();
                    return null;
                }
                catch (Exception ex)
                {
                    tx.Rollback();
                    return ErroresBD.Mensaje(ex);
                }
            }
        }
    }
}
