// Genera un PDF sencillo para el comprobante, sin librerías externas.
using System;
using System.Collections.Generic;
using System.Globalization;
using System.IO;
using System.Text;

namespace AerolineaVIP.Servicios
{
    public class PdfSimple
    {
        public const double Ancho = 595.28;   // A4 en puntos
        public const double Alto = 841.89;

        private readonly List<StringBuilder> paginas = new List<StringBuilder>();
        private StringBuilder actual;
        private static readonly Encoding WinAnsi = ObtenerWinAnsi();

        public PdfSimple()
        {
            NuevaPagina();
        }

        private static Encoding ObtenerWinAnsi()
        {
            try { return Encoding.GetEncoding(1252); }
            catch { return Encoding.GetEncoding("ISO-8859-1"); }
        }

        public void NuevaPagina()
        {
            actual = new StringBuilder();
            paginas.Add(actual);
        }

        private static string N(double v)
        {
            return v.ToString("0.##", CultureInfo.InvariantCulture);
        }

        private static string Escapar(string texto)
        {
            var sb = new StringBuilder();
            foreach (var c in texto ?? "")
            {
                char ch = c;
                if (ch == '→') { sb.Append("->"); continue; }
                if (ch == '–' || ch == '—') ch = '-';
                if (ch == '\\' || ch == '(' || ch == ')') sb.Append('\\');
                sb.Append(ch);
            }
            return sb.ToString();
        }

        /// <summary>Texto en coordenadas desde la esquina superior izquierda (y crece hacia abajo).</summary>
        public void Texto(double x, double y, string texto, double tamano = 10, bool negrita = false, double gris = 0)
        {
            actual.AppendFormat("{0} g BT /{1} {2} Tf {3} {4} Td ({5}) Tj ET\n",
                N(gris), negrita ? "F2" : "F1", N(tamano), N(x), N(Alto - y), Escapar(texto));
        }

        /// <summary>Texto alineado a la derecha (ancho aproximado de Helvetica).</summary>
        public void TextoDerecha(double xDerecha, double y, string texto, double tamano = 10, bool negrita = false)
        {
            Texto(xDerecha - AnchoTexto(texto, tamano, negrita), y, texto, tamano, negrita);
        }

        public static double AnchoTexto(string texto, double tamano, bool negrita)
        {
            double total = 0;
            foreach (var c in texto ?? "")
            {
                double w;
                if (c == ' ') w = 0.278;
                else if ("il.,:;'|!".IndexOf(c) >= 0) w = 0.25;
                else if ("ftjrI()-/".IndexOf(c) >= 0) w = 0.36;
                else if (char.IsDigit(c)) w = 0.556;
                else if ("mwMW".IndexOf(c) >= 0) w = 0.85;
                else if (char.IsUpper(c)) w = 0.68;
                else w = 0.53;
                total += w;
            }
            return total * tamano * (negrita ? 1.05 : 1.0);
        }

        public void Linea(double x1, double y1, double x2, double y2, double grosor = 0.5, double gris = 0.6)
        {
            actual.AppendFormat("{0} G {1} w {2} {3} m {4} {5} l S\n", N(gris), N(grosor), N(x1), N(Alto - y1), N(x2), N(Alto - y2));
        }

        /// <summary>Rectángulo relleno (r, g, b de 0 a 1).</summary>
        public void Rectangulo(double x, double y, double ancho, double alto, double r, double g, double b)
        {
            actual.AppendFormat("{0} {1} {2} rg {3} {4} {5} {6} re f 0 g\n", N(r), N(g), N(b), N(x), N(Alto - y - alto), N(ancho), N(alto));
        }

        /// <summary>Texto con color (r, g, b de 0 a 1).</summary>
        public void TextoColor(double x, double y, string texto, double tamano, bool negrita, double r, double g, double b)
        {
            actual.AppendFormat("{0} {1} {2} rg BT /{3} {4} Tf {5} {6} Td ({7}) Tj ET 0 g\n",
                N(r), N(g), N(b), negrita ? "F2" : "F1", N(tamano), N(x), N(Alto - y), Escapar(texto));
        }

        /// <summary>Círculo relleno con borde (centro y radio en puntos; colores r, g, b de 0 a 1).</summary>
        public void Circulo(double cx, double cy, double radio, double[] relleno, double[] borde, double grosor)
        {
            const double k = 0.5523;  // aproximación del círculo con cuatro curvas de Bézier
            double x = cx, y = Alto - cy, r = radio, c = radio * k;
            actual.AppendFormat("{0} {1} {2} rg {3} {4} {5} RG {6} w\n", N(relleno[0]), N(relleno[1]), N(relleno[2]), N(borde[0]), N(borde[1]), N(borde[2]), N(grosor));
            actual.AppendFormat("{0} {1} m\n", N(x + r), N(y));
            actual.AppendFormat("{0} {1} {2} {3} {4} {5} c\n", N(x + r), N(y + c), N(x + c), N(y + r), N(x), N(y + r));
            actual.AppendFormat("{0} {1} {2} {3} {4} {5} c\n", N(x - c), N(y + r), N(x - r), N(y + c), N(x - r), N(y));
            actual.AppendFormat("{0} {1} {2} {3} {4} {5} c\n", N(x - r), N(y - c), N(x - c), N(y - r), N(x), N(y - r));
            actual.AppendFormat("{0} {1} {2} {3} {4} {5} c\n", N(x + c), N(y - r), N(x + r), N(y - c), N(x + r), N(y));
            actual.Append("B 0 g 0 G\n");
        }

        /// <summary>Polígono relleno a partir de pares x, y (coordenadas desde arriba a la izquierda).</summary>
        public void Poligono(double[] puntos, double r, double g, double b)
        {
            actual.AppendFormat("{0} {1} {2} rg ", N(r), N(g), N(b));
            for (int i = 0; i < puntos.Length; i += 2)
                actual.AppendFormat("{0} {1} {2} ", N(puntos[i]), N(Alto - puntos[i + 1]), i == 0 ? "m" : "l");
            actual.Append("h f 0 g\n");
        }

        /// <summary>Logo de la aerolínea (el mismo dibujo de Content/img/logo.svg) en un cuadro de "tamano" puntos.</summary>
        public void Logo(double x, double y, double tamano)
        {
            var e = tamano / 64.0;   // el SVG mide 64 x 64
            Circulo(x + 32 * e, y + 32 * e, 30 * e, new[] { 0.06, 0.15, 0.29 }, new[] { 0.72, 0.57, 0.18 }, 3 * e);
            var avion = new double[] { 14, 36, 50, 22, 46, 28, 30, 35, 37, 46, 33, 47, 24, 37, 17, 40 };
            for (int i = 0; i < avion.Length; i += 2) { avion[i] = x + avion[i] * e; avion[i + 1] = y + avion[i + 1] * e; }
            Poligono(avion, 0.91, 0.85, 0.66);
        }
        public byte[] Generar()
        {
            using (var ms = new MemoryStream())
            {
                var offsets = new List<long>();
                Action<string> escribir = s => { var b = WinAnsi.GetBytes(s); ms.Write(b, 0, b.Length); };
                Action<int, string> objeto = (id, contenido) =>
                {
                    while (offsets.Count < id) offsets.Add(0);
                    offsets[id - 1] = ms.Position;
                    escribir(id + " 0 obj\n" + contenido + "\nendobj\n");
                };

                escribir("%PDF-1.4\n");
                ms.Write(new byte[] { 0x25, 0xE2, 0xE3, 0xCF, 0xD3, 0x0A }, 0, 6);

                int n = paginas.Count;
                // 1 catálogo, 2 árbol de páginas, 3 y 4 fuentes, luego página/contenido alternados.
                var kids = new StringBuilder();
                for (int i = 0; i < n; i++) kids.Append((5 + i * 2) + " 0 R ");

                objeto(1, "<< /Type /Catalog /Pages 2 0 R >>");
                objeto(2, "<< /Type /Pages /Kids [" + kids + "] /Count " + n + " >>");
                objeto(3, "<< /Type /Font /Subtype /Type1 /BaseFont /Helvetica /Encoding /WinAnsiEncoding >>");
                objeto(4, "<< /Type /Font /Subtype /Type1 /BaseFont /Helvetica-Bold /Encoding /WinAnsiEncoding >>");

                for (int i = 0; i < n; i++)
                {
                    int idPagina = 5 + i * 2, idContenido = 6 + i * 2;
                    objeto(idPagina, "<< /Type /Page /Parent 2 0 R /MediaBox [0 0 " + N(Ancho) + " " + N(Alto) + "] " +
                                     "/Resources << /Font << /F1 3 0 R /F2 4 0 R >> >> /Contents " + idContenido + " 0 R >>");
                    var bytes = WinAnsi.GetBytes(paginas[i].ToString());
                    while (offsets.Count < idContenido) offsets.Add(0);
                    offsets[idContenido - 1] = ms.Position;
                    escribir(idContenido + " 0 obj\n<< /Length " + bytes.Length + " >>\nstream\n");
                    ms.Write(bytes, 0, bytes.Length);
                    escribir("\nendstream\nendobj\n");
                }

                var xref = ms.Position;
                var sb = new StringBuilder();
                sb.Append("xref\n0 " + (offsets.Count + 1) + "\n0000000000 65535 f \n");
                foreach (var o in offsets) sb.Append(o.ToString("0000000000") + " 00000 n \n");
                sb.Append("trailer\n<< /Size " + (offsets.Count + 1) + " /Root 1 0 R >>\nstartxref\n" + xref + "\n%%EOF\n");
                escribir(sb.ToString());
                return ms.ToArray();
            }
        }
    }
}
