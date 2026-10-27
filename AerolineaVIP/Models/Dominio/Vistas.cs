// -----------------------------------------------------------------------------
// Vistas de SQL Server mapeadas como entidades de solo lectura (sección A6 del
// script). Nunca se insertan ni actualizan desde la aplicación.
// -----------------------------------------------------------------------------
using System;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace AerolineaVIP.Models.Dominio
{
    /// <summary>Detalle de cada vuelo: código, ruta, aeropuertos y horario completo.</summary>
    [Table("vw_VueloDetalle")]
    public partial class vw_VueloDetalle
    {
        [Key, DatabaseGenerated(DatabaseGeneratedOption.None)]
        public int VueloId { get; set; }
        public string CodigoVuelo { get; set; }
        [Column(TypeName = "date")]
        public DateTime FechaOperacion { get; set; }
        public byte DiaSemana { get; set; }
        public int FrecuenciaId { get; set; }
        public int RutaId { get; set; }
        public decimal TarifaReferencial { get; set; }
        public int AeropuertoOrigenId { get; set; }
        public int AeropuertoDestinoId { get; set; }
        public string Origen { get; set; }
        public string CiudadOrigen { get; set; }
        public string Destino { get; set; }
        public string CiudadDestino { get; set; }
        public TimeSpan HoraSalida { get; set; }
        public TimeSpan HoraLlegada { get; set; }
        [Column(TypeName = "datetime")]
        public DateTime SalidaProgramada { get; set; }
        [Column(TypeName = "datetime")]
        public DateTime LlegadaProgramada { get; set; }
        public int? AeronaveId { get; set; }
        public string EstadoVueloCodigo { get; set; }
    }

    /// <summary>Asientos disponibles por vuelo (derivados de las asignaciones vigentes).</summary>
    [Table("vw_DisponibilidadVuelo")]
    public partial class vw_DisponibilidadVuelo
    {
        [Key, DatabaseGenerated(DatabaseGeneratedOption.None)]
        public int VueloId { get; set; }
        public string CodigoVuelo { get; set; }
        [Column(TypeName = "date")]
        public DateTime FechaOperacion { get; set; }
        public string Origen { get; set; }
        public string Destino { get; set; }
        public TimeSpan HoraSalida { get; set; }
        public TimeSpan HoraLlegada { get; set; }
        public string EstadoVueloCodigo { get; set; }
        public string Matricula { get; set; }
        public int Capacidad { get; set; }
        public int Ocupados { get; set; }
        public int Disponibles { get; set; }
    }

    /// <summary>Los siete asientos de cada aeronave de la flota.</summary>
    [Table("vw_AsientoAeronave")]
    public partial class vw_AsientoAeronave
    {
        [Key, Column(Order = 0), DatabaseGenerated(DatabaseGeneratedOption.None)]
        public int AeronaveId { get; set; }
        public string Matricula { get; set; }
        [Key, Column(Order = 1), DatabaseGenerated(DatabaseGeneratedOption.None)]
        public byte NumeroAsiento { get; set; }
        public string CodigoAsiento { get; set; }
        public byte Fila { get; set; }
        public string Lado { get; set; }
        public string Ubicacion { get; set; }
    }

    /// <summary>
    /// Estado visual de cada asiento por vuelo: DISPONIBLE (verde), RESERVADO (morado)
    /// u OCUPADO (rojo). Solo incluye vuelos con aeronave asignada (RNP-01).
    /// </summary>
    [Table("vw_EstadoAsientoVuelo")]
    public partial class vw_EstadoAsientoVuelo
    {
        [Key, Column(Order = 0), DatabaseGenerated(DatabaseGeneratedOption.None)]
        public int VueloId { get; set; }
        public int AeronaveId { get; set; }
        [Key, Column(Order = 1), DatabaseGenerated(DatabaseGeneratedOption.None)]
        public byte NumeroAsiento { get; set; }
        public string CodigoAsiento { get; set; }
        public byte Fila { get; set; }
        public string Lado { get; set; }
        public string Ubicacion { get; set; }
        public string EstadoVisual { get; set; }
        public string ColorEstado { get; set; }
        public int? AsignacionId { get; set; }
        public int? ReservaId { get; set; }
        public int? PasajeroId { get; set; }
    }

    /// <summary>Encabezado del comprobante académico (emisor, reserva, compra y pago simulado).</summary>
    [Table("vw_ComprobanteDetalle")]
    public partial class vw_ComprobanteDetalle
    {
        [Key, DatabaseGenerated(DatabaseGeneratedOption.None)]
        public int ComprobanteId { get; set; }
        public string NumeroComprobante { get; set; }
        public string TipoComprobanteCodigo { get; set; }
        public string TipoComprobante { get; set; }
        [Column(TypeName = "datetime2")]
        public DateTime FechaEmision { get; set; }
        public string EstadoComprobanteCodigo { get; set; }
        public string EmisorNombre { get; set; }
        public string EmisorIdentificacion { get; set; }
        public string EmisorDireccion { get; set; }
        public string LeyendaComprobante { get; set; }
        public int ReservaId { get; set; }
        public string CodigoReserva { get; set; }
        public string UsuarioId { get; set; }
        public int CompraId { get; set; }
        [Column(TypeName = "datetime2")]
        public DateTime FechaCompra { get; set; }
        public decimal TotalOperacion { get; set; }
        public string MonedaCodigo { get; set; }
        public string EstadoCompraCodigo { get; set; }
        public int? PagoId { get; set; }
        public string CodigoOperacion { get; set; }
        [Column(TypeName = "datetime2")]
        public DateTime? FechaPago { get; set; }
        public string EstadoPagoCodigo { get; set; }
        public string MetodoPago { get; set; }
        public string MarcaTarjeta { get; set; }
        public string TarjetaEnmascarada { get; set; }
        public bool? EsSimulado { get; set; }
    }
}
