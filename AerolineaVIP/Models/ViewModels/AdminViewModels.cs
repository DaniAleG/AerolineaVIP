using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.Web.Mvc;
using AerolineaVIP.Models.Dominio;

namespace AerolineaVIP.Models.ViewModels
{
    // ===================== Pantalla 18 =====================
    public class FilaVueloAdminVM
    {
        public vw_VueloDetalle Vuelo { get; set; }
        public string Matricula { get; set; }
        public int Capacidad { get; set; }
        public int Ocupados { get; set; }
        public int Disponibles { get; set; }
    }

    public class ConflictoVM
    {
        public string Tipo { get; set; }          // Superposición / Preparación / Continuidad
        public string Aeronave { get; set; }
        public string Vuelo1 { get; set; }
        public string Vuelo2 { get; set; }
        public string Detalle { get; set; }
        public bool Bloqueante { get; set; }
    }

    public class EditarVueloVM
    {
        public int VueloId { get; set; }
        public string CodigoVuelo { get; set; }
        public string Descripcion { get; set; }

        [Display(Name = "Aeronave asignada")]
        public int? AeronaveId { get; set; }

        [Display(Name = "Estado")]
        [Required(ErrorMessage = "Seleccione el estado.")]
        public string EstadoVueloCodigo { get; set; }
    }

    public class AdminVuelosVM : IValidatableObject
    {
        public string Tab { get; set; }

        [Display(Name = "Fecha desde")]
        [Required(ErrorMessage = "Indique la fecha desde.")]
        [DataType(DataType.Date)]
        [DisplayFormat(DataFormatString = "{0:yyyy-MM-dd}", ApplyFormatInEditMode = true)]
        public DateTime? Desde { get; set; }

        [Display(Name = "Fecha hasta")]
        [Required(ErrorMessage = "Indique la fecha hasta.")]
        [DataType(DataType.Date)]
        [DisplayFormat(DataFormatString = "{0:yyyy-MM-dd}", ApplyFormatInEditMode = true)]
        public DateTime? Hasta { get; set; }

        [Display(Name = "Ruta (opcional)")]
        public int? RutaId { get; set; }

        [Display(Name = "Estado (opcional)")]
        public string Estado { get; set; }

        public bool Conflictos { get; set; }
        public int? Editar { get; set; }

        public List<FilaVueloAdminVM> Vuelos { get; set; }
        public List<Frecuencia> Frecuencias { get; set; }
        public List<Itinerario> Itinerarios { get; set; }
        public List<ConflictoVM> ListaConflictos { get; set; }
        public EditarVueloVM Edicion { get; set; }
        public IEnumerable<SelectListItem> Rutas { get; set; }
        public IEnumerable<SelectListItem> Estados { get; set; }
        public IEnumerable<SelectListItem> Aeronaves { get; set; }
        public IEnumerable<SelectListItem> EstadosEdicion { get; set; }
        public Dictionary<int, string> CodigosVuelo { get; set; }

        public IEnumerable<ValidationResult> Validate(ValidationContext validationContext)
        {
            if (Desde.HasValue && Hasta.HasValue)
            {
                if (Hasta.Value < Desde.Value)
                    yield return new ValidationResult("«Fecha hasta» no puede ser anterior a «Fecha desde».", new[] { "Hasta" });
                else if ((Hasta.Value - Desde.Value).TotalDays > 365)
                    yield return new ValidationResult("El rango no puede superar 366 días.", new[] { "Hasta" });
            }
        }
    }

    // ===================== Pantalla 19 =====================
    public class FilaAeronaveVM
    {
        public Aeronave Aeronave { get; set; }
        public int VuelosProximos { get; set; }
    }

    public class EditarAeronaveVM
    {
        public int AeronaveId { get; set; }

        [Display(Name = "Matrícula / identificador")]
        [Required(ErrorMessage = "Ingrese la matrícula.")]
        [StringLength(15, ErrorMessage = "Máximo {1} caracteres.")]
        [RegularExpression(@"^[A-Z0-9-]+$", ErrorMessage = "Use mayúsculas, números y guion.")]
        public string Matricula { get; set; }

        [Display(Name = "Modelo")]
        [Required(ErrorMessage = "Seleccione el modelo.")]
        public int ModeloAeronaveId { get; set; }

        [Display(Name = "Capacidad")]
        public int Capacidad { get; set; }

        [Display(Name = "Estado operativo")]
        [Required(ErrorMessage = "Seleccione el estado operativo.")]
        public string EstadoAeronaveCodigo { get; set; }
    }

    public class AdminAeronavesVM
    {
        public List<FilaAeronaveVM> Flota { get; set; }
        public EditarAeronaveVM Edicion { get; set; }
        public MapaAsientosVM Distribucion { get; set; }
        public List<vw_VueloDetalle> Asignaciones { get; set; }
        public IEnumerable<SelectListItem> Modelos { get; set; }
        public IEnumerable<SelectListItem> Estados { get; set; }
    }

    // ===================== Pantalla 20 =====================
    public class FilaUsuarioVM
    {
        public string Id { get; set; }
        public string UserName { get; set; }
        public string Email { get; set; }
        public string NombreCompleto { get; set; }
        public List<string> Roles { get; set; }
        public bool Bloqueado { get; set; }
        public bool EsElevado { get; set; }
    }

    public class FilaPasajeroVM
    {
        public Pasajero Pasajero { get; set; }
        public int Reservas { get; set; }
    }

    public class EditarUsuarioVM
    {
        public string Id { get; set; }
        public string UserName { get; set; }

        [Required(ErrorMessage = "Ingrese los nombres.")]
        [StringLength(100, MinimumLength = 2)]
        [RegularExpression(@"^[A-Za-zÁÉÍÓÚÜÑáéíóúüñ ]+$", ErrorMessage = "Use solo letras y espacios.")]
        [Display(Name = "Nombres")]
        public string Nombres { get; set; }

        [Required(ErrorMessage = "Ingrese los apellidos.")]
        [StringLength(100, MinimumLength = 2)]
        [RegularExpression(@"^[A-Za-zÁÉÍÓÚÜÑáéíóúüñ ]+$", ErrorMessage = "Use solo letras y espacios.")]
        [Display(Name = "Apellidos")]
        public string Apellidos { get; set; }

        [Required(ErrorMessage = "Ingrese el correo.")]
        [EmailAddress(ErrorMessage = "El correo no tiene un formato válido.")]
        [Display(Name = "Correo electrónico")]
        public string Email { get; set; }

        [Display(Name = "Teléfono (opcional)")]
        [RegularExpression(@"^[0-9+ ]{7,20}$", ErrorMessage = "Use solo dígitos, espacios y «+» (7 a 20).")]
        public string PhoneNumber { get; set; }
    }

    public class AdminGestionVM
    {
        public string Tab { get; set; }

        [Display(Name = "Buscar (opcional)")]
        [StringLength(256)]
        public string Buscar { get; set; }

        [Display(Name = "Rol (opcional)")]
        public string Rol { get; set; }

        [Display(Name = "Estado (opcional)")]
        public string Estado { get; set; }

        public List<FilaUsuarioVM> Usuarios { get; set; }
        public List<FilaPasajeroVM> Pasajeros { get; set; }
        public List<FilaReservaVM> Reservas { get; set; }
        public IEnumerable<SelectListItem> Roles { get; set; }
        public IEnumerable<SelectListItem> Estados { get; set; }
        public bool EsSuperUsuario { get; set; }
        public bool PuedeVerUsuarios { get; set; }
    }

    // ===================== Pantalla 21 =====================
    public class ReportesVM : IValidatableObject
    {
        [Display(Name = "Consulta")]
        [Required(ErrorMessage = "Seleccione la consulta.")]
        public string Consulta { get; set; }

        [Display(Name = "Fecha desde")]
        [Required(ErrorMessage = "Indique la fecha desde.")]
        [DataType(DataType.Date)]
        [DisplayFormat(DataFormatString = "{0:yyyy-MM-dd}", ApplyFormatInEditMode = true)]
        public DateTime? Desde { get; set; }

        [Display(Name = "Fecha hasta")]
        [Required(ErrorMessage = "Indique la fecha hasta.")]
        [DataType(DataType.Date)]
        [DisplayFormat(DataFormatString = "{0:yyyy-MM-dd}", ApplyFormatInEditMode = true)]
        public DateTime? Hasta { get; set; }

        [Display(Name = "Ruta (opcional)")]
        public int? RutaId { get; set; }

        [Display(Name = "Aeronave (opcional)")]
        public int? AeronaveId { get; set; }

        [Display(Name = "Estado (opcional)")]
        public string Estado { get; set; }

        public bool Ejecutada { get; set; }
        public DateTime Corte { get; set; }
        public List<vw_DisponibilidadVuelo> Ocupacion { get; set; }
        public List<FilaReporteReservaVM> ReservasLista { get; set; }
        public List<FilaReporteCompraVM> Compras { get; set; }

        public IEnumerable<SelectListItem> Consultas { get; set; }
        public IEnumerable<SelectListItem> Rutas { get; set; }
        public IEnumerable<SelectListItem> AeronavesLista { get; set; }
        public IEnumerable<SelectListItem> Estados { get; set; }

        public IEnumerable<ValidationResult> Validate(ValidationContext validationContext)
        {
            if (Desde.HasValue && Hasta.HasValue)
            {
                if (Hasta.Value < Desde.Value)
                    yield return new ValidationResult("«Fecha hasta» no puede ser anterior a «Fecha desde».", new[] { "Hasta" });
                else if ((Hasta.Value - Desde.Value).TotalDays > 365)
                    yield return new ValidationResult("El rango no puede superar 366 días.", new[] { "Hasta" });
            }
        }
    }

    public class FilaReporteReservaVM
    {
        public string Estado { get; set; }
        public int Reservas { get; set; }
        public int Asientos { get; set; }
        public decimal Importe { get; set; }
    }

    public class FilaReporteCompraVM
    {
        public int ReservaId { get; set; }
        public string CodigoReserva { get; set; }
        public DateTime FechaCompra { get; set; }
        public decimal Importe { get; set; }
        public string Moneda { get; set; }
        public string EstadoCompra { get; set; }
        public int Intentos { get; set; }
        public string UltimoPago { get; set; }
        public string CodigoOperacion { get; set; }
        public string Marca { get; set; }
    }
}
