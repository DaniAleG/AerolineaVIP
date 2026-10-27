// -----------------------------------------------------------------------------
// Traduce las excepciones de SQL Server a mensajes comprensibles.
// Los triggers de reglas de negocio del script lanzan errores 50001–50052 con un
// texto en español; las restricciones de integridad lanzan 2601/2627 (únicos) y
// 547 (claves foráneas o CHECK).
// -----------------------------------------------------------------------------
using System;
using System.Data.Entity.Infrastructure;
using System.Data.Entity.Validation;
using System.Data.SqlClient;
using System.Linq;

namespace AerolineaVIP.Infraestructura
{
    public static class ErroresBD
    {
        /// <summary>Busca la SqlException dentro de la cadena de excepciones.</summary>
        public static SqlException ObtenerSql(Exception ex)
        {
            while (ex != null)
            {
                var sql = ex as SqlException;
                if (sql != null) return sql;
                ex = ex.InnerException;
            }
            return null;
        }

        public static bool EsUnicidad(Exception ex)
        {
            var sql = ObtenerSql(ex);
            return sql != null && (sql.Number == 2601 || sql.Number == 2627);
        }

        public static bool EsReglaNegocio(Exception ex)
        {
            var sql = ObtenerSql(ex);
            return sql != null && sql.Number >= 50000;
        }

        /// <summary>Mensaje para mostrar al usuario.</summary>
        public static string Mensaje(Exception ex)
        {
            var validacion = ex as DbEntityValidationException;
            if (validacion != null)
            {
                return string.Join(" ", validacion.EntityValidationErrors
                    .SelectMany(e => e.ValidationErrors)
                    .Select(e => e.ErrorMessage));
            }

            var sql = ObtenerSql(ex);
            if (sql == null)
                return "No se pudo completar la operación. " + (ex != null ? ex.Message : "");

            if (sql.Number >= 50000)
                return sql.Message;                         // regla de negocio del trigger (texto en español)

            switch (sql.Number)
            {
                case 2601:
                case 2627:
                    if (sql.Message.Contains("UX_RPV_AsientoActivo"))
                        return "El asiento ya fue reservado u ocupado por otra operación. Seleccione otro.";
                    if (sql.Message.Contains("UX_RPV_PasajeroActivo"))
                        return "Este pasajero ya tiene un asiento vigente en ese vuelo.";
                    if (sql.Message.Contains("UX_Compra_ReservaVigente"))
                        return "La reserva ya tiene una compra vigente.";
                    if (sql.Message.Contains("UX_Pago_AprobadoPorCompra"))
                        return "La compra ya tiene un pago aprobado; no se registró un cobro duplicado.";
                    if (sql.Message.Contains("UX_CheckIn_Vigente"))
                        return "El pasajero ya tiene un check-in completado en ese vuelo.";
                    return "Ya existe un registro con esos datos (valor duplicado).";
                case 547:
                    if (sql.Message.Contains("DELETE"))
                        return "No se puede eliminar: el registro tiene información asociada (integridad referencial).";
                    if (sql.Message.Contains("CHECK"))
                        return "Los datos no cumplen una restricción de la base de datos: " + Restriccion(sql.Message) + ".";
                    return "El registro hace referencia a un dato inexistente (integridad referencial).";
                case 2:
                case 53:
                case 4060:
                case 18456:
                    return "No se pudo conectar con SQL Server. Revise la cadena AerolineaVIPEntities en Web.config.";
                default:
                    return "Error de base de datos (" + sql.Number + "): " + sql.Message;
            }
        }

        private static string Restriccion(string mensaje)
        {
            var i = mensaje.IndexOf("\"CK_", StringComparison.Ordinal);
            if (i < 0) return "restricción CHECK";
            var f = mensaje.IndexOf('"', i + 1);
            return f > i ? mensaje.Substring(i + 1, f - i - 1) : "restricción CHECK";
        }
    }
}
