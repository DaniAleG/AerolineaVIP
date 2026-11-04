using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.Web.Mvc;

namespace AerolineaVIP.Infraestructura
{
    /// <summary>Valida un RUC ecuatoriano (también se valida en el cliente).</summary>
    public class RucAttribute : ValidationAttribute, IClientValidatable
    {
        public RucAttribute() : base("El RUC no es válido (13 dígitos, revise el número).") { }

        public override bool IsValid(object value)
        {
            var texto = value as string;
            return string.IsNullOrEmpty(texto) || DocumentoEcuador.RucValido(texto);
        }

        public IEnumerable<ModelClientValidationRule> GetClientValidationRules(ModelMetadata metadata, ControllerContext context)
        {
            yield return new ModelClientValidationRule
            {
                ErrorMessage = FormatErrorMessage(metadata.GetDisplayName()),
                ValidationType = "rucec"
            };
        }
    }
}