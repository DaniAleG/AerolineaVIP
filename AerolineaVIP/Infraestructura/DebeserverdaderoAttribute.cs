using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.Web.Mvc;

namespace AerolineaVIP.Infraestructura
{
    /// <summary>Obliga a marcar una casilla (también se valida en el cliente).</summary>
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
