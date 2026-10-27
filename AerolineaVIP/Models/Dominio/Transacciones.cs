// -----------------------------------------------------------------------------
// Entidades transaccionales: pasajeros, reservas, asignaciones de asiento,
// compras, pagos simulados, check-in y comprobantes académicos (A4 y A5).
// -----------------------------------------------------------------------------
using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace AerolineaVIP.Models.Dominio
{
    /// <summary>Persona que viaja (distinta del usuario de Identity que gestiona la reserva).</summary>
    [Table("Pasajero")]
    public partial class Pasajero
    {
        public Pasajero()
        {
            Asignaciones = new HashSet<ReservaPasajeroVuelo>();
        }

        [Key]
        public int PasajeroId { get; set; }
        [Column(TypeName = "varchar")]
        public string TipoDocumentoCodigo { get; set; }
        [Column(TypeName = "varchar")]
        public string NumeroDocumento { get; set; }
        public string Nombres { get; set; }
        public string Apellidos { get; set; }
        [Column(TypeName = "date")]
        public DateTime? FechaNacimiento { get; set; }
        public string Correo { get; set; }
        [Column(TypeName = "varchar")]
        public string Telefono { get; set; }
        [Column(TypeName = "datetime2")]
        public DateTime FechaRegistro { get; set; }

        public virtual TipoDocumentoIdentidad TipoDocumentoIdentidad { get; set; }
        public virtual ICollection<ReservaPasajeroVuelo> Asignaciones { get; set; }

        [NotMapped]
        public string NombreCompleto { get { return (Nombres + " " + Apellidos).Trim(); } }
    }

    /// <summary>
    /// Reserva. UsuarioId = Id de AspNetUsers en AerolineaVIP_Identity (sin FK física
    /// porque está en otra base; la aplicación valida que el usuario exista).
    /// </summary>
    [Table("Reserva")]
    public partial class Reserva
    {
        public Reserva()
        {
            Asignaciones = new HashSet<ReservaPasajeroVuelo>();
            Compras = new HashSet<Compra>();
        }

        [Key]
        public int ReservaId { get; set; }

        /// <summary>Columna calculada persistida: 'RV' + 8 dígitos.</summary>
        [DatabaseGenerated(DatabaseGeneratedOption.Computed), Column(TypeName = "varchar")]
        public string CodigoReserva { get; private set; }

        public string UsuarioId { get; set; }
        [Column(TypeName = "datetime2")]
        public DateTime FechaCreacion { get; set; }
        [Column(TypeName = "datetime2")]
        public DateTime? FechaExpiracion { get; set; }
        [Column(TypeName = "varchar")]
        public string EstadoReservaCodigo { get; set; }

        public virtual EstadoReserva EstadoReserva { get; set; }
        public virtual ICollection<ReservaPasajeroVuelo> Asignaciones { get; set; }
        public virtual ICollection<Compra> Compras { get; set; }
    }

    /// <summary>Asiento de un pasajero en un vuelo de una reserva.</summary>
    [Table("ReservaPasajeroVuelo")]
    public partial class ReservaPasajeroVuelo
    {
        public ReservaPasajeroVuelo()
        {
            CheckIns = new HashSet<CheckIn>();
        }

        [Key]
        public int AsignacionId { get; set; }
        public int ReservaId { get; set; }
        public int PasajeroId { get; set; }
        public int VueloId { get; set; }
        public byte NumeroAsiento { get; set; }
        public decimal ImporteTramo { get; set; }
        [Column(TypeName = "varchar")]
        public string EstadoAsignacionCodigo { get; set; }
        [Column(TypeName = "datetime2")]
        public DateTime FechaAsignacion { get; set; }

        public virtual Reserva Reserva { get; set; }
        public virtual Pasajero Pasajero { get; set; }
        public virtual Vuelo Vuelo { get; set; }
        public virtual EstadoAsignacion EstadoAsignacion { get; set; }
        public virtual ICollection<CheckIn> CheckIns { get; set; }
    }

    [Table("Compra")]
    public partial class Compra
    {
        public Compra()
        {
            Pagos = new HashSet<Pago>();
            Comprobantes = new HashSet<Comprobante>();
        }

        [Key]
        public int CompraId { get; set; }
        public int ReservaId { get; set; }
        [Column(TypeName = "datetime2")]
        public DateTime FechaCompra { get; set; }
        public decimal Importe { get; set; }
        [Column(TypeName = "char")]
        public string MonedaCodigo { get; set; }
        [Column(TypeName = "varchar")]
        public string EstadoCompraCodigo { get; set; }
        /// <summary>Evita compras duplicadas por solicitudes repetidas (RNP-10).</summary>
        public Guid ClaveIdempotencia { get; set; }

        public virtual Reserva Reserva { get; set; }
        public virtual Moneda Moneda { get; set; }
        public virtual EstadoCompra EstadoCompra { get; set; }
        public virtual ICollection<Pago> Pagos { get; set; }
        public virtual ICollection<Comprobante> Comprobantes { get; set; }
    }

    /// <summary>
    /// Pago SIMULADO. Nunca se guarda el número completo, el código de seguridad ni la
    /// fecha de vencimiento de la tarjeta.
    /// </summary>
    [Table("Pago")]
    public partial class Pago
    {
        [Key]
        public int PagoId { get; set; }
        public int CompraId { get; set; }
        [Column(TypeName = "datetime2")]
        public DateTime FechaPago { get; set; }
        public decimal Importe { get; set; }
        [Column(TypeName = "varchar")]
        public string EstadoPagoCodigo { get; set; }
        [Column(TypeName = "varchar")]
        public string MetodoPagoCodigo { get; set; }
        [Column(TypeName = "varchar")]
        public string MarcaTarjetaCodigo { get; set; }
        [Column(TypeName = "char")]
        public string UltimosCuatroDigitos { get; set; }
        public string NombreTitular { get; set; }
        [Column(TypeName = "varchar")]
        public string CodigoOperacion { get; set; }
        public bool EsSimulado { get; set; }
        public string MensajeResultado { get; set; }

        public virtual Compra Compra { get; set; }
        public virtual EstadoPago EstadoPago { get; set; }
        public virtual MetodoPago MetodoPago { get; set; }
        public virtual MarcaTarjeta MarcaTarjeta { get; set; }
    }

    [Table("CheckIn")]
    public partial class CheckIn
    {
        [Key]
        public int CheckInId { get; set; }
        public int AsignacionId { get; set; }
        [Column(TypeName = "datetime2")]
        public DateTime FechaRegistro { get; set; }
        [Column(TypeName = "varchar")]
        public string EstadoCheckInCodigo { get; set; }

        public virtual ReservaPasajeroVuelo Asignacion { get; set; }
        public virtual EstadoCheckIn EstadoCheckIn { get; set; }
    }

    /// <summary>Comprobante ACADÉMICO (no tributario).</summary>
    [Table("Comprobante")]
    public partial class Comprobante
    {
        [Key]
        public int ComprobanteId { get; set; }
        public int CompraId { get; set; }
        [Column(TypeName = "varchar")]
        public string TipoComprobanteCodigo { get; set; }
        [Column(TypeName = "varchar")]
        public string NumeroComprobante { get; set; }
        [Column(TypeName = "datetime2")]
        public DateTime FechaEmision { get; set; }
        [Column(TypeName = "varchar")]
        public string EstadoComprobanteCodigo { get; set; }

        public virtual Compra Compra { get; set; }
        public virtual TipoComprobante TipoComprobante { get; set; }
        public virtual EstadoComprobante EstadoComprobante { get; set; }
    }
}
