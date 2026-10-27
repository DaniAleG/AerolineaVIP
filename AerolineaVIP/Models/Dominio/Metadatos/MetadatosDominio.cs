// -----------------------------------------------------------------------------
// Validaciones centralizadas del modelo (RN-10 y apartado 11.1.4 del enunciado).
// Cada entidad se completa con una clase parcial asociada a una clase de
// metadatos ([MetadataType]). Las vistas Razor generan con estas reglas los
// atributos data-val-* que usa jQuery Validation (cliente) y el servidor las
// vuelve a comprobar con ModelState.IsValid. Las reglas que dependen de varias
// entidades o de la disponibilidad se validan en los servicios y en SQL Server.
// -----------------------------------------------------------------------------
using System;
using System.ComponentModel.DataAnnotations;

namespace AerolineaVIP.Models.Dominio
{
    internal static class Patrones
    {
        public const string SoloLetras = @"^[A-Za-zÁÉÍÓÚÜÑáéíóúüñ' .-]+$";
        public const string Iata = @"^[A-Z]{3}$";
        public const string Telefono = @"^[0-9+ ]{7,20}$";
        public const string Codigo = @"^[A-Z0-9_]+$";
        public const string MsgRequerido = "El campo {0} es obligatorio.";
    }

    // ===================== Catálogos =====================
    [MetadataType(typeof(CatalogoMetadata))] public partial class TipoDocumentoIdentidad { }
    [MetadataType(typeof(CatalogoMetadata))] public partial class EstadoAeronave { }
    [MetadataType(typeof(CatalogoMetadata))] public partial class EstadoVuelo { }
    [MetadataType(typeof(CatalogoMetadata))] public partial class EstadoItinerario { }
    [MetadataType(typeof(CatalogoMetadata))] public partial class EstadoReserva { }
    [MetadataType(typeof(CatalogoMetadata))] public partial class EstadoCompra { }
    [MetadataType(typeof(CatalogoMetadata))] public partial class EstadoPago { }
    [MetadataType(typeof(CatalogoMetadata))] public partial class EstadoCheckIn { }
    [MetadataType(typeof(CatalogoMetadata))] public partial class TipoComprobante { }
    [MetadataType(typeof(CatalogoMetadata))] public partial class EstadoComprobante { }

    public class CatalogoMetadata
    {
        [Display(Name = "Código")]
        [Required(ErrorMessage = Patrones.MsgRequerido)]
        [StringLength(20, ErrorMessage = "El código admite como máximo {1} caracteres.")]
        [RegularExpression(Patrones.Codigo, ErrorMessage = "Use solo mayúsculas, números y guion bajo.")]
        public string Codigo { get; set; }

        [Display(Name = "Descripción")]
        [Required(ErrorMessage = Patrones.MsgRequerido)]
        [StringLength(100, ErrorMessage = "La descripción admite como máximo {1} caracteres.")]
        public string Descripcion { get; set; }
    }

    [MetadataType(typeof(MetodoPagoMetadata))]
    public partial class MetodoPago { }
    public class MetodoPagoMetadata
    {
        [Display(Name = "Código")]
        [Required(ErrorMessage = Patrones.MsgRequerido)]
        [StringLength(20)]
        [RegularExpression(Patrones.Codigo, ErrorMessage = "Use solo mayúsculas, números y guion bajo.")]
        public string Codigo { get; set; }

        [Display(Name = "Nombre")]
        [Required(ErrorMessage = Patrones.MsgRequerido)]
        [StringLength(100)]
        public string Nombre { get; set; }

        [Display(Name = "Activo")]
        public bool Activo { get; set; }
    }

    [MetadataType(typeof(MarcaTarjetaMetadata))]
    public partial class MarcaTarjeta { }
    public class MarcaTarjetaMetadata
    {
        [Display(Name = "Código")]
        [Required(ErrorMessage = Patrones.MsgRequerido)]
        [StringLength(20)]
        [RegularExpression(Patrones.Codigo, ErrorMessage = "Use solo mayúsculas, números y guion bajo.")]
        public string Codigo { get; set; }

        [Display(Name = "Nombre")]
        [Required(ErrorMessage = Patrones.MsgRequerido)]
        [StringLength(60)]
        public string Nombre { get; set; }

        [Display(Name = "Longitud del número")]
        [Range(12, 19, ErrorMessage = "La longitud del número debe estar entre {1} y {2}.")]
        public byte LongitudNumero { get; set; }

        [Display(Name = "Longitud del código de seguridad")]
        [Range(3, 4, ErrorMessage = "El código de seguridad tiene {1} o {2} dígitos.")]
        public byte LongitudCodigoSeguridad { get; set; }

        [Display(Name = "Prefijos de referencia")]
        [Required(ErrorMessage = Patrones.MsgRequerido)]
        [StringLength(40)]
        [RegularExpression(@"^[0-9 ,-]+$", ErrorMessage = "Use números, comas y rangos con guion (ej.: 51-55, 2221-2720).")]
        public string PrefijosReferencia { get; set; }

        [Display(Name = "Ruta del logotipo")]
        [Required(ErrorMessage = Patrones.MsgRequerido)]
        [StringLength(150)]
        public string RutaLogo { get; set; }

        [Display(Name = "Activa")]
        public bool Activa { get; set; }
    }

    [MetadataType(typeof(MonedaMetadata))]
    public partial class Moneda { }
    public class MonedaMetadata
    {
        [Display(Name = "Código ISO 4217")]
        [Required(ErrorMessage = Patrones.MsgRequerido)]
        [RegularExpression(@"^[A-Z]{3}$", ErrorMessage = "El código debe tener 3 letras mayúsculas.")]
        public string Codigo { get; set; }

        [Display(Name = "Descripción")]
        [Required(ErrorMessage = Patrones.MsgRequerido)]
        [StringLength(100)]
        public string Descripcion { get; set; }
    }

    [MetadataType(typeof(DatosEmisorMetadata))]
    public partial class DatosEmisor { }
    public class DatosEmisorMetadata
    {
        [Display(Name = "Id")]
        [Range(1, 1, ErrorMessage = "Solo existe un registro de emisor (Id = 1).")]
        public byte DatosEmisorId { get; set; }

        [Display(Name = "Nombre comercial")]
        [Required(ErrorMessage = Patrones.MsgRequerido)]
        [StringLength(150)]
        public string NombreComercial { get; set; }

        [Display(Name = "Identificación")]
        [Required(ErrorMessage = Patrones.MsgRequerido)]
        [StringLength(60)]
        public string Identificacion { get; set; }

        [Display(Name = "Dirección")]
        [Required(ErrorMessage = Patrones.MsgRequerido)]
        [StringLength(200)]
        public string Direccion { get; set; }

        [Display(Name = "Teléfono (opcional)")]
        [RegularExpression(Patrones.Telefono, ErrorMessage = "Use solo dígitos, espacios y «+» (7 a 20).")]
        public string Telefono { get; set; }

        [Display(Name = "Correo (opcional)")]
        [EmailAddress(ErrorMessage = "El correo no tiene un formato válido.")]
        [StringLength(256)]
        public string Correo { get; set; }

        [Display(Name = "Leyenda del comprobante")]
        [Required(ErrorMessage = Patrones.MsgRequerido)]
        [StringLength(300)]
        [DataType(DataType.MultilineText)]
        public string LeyendaComprobante { get; set; }
    }

    // ===================== Operación aérea =====================
    [MetadataType(typeof(AeropuertoMetadata))]
    public partial class Aeropuerto { }
    public class AeropuertoMetadata
    {
        [Display(Name = "Código IATA")]
        [Required(ErrorMessage = Patrones.MsgRequerido)]
        [RegularExpression(Patrones.Iata, ErrorMessage = "El código IATA tiene 3 letras mayúsculas.")]
        public string CodigoIATA { get; set; }

        [Display(Name = "Nombre")]
        [Required(ErrorMessage = Patrones.MsgRequerido)]
        [StringLength(150, MinimumLength = 3, ErrorMessage = "El nombre debe tener entre {2} y {1} caracteres.")]
        public string Nombre { get; set; }

        [Display(Name = "Localidad")]
        [Required(ErrorMessage = Patrones.MsgRequerido)]
        [StringLength(100, MinimumLength = 2, ErrorMessage = "La localidad debe tener entre {2} y {1} caracteres.")]
        public string Localidad { get; set; }

        [Display(Name = "Activo")]
        public bool Activo { get; set; }
    }

    [MetadataType(typeof(RutaMetadata))]
    public partial class Ruta : IValidatableObject
    {
        public System.Collections.Generic.IEnumerable<ValidationResult> Validate(ValidationContext validationContext)
        {
            if (AeropuertoOrigenId != 0 && AeropuertoOrigenId == AeropuertoDestinoId)
                yield return new ValidationResult("El origen y el destino deben ser distintos.", new[] { "AeropuertoDestinoId" });
        }
    }
    public class RutaMetadata
    {
        [Display(Name = "Origen")]
        [Required(ErrorMessage = Patrones.MsgRequerido)]
        public int AeropuertoOrigenId { get; set; }

        [Display(Name = "Destino")]
        [Required(ErrorMessage = Patrones.MsgRequerido)]
        public int AeropuertoDestinoId { get; set; }

        [Display(Name = "Tarifa referencial (USD)")]
        [Required(ErrorMessage = Patrones.MsgRequerido)]
        [Range(typeof(decimal), "0", "99999", ErrorMessage = "La tarifa debe estar entre {1} y {2}.")]
        [DisplayFormat(DataFormatString = "{0:N2}", ApplyFormatInEditMode = true)]
        public decimal TarifaReferencial { get; set; }

        [Display(Name = "Activa")]
        public bool Activa { get; set; }
    }

    [MetadataType(typeof(FrecuenciaMetadata))]
    public partial class Frecuencia : IValidatableObject
    {
        public System.Collections.Generic.IEnumerable<ValidationResult> Validate(ValidationContext validationContext)
        {
            if (HoraLlegada <= HoraSalida)
                yield return new ValidationResult("La hora de llegada debe ser posterior a la de salida.", new[] { "HoraLlegada" });
        }
    }
    public class FrecuenciaMetadata
    {
        [Display(Name = "Ruta")]
        [Required(ErrorMessage = Patrones.MsgRequerido)]
        public int RutaId { get; set; }

        [Display(Name = "Día de la semana")]
        [Range(1, 7, ErrorMessage = "El día debe estar entre 1 (lunes) y 7 (domingo).")]
        public byte DiaSemana { get; set; }

        [Display(Name = "Hora de salida")]
        [Required(ErrorMessage = Patrones.MsgRequerido)]
        [DataType(DataType.Time)]
        [DisplayFormat(DataFormatString = @"{0:hh\:mm}", ApplyFormatInEditMode = true)]
        public TimeSpan HoraSalida { get; set; }

        [Display(Name = "Hora de llegada")]
        [Required(ErrorMessage = Patrones.MsgRequerido)]
        [DataType(DataType.Time)]
        [DisplayFormat(DataFormatString = @"{0:hh\:mm}", ApplyFormatInEditMode = true)]
        public TimeSpan HoraLlegada { get; set; }

        [Display(Name = "Activa")]
        public bool Activa { get; set; }
    }

    [MetadataType(typeof(ModeloAeronaveMetadata))]
    public partial class ModeloAeronave { }
    public class ModeloAeronaveMetadata
    {
        [Display(Name = "Fabricante")]
        [Required(ErrorMessage = Patrones.MsgRequerido)]
        [StringLength(60)]
        public string Fabricante { get; set; }

        [Display(Name = "Modelo")]
        [Required(ErrorMessage = Patrones.MsgRequerido)]
        [StringLength(60)]
        public string Nombre { get; set; }

        [Display(Name = "Capacidad de asientos")]
        [Range(1, 7, ErrorMessage = "La capacidad máxima es de 7 asientos por vuelo (RN-03).")]
        public byte CapacidadAsientos { get; set; }
    }

    [MetadataType(typeof(AsientoMetadata))]
    public partial class Asiento { }
    public class AsientoMetadata
    {
        [Display(Name = "Modelo de aeronave")]
        [Required(ErrorMessage = Patrones.MsgRequerido)]
        public int ModeloAeronaveId { get; set; }

        [Display(Name = "N.º de asiento")]
        [Range(1, 7, ErrorMessage = "El número de asiento va de 1 a 7 (RN-03).")]
        public byte NumeroAsiento { get; set; }

        [Display(Name = "Código")]
        [Required(ErrorMessage = Patrones.MsgRequerido)]
        [RegularExpression(@"^[1-4][DI]$", ErrorMessage = "El código es fila (1-4) + lado (D o I), por ejemplo 3D.")]
        public string Codigo { get; set; }

        [Display(Name = "Fila")]
        [Range(1, 4, ErrorMessage = "La fila va de 1 a 4.")]
        public byte Fila { get; set; }

        [Display(Name = "Lado")]
        [Required(ErrorMessage = Patrones.MsgRequerido)]
        [RegularExpression(@"^[DI]$", ErrorMessage = "El lado es D (derecho) o I (izquierdo).")]
        public string Lado { get; set; }

        [Display(Name = "Ubicación")]
        [Required(ErrorMessage = Patrones.MsgRequerido)]
        [StringLength(100)]
        public string Ubicacion { get; set; }
    }

    [MetadataType(typeof(AeronaveMetadata))]
    public partial class Aeronave { }
    public class AeronaveMetadata
    {
        [Display(Name = "Matrícula / identificador")]
        [Required(ErrorMessage = Patrones.MsgRequerido)]
        [StringLength(15, MinimumLength = 1, ErrorMessage = "La matrícula admite como máximo {1} caracteres.")]
        [RegularExpression(@"^[A-Z0-9-]+$", ErrorMessage = "Use mayúsculas, números y guion.")]
        public string Matricula { get; set; }

        [Display(Name = "Modelo")]
        [Required(ErrorMessage = Patrones.MsgRequerido)]
        public int ModeloAeronaveId { get; set; }

        [Display(Name = "Estado operativo")]
        [Required(ErrorMessage = Patrones.MsgRequerido)]
        public string EstadoAeronaveCodigo { get; set; }
    }

    [MetadataType(typeof(VueloMetadata))]
    public partial class Vuelo { }
    public class VueloMetadata
    {
        [Display(Name = "Frecuencia")]
        [Required(ErrorMessage = Patrones.MsgRequerido)]
        public int FrecuenciaId { get; set; }

        [Display(Name = "Fecha de operación")]
        [Required(ErrorMessage = Patrones.MsgRequerido)]
        [DataType(DataType.Date)]
        [DisplayFormat(DataFormatString = "{0:yyyy-MM-dd}", ApplyFormatInEditMode = true)]
        public DateTime FechaOperacion { get; set; }

        [Display(Name = "Aeronave asignada")]
        public int? AeronaveId { get; set; }

        [Display(Name = "Estado")]
        [Required(ErrorMessage = Patrones.MsgRequerido)]
        public string EstadoVueloCodigo { get; set; }
    }

    [MetadataType(typeof(ItinerarioMetadata))]
    public partial class Itinerario { }
    public class ItinerarioMetadata
    {
        [Display(Name = "Referencia")]
        [Required(ErrorMessage = Patrones.MsgRequerido)]
        [StringLength(30)]
        [RegularExpression(@"^[A-Z0-9-]+$", ErrorMessage = "Use mayúsculas, números y guion (ej.: IT-20261108).")]
        public string Referencia { get; set; }

        [Display(Name = "Nombre")]
        [Required(ErrorMessage = Patrones.MsgRequerido)]
        [StringLength(150)]
        public string Nombre { get; set; }

        [Display(Name = "Estado")]
        [Required(ErrorMessage = Patrones.MsgRequerido)]
        public string EstadoItinerarioCodigo { get; set; }
    }

    [MetadataType(typeof(TramoItinerarioMetadata))]
    public partial class TramoItinerario { }
    public class TramoItinerarioMetadata
    {
        [Display(Name = "Itinerario")]
        [Required(ErrorMessage = Patrones.MsgRequerido)]
        public int ItinerarioId { get; set; }

        [Display(Name = "Vuelo")]
        [Required(ErrorMessage = Patrones.MsgRequerido)]
        public int VueloId { get; set; }

        [Display(Name = "Orden")]
        [Range(1, 10, ErrorMessage = "El orden va de {1} a {2}.")]
        public byte Orden { get; set; }
    }

    // ===================== Transacciones =====================
    [MetadataType(typeof(PasajeroMetadata))]
    public partial class Pasajero : IValidatableObject
    {
        public System.Collections.Generic.IEnumerable<ValidationResult> Validate(ValidationContext validationContext)
        {
            if (TipoDocumentoCodigo == "CEDULA" && !string.IsNullOrEmpty(NumeroDocumento)
                && !System.Text.RegularExpressions.Regex.IsMatch(NumeroDocumento, @"^\d{10}$"))
                yield return new ValidationResult("La cédula debe tener 10 dígitos.", new[] { "NumeroDocumento" });
            if (FechaNacimiento.HasValue && (FechaNacimiento.Value <= new DateTime(1900, 1, 1) || FechaNacimiento.Value > DateTime.Today))
                yield return new ValidationResult("La fecha de nacimiento debe ser posterior a 01/01/1900 y no futura.", new[] { "FechaNacimiento" });
        }
    }
    public class PasajeroMetadata
    {
        [Display(Name = "Tipo de documento")]
        [Required(ErrorMessage = Patrones.MsgRequerido)]
        public string TipoDocumentoCodigo { get; set; }

        [Display(Name = "Número de documento")]
        [Required(ErrorMessage = Patrones.MsgRequerido)]
        [StringLength(20, MinimumLength = 5, ErrorMessage = "El documento debe tener entre {2} y {1} caracteres.")]
        [RegularExpression(@"^[A-Za-z0-9]+$", ErrorMessage = "Use solo letras y números.")]
        public string NumeroDocumento { get; set; }

        [Display(Name = "Nombres")]
        [Required(ErrorMessage = Patrones.MsgRequerido)]
        [StringLength(100)]
        [RegularExpression(Patrones.SoloLetras, ErrorMessage = "Use solo letras y espacios.")]
        public string Nombres { get; set; }

        [Display(Name = "Apellidos")]
        [Required(ErrorMessage = Patrones.MsgRequerido)]
        [StringLength(100)]
        [RegularExpression(Patrones.SoloLetras, ErrorMessage = "Use solo letras y espacios.")]
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
        [RegularExpression(Patrones.Telefono, ErrorMessage = "Use solo dígitos, espacios y «+» (7 a 20).")]
        public string Telefono { get; set; }

        [Display(Name = "Fecha de registro")]
        public DateTime FechaRegistro { get; set; }
    }
}
