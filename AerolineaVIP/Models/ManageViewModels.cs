using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;

namespace AerolineaVIP.Models
{
    /// <summary>Perfil del usuario (Área del cliente · Perfil, cuenta de Identity).</summary>
    public class PerfilViewModel
    {
        [Display(Name = "Nombre de usuario")]
        public string UserName { get; set; }

        [Required(ErrorMessage = "Ingrese sus nombres.")]
        [Display(Name = "Nombres")]
        [StringLength(100, MinimumLength = 2, ErrorMessage = "Los nombres deben tener entre {2} y {1} caracteres.")]
        [RegularExpression(@"^[A-Za-zÁÉÍÓÚÜÑáéíóúüñ ]+$", ErrorMessage = "Use solo letras y espacios.")]
        public string Nombres { get; set; }

        [Required(ErrorMessage = "Ingrese sus apellidos.")]
        [Display(Name = "Apellidos")]
        [StringLength(100, MinimumLength = 2, ErrorMessage = "Los apellidos deben tener entre {2} y {1} caracteres.")]
        [RegularExpression(@"^[A-Za-zÁÉÍÓÚÜÑáéíóúüñ ]+$", ErrorMessage = "Use solo letras y espacios.")]
        public string Apellidos { get; set; }

        [Required(ErrorMessage = "Ingrese su correo electrónico.")]
        [EmailAddress(ErrorMessage = "El correo no tiene un formato válido.")]
        [Display(Name = "Correo electrónico")]
        public string Email { get; set; }

        [Display(Name = "Teléfono (opcional)")]
        [RegularExpression(@"^[0-9+ ]{7,20}$", ErrorMessage = "Use solo dígitos, espacios y «+» (7 a 20).")]
        public string PhoneNumber { get; set; }

        public IList<string> Roles { get; set; }
    }

    public class ChangePasswordViewModel
    {
        [Required(ErrorMessage = "Ingrese su contraseña actual.")]
        [DataType(DataType.Password)]
        [Display(Name = "Contraseña actual")]
        public string OldPassword { get; set; }

        [Required(ErrorMessage = "Ingrese la nueva contraseña.")]
        [StringLength(100, MinimumLength = 8, ErrorMessage = "La contraseña debe tener al menos {2} caracteres.")]
        [RegularExpression(@"^(?=.*[a-z])(?=.*[A-Z])(?=.*\d)(?=.*[^A-Za-z0-9]).{8,}$",
            ErrorMessage = "La contraseña no cumple los requisitos: mínimo 8 caracteres, una mayúscula, una minúscula, un número y un símbolo.")]
        [DataType(DataType.Password)]
        [Display(Name = "Nueva contraseña")]
        public string NewPassword { get; set; }

        [DataType(DataType.Password)]
        [Display(Name = "Confirmar nueva contraseña")]
        [System.ComponentModel.DataAnnotations.Compare("NewPassword", ErrorMessage = "Las contraseñas no coinciden.")]
        public string ConfirmPassword { get; set; }
    }
}
