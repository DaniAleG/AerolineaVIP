// Valores de las políticas del grupo (DP-xx). Se leen de Web.config.
using System;
using System.Configuration;
using System.Globalization;

namespace AerolineaVIP.Servicios
{
    public static class Politicas
    {
        private static int Entero(string clave, int porDefecto)
        {
            int valor;
            var texto = ConfigurationManager.AppSettings[clave];
            return int.TryParse(texto, NumberStyles.Integer, CultureInfo.InvariantCulture, out valor) ? valor : porDefecto;
        }

        private static string Texto(string clave, string porDefecto)
        {
            var texto = ConfigurationManager.AppSettings[clave];
            return string.IsNullOrWhiteSpace(texto) ? porDefecto : texto.Trim();
        }

        /// <summary>DP-07: horas que se retiene una reserva pendiente antes de expirar.</summary>
        public static int HorasRetencionReserva { get { return Entero("Politica:HorasRetencionReserva", 24); } }

        /// <summary>DP-07: la reserva expira a más tardar estas horas antes de la primera salida.</summary>
        public static int HorasCierreVentaAntesSalida { get { return Entero("Politica:HorasCierreVentaAntesSalida", 2); } }

        /// <summary>DP-08: anticipación mínima (horas) para que el cliente cancele una reserva pagada.</summary>
        public static int HorasMinimasCancelacionPagada { get { return Entero("Politica:HorasMinimasCancelacionPagada", 48); } }

        /// <summary>DP-12: horas antes de la salida en que abre el check-in web.</summary>
        public static int HorasAperturaCheckIn { get { return Entero("Politica:HorasAperturaCheckIn", 24); } }

        /// <summary>DP-12: horas antes de la salida en que cierra el check-in web.</summary>
        public static int HorasCierreCheckIn { get { return Entero("Politica:HorasCierreCheckIn", 1); } }

        /// <summary>DP-10: minutos mínimos de conexión en Guayaquil entre dos tramos.</summary>
        public static int MinutosMinimosConexion { get { return Entero("Politica:MinutosMinimosConexion", 60); } }

        /// <summary>DP-10: minutos máximos de conexión para ofrecer un itinerario con escala.</summary>
        public static int MinutosMaximosConexion { get { return Entero("Politica:MinutosMaximosConexion", 240); } }

        /// <summary>DP-03: minutos mínimos de preparación entre dos vuelos de la misma aeronave.</summary>
        public static int MinutosPreparacionAeronave { get { return Entero("Politica:MinutosPreparacionAeronave", 60); } }

        /// <summary>Regla de la simulación de pago: un número que termina así se rechaza.</summary>
        public static string SufijoTarjetaRechazada { get { return Texto("Politica:SufijoTarjetaRechazada", "0000"); } }

        /// <summary>DP-04: moneda de las compras.</summary>
        public static string Moneda { get { return Texto("Politica:Moneda", "USD"); } }

        /// <summary>Hora actual de la aplicación (hora local del servidor, Ecuador continental).</summary>
        public static DateTime Ahora { get { return DateTime.Now; } }
    }
}
