// Contexto de EF para la base AerolineaVIP. La base la crea el script SQL,
// por eso no se usa inicializador.
using System.Data.Entity;
using System.Data.Entity.ModelConfiguration.Conventions;

namespace AerolineaVIP.Models.Dominio
{
    public partial class AerolineaVIPEntities : DbContext
    {
        static AerolineaVIPEntities()
        {
            Database.SetInitializer<AerolineaVIPEntities>(null);
        }

        public AerolineaVIPEntities() : base("name=AerolineaVIPEntities")
        {
        }

        // Catálogos
        public virtual DbSet<EstadoAeronave> EstadoAeronave { get; set; }
        public virtual DbSet<EstadoVuelo> EstadoVuelo { get; set; }
        public virtual DbSet<EstadoItinerario> EstadoItinerario { get; set; }
        public virtual DbSet<TipoDocumentoIdentidad> TipoDocumentoIdentidad { get; set; }
        public virtual DbSet<EstadoReserva> EstadoReserva { get; set; }
        public virtual DbSet<EstadoAsignacion> EstadoAsignacion { get; set; }
        public virtual DbSet<Moneda> Moneda { get; set; }
        public virtual DbSet<EstadoCompra> EstadoCompra { get; set; }
        public virtual DbSet<EstadoPago> EstadoPago { get; set; }
        public virtual DbSet<EstadoCheckIn> EstadoCheckIn { get; set; }
        public virtual DbSet<TipoComprobante> TipoComprobante { get; set; }
        public virtual DbSet<EstadoComprobante> EstadoComprobante { get; set; }
        public virtual DbSet<MetodoPago> MetodoPago { get; set; }
        public virtual DbSet<MarcaTarjeta> MarcaTarjeta { get; set; }
        public virtual DbSet<DatosEmisor> DatosEmisor { get; set; }

        // Operación aérea
        public virtual DbSet<Aeropuerto> Aeropuerto { get; set; }
        public virtual DbSet<Ruta> Ruta { get; set; }
        public virtual DbSet<Frecuencia> Frecuencia { get; set; }
        public virtual DbSet<ModeloAeronave> ModeloAeronave { get; set; }
        public virtual DbSet<Asiento> Asiento { get; set; }
        public virtual DbSet<Aeronave> Aeronave { get; set; }
        public virtual DbSet<Vuelo> Vuelo { get; set; }
        public virtual DbSet<Itinerario> Itinerario { get; set; }
        public virtual DbSet<TramoItinerario> TramoItinerario { get; set; }

        // Transacciones
        public virtual DbSet<Pasajero> Pasajero { get; set; }
        public virtual DbSet<Reserva> Reserva { get; set; }
        public virtual DbSet<ReservaPasajeroVuelo> ReservaPasajeroVuelo { get; set; }
        public virtual DbSet<Compra> Compra { get; set; }
        public virtual DbSet<Pago> Pago { get; set; }
        public virtual DbSet<CheckIn> CheckIn { get; set; }
        public virtual DbSet<Comprobante> Comprobante { get; set; }

        // Vistas (solo lectura)
        public virtual DbSet<vw_VueloDetalle> vw_VueloDetalle { get; set; }
        public virtual DbSet<vw_DisponibilidadVuelo> vw_DisponibilidadVuelo { get; set; }
        public virtual DbSet<vw_AsientoAeronave> vw_AsientoAeronave { get; set; }
        public virtual DbSet<vw_EstadoAsientoVuelo> vw_EstadoAsientoVuelo { get; set; }
        public virtual DbSet<vw_ComprobanteDetalle> vw_ComprobanteDetalle { get; set; }

        protected override void OnModelCreating(DbModelBuilder mb)
        {
            // Los nombres de tabla del script están en singular.
            mb.Conventions.Remove<PluralizingTableNameConvention>();
            // Ninguna FK del script tiene ON DELETE CASCADE.
            mb.Conventions.Remove<OneToManyCascadeDeleteConvention>();
            mb.Conventions.Remove<ManyToManyCascadeDeleteConvention>();

            // ---------- Operación aérea ----------
            mb.Entity<Ruta>().HasRequired(r => r.AeropuertoOrigen).WithMany(a => a.RutasOrigen).HasForeignKey(r => r.AeropuertoOrigenId);
            mb.Entity<Ruta>().HasRequired(r => r.AeropuertoDestino).WithMany(a => a.RutasDestino).HasForeignKey(r => r.AeropuertoDestinoId);
            mb.Entity<Ruta>().Property(r => r.TarifaReferencial).HasPrecision(12, 2);

            mb.Entity<Frecuencia>().HasRequired(f => f.Ruta).WithMany(r => r.Frecuencias).HasForeignKey(f => f.RutaId);

            mb.Entity<Asiento>().HasRequired(s => s.ModeloAeronave).WithMany(m => m.Asientos).HasForeignKey(s => s.ModeloAeronaveId);

            mb.Entity<Aeronave>().HasRequired(a => a.ModeloAeronave).WithMany(m => m.Aeronaves).HasForeignKey(a => a.ModeloAeronaveId);
            mb.Entity<Aeronave>().HasRequired(a => a.EstadoAeronave).WithMany().HasForeignKey(a => a.EstadoAeronaveCodigo);

            mb.Entity<Vuelo>().HasRequired(v => v.Frecuencia).WithMany(f => f.Vuelos).HasForeignKey(v => v.FrecuenciaId);
            mb.Entity<Vuelo>().HasOptional(v => v.Aeronave).WithMany(a => a.Vuelos).HasForeignKey(v => v.AeronaveId);
            mb.Entity<Vuelo>().HasRequired(v => v.EstadoVuelo).WithMany().HasForeignKey(v => v.EstadoVueloCodigo);

            mb.Entity<Itinerario>().HasRequired(i => i.EstadoItinerario).WithMany().HasForeignKey(i => i.EstadoItinerarioCodigo);
            mb.Entity<TramoItinerario>().HasRequired(t => t.Itinerario).WithMany(i => i.Tramos).HasForeignKey(t => t.ItinerarioId);
            mb.Entity<TramoItinerario>().HasRequired(t => t.Vuelo).WithMany(v => v.Tramos).HasForeignKey(t => t.VueloId);

            // ---------- Transacciones ----------
            mb.Entity<Pasajero>().HasRequired(p => p.TipoDocumentoIdentidad).WithMany().HasForeignKey(p => p.TipoDocumentoCodigo);

            mb.Entity<Reserva>().HasRequired(r => r.EstadoReserva).WithMany().HasForeignKey(r => r.EstadoReservaCodigo);

            mb.Entity<ReservaPasajeroVuelo>().HasRequired(a => a.Reserva).WithMany(r => r.Asignaciones).HasForeignKey(a => a.ReservaId);
            mb.Entity<ReservaPasajeroVuelo>().HasRequired(a => a.Pasajero).WithMany(p => p.Asignaciones).HasForeignKey(a => a.PasajeroId);
            mb.Entity<ReservaPasajeroVuelo>().HasRequired(a => a.Vuelo).WithMany(v => v.Asignaciones).HasForeignKey(a => a.VueloId);
            mb.Entity<ReservaPasajeroVuelo>().HasRequired(a => a.EstadoAsignacion).WithMany().HasForeignKey(a => a.EstadoAsignacionCodigo);
            mb.Entity<ReservaPasajeroVuelo>().Property(a => a.ImporteTramo).HasPrecision(12, 2);

            mb.Entity<Compra>().HasRequired(c => c.Reserva).WithMany(r => r.Compras).HasForeignKey(c => c.ReservaId);
            mb.Entity<Compra>().HasRequired(c => c.Moneda).WithMany().HasForeignKey(c => c.MonedaCodigo);
            mb.Entity<Compra>().HasRequired(c => c.EstadoCompra).WithMany().HasForeignKey(c => c.EstadoCompraCodigo);
            mb.Entity<Compra>().Property(c => c.Importe).HasPrecision(12, 2);

            mb.Entity<Pago>().HasRequired(p => p.Compra).WithMany(c => c.Pagos).HasForeignKey(p => p.CompraId);
            mb.Entity<Pago>().HasRequired(p => p.EstadoPago).WithMany().HasForeignKey(p => p.EstadoPagoCodigo);
            mb.Entity<Pago>().HasRequired(p => p.MetodoPago).WithMany().HasForeignKey(p => p.MetodoPagoCodigo);
            mb.Entity<Pago>().HasRequired(p => p.MarcaTarjeta).WithMany().HasForeignKey(p => p.MarcaTarjetaCodigo);
            mb.Entity<Pago>().Property(p => p.Importe).HasPrecision(12, 2);

            mb.Entity<CheckIn>().HasRequired(c => c.Asignacion).WithMany(a => a.CheckIns).HasForeignKey(c => c.AsignacionId);
            mb.Entity<CheckIn>().HasRequired(c => c.EstadoCheckIn).WithMany().HasForeignKey(c => c.EstadoCheckInCodigo);

            mb.Entity<Comprobante>().HasRequired(c => c.Compra).WithMany(p => p.Comprobantes).HasForeignKey(c => c.CompraId);
            mb.Entity<Comprobante>().HasRequired(c => c.TipoComprobante).WithMany().HasForeignKey(c => c.TipoComprobanteCodigo);
            mb.Entity<Comprobante>().HasRequired(c => c.EstadoComprobante).WithMany().HasForeignKey(c => c.EstadoComprobanteCodigo);

            // ---------- Vistas ----------
            mb.Entity<vw_VueloDetalle>().Property(v => v.TarifaReferencial).HasPrecision(12, 2);
            mb.Entity<vw_ComprobanteDetalle>().Property(v => v.TotalOperacion).HasPrecision(12, 2);
        }
    }
}
