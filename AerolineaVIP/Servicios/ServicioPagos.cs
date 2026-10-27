// -----------------------------------------------------------------------------
// Compra y pago SIMULADO (pantalla 11). No existe pasarela ni banco: el resultado
// lo decide una regla académica documentada (DP-06):
//     número de tarjeta terminado en 0000  ->  RECHAZADO
//     cualquier otro número con formato válido -> APROBADO
// Solo se guardan marca, últimos 4 dígitos, titular y código de operación.
// Reserva, compra y pago se administran por separado (RNP-06).
// -----------------------------------------------------------------------------
using System;
using System.Collections.Generic;
using System.Linq;
using AerolineaVIP.Infraestructura;
using AerolineaVIP.Models.Dominio;
using AerolineaVIP.Models.ViewModels;

namespace AerolineaVIP.Servicios
{
    public class ServicioPagos
    {
        private readonly AerolineaVIPEntities db;

        public ServicioPagos(AerolineaVIPEntities db)
        {
            this.db = db;
        }

        /// <summary>Siguiente número de comprobante académico por tipo (CNF-, ACAD-, NC-ACAD-).</summary>
        public static string SiguienteNumero(AerolineaVIPEntities db, string tipo)
        {
            string prefijo = tipo == "CONFIRMACION" ? "CNF-" : tipo == "FACTURA" ? "ACAD-" : "NC-ACAD-";
            var numeros = db.Comprobante.Where(c => c.TipoComprobanteCodigo == tipo).Select(c => c.NumeroComprobante).ToList();
            // Incluye los agregados en esta misma unidad de trabajo y aún no guardados.
            numeros.AddRange(db.Comprobante.Local.Where(c => c.TipoComprobanteCodigo == tipo).Select(c => c.NumeroComprobante));
            var max = 0;
            foreach (var n in numeros)
            {
                int valor;
                if (n != null && n.StartsWith(prefijo) && int.TryParse(n.Substring(prefijo.Length), out valor) && valor > max) max = valor;
            }
            return prefijo + (max + 1).ToString("000000");
        }

        /// <summary>Comprueba si el número coincide con algún prefijo de referencia de la marca ("4", "51-55, 2221-2720"...).</summary>
        public static bool CoincidePrefijo(string numero, string prefijos)
        {
            if (string.IsNullOrWhiteSpace(prefijos)) return true;
            foreach (var parte in prefijos.Split(','))
            {
                var p = parte.Trim();
                if (p.Length == 0) continue;
                var rango = p.Split('-');
                if (rango.Length == 2)
                {
                    int desde, hasta;
                    var largo = rango[0].Trim().Length;
                    if (numero.Length >= largo && int.TryParse(rango[0], out desde) && int.TryParse(rango[1], out hasta))
                    {
                        int inicio;
                        if (int.TryParse(numero.Substring(0, largo), out inicio) && inicio >= desde && inicio <= hasta) return true;
                    }
                }
                else if (numero.StartsWith(p))
                {
                    return true;
                }
            }
            return false;
        }

        /// <summary>Validación de formato del formulario de tarjeta (repite en el servidor lo que hace jQuery).</summary>
        public Dictionary<string, string> ValidarTarjeta(PagoVM vm, MarcaTarjeta marca)
        {
            var errores = new Dictionary<string, string>();
            var numero = (vm.NumeroTarjeta ?? "").Replace(" ", "");
            if (marca == null)
            {
                errores["MarcaTarjetaCodigo"] = "Seleccione una marca de tarjeta válida.";
                return errores;
            }
            if (!numero.All(char.IsDigit) || numero.Length != marca.LongitudNumero)
                errores["NumeroTarjeta"] = "El número debe tener " + marca.LongitudNumero + " dígitos para " + marca.Nombre + ".";
            else if (!CoincidePrefijo(numero, marca.PrefijosReferencia))
                errores["NumeroTarjeta"] = "El número no corresponde a los prefijos de referencia de " + marca.Nombre + " (" + marca.PrefijosReferencia + ").";

            var codigo = vm.CodigoSeguridad ?? "";
            if (codigo.Length != marca.LongitudCodigoSeguridad || !codigo.All(char.IsDigit))
                errores["CodigoSeguridad"] = "El código de seguridad tiene " + marca.LongitudCodigoSeguridad + " dígitos para " + marca.Nombre + ".";

            var venc = vm.Vencimiento ?? "";
            int mes, anio;
            if (venc.Length == 5 && int.TryParse(venc.Substring(0, 2), out mes) && int.TryParse(venc.Substring(3, 2), out anio) && mes >= 1 && mes <= 12)
            {
                var ultimoDia = new DateTime(2000 + anio, mes, 1).AddMonths(1).AddDays(-1);
                if (ultimoDia < DateTime.Today) errores["Vencimiento"] = "La tarjeta está vencida.";
            }
            else
            {
                errores["Vencimiento"] = "Use el formato MM/AA.";
            }
            return errores;
        }

        /// <summary>
        /// Registra la compra (si no existe) y el pago simulado. La compra solo pasa a PAGADA
        /// después de registrar un pago APROBADO. La clave de idempotencia evita duplicados.
        /// </summary>
        public ResultadoPago Procesar(PagoVM vm, Reserva reserva)
        {
            var resultado = new ResultadoPago();
            var ahora = Politicas.Ahora;

            // Pago repetido: la compra ya está pagada (doble clic, recarga o reenvío).
            var pagada = reserva.Compras.FirstOrDefault(c => c.EstadoCompraCodigo == "PAGADA");
            if (pagada != null)
            {
                resultado.Aprobado = true;
                resultado.Duplicado = true;
                resultado.Mensaje = "La compra ya estaba pagada; no se registró un cobro duplicado.";
                return resultado;
            }

            var numero = vm.NumeroTarjeta.Replace(" ", "");
            using (var tx = db.Database.BeginTransaction())
            {
                try
                {
                    var compra = reserva.Compras.FirstOrDefault(c => c.EstadoCompraCodigo == "PENDIENTE");
                    var importe = reserva.Asignaciones.Where(a => a.EstadoAsignacionCodigo == "PENDIENTE").Sum(a => a.ImporteTramo);
                    if (compra == null)
                    {
                        compra = new Compra
                        {
                            ReservaId = reserva.ReservaId,
                            FechaCompra = ahora,
                            Importe = importe,                 // calculado en el servidor
                            MonedaCodigo = Politicas.Moneda,
                            EstadoCompraCodigo = "PENDIENTE",
                            ClaveIdempotencia = vm.Token == Guid.Empty ? Guid.NewGuid() : vm.Token
                        };
                        if (db.Compra.Any(c => c.ClaveIdempotencia == compra.ClaveIdempotencia))
                            compra.ClaveIdempotencia = Guid.NewGuid();
                        db.Compra.Add(compra);
                        reserva.Compras.Add(compra);
                        db.SaveChanges();
                    }
                    else
                    {
                        compra.Importe = importe;
                    }

                    var intento = db.Pago.Count(p => p.CompraId == compra.CompraId) + 1;
                    var codigoOperacion = "SIM-" + reserva.CodigoReserva + "-" + intento.ToString("00");
                    while (db.Pago.Any(p => p.CodigoOperacion == codigoOperacion))
                        codigoOperacion = "SIM-" + reserva.CodigoReserva + "-" + (++intento).ToString("00");

                    var aprobado = !numero.EndsWith(Politicas.SufijoTarjetaRechazada);
                    var pago = new Pago
                    {
                        CompraId = compra.CompraId,
                        FechaPago = ahora,
                        Importe = compra.Importe,
                        EstadoPagoCodigo = aprobado ? "APROBADO" : "RECHAZADO",
                        MetodoPagoCodigo = vm.MetodoPagoCodigo,
                        MarcaTarjetaCodigo = vm.MarcaTarjetaCodigo,
                        UltimosCuatroDigitos = numero.Substring(numero.Length - 4),
                        NombreTitular = vm.NombreTitular.Trim().ToUpper(),
                        CodigoOperacion = codigoOperacion,
                        EsSimulado = true,
                        MensajeResultado = aprobado
                            ? "Pago simulado aprobado."
                            : "Pago simulado rechazado (regla académica: número terminado en " + Politicas.SufijoTarjetaRechazada + ")."
                    };
                    db.Pago.Add(pago);

                    if (aprobado)
                    {
                        compra.EstadoCompraCodigo = "PAGADA";
                        reserva.EstadoReservaCodigo = "CONFIRMADA";
                        reserva.FechaExpiracion = null;
                        foreach (var a in reserva.Asignaciones.Where(a => a.EstadoAsignacionCodigo == "PENDIENTE"))
                            a.EstadoAsignacionCodigo = "CONFIRMADA";      // asientos morados -> rojos

                        db.Comprobante.Add(new Comprobante
                        {
                            CompraId = compra.CompraId,
                            TipoComprobanteCodigo = "CONFIRMACION",
                            NumeroComprobante = SiguienteNumero(db, "CONFIRMACION"),
                            FechaEmision = ahora,
                            EstadoComprobanteCodigo = "EMITIDO"
                        });
                        db.Comprobante.Add(new Comprobante
                        {
                            CompraId = compra.CompraId,
                            TipoComprobanteCodigo = "FACTURA",
                            NumeroComprobante = SiguienteNumero(db, "FACTURA"),
                            FechaEmision = ahora,
                            EstadoComprobanteCodigo = "EMITIDO"
                        });
                    }

                    db.SaveChanges();
                    tx.Commit();

                    resultado.Aprobado = aprobado;
                    resultado.CodigoOperacion = codigoOperacion;
                    resultado.Mensaje = aprobado
                        ? "Compra " + reserva.CodigoReserva + " pagada. Pago simulado aprobado " + codigoOperacion + "."
                        : "Pago simulado rechazado. La reserva sigue pendiente; puede intentar con otra tarjeta.";
                    return resultado;
                }
                catch (Exception ex)
                {
                    tx.Rollback();
                    if (ErroresBD.EsUnicidad(ex) && ex.ToString().Contains("UX_Pago_AprobadoPorCompra"))
                    {
                        resultado.Aprobado = true;
                        resultado.Duplicado = true;
                        resultado.Mensaje = "La compra ya tenía un pago aprobado; no se registró un cobro duplicado.";
                        return resultado;
                    }
                    resultado.Mensaje = ErroresBD.Mensaje(ex);
                    return resultado;
                }
            }
        }
    }
}
