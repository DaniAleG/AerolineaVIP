using System;
using System.Globalization;
using System.Web.Mvc;

namespace AerolineaVIP.Infraestructura
{
    /// <summary>Acepta coma o punto como separador decimal.</summary>
    public class DecimalModelBinder : IModelBinder
    {
        public object BindModel(ControllerContext controllerContext, ModelBindingContext bindingContext)
        {
            var resultado = bindingContext.ValueProvider.GetValue(bindingContext.ModelName);
            if (resultado == null) return null;
            bindingContext.ModelState.SetModelValue(bindingContext.ModelName, resultado);

            var texto = (resultado.AttemptedValue ?? "").Trim().Replace(" ", "");
            if (texto.Length == 0)
            {
                if (Nullable.GetUnderlyingType(bindingContext.ModelType) != null) return null;
                bindingContext.ModelState.AddModelError(bindingContext.ModelName, "El valor es obligatorio.");
                return 0m;
            }

            // Si hay coma y punto, el último símbolo es el separador decimal.
            int coma = texto.LastIndexOf(','), punto = texto.LastIndexOf('.');
            if (coma >= 0 && punto >= 0)
                texto = coma > punto ? texto.Replace(".", "").Replace(',', '.') : texto.Replace(",", "");
            else if (coma >= 0)
                texto = texto.Replace(',', '.');

            decimal valor;
            if (decimal.TryParse(texto, NumberStyles.Number, CultureInfo.InvariantCulture, out valor))
                return valor;

            bindingContext.ModelState.AddModelError(bindingContext.ModelName, "Ingrese un número válido.");
            return 0m;
        }
    }
}
