using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.Linq;
using System.Web.Mvc;
using AerolineaVIP.Models.Dominio;

namespace AerolineaVIP.Models.ViewModels
{
    /// <summary>Criterios de búsqueda (pantallas 1 y 5).</summary>
    public class BusquedaVM : IValidatableObject
    {
        [Display(Name = "Origen")]
        [Required(ErrorMessage = "Seleccione el origen.")]
        [RegularExpression(@"^[A-Z]{3}$", ErrorMessage = "Seleccione un origen válido.")]
        public string Origen { get; set; }

        [Display(Name = "Destino")]
        [Required(ErrorMessage = "Seleccione el destino.")]
        [RegularExpression(@"^[A-Z]{3}$", ErrorMessage = "Seleccione un destino válido.")]
        public string Destino { get; set; }

        [Display(Name = "Fecha de salida")]
        [Required(ErrorMessage = "Indique la fecha de salida.")]
        [DataType(DataType.Date)]
        [DisplayFormat(DataFormatString = "{0:yyyy-MM-dd}", ApplyFormatInEditMode = true)]
        public DateTime? Fecha { get; set; }

        [Display(Name = "Pasajeros")]
        [Required(ErrorMessage = "Indique la cantidad de pasajeros.")]
        [Range(1, 7, ErrorMessage = "La cantidad de pasajeros debe estar entre 1 y 7.")]
        public int? Pasajeros { get; set; }

        public IEnumerable<SelectListItem> Origenes { get; set; }
        public IEnumerable<SelectListItem> Destinos { get; set; }

        /// <summary>Días de operación de cada ruta (panel informativo).</summary>
        public List<RutaDiasVM> DiasOperacion { get; set; }

        public IEnumerable<ValidationResult> Validate(ValidationContext validationContext)
        {
            if (!string.IsNullOrEmpty(Origen) && Origen == Destino)
                yield return new ValidationResult("El origen y el destino deben ser distintos.", new[] { "Destino" });
            if (Fecha.HasValue && Fecha.Value.Date < DateTime.Today)
                yield return new ValidationResult("La fecha de salida no puede ser anterior a hoy.", new[] { "Fecha" });
        }
    }

    public class RutaDiasVM
    {
        public string Nombre { get; set; }
        public string Dias { get; set; }
        public string Nota { get; set; }
    }

    /// <summary>Vuelo mostrado en los resultados (pantalla 6).</summary>
    public class VueloResultadoVM
    {
        public vw_VueloDetalle Vuelo { get; set; }
        public string Matricula { get; set; }
        public int Capacidad { get; set; }
        public int Disponibles { get; set; }
        public bool Seleccionable { get; set; }
        /// <summary>Motivo cuando no es seleccionable (cancelado, sin aeronave, sin asientos...).</summary>
        public string Motivo { get; set; }
        public string Franja { get { return Servicios.Formato.FranjaHoraria(Vuelo.HoraSalida); } }
    }

    /// <summary>Itinerario con escala (Quito–Galápagos vía Guayaquil, DP-09/DP-10).</summary>
    public class ItinerarioResultadoVM
    {
        public List<VueloResultadoVM> Tramos { get; set; }
        public int MinutosConexion { get; set; }
        public bool Seleccionable { get { return Tramos.All(t => t.Seleccionable); } }
        public string IdsVuelos { get { return string.Join(",", Tramos.Select(t => t.Vuelo.VueloId)); } }
        public string Escala { get { return Tramos.First().Vuelo.CiudadDestino; } }
    }

    public class ResultadosVM
    {
        public BusquedaVM Busqueda { get; set; }
        public string CiudadOrigen { get; set; }
        public string CiudadDestino { get; set; }
        public List<VueloResultadoVM> Directos { get; set; }
        public List<ItinerarioResultadoVM> ConEscala { get; set; }
        public string MensajeInfo { get; set; }
        public List<DateTime> ProximasFechas { get; set; }
        public bool HayResultados { get { return Directos.Any() || ConEscala.Any(); } }
    }

    /// <summary>Un asiento en el mapa de cabina (estado tomado de vw_EstadoAsientoVuelo).</summary>
    public class AsientoVM
    {
        public byte NumeroAsiento { get; set; }
        public string Codigo { get; set; }
        public byte Fila { get; set; }
        public string Lado { get; set; }
        public string Ubicacion { get; set; }
        /// <summary>DISPONIBLE | RESERVADO | OCUPADO (dato del servidor).</summary>
        public string EstadoVisual { get; set; }

        public string ClaseCss
        {
            get
            {
                switch (EstadoVisual)
                {
                    case "DISPONIBLE": return "asiento-disponible";
                    case "RESERVADO": return "asiento-reservado";
                    default: return "asiento-ocupado";
                }
            }
        }
        public string TextoEstado { get { return Servicios.Formato.TextoEstadoAsiento(EstadoVisual); } }
        public string Descripcion
        {
            get { return "Asiento " + Codigo + ", ventanilla lado " + Servicios.Formato.NombreLado(Lado) + ", fila " + Fila + ", " + TextoEstado.ToLower(); }
        }
    }

    public class MapaAsientosVM
    {
        public int VueloId { get; set; }
        public int Tramo { get; set; }
        public List<AsientoVM> Asientos { get; set; }
        /// <summary>true en la pantalla 8 (selección); false en 7 y 19 (consulta).</summary>
        public bool Interactivo { get; set; }
        public bool MostrarEstados { get; set; }

        public MapaAsientosVM()
        {
            Asientos = new List<AsientoVM>();
            MostrarEstados = true;
        }
    }

    /// <summary>Barra de datos del vuelo (pantallas 7 a 12).</summary>
    public class VueloInfoVM
    {
        public vw_VueloDetalle Vuelo { get; set; }
        public string Matricula { get; set; }
        public string Modelo { get; set; }
        public int Capacidad { get; set; }
        public int Disponibles { get; set; }
        public int Reservados { get; set; }
        public int Ocupados { get; set; }
        public bool TieneAeronave { get { return !string.IsNullOrEmpty(Matricula); } }
    }

    /// <summary>Pantalla 7: consulta de disponibilidad.</summary>
    public class DisponibilidadVM
    {
        public VueloInfoVM Info { get; set; }
        public MapaAsientosVM Mapa { get; set; }
        public DateTime Corte { get; set; }
        public string Mensaje { get; set; }
        public string TipoMensaje { get; set; }
        public bool PuedeContinuar { get; set; }
        public int Pasajeros { get; set; }
        public string UrlResultados { get; set; }
    }
}
