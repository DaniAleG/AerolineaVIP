using System;
using System.Globalization;

namespace AerolineaVIP.Servicios
{
    /// <summary>Formatos de presentación comunes a todas las vistas.</summary>
    public static class Formato
    {
        public static readonly CultureInfo Cultura = CultureInfo.GetCultureInfo("es-EC");

        private static readonly string[] DiasCortos = { "", "lun.", "mar.", "mié.", "jue.", "vie.", "sáb.", "dom." };
        private static readonly string[] DiasLargos = { "", "Lunes", "Martes", "Miércoles", "Jueves", "Viernes", "Sábado", "Domingo" };

        /// <summary>1 = lunes ... 7 = domingo (misma convención que Frecuencia.DiaSemana).</summary>
        public static int DiaSemana(DateTime fecha)
        {
            return ((int)fecha.DayOfWeek + 6) % 7 + 1;
        }

        public static string NombreDia(int dia) { return dia >= 1 && dia <= 7 ? DiasLargos[dia] : "?"; }
        public static string NombreDiaCorto(int dia) { return dia >= 1 && dia <= 7 ? DiasCortos[dia] : "?"; }

        public static string Fecha(DateTime fecha) { return fecha.ToString("dd/MM/yyyy", CultureInfo.InvariantCulture); }
        public static string Fecha(DateTime? fecha) { return fecha.HasValue ? Fecha(fecha.Value) : "—"; }

        /// <summary>Ejemplo: "dom. 08/11/2026".</summary>
        public static string FechaConDia(DateTime fecha) { return DiasCortos[DiaSemana(fecha)] + " " + Fecha(fecha); }

        public static string FechaHora(DateTime fecha) { return fecha.ToString("dd/MM/yyyy HH:mm", CultureInfo.InvariantCulture); }
        public static string FechaHora(DateTime? fecha) { return fecha.HasValue ? FechaHora(fecha.Value) : "—"; }

        public static string Hora(TimeSpan hora) { return hora.ToString(@"hh\:mm"); }
        public static string Horario(TimeSpan salida, TimeSpan llegada) { return Hora(salida) + " – " + Hora(llegada); }

        /// <summary>Ejemplo: "1.100,00 USD".</summary>
        public static string Dinero(decimal valor, string moneda = "USD")
        {
            return valor.ToString("N2", Cultura) + " " + moneda;
        }

        public static string FranjaHoraria(TimeSpan salida)
        {
            if (salida.Hours < 12) return "Vuelo de mañana";
            if (salida.Hours < 18) return "Vuelo de tarde";
            return "Vuelo nocturno";
        }

        // ---------- Estados (texto + clase CSS; nunca solo color, regla A5) ----------
        public static string TextoEstadoReserva(string codigo)
        {
            switch (codigo)
            {
                case "PENDIENTE": return "Pendiente de pago";
                case "CONFIRMADA": return "Confirmada";
                case "CANCELADA": return "Cancelada";
                case "EXPIRADA": return "Expirada";
                default: return codigo ?? "—";
            }
        }

        public static string TextoEstadoCompra(string codigo)
        {
            switch (codigo)
            {
                case "PENDIENTE": return "Pendiente de pago";
                case "PAGADA": return "Pagada";
                case "ANULADA": return "Anulada";
                case null: return "Sin compra";
                default: return codigo;
            }
        }

        public static string TextoEstadoPago(string codigo)
        {
            switch (codigo)
            {
                case "PENDIENTE": return "Pendiente";
                case "APROBADO": return "Aprobado";
                case "RECHAZADO": return "Rechazado";
                case "REVERSADO": return "Reversado";
                case null: return "Sin pago";
                default: return codigo;
            }
        }

        public static string TextoEstadoVuelo(string codigo)
        {
            switch (codigo)
            {
                case "PROGRAMADO": return "Programado";
                case "CONFIRMADO": return "Confirmado";
                case "CANCELADO": return "Cancelado";
                case "COMPLETADO": return "Completado";
                default: return codigo ?? "—";
            }
        }

        public static string TextoEstadoAsiento(string estadoVisual)
        {
            switch (estadoVisual)
            {
                case "DISPONIBLE": return "Disponible";
                case "RESERVADO": return "Reservado";
                case "OCUPADO": return "Ocupado";
                default: return estadoVisual;
            }
        }

        /// <summary>Clase CSS de la etiqueta de estado (verde/ámbar/rojo/gris/azul).</summary>
        public static string ClaseEstado(string codigo)
        {
            switch (codigo)
            {
                case "CONFIRMADA": case "PAGADA": case "APROBADO": case "EMITIDO": case "COMPLETADO":
                case "CONFIRMADO": case "DISPONIBLE": case "ACTIVO":
                    return "estado estado-ok";
                case "PENDIENTE": case "PROGRAMADO":
                    return "estado estado-pendiente";
                case "CANCELADA": case "ANULADA": case "RECHAZADO": case "CANCELADO": case "ANULADO": case "NO_DISPONIBLE":
                    return "estado estado-error";
                case "EXPIRADA": case "REVERSADO": case "LIBERADA": case "INACTIVO":
                    return "estado estado-neutro";
                default:
                    return "estado estado-info";
            }
        }

        public static string NombreLado(string lado) { return lado == "D" ? "derecho" : "izquierdo"; }
    }
}
