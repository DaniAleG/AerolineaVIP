using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.Web.Mvc;

namespace AerolineaVIP.Infraestructura
{
    /// <summary>
    /// Obliga a marcar una casilla (aceptación de condiciones). Valida en el servidor y,
    /// mediante el adaptador "debeserverdadero" de site.js, también en el cliente con jQuery.
    /// </summary>
    public class DebeSerVerdaderoAttribute : ValidationAttribute, IClientValidatable
    {
        public DebeSerVerdaderoAttribute() : base("Debe marcar la casilla {0}.") { }

        public override bool IsValid(object value)
        {
            return value is bool && (bool)value;
        }

        public IEnumerable<ModelClientValidationRule> GetClientValidationRules(ModelMetadata metadata, ControllerContext context)
        {
            yield return new ModelClientValidationRule
            {
                ErrorMessage = FormatErrorMessage(metadata.GetDisplayName()),
                ValidationType = "debeserverdadero"
            };
        }
    }
}
