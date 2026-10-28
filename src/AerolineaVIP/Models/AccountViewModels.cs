using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.Web.Mvc;
using AerolineaVIP.Infraestructura;

namespace AerolineaVIP.Models
{
    /// <summary>Pantalla 2. Inicio de sesión con nombre de usuario (o correo) y contraseña.</summary>
    public class LoginViewModel
    {
        [Required(ErrorMessage = "Ingrese su nombre de usuario.")]
        [Display(Name = "Nombre de usuario o correo")]
        [StringLength(256, ErrorMessage = "Máximo {1} caracteres.")]
        public string UsuarioOCorreo { get; set; }

        [Required(ErrorMessage = "Ingrese su contraseña.")]
        [DataType(DataType.Password)]
        [Display(Name = "Contraseña")]
        public string Password { get; set; }

        [Display(Name = "Recordarme")]
        public bool RememberMe { get; set; }
    }

    /// <summary>Pantalla 3. Registro con nombre de usuario, correo, contraseña y rol.</summary>
    public class RegisterViewModel
    {
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

        [Required(ErrorMessage = "Ingrese un nombre de usuario.")]
        [Display(Name = "Nombre de usuario")]
        [StringLength(50, MinimumLength = 4, ErrorMessage = "El nombre de usuario debe tener entre {2} y {1} caracteres.")]
        [RegularExpression(@"^[A-Za-z0-9._]+$", ErrorMessage = "Use letras, números, punto o guion bajo.")]
        public string UserName { get; set; }

        [Required(ErrorMessage = "Ingrese su correo electrónico.")]
        [EmailAddress(ErrorMessage = "El correo no tiene un formato válido.")]
        [StringLength(256)]
        [Display(Name = "Correo electrónico")]
        public string Email { get; set; }

        [Required(ErrorMessage = "Ingrese una contraseña.")]
        [StringLength(100, MinimumLength = 8, ErrorMessage = "La contraseña debe tener al menos {2} caracteres.")]
        [RegularExpression(@"^(?=.*[a-z])(?=.*[A-Z])(?=.*\d)(?=.*[^A-Za-z0-9]).{8,}$",
            ErrorMessage = "La contraseña no cumple los requisitos: mínimo 8 caracteres, una mayúscula, una minúscula, un número y un símbolo.")]
        [DataType(DataType.Password)]
        [Display(Name = "Contraseña")]
        public string Password { get; set; }

        [Required(ErrorMessage = "Confirme la contraseña.")]
        [DataType(DataType.Password)]
        [Display(Name = "Confirmar contraseña")]
        [System.ComponentModel.DataAnnotations.Compare("Password", ErrorMessage = "Las contraseñas no coinciden.")]
        public string ConfirmPassword { get; set; }

        [Required(ErrorMessage = "Seleccione el rol.")]
        [Display(Name = "Rol")]
        public string Rol { get; set; }

        [DebeSerVerdadero(ErrorMessage = "Debe aceptar el aviso de privacidad y las condiciones.")]
        [Display(Name = "Acepto el aviso de privacidad y las condiciones")]
        public bool AceptaCondiciones { get; set; }

        public IEnumerable<SelectListItem> Roles { get; set; }
        public bool PuedeElegirRol { get; set; }
    }

    /// <summary>Pantalla 4. Recuperación de acceso.</summary>
    public class ForgotPasswordViewModel
    {
        [Required(ErrorMessage = "Ingrese su correo o nombre de usuario.")]
        [StringLength(256, ErrorMessage = "Máximo {1} caracteres.")]
        [Display(Name = "Correo o nombre de usuario")]
        public string UsuarioOCorreo { get; set; }
    }

    public class ResetPasswordViewModel
    {
        [Required(ErrorMessage = "Ingrese su correo electrónico.")]
        [EmailAddress(ErrorMessage = "El correo no tiene un formato válido.")]
        [Display(Name = "Correo electrónico")]
        public string Email { get; set; }

        [Required(ErrorMessage = "Ingrese una contraseña.")]
        [StringLength(100, MinimumLength = 8, ErrorMessage = "La contraseña debe tener al menos {2} caracteres.")]
        [RegularExpression(@"^(?=.*[a-z])(?=.*[A-Z])(?=.*\d)(?=.*[^A-Za-z0-9]).{8,}$",
            ErrorMessage = "La contraseña no cumple los requisitos: mínimo 8 caracteres, una mayúscula, una minúscula, un número y un símbolo.")]
        [DataType(DataType.Password)]
        [Display(Name = "Nueva contraseña")]
        public string Password { get; set; }

        [DataType(DataType.Password)]
        [Display(Name = "Confirmar contraseña")]
        [System.ComponentModel.DataAnnotations.Compare("Password", ErrorMessage = "Las contraseñas no coinciden.")]
        public string ConfirmPassword { get; set; }

        public string Code { get; set; }
    }
}
