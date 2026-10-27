using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using AerolineaVIP.Models.Dominio;

namespace AerolineaVIP.Models.ViewModels
{
    /// <summary>
    /// Pantalla 11. Pago SIMULADO: el número completo, el vencimiento y el código de
    /// seguridad solo se validan en memoria; nunca se guardan.
    /// </summary>
    public class PagoVM
    {
        public int ReservaId { get; set; }

        /// <summary>Clave de idempotencia de la compra (RNP-10).</summary>
        public Guid Token { get; set; }

        [Display(Name = "Método de pago")]
        [Required(ErrorMessage = "Seleccione el método de pago.")]
        public string MetodoPagoCodigo { get; set; }

        [Display(Name = "Marca de la tarjeta")]
        [Required(ErrorMessage = "Seleccione la marca de la tarjeta.")]
        public string MarcaTarjetaCodigo { get; set; }

        [Display(Name = "Número de tarjeta")]
        [Required(ErrorMessage = "Ingrese el número de tarjeta.")]
        [RegularExpression(@"^[0-9 ]{14,23}$", ErrorMessage = "El número de tarjeta solo admite dígitos.")]
        public string NumeroTarjeta { get; set; }

        [Display(Name = "Nombre del titular")]
        [Required(ErrorMessage = "Ingrese el nombre del titular.")]
        [StringLength(100, MinimumLength = 2, ErrorMessage = "El nombre del titular debe tener entre {2} y {1} caracteres.")]
        [RegularExpression(@"^[A-Za-zÁÉÍÓÚÜÑáéíóúüñ' .-]+$", ErrorMessage = "Use solo letras y espacios.")]
        public string NombreTitular { get; set; }

        [Display(Name = "Vencimiento (MM/AA)")]
        [Required(ErrorMessage = "Ingrese el vencimiento.")]
        [RegularExpression(@"^(0[1-9]|1[0-2])\/[0-9]{2}$", ErrorMessage = "Use el formato MM/AA.")]
        public string Vencimiento { get; set; }

        [Display(Name = "Código de seguridad")]
        [Required(ErrorMessage = "Ingrese el código de seguridad.")]
        [RegularExpression(@"^[0-9]{3,4}$", ErrorMessage = "El código tiene 3 dígitos (4 en American Express).")]
        [DataType(DataType.Password)]
        public string CodigoSeguridad { get; set; }

        // Datos de presentación
        public DetalleReservaVM Detalle { get; set; }
        public List<MetodoPago> Metodos { get; set; }
        public List<MarcaTarjeta> Marcas { get; set; }
        public string Mensaje { get; set; }
        public string TipoMensaje { get; set; }
    }

    /// <summary>Resultado de procesar un pago simulado.</summary>
    public class ResultadoPago
    {
        public bool Aprobado { get; set; }
        public bool Duplicado { get; set; }
        public string Mensaje { get; set; }
        public string CodigoOperacion { get; set; }
    }

    // ===================== Check-in (15) =====================
    public class FilaCheckInVM
    {
        public int AsignacionId { get; set; }
        public string Pasajero { get; set; }
        public string Documento { get; set; }
        public string CodigoVuelo { get; set; }
        public string Ruta { get; set; }
        public DateTime Salida { get; set; }
        public string Asiento { get; set; }
        public bool Elegible { get; set; }
        public string Elegibilidad { get; set; }
        public string EstadoCheckIn { get; set; }
        public DateTime? FechaCheckIn { get; set; }
    }

    public class CheckInVM
    {
        [Display(Name = "Código de reserva")]
        [Required(ErrorMessage = "Ingrese el código de reserva.")]
        [RegularExpression(@"^[Rr][Vv][0-9]{8}$", ErrorMessage = "El código tiene el formato RV + 8 dígitos (ej.: RV00000013).")]
        public string CodigoReserva { get; set; }

        [Display(Name = "Número de documento del pasajero")]
        [Required(ErrorMessage = "Ingrese el número de documento.")]
        [StringLength(20, MinimumLength = 5, ErrorMessage = "El documento debe tener entre {2} y {1} caracteres.")]
        public string NumeroDocumento { get; set; }

        public List<FilaCheckInVM> Filas { get; set; }
        public List<int> Seleccionados { get; set; }
        public bool Buscado { get; set; }
        public string Mensaje { get; set; }
        public string TipoMensaje { get; set; }
        public int HorasApertura { get; set; }
        public int HorasCierre { get; set; }

        public CheckInVM()
        {
            Filas = new List<FilaCheckInVM>();
            Seleccionados = new List<int>();
        }
    }

    // ===================== Comprobantes (16) =====================
    public class LineaComprobanteVM
    {
        public string Pasajero { get; set; }
        public string Documento { get; set; }
        public string CodigoVuelo { get; set; }
        public string Ruta { get; set; }
        public DateTime Fecha { get; set; }
        public string Horario { get; set; }
        public string Asiento { get; set; }
        public decimal Importe { get; set; }
    }

    public class ComprobanteDocumentoVM
    {
        public vw_ComprobanteDetalle Encabezado { get; set; }
        public string Cliente { get; set; }
        public string ClienteCorreo { get; set; }
        public List<LineaComprobanteVM> Lineas { get; set; }
        public bool MostrarImportes { get { return Encabezado.TipoComprobanteCodigo != "CONFIRMACION"; } }
    }

    public class ComprobantesVM : IValidatableObject
    {
        [Display(Name = "Desde (opcional)")]
        [DataType(DataType.Date)]
        [DisplayFormat(DataFormatString = "{0:yyyy-MM-dd}", ApplyFormatInEditMode = true)]
        public DateTime? Desde { get; set; }

        [Display(Name = "Hasta (opcional)")]
        [DataType(DataType.Date)]
        [DisplayFormat(DataFormatString = "{0:yyyy-MM-dd}", ApplyFormatInEditMode = true)]
        public DateTime? Hasta { get; set; }

        [Display(Name = "Tipo (opcional)")]
        public string Tipo { get; set; }

        [Display(Name = "Código de reserva o número (opcional)")]
        [StringLength(30)]
        public string Buscar { get; set; }

        public int? Id { get; set; }
        public bool EsPersonal { get; set; }
        public List<vw_ComprobanteDetalle> Lista { get; set; }
        public ComprobanteDocumentoVM Seleccionado { get; set; }
        public IEnumerable<System.Web.Mvc.SelectListItem> Tipos { get; set; }

        public IEnumerable<ValidationResult> Validate(ValidationContext validationContext)
        {
            if (Desde.HasValue && Hasta.HasValue && Hasta.Value < Desde.Value)
                yield return new ValidationResult("«Hasta» no puede ser anterior a «Desde».", new[] { "Hasta" });
        }
    }
}
