// Tablas de catálogo (estados, monedas, métodos de pago, etc.).
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace AerolineaVIP.Models.Dominio
{
    [Table("EstadoAeronave")]
    public partial class EstadoAeronave
    {
        [Key, Column(TypeName = "varchar"), StringLength(20)]
        public string Codigo { get; set; }
        public string Descripcion { get; set; }
    }

    [Table("EstadoVuelo")]
    public partial class EstadoVuelo
    {
        [Key, Column(TypeName = "varchar"), StringLength(20)]
        public string Codigo { get; set; }
        public string Descripcion { get; set; }
    }

    [Table("EstadoItinerario")]
    public partial class EstadoItinerario
    {
        [Key, Column(TypeName = "varchar"), StringLength(20)]
        public string Codigo { get; set; }
        public string Descripcion { get; set; }
    }

    [Table("TipoDocumentoIdentidad")]
    public partial class TipoDocumentoIdentidad
    {
        [Key, Column(TypeName = "varchar"), StringLength(20)]
        public string Codigo { get; set; }
        public string Descripcion { get; set; }
    }

    [Table("EstadoReserva")]
    public partial class EstadoReserva
    {
        [Key, Column(TypeName = "varchar"), StringLength(20)]
        public string Codigo { get; set; }
        public string Descripcion { get; set; }
    }

    /// <summary>Estado del asiento en la reserva: PENDIENTE y CONFIRMADA lo ocupan.</summary>
    [Table("EstadoAsignacion")]
    public partial class EstadoAsignacion
    {
        [Key, Column(TypeName = "varchar"), StringLength(20)]
        public string Codigo { get; set; }
        public string Descripcion { get; set; }
        public bool OcupaInventario { get; set; }
        [Column(TypeName = "varchar")]
        public string EstadoVisual { get; set; }
    }

    [Table("Moneda")]
    public partial class Moneda
    {
        [Key, Column(TypeName = "char"), StringLength(3)]
        public string Codigo { get; set; }
        public string Descripcion { get; set; }
    }

    [Table("EstadoCompra")]
    public partial class EstadoCompra
    {
        [Key, Column(TypeName = "varchar"), StringLength(20)]
        public string Codigo { get; set; }
        public string Descripcion { get; set; }
    }

    [Table("EstadoPago")]
    public partial class EstadoPago
    {
        [Key, Column(TypeName = "varchar"), StringLength(20)]
        public string Codigo { get; set; }
        public string Descripcion { get; set; }
    }

    [Table("EstadoCheckIn")]
    public partial class EstadoCheckIn
    {
        [Key, Column(TypeName = "varchar"), StringLength(20)]
        public string Codigo { get; set; }
        public string Descripcion { get; set; }
    }

    [Table("TipoComprobante")]
    public partial class TipoComprobante
    {
        [Key, Column(TypeName = "varchar"), StringLength(20)]
        public string Codigo { get; set; }
        public string Descripcion { get; set; }
    }

    [Table("EstadoComprobante")]
    public partial class EstadoComprobante
    {
        [Key, Column(TypeName = "varchar"), StringLength(20)]
        public string Codigo { get; set; }
        public string Descripcion { get; set; }
    }

    /// <summary>Métodos de pago simulados (tarjeta de crédito / débito).</summary>
    [Table("MetodoPago")]
    public partial class MetodoPago
    {
        [Key, Column(TypeName = "varchar"), StringLength(20)]
        public string Codigo { get; set; }
        public string Nombre { get; set; }
        public bool Activo { get; set; }
    }

    /// <summary>Marcas de tarjeta de la simulación académica.</summary>
    [Table("MarcaTarjeta")]
    public partial class MarcaTarjeta
    {
        [Key, Column(TypeName = "varchar"), StringLength(20)]
        public string Codigo { get; set; }
        public string Nombre { get; set; }
        public byte LongitudNumero { get; set; }
        public byte LongitudCodigoSeguridad { get; set; }
        [Column(TypeName = "varchar")]
        public string PrefijosReferencia { get; set; }
        [Column(TypeName = "varchar")]
        public string RutaLogo { get; set; }
        public bool Activa { get; set; }
    }

    /// <summary>Datos del emisor del comprobante académico (una sola fila, Id = 1).</summary>
    [Table("DatosEmisor")]
    public partial class DatosEmisor
    {
        [Key, DatabaseGenerated(DatabaseGeneratedOption.None)]
        public byte DatosEmisorId { get; set; }
        public string NombreComercial { get; set; }
        public string Identificacion { get; set; }
        public string Direccion { get; set; }
        [Column(TypeName = "varchar")]
        public string Telefono { get; set; }
        public string Correo { get; set; }
        public string LeyendaComprobante { get; set; }
    }
}
