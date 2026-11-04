using System;
using System.Collections.Generic;
using System.Linq;
using System.Web;

namespace AerolineaVIP.Infraestructura
{
    /// <summary>Formatos de entrada usados en los formularios (nombres, teléfonos y correos).</summary>
    public static class Formatos
    {
        // Letras con tildes y ñ; un solo espacio, apóstrofo o guion entre palabras.
        public const string Nombre = @"^[A-Za-zÁÉÍÓÚÜÑáéíóúüñ]+(?:[ '-][A-Za-zÁÉÍÓÚÜÑáéíóúüñ]+)*$";
        public const string MsgNombre = "Use solo letras, con un espacio entre palabras.";

        // Celular 09XXXXXXXX, fijo 0[2-7]XXXXXXX o internacional +código y número.
        public const string Telefono = @"^(?:09[0-9]{8}|0[2-7][0-9]{7}|\+[0-9]{1,3} ?[0-9]{6,12})$";
        public const string MsgTelefono = "Ingrese un celular (09 + 8 dígitos), un fijo (02 a 07 + 7 dígitos) o un número internacional (+código y número).";

        public const string Correo = @"^[A-Za-z0-9._%+-]+@[A-Za-z0-9.-]+\.[A-Za-z]{2,}$";
        public const string MsgCorreo = "Ingrese un correo válido (ej.: nombre@dominio.com).";
    }

    /// <summary>Validación de cédula y RUC ecuatorianos.</summary>
    public static class DocumentoEcuador
    {
        /// <summary>Cédula: 10 dígitos, provincia 01-24 o 30, tercer dígito menor a 6 y dígito verificador (módulo 10).</summary>
        public static bool CedulaValida(string cedula)
        {
            if (!SoloDigitos(cedula, 10) || !ProvinciaValida(cedula)) return false;
            if (cedula[2] - '0' >= 6) return false;

            int suma = 0;
            for (int i = 0; i < 9; i++)
            {
                int digito = cedula[i] - '0';
                if (i % 2 == 0)
                {
                    digito *= 2;
                    if (digito > 9) digito -= 9;
                }
                suma += digito;
            }
            int calculado = (10 - suma % 10) % 10;
            return calculado == cedula[9] - '0';
        }

        /// <summary>RUC: 13 dígitos. Persona natural (cédula + 001), sociedad pública (6) o privada (9) con módulo 11.</summary>
        public static bool RucValido(string ruc)
        {
            if (!SoloDigitos(ruc, 13) || !ProvinciaValida(ruc)) return false;
            int tercero = ruc[2] - '0';

            if (tercero < 6)
                return CedulaValida(ruc.Substring(0, 10)) && ruc.Substring(10) != "000";
            if (tercero == 6)
                return Modulo11(ruc, new[] { 3, 2, 7, 6, 5, 4, 3, 2 }) && ruc.Substring(9) != "0000";
            if (tercero == 9)
                return Modulo11(ruc, new[] { 4, 3, 2, 7, 6, 5, 4, 3, 2 }) && ruc.Substring(10) != "000";
            return false;
        }

        // El dígito verificador va justo después de los dígitos ponderados.
        private static bool Modulo11(string ruc, int[] coeficientes)
        {
            int suma = 0;
            for (int i = 0; i < coeficientes.Length; i++)
                suma += (ruc[i] - '0') * coeficientes[i];
            int calculado = 11 - suma % 11;
            if (calculado == 11) calculado = 0;
            if (calculado == 10) return false;
            return calculado == ruc[coeficientes.Length] - '0';
        }

        private static bool SoloDigitos(string valor, int longitud)
        {
            if (valor == null || valor.Length != longitud) return false;
            foreach (char c in valor)
                if (c < '0' || c > '9') return false;
            return true;
        }

        private static bool ProvinciaValida(string valor)
        {
            int provincia = int.Parse(valor.Substring(0, 2));
            return (provincia >= 1 && provincia <= 24) || provincia == 30;
        }
    }
}