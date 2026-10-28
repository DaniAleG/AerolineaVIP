using System.Linq;
using AerolineaVIP.Models.Dominio;
using AerolineaVIP.Models.ViewModels;

namespace AerolineaVIP.Servicios
{
    /// <summary>Comprobantes académicos (pantalla 16): vista previa y PDF.</summary>
    public class ServicioComprobantes
    {
        private readonly AerolineaVIPEntities db;

        public ServicioComprobantes(AerolineaVIPEntities db)
        {
            this.db = db;
        }

        public ComprobanteDocumentoVM Documento(int comprobanteId)
        {
            var enc = db.vw_ComprobanteDetalle.AsNoTracking().FirstOrDefault(c => c.ComprobanteId == comprobanteId);
            if (enc == null) return null;

            var asignaciones = db.ReservaPasajeroVuelo.AsNoTracking()
                .Where(a => a.ReservaId == enc.ReservaId && (a.EstadoAsignacionCodigo == "CONFIRMADA" || a.EstadoAsignacionCodigo == "CANCELADA"))
                .Select(a => new { a.VueloId, a.NumeroAsiento, a.ImporteTramo, a.Pasajero.Nombres, a.Pasajero.Apellidos, a.Pasajero.TipoDocumentoCodigo, a.Pasajero.NumeroDocumento })
                .ToList();
            var ids = asignaciones.Select(a => a.VueloId).Distinct().ToList();
            var vuelos = db.vw_VueloDetalle.AsNoTracking().Where(v => ids.Contains(v.VueloId)).ToDictionary(v => v.VueloId);
            var codigos = new ServicioReservas(db).CodigosAsiento(ids);
            var usuario = ServicioUsuarios.Obtener(enc.UsuarioId);

            return new ComprobanteDocumentoVM
            {
                Encabezado = enc,
                Cliente = usuario == null ? "Usuario no registrado en Identity (" + enc.UsuarioId + ")" : usuario.Etiqueta,
                ClienteCorreo = usuario == null ? null : usuario.Email,
                Lineas = asignaciones
                    .OrderBy(a => vuelos[a.VueloId].SalidaProgramada).ThenBy(a => a.NumeroAsiento)
                    .Select(a =>
                    {
                        var v = vuelos[a.VueloId];
                        string codigo;
                        return new LineaComprobanteVM
                        {
                            Pasajero = (a.Nombres + " " + a.Apellidos).Trim(),
                            Documento = ServicioReservas.Documento(a.TipoDocumentoCodigo, a.NumeroDocumento),
                            CodigoVuelo = v.CodigoVuelo,
                            Ruta = v.CiudadOrigen + " → " + v.CiudadDestino,
                            Fecha = v.FechaOperacion,
                            Horario = Formato.Horario(v.HoraSalida, v.HoraLlegada),
                            Asiento = codigos.TryGetValue(a.VueloId + "|" + a.NumeroAsiento, out codigo) ? codigo : a.NumeroAsiento.ToString(),
                            Importe = a.ImporteTramo
                        };
                    }).ToList()
            };
        }

        /// <summary>Genera el PDF del comprobante con los datos persistidos.</summary>
        public byte[] Pdf(ComprobanteDocumentoVM d)
        {
            var e = d.Encabezado;
            var pdf = new PdfSimple();
            const double izq = 40, der = 555;

            // Encabezado
            pdf.Rectangulo(0, 0, PdfSimple.Ancho, 70, 0.05, 0.12, 0.25);
            pdf.TextoColor(izq, 32, "AEROLÍNEA VIP", 18, true, 1, 1, 1);
            pdf.TextoColor(izq, 50, "Proyecto académico · Comprobante de una operación SIMULADA", 9, false, 0.85, 0.75, 0.45);
            var tipo = e.TipoComprobanteCodigo == "FACTURA" ? "Comprobante de venta académico"
                     : e.TipoComprobanteCodigo == "NOTA_CREDITO" ? "Nota de crédito académica"
                     : e.TipoComprobanteCodigo == "CONFIRMACION" ? "Confirmación de la operación" : e.TipoComprobante;
            pdf.TextoColor(380, 32, tipo, 9, true, 1, 1, 1);
            pdf.TextoColor(380, 48, "N.º " + e.NumeroComprobante, 12, true, 1, 1, 1);

            double y = 100;
            pdf.Texto(izq, y, "Emisor", 9, true, 0.35);
            pdf.Texto(300, y, "Cliente", 9, true, 0.35);
            y += 14;
            pdf.Texto(izq, y, e.EmisorNombre, 10, true);
            pdf.Texto(300, y, d.Cliente.Length > 50 ? d.Cliente.Substring(0, 50) : d.Cliente, 10, true);
            y += 13;
            pdf.Texto(izq, y, "Identificación: " + e.EmisorIdentificacion, 9);
            pdf.Texto(300, y, "Correo: " + (d.ClienteCorreo ?? "—"), 9);
            y += 13;
            pdf.Texto(izq, y, "Dirección: " + e.EmisorDireccion, 9);
            pdf.Texto(300, y, "Reserva: " + e.CodigoReserva, 9);
            y += 13;
            pdf.Texto(izq, y, "Fecha de emisión: " + Formato.FechaHora(e.FechaEmision), 9);
            pdf.Texto(300, y, "Estado del comprobante: " + e.EstadoComprobanteCodigo, 9);
            y += 13;
            pdf.Texto(izq, y, "Pago: " + (e.PagoId.HasValue ? e.MetodoPago + " · " + e.MarcaTarjeta + " " + e.TarjetaEnmascarada : "Sin pago registrado"), 9);
            y += 13;
            pdf.Texto(izq, y, "Operación simulada: " + (e.CodigoOperacion ?? "—") + " · Estado del pago: " + Formato.TextoEstadoPago(e.EstadoPagoCodigo), 9);

            // Detalle
            y += 26;
            pdf.Rectangulo(izq, y - 12, der - izq, 18, 0.92, 0.93, 0.96);
            pdf.Texto(izq + 4, y, "Pasajero", 8.5, true);
            pdf.Texto(180, y, "Vuelo", 8.5, true);
            pdf.Texto(230, y, "Ruta", 8.5, true);
            pdf.Texto(370, y, "Fecha / horario", 8.5, true);
            pdf.Texto(470, y, "Asiento", 8.5, true);
            if (d.MostrarImportes) pdf.TextoDerecha(der - 4, y, "Importe", 8.5, true);
            y += 18;

            foreach (var l in d.Lineas)
            {
                if (y > 760) { pdf.NuevaPagina(); y = 60; }
                var nombre = l.Pasajero.Length > 30 ? l.Pasajero.Substring(0, 30) : l.Pasajero;
                pdf.Texto(izq + 4, y, nombre, 8.5);
                pdf.Texto(izq + 4, y + 10, l.Documento, 7, false, 0.4);
                pdf.Texto(180, y, l.CodigoVuelo, 8.5);
                pdf.Texto(230, y, l.Ruta, 8.5);
                pdf.Texto(370, y, Formato.Fecha(l.Fecha), 8.5);
                pdf.Texto(370, y + 10, l.Horario, 7, false, 0.4);
                pdf.Texto(470, y, l.Asiento, 8.5, true);
                if (d.MostrarImportes) pdf.TextoDerecha(der - 4, y, Formato.Dinero(l.Importe, e.MonedaCodigo), 8.5);
                y += 22;
                pdf.Linea(izq, y - 8, der, y - 8, 0.3, 0.8);
            }

            if (d.MostrarImportes)
            {
                y += 6;
                pdf.TextoDerecha(der - 4, y, "Total: " + Formato.Dinero(e.TotalOperacion, e.MonedaCodigo), 11, true);
                y += 14;
                pdf.TextoDerecha(der - 4, y, "Tarifas de ejemplo (DP-04). Valores sin desglose tributario (DP-05).", 7.5);
            }

            // Leyenda obligatoria
            y += 30;
            pdf.Rectangulo(izq, y - 12, der - izq, 34, 1, 0.97, 0.88);
            pdf.Texto(izq + 8, y, "Aviso", 9, true);
            pdf.Texto(izq + 8, y + 13, e.LeyendaComprobante.Length > 110 ? e.LeyendaComprobante.Substring(0, 110) : e.LeyendaComprobante, 8);
            pdf.Texto(izq, 815, "Documento generado por el sistema web Aerolínea VIP · ESPE · Desarrollo Web para Integración de Tecnología · Grupo 4", 7, false, 0.5);
            return pdf.Generar();
        }
    }
}
