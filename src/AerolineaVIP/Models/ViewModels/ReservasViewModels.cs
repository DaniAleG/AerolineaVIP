using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.Linq;
using System.Web.Mvc;
using AerolineaVIP.Models.Dominio;

namespace AerolineaVIP.Models.ViewModels
{
    /// <summary>Reserva en curso (pantallas 8 a 10), guardada en sesión.</summary>
    [Serializable]
    public class ReservaEnCurso
    {
        public List<int> VueloIds { get; set; }
        public int Pasajeros { get; set; }
        /// <summary>Asientos[pasajero][tramo] = NumeroAsiento (0 = sin asignar).</summary>
        public List<List<byte>> Asientos { get; set; }
        public List<PasajeroFormVM> Datos { get; set; }
        /// <summary>Clave para evitar reservas duplicadas por doble envío.</summary>
        public Guid Token { get; set; }

        public ReservaEnCurso()
        {
            VueloIds = new List<int>();
            Asientos = new List<List<byte>>();
            Datos = new List<PasajeroFormVM>();
            Token = Guid.NewGuid();
        }

        public bool AsientosCompletos
        {
            get
            {
                return Asientos.Count == Pasajeros && Asientos.All(a => a.Count == VueloIds.Count && a.All(n => n > 0));
            }
        }

        public bool DatosCompletos { get { return Datos.Count == Pasajeros; } }
    }

    /// <summary>Datos de un pasajero (pantalla 9).</summary>
    [Serializable]
    public class PasajeroFormVM : IValidatableObject
    {
        [Display(Name = "Tipo de documento")]
        [Required(ErrorMessage = "Seleccione el tipo de documento.")]
        public string TipoDocumentoCodigo { get; set; }

        [Display(Name = "Número de documento")]
        [Required(ErrorMessage = "Ingrese el número de documento.")]
        [StringLength(20, MinimumLength = 5, ErrorMessage = "El documento debe tener entre {2} y {1} caracteres.")]
        [RegularExpression(@"^[A-Za-z0-9]+$", ErrorMessage = "Use solo letras y números.")]
        public string NumeroDocumento { get; set; }

        [Display(Name = "Nombres")]
        [Required(ErrorMessage = "Ingrese los nombres.")]
        [StringLength(100)]
        [RegularExpression(@"^[A-Za-zÁÉÍÓÚÜÑáéíóúüñ' .-]+$", ErrorMessage = "Use solo letras y espacios.")]
        public string Nombres { get; set; }

        [Display(Name = "Apellidos")]
        [Required(ErrorMessage = "Ingrese los apellidos.")]
        [StringLength(100)]
        [RegularExpression(@"^[A-Za-zÁÉÍÓÚÜÑáéíóúüñ' .-]+$", ErrorMessage = "Use solo letras y espacios.")]
        public string Apellidos { get; set; }

        [Display(Name = "Fecha de nacimiento (opcional)")]
        [DataType(DataType.Date)]
        [DisplayFormat(DataFormatString = "{0:yyyy-MM-dd}", ApplyFormatInEditMode = true)]
        public DateTime? FechaNacimiento { get; set; }

        [Display(Name = "Correo (opcional)")]
        [EmailAddress(ErrorMessage = "El correo no tiene un formato válido.")]
        [StringLength(256)]
        public string Correo { get; set; }

        [Display(Name = "Teléfono (opcional)")]
        [RegularExpression(@"^[0-9+ ]{7,20}$", ErrorMessage = "Use solo dígitos, espacios y «+» (7 a 20).")]
        public string Telefono { get; set; }

        public IEnumerable<ValidationResult> Validate(ValidationContext validationContext)
        {
            if (TipoDocumentoCodigo == "CEDULA" && !string.IsNullOrEmpty(NumeroDocumento)
                && !System.Text.RegularExpressions.Regex.IsMatch(NumeroDocumento, @"^\d{10}$"))
                yield return new ValidationResult("La cédula debe tener 10 dígitos.", new[] { "NumeroDocumento" });
            if (FechaNacimiento.HasValue && (FechaNacimiento.Value <= new DateTime(1900, 1, 1) || FechaNacimiento.Value.Date > DateTime.Today))
                yield return new ValidationResult("La fecha de nacimiento debe ser posterior a 01/01/1900 y no futura.", new[] { "FechaNacimiento" });
        }

        public string NombreCompleto { get { return ((Nombres ?? "") + " " + (Apellidos ?? "")).Trim(); } }
    }

    /// <summary>Tramo de la operación con su información y su mapa.</summary>
    public class TramoSeleccionVM
    {
        public int Indice { get; set; }
        public VueloInfoVM Info { get; set; }
        public MapaAsientosVM Mapa { get; set; }
    }

    /// <summary>Pantalla 8.</summary>
    public class SeleccionAsientoVM
    {
        public List<TramoSeleccionVM> Tramos { get; set; }
        public int Pasajeros { get; set; }
        public ReservaEnCurso Operacion { get; set; }
        public string Mensaje { get; set; }
        public string TipoMensaje { get; set; }
        public bool SinAsientos { get; set; }
    }

    /// <summary>Resumen de la operación (pantallas 9, 10 y 11).</summary>
    public class ResumenOperacionVM
    {
        public List<TramoResumenVM> Tramos { get; set; }
        public List<PasajeroResumenVM> Pasajeros { get; set; }
        public decimal Total { get { return Pasajeros.Sum(p => p.Importe); } }
        public string Moneda { get; set; }
        public string RutaTexto
        {
            get
            {
                if (!Tramos.Any()) return "";
                var ciudades = new List<string> { Tramos.First().CiudadOrigen };
                ciudades.AddRange(Tramos.Select(t => t.CiudadDestino));
                return Tramos.Count > 1 ? ciudades.First() + " → " + ciudades.Last() + " (escala en " + string.Join(", ", ciudades.Skip(1).Take(ciudades.Count - 2)) + ")"
                                         : ciudades.First() + " → " + ciudades.Last();
            }
        }

        public ResumenOperacionVM()
        {
            Tramos = new List<TramoResumenVM>();
            Pasajeros = new List<PasajeroResumenVM>();
            Moneda = "USD";
        }
    }

    public class TramoResumenVM
    {
        public int Orden { get; set; }
        public int VueloId { get; set; }
        public string CodigoVuelo { get; set; }
        public string CiudadOrigen { get; set; }
        public string CiudadDestino { get; set; }
        public string Origen { get; set; }
        public string Destino { get; set; }
        public DateTime Fecha { get; set; }
        public TimeSpan Salida { get; set; }
        public TimeSpan Llegada { get; set; }
        public decimal Tarifa { get; set; }
        public string Matricula { get; set; }
    }

    public class PasajeroResumenVM
    {
        public string Nombre { get; set; }
        public string Documento { get; set; }
        public List<string> Asientos { get; set; }
        public decimal Importe { get; set; }
        public PasajeroResumenVM() { Asientos = new List<string>(); }
    }

    /// <summary>Pantalla 9.</summary>
    public class PasajerosVM
    {
        public List<PasajeroFormVM> Pasajeros { get; set; }
        public ResumenOperacionVM Resumen { get; set; }
        public IEnumerable<SelectListItem> TiposDocumento { get; set; }
        /// <summary>Código de asiento de cada pasajero por tramo (para los títulos de los bloques).</summary>
        public List<string> AsientosPorPasajero { get; set; }
    }

    /// <summary>Pantalla 10.</summary>
    public class ConfirmarReservaVM
    {
        public ResumenOperacionVM Resumen { get; set; }

        [Display(Name = "Acepto las condiciones de la reserva")]
        [Infraestructura.DebeSerVerdadero(ErrorMessage = "Debe aceptar las condiciones de la reserva.")]
        public bool AceptaCondiciones { get; set; }

        /// <summary>Solo personal de counter: cliente titular (usuario o correo).</summary>
        [Display(Name = "Cliente titular (usuario o correo)")]
        [StringLength(256)]
        public string ClienteTitular { get; set; }

        public bool EsPersonal { get; set; }
        public int HorasRetencion { get; set; }
        public DateTime ExpiracionEstimada { get; set; }
        public string Accion { get; set; }
    }

    // ----- Consulta y gestión (13 y 14) -----
    public class FilaReservaVM
    {
        public int ReservaId { get; set; }
        public string CodigoReserva { get; set; }
        public DateTime? FechaViaje { get; set; }
        public string Ruta { get; set; }
        public int Pasajeros { get; set; }
        public string EstadoReserva { get; set; }
        public string EstadoCompra { get; set; }
        public DateTime? FechaExpiracion { get; set; }
        public string Usuario { get; set; }
        public bool PuedePagar { get { return EstadoReserva == "PENDIENTE"; } }
    }

    public class MisReservasVM : IValidatableObject
    {
        [Display(Name = "Fecha de viaje desde (opcional)")]
        [DataType(DataType.Date)]
        [DisplayFormat(DataFormatString = "{0:yyyy-MM-dd}", ApplyFormatInEditMode = true)]
        public DateTime? Desde { get; set; }

        [Display(Name = "Hasta (opcional)")]
        [DataType(DataType.Date)]
        [DisplayFormat(DataFormatString = "{0:yyyy-MM-dd}", ApplyFormatInEditMode = true)]
        public DateTime? Hasta { get; set; }

        [Display(Name = "Estado (opcional)")]
        public string Estado { get; set; }

        [Display(Name = "Código, usuario o documento (opcional)")]
        [StringLength(256)]
        public string Buscar { get; set; }

        public int Pagina { get; set; }
        public int TotalPaginas { get; set; }
        public bool EsPersonal { get; set; }
        public List<FilaReservaVM> Filas { get; set; }
        public IEnumerable<SelectListItem> Estados { get; set; }

        public IEnumerable<ValidationResult> Validate(ValidationContext validationContext)
        {
            if (Desde.HasValue && Hasta.HasValue && Hasta.Value < Desde.Value)
                yield return new ValidationResult("«Hasta» no puede ser anterior a «Desde».", new[] { "Hasta" });
        }
    }

    public class LineaReservaVM
    {
        public int AsignacionId { get; set; }
        public int Orden { get; set; }
        public string CodigoVuelo { get; set; }
        public string Ruta { get; set; }
        public DateTime Fecha { get; set; }
        public TimeSpan Salida { get; set; }
        public TimeSpan Llegada { get; set; }
        public DateTime SalidaProgramada { get; set; }
        public string Pasajero { get; set; }
        public string Documento { get; set; }
        public string Asiento { get; set; }
        public decimal Importe { get; set; }
        public string EstadoAsignacion { get; set; }
        public string CheckIn { get; set; }
    }

    /// <summary>Pantallas 12 y 14: detalle completo de una reserva.</summary>
    public class DetalleReservaVM
    {
        public Reserva Reserva { get; set; }
        public string Titular { get; set; }
        public string TitularCorreo { get; set; }
        public List<LineaReservaVM> Lineas { get; set; }
        public Compra Compra { get; set; }
        public Pago UltimoPago { get; set; }
        public List<Comprobante> Comprobantes { get; set; }
        public string EstadoCompra { get { return Compra == null ? null : Compra.EstadoCompraCodigo; } }
        public string EstadoPago { get { return UltimoPago == null ? null : UltimoPago.EstadoPagoCodigo; } }
        public decimal Total { get { return Lineas.Sum(l => l.Importe); } }
        public string AsientosTexto { get { return string.Join(", ", Lineas.Select(l => l.Asiento).Distinct()); } }

        // Cancelación (DP-08)
        public bool PuedeCancelar { get; set; }
        public string MotivoNoCancelar { get; set; }
        public bool RequiereMotivo { get; set; }
        public string CondicionesCancelacion { get; set; }

        [Display(Name = "Motivo de cancelación")]
        [StringLength(300, MinimumLength = 10, ErrorMessage = "El motivo debe tener entre {2} y {1} caracteres.")]
        public string Motivo { get; set; }

        public string Mensaje { get; set; }
        public string TipoMensaje { get; set; }
    }
}
