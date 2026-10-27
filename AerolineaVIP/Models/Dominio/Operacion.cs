// -----------------------------------------------------------------------------
// Entidades de operación aérea: aeropuertos, rutas, frecuencias, flota, asientos,
// vuelos e itinerarios (tablas A3 y A4 del script).
// -----------------------------------------------------------------------------
using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace AerolineaVIP.Models.Dominio
{
    [Table("Aeropuerto")]
    public partial class Aeropuerto
    {
        public Aeropuerto()
        {
            RutasOrigen = new HashSet<Ruta>();
            RutasDestino = new HashSet<Ruta>();
        }

        [Key]
        public int AeropuertoId { get; set; }
        [Column(TypeName = "char")]
        public string CodigoIATA { get; set; }
        public string Nombre { get; set; }
        public string Localidad { get; set; }
        public bool Activo { get; set; }

        public virtual ICollection<Ruta> RutasOrigen { get; set; }
        public virtual ICollection<Ruta> RutasDestino { get; set; }
    }

    [Table("Ruta")]
    public partial class Ruta
    {
        public Ruta()
        {
            Frecuencias = new HashSet<Frecuencia>();
        }

        [Key]
        public int RutaId { get; set; }
        public int AeropuertoOrigenId { get; set; }
        public int AeropuertoDestinoId { get; set; }
        public decimal TarifaReferencial { get; set; }
        public bool Activa { get; set; }

        public virtual Aeropuerto AeropuertoOrigen { get; set; }
        public virtual Aeropuerto AeropuertoDestino { get; set; }
        public virtual ICollection<Frecuencia> Frecuencias { get; set; }
    }

    /// <summary>Día de la semana (1 = lunes ... 7 = domingo) y horario aprobados de una ruta.</summary>
    [Table("Frecuencia")]
    public partial class Frecuencia
    {
        public Frecuencia()
        {
            Vuelos = new HashSet<Vuelo>();
        }

        [Key]
        public int FrecuenciaId { get; set; }
        public int RutaId { get; set; }
        public byte DiaSemana { get; set; }
        public TimeSpan HoraSalida { get; set; }
        public TimeSpan HoraLlegada { get; set; }
        public bool Activa { get; set; }

        public virtual Ruta Ruta { get; set; }
        public virtual ICollection<Vuelo> Vuelos { get; set; }
    }

    [Table("ModeloAeronave")]
    public partial class ModeloAeronave
    {
        public ModeloAeronave()
        {
            Aeronaves = new HashSet<Aeronave>();
            Asientos = new HashSet<Asiento>();
        }

        [Key]
        public int ModeloAeronaveId { get; set; }
        public string Fabricante { get; set; }
        public string Nombre { get; set; }
        public byte CapacidadAsientos { get; set; }

        public virtual ICollection<Aeronave> Aeronaves { get; set; }
        public virtual ICollection<Asiento> Asientos { get; set; }
    }

    /// <summary>
    /// Distribución de cabina por modelo (DP-11). Clave compuesta
    /// (ModeloAeronaveId, NumeroAsiento). Código = fila + lado (D/I).
    /// </summary>
    [Table("Asiento")]
    public partial class Asiento
    {
        [Key, Column(Order = 0), DatabaseGenerated(DatabaseGeneratedOption.None)]
        public int ModeloAeronaveId { get; set; }
        [Key, Column(Order = 1), DatabaseGenerated(DatabaseGeneratedOption.None)]
        public byte NumeroAsiento { get; set; }
        [Column(TypeName = "char")]
        public string Codigo { get; set; }
        public byte Fila { get; set; }
        [Column(TypeName = "char")]
        public string Lado { get; set; }
        public string Ubicacion { get; set; }

        public virtual ModeloAeronave ModeloAeronave { get; set; }
    }

    [Table("Aeronave")]
    public partial class Aeronave
    {
        public Aeronave()
        {
            Vuelos = new HashSet<Vuelo>();
        }

        [Key]
        public int AeronaveId { get; set; }
        [Column(TypeName = "varchar")]
        public string Matricula { get; set; }
        public int ModeloAeronaveId { get; set; }
        [Column(TypeName = "varchar")]
        public string EstadoAeronaveCodigo { get; set; }

        public virtual ModeloAeronave ModeloAeronave { get; set; }
        public virtual EstadoAeronave EstadoAeronave { get; set; }
        public virtual ICollection<Vuelo> Vuelos { get; set; }
    }

    /// <summary>Una frecuencia en una fecha concreta. Ruta y horas se obtienen de la frecuencia.</summary>
    [Table("Vuelo")]
    public partial class Vuelo
    {
        public Vuelo()
        {
            Asignaciones = new HashSet<ReservaPasajeroVuelo>();
            Tramos = new HashSet<TramoItinerario>();
        }

        [Key]
        public int VueloId { get; set; }
        public int FrecuenciaId { get; set; }
        [Column(TypeName = "date")]
        public DateTime FechaOperacion { get; set; }
        public int? AeronaveId { get; set; }
        [Column(TypeName = "varchar")]
        public string EstadoVueloCodigo { get; set; }

        public virtual Frecuencia Frecuencia { get; set; }
        public virtual Aeronave Aeronave { get; set; }
        public virtual EstadoVuelo EstadoVuelo { get; set; }
        public virtual ICollection<ReservaPasajeroVuelo> Asignaciones { get; set; }
        public virtual ICollection<TramoItinerario> Tramos { get; set; }

        /// <summary>Código comercial VIP00000 (igual que vw_VueloDetalle.CodigoVuelo).</summary>
        [NotMapped]
        public string CodigoVuelo { get { return "VIP" + VueloId.ToString("00000"); } }
    }

    [Table("Itinerario")]
    public partial class Itinerario
    {
        public Itinerario()
        {
            Tramos = new HashSet<TramoItinerario>();
        }

        [Key]
        public int ItinerarioId { get; set; }
        [Column(TypeName = "varchar")]
        public string Referencia { get; set; }
        public string Nombre { get; set; }
        [Column(TypeName = "varchar")]
        public string EstadoItinerarioCodigo { get; set; }

        public virtual EstadoItinerario EstadoItinerario { get; set; }
        public virtual ICollection<TramoItinerario> Tramos { get; set; }
    }

    [Table("TramoItinerario")]
    public partial class TramoItinerario
    {
        [Key]
        public int TramoItinerarioId { get; set; }
        public int ItinerarioId { get; set; }
        public int VueloId { get; set; }
        public byte Orden { get; set; }

        public virtual Itinerario Itinerario { get; set; }
        public virtual Vuelo Vuelo { get; set; }
    }
}
