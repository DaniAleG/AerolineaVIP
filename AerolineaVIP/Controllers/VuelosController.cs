using System;
using System.Linq;
using System.Web.Mvc;
using AerolineaVIP.Models.Dominio;
using AerolineaVIP.Models.ViewModels;
using AerolineaVIP.Servicios;

namespace AerolineaVIP.Controllers
{
    /// <summary>Pantallas 5, 6 y 7 (área pública: consulta de itinerarios).</summary>
    public class VuelosController : Controller
    {
        private readonly AerolineaVIPEntities db = new AerolineaVIPEntities();
        private ServicioReservas servicio;
        private ServicioReservas Servicio { get { return servicio ?? (servicio = new ServicioReservas(db)); } }

        private void CargarListas(BusquedaVM vm)
        {
            var origenes = db.Aeropuerto.Where(a => a.Activo).OrderBy(a => a.Localidad).ToList();
            vm.Origenes = new SelectList(origenes.Select(a => new { a.CodigoIATA, Texto = a.Localidad + " (" + a.CodigoIATA + ")" }), "CodigoIATA", "Texto", vm.Origen);
            var destinos = string.IsNullOrEmpty(vm.Origen) ? origenes.Where(a => false).ToList() : Servicio.DestinosDesde(vm.Origen);
            vm.Destinos = new SelectList(destinos.Select(a => new { a.CodigoIATA, Texto = a.Localidad + " (" + a.CodigoIATA + ")" }), "CodigoIATA", "Texto", vm.Destino);
            vm.DiasOperacion = Servicio.PanelDiasOperacion();
        }

        // ===================== Pantalla 5: búsqueda =====================
        public ActionResult Buscar(string origen, string destino, DateTime? fecha, int? pasajeros)
        {
            var vm = new BusquedaVM
            {
                Origen = string.IsNullOrEmpty(origen) ? "UIO" : origen,
                Destino = destino,
                Fecha = fecha,
                Pasajeros = pasajeros ?? 1
            };
            CargarListas(vm);
            if (TempData["Mensaje"] != null) ViewBag.Informacion = TempData["Mensaje"];
            return View(vm);
        }

        /// <summary>Destinos alcanzables desde un origen (selector dependiente con jQuery).</summary>
        public JsonResult Destinos(string origen)
        {
            var lista = Servicio.DestinosDesde(origen ?? "")
                .Select(a => new { codigo = a.CodigoIATA, texto = a.Localidad + " (" + a.CodigoIATA + ")" }).ToList();
            return Json(lista, JsonRequestBehavior.AllowGet);
        }

        // ===================== Pantalla 6: resultados =====================
        public ActionResult Resultados(BusquedaVM busqueda)
        {
            // Validación en el servidor de los criterios recibidos por GET.
            if (!ModelState.IsValid)
            {
                CargarListas(busqueda);
                ViewBag.Advertencia = "Revise los criterios de búsqueda.";
                return View("Buscar", busqueda);
            }

            var rutaExiste = Servicio.DiasDeOperacion(busqueda.Origen, busqueda.Destino).Any();
            if (!rutaExiste)
            {
                CargarListas(busqueda);
                ViewBag.Informacion = "No existe una ruta entre el origen y el destino seleccionados.";
                return View("Buscar", busqueda);
            }

            var vm = Servicio.Buscar(busqueda);
            return View(vm);
        }

        // ===================== Pantalla 7: disponibilidad de asientos =====================
        public ActionResult Disponibilidad(int? id, int? pasajeros, string vuelos)
        {
            if (!id.HasValue) return RedirectToAction("Buscar");
            Servicio.ExpirarVencidas();
            var info = Servicio.Info(id.Value);
            if (info == null)
            {
                TempData["Mensaje"] = "El vuelo seleccionado no está disponible.";
                return RedirectToAction("Buscar");
            }

            var n = Math.Max(1, Math.Min(7, pasajeros ?? 1));
            var vm = new DisponibilidadVM
            {
                Info = info,
                Mapa = new MapaAsientosVM { VueloId = info.Vuelo.VueloId, Asientos = Servicio.Mapa(info.Vuelo.VueloId), Interactivo = false },
                Corte = Politicas.Ahora,
                Pasajeros = n,
                UrlResultados = Url.Action("Resultados", new
                {
                    Origen = info.Vuelo.Origen,
                    Destino = info.Vuelo.Destino,
                    Fecha = info.Vuelo.FechaOperacion.ToString("yyyy-MM-dd"),
                    Pasajeros = n
                })
            };
            var motivo = Servicio.MotivoNoComercializable(info.Vuelo, info.TieneAeronave, info.Disponibles, n);
            vm.PuedeContinuar = motivo == null;
            if (info.Vuelo.EstadoVueloCodigo == "CANCELADO" || info.Vuelo.EstadoVueloCodigo == "COMPLETADO")
            {
                vm.Mensaje = "El vuelo seleccionado no está disponible (" + Formato.TextoEstadoVuelo(info.Vuelo.EstadoVueloCodigo).ToLower() + ").";
                vm.TipoMensaje = "warning";
            }
            else if (!info.TieneAeronave)
            {
                vm.Mensaje = "Este vuelo aún no tiene aeronave asignada; sus asientos no pueden reservarse.";
                vm.TipoMensaje = "info";
            }
            else if (info.Disponibles == 0)
            {
                vm.Mensaje = "No quedan asientos disponibles en este vuelo.";
                vm.TipoMensaje = "warning";
            }
            else if (motivo != null)
            {
                vm.Mensaje = motivo;
                vm.TipoMensaje = "warning";
            }
            ViewBag.Vuelos = string.IsNullOrEmpty(vuelos) ? id.Value.ToString() : vuelos;
            return View(vm);
        }

        /// <summary>"Actualizar disponibilidad": estado actual de los asientos en JSON.</summary>
        public JsonResult EstadoAsientos(int id)
        {
            Servicio.ExpirarVencidas();
            var asientos = Servicio.Mapa(id).Select(a => new
            {
                numero = a.NumeroAsiento,
                codigo = a.Codigo,
                estado = a.EstadoVisual,
                clase = a.ClaseCss,
                texto = a.TextoEstado,
                descripcion = a.Descripcion
            }).ToList();
            return Json(new
            {
                corte = Politicas.Ahora.ToString("HH:mm"),
                disponibles = asientos.Count(a => a.estado == "DISPONIBLE"),
                reservados = asientos.Count(a => a.estado == "RESERVADO"),
                ocupados = asientos.Count(a => a.estado == "OCUPADO"),
                asientos = asientos
            }, JsonRequestBehavior.AllowGet);
        }

        protected override void Dispose(bool disposing)
        {
            if (disposing) db.Dispose();
            base.Dispose(disposing);
        }
    }
}
