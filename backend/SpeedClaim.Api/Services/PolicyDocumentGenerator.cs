using System.Globalization;
using System.IO.Compression;
using System.Reflection;
using System.Text;
using SpeedClaim.Api.Models;

namespace SpeedClaim.Api.Services;

public static class PolicyDocumentGenerator
{
    public const string ContentType = "application/pdf";

    // wwwroot/assets/logo.png (a rasterized copy of frontend/public/favicon.svg) embedded as a
    // manifest resource — decoded and re-Flate-compressed once, then reused for every certificate.
    private static readonly Lazy<LogoImage> _logo = new(LoadLogo);

    public static byte[] GenerateCertificatePdf(Policy policy, string customerName, string productName)
    {
        var issuedAt = policy.IssuedAt.HasValue ? policy.IssuedAt.Value.ToString("dd MMM yyyy", CultureInfo.InvariantCulture) : "Pending";
        var tableRows = new[]
        {
            ("Policy number", policy.PolicyNumber),
            ("Product", productName),
            ("Policyholder", customerName),
            ("Status", policy.Status.ToString()),
            ("Coverage amount", FormatMoney(policy.SumAssured)),
            ("Premium", $"{FormatMoney(policy.PremiumAmount)} / {policy.PaymentFrequency}"),
            ("Valid from", policy.StartDate.ToString("dd MMM yyyy", CultureInfo.InvariantCulture)),
            ("Valid to", policy.EndDate.ToString("dd MMM yyyy", CultureInfo.InvariantCulture)),
            ("Issued At", issuedAt)
        };

        var content = new PdfCanvas();
        DrawHeader(content);
        DrawIntro(content, policy);
        DrawTable(content, tableRows);
        DrawContactPanel(content);
        DrawFooter(content);

        return BuildPdf(content.ToString(), _logo.Value);
    }

    private static LogoImage LoadLogo()
    {
        using var stream = typeof(PolicyDocumentGenerator).Assembly
            .GetManifestResourceStream("SpeedClaim.Api.Assets.logo.png")
            ?? throw new InvalidOperationException("Embedded resource SpeedClaim.Api.Assets.logo.png not found.");
        using var buffer = new MemoryStream();
        stream.CopyTo(buffer);

        var (rgb, width, height) = PngDecoder.DecodeRgb24(buffer.ToArray());

        using var flate = new MemoryStream();
        using (var deflate = new ZLibStream(flate, CompressionLevel.Optimal, leaveOpen: true))
            deflate.Write(rgb);

        return new LogoImage(flate.ToArray(), width, height);
    }

    private static void DrawHeader(PdfCanvas c)
    {
        c.FillRect(0, 706, 612, 86, 0.06, 0.43, 0.55);
        c.FillRect(0, 706, 612, 10, 0.12, 0.62, 0.42);
        c.DrawImage("Im1", 56, 742, 36, 36);
        c.Text("SpeedClaim", 94, 755, 22, "F2", 1, 1, 1);
        c.Text("Insurance policy services", 96, 736, 10, "F1", 0.82, 0.94, 0.97);
        c.Text("Policy Certificate", 410, 754, 18, "F2", 1, 1, 1);
        c.Text("Generated document", 438, 736, 9, "F1", 0.82, 0.94, 0.97);
    }

    private static void DrawIntro(PdfCanvas c, Policy policy)
    {
        c.Text("Your cover is active", 54, 662, 22, "F2", 0.10, 0.13, 0.19);
        c.Text("This certificate summarizes the active policy issued in SpeedClaim records.", 54, 638, 10.5, "F1", 0.24, 0.27, 0.33);
        c.Text("Keep it with your records and verify the policy details below.", 54, 622, 10.5, "F1", 0.24, 0.27, 0.33);

        c.FillRoundedRect(444, 626, 98, 30, 6, 0.88, 0.97, 0.93);
        c.StrokeRoundedRect(444, 626, 98, 30, 6, 0.77, 0.90, 0.84);
        c.Text(policy.Status.ToString().ToUpperInvariant(), 473, 636, 9.5, "F2", 0.12, 0.49, 0.32);
    }

    private static void DrawTable(PdfCanvas c, IReadOnlyList<(string Label, string Value)> rows)
    {
        const double left = 54;
        const double top = 584;
        const double labelWidth = 178;
        const double valueWidth = 326;
        const double rowHeight = 32;

        c.Text("Policy summary", left, top + 20, 14, "F2", 0.10, 0.13, 0.19);
        c.StrokeRoundedRect(left, top - rows.Count * rowHeight, labelWidth + valueWidth, rows.Count * rowHeight, 8, 0.86, 0.89, 0.92);

        for (var i = 0; i < rows.Count; i++)
        {
            var y = top - ((i + 1) * rowHeight);
            var isEven = i % 2 == 0;
            c.FillRect(left, y, labelWidth, rowHeight, 0.96, 0.98, 0.99);
            if (isEven)
                c.FillRect(left + labelWidth, y, valueWidth, rowHeight, 0.99, 0.995, 1);

            if (i > 0)
                c.Line(left, y + rowHeight, left + labelWidth + valueWidth, y + rowHeight, 0.88, 0.90, 0.93);

            c.Line(left + labelWidth, y, left + labelWidth, y + rowHeight, 0.88, 0.90, 0.93);
            c.Text(rows[i].Label, left + 14, y + 11, 9.5, "F2", 0.42, 0.46, 0.52);
            c.Text(rows[i].Value, left + labelWidth + 14, y + 11, 10.5, "F1", 0.10, 0.13, 0.19);
        }
    }

    private static void DrawContactPanel(PdfCanvas c)
    {
        c.FillRect(54, 132, 504, 72, 0.97, 0.99, 0.98);
        c.StrokeRoundedRect(54, 132, 504, 72, 0, 0.82, 0.90, 0.88);
        c.Text("Need help?", 76, 176, 13, "F2", 0.10, 0.13, 0.19);
        c.Text("For corrections, claim support, or policy servicing, contact SpeedClaim support.", 76, 158, 10, "F1", 0.24, 0.27, 0.33);
        c.Text("support@speedclaim.local  |  +91 1800 000 000  |  speedclaim.local", 76, 140, 10, "F2", 0.06, 0.43, 0.55);
    }

    private static void DrawFooter(PdfCanvas c)
    {
        c.Line(54, 94, 558, 94, 0.88, 0.90, 0.93);
        c.Text("This is a system-generated certificate. It is valid only when policy status is Active in SpeedClaim records.", 54, 72, 8.5, "F1", 0.42, 0.46, 0.52);
        c.Text("SpeedClaim Insurance | Confidential policy document", 54, 54, 8.5, "F1", 0.42, 0.46, 0.52);
        c.Text("Page 1 of 1", 514, 54, 8.5, "F1", 0.42, 0.46, 0.52);
    }

    private static string FormatMoney(decimal value) =>
        string.Create(CultureInfo.InvariantCulture, $"{value:0.00} INR");

    // Objects are raw bytes (not strings) so the image XObject's Flate-compressed binary stream
    // survives unmangled — an ASCII StreamWriter would corrupt any byte outside the 7-bit range.
    private static byte[] BuildPdf(string contentStream, LogoImage logo)
    {
        var objects = new List<byte[]>
        {
            Ascii("<< /Type /Catalog /Pages 2 0 R >>"),
            Ascii("<< /Type /Pages /Kids [3 0 R] /Count 1 >>"),
            Ascii("<< /Type /Page /Parent 2 0 R /MediaBox [0 0 612 792] " +
                  "/Resources << /Font << /F1 4 0 R /F2 5 0 R >> /XObject << /Im1 6 0 R >> >> " +
                  "/Contents 7 0 R >>"),
            Ascii("<< /Type /Font /Subtype /Type1 /BaseFont /Helvetica >>"),
            Ascii("<< /Type /Font /Subtype /Type1 /BaseFont /Helvetica-Bold >>"),
            Concat(
                Ascii($"<< /Type /XObject /Subtype /Image /Width {logo.Width} /Height {logo.Height} " +
                      $"/ColorSpace /DeviceRGB /BitsPerComponent 8 /Filter /FlateDecode /Length {logo.FlateData.Length} >>\nstream\n"),
                logo.FlateData,
                Ascii("\nendstream")),
            Concat(Ascii($"<< /Length {Encoding.ASCII.GetByteCount(contentStream)} >>\nstream\n"), Ascii(contentStream), Ascii("endstream"))
        };

        using var output = new MemoryStream();
        void WriteAscii(string s) => output.Write(Ascii(s));

        WriteAscii("%PDF-1.4\n");

        var offsets = new List<long> { 0 };
        for (var i = 0; i < objects.Count; i++)
        {
            offsets.Add(output.Position);
            WriteAscii($"{i + 1} 0 obj\n");
            output.Write(objects[i]);
            WriteAscii("\nendobj\n");
        }

        var xrefOffset = output.Position;
        WriteAscii("xref\n");
        WriteAscii($"0 {objects.Count + 1}\n");
        WriteAscii("0000000000 65535 f \n");
        foreach (var offset in offsets.Skip(1))
            WriteAscii($"{offset:0000000000} 00000 n \n");

        WriteAscii("trailer\n");
        WriteAscii($"<< /Size {objects.Count + 1} /Root 1 0 R >>\n");
        WriteAscii("startxref\n");
        WriteAscii($"{xrefOffset}\n");
        WriteAscii("%%EOF");

        return output.ToArray();
    }

    private static byte[] Ascii(string value) => Encoding.ASCII.GetBytes(value);

    private static byte[] Concat(params byte[][] parts)
    {
        var result = new byte[parts.Sum(p => p.Length)];
        var offset = 0;
        foreach (var part in parts)
        {
            Buffer.BlockCopy(part, 0, result, offset, part.Length);
            offset += part.Length;
        }
        return result;
    }

    private sealed record LogoImage(byte[] FlateData, int Width, int Height);

    // Minimal decoder for the one PNG shape we ourselves produce (8-bit, non-interlaced,
    // truecolor-without-alpha) — not a general-purpose PNG reader.
    private static class PngDecoder
    {
        public static (byte[] Rgb, int Width, int Height) DecodeRgb24(byte[] png)
        {
            var pos = 8; // skip the 8-byte PNG signature
            int width = 0, height = 0;
            using var idat = new MemoryStream();

            while (pos < png.Length)
            {
                var length = ReadUInt32BigEndian(png, pos);
                var type = Encoding.ASCII.GetString(png, pos + 4, 4);
                var dataStart = pos + 8;

                if (type == "IHDR")
                {
                    width = (int)ReadUInt32BigEndian(png, dataStart);
                    height = (int)ReadUInt32BigEndian(png, dataStart + 4);
                    var bitDepth = png[dataStart + 8];
                    var colorType = png[dataStart + 9];
                    var interlace = png[dataStart + 12];
                    if (bitDepth != 8 || colorType != 2 || interlace != 0)
                        throw new NotSupportedException(
                            $"logo.png must be 8-bit non-interlaced RGB (got bitDepth={bitDepth}, colorType={colorType}, interlace={interlace}).");
                }
                else if (type == "IDAT")
                {
                    idat.Write(png, dataStart, (int)length);
                }
                else if (type == "IEND")
                {
                    break;
                }

                pos = dataStart + (int)length + 4; // + 4 for the trailing CRC
            }

            using var inflated = new MemoryStream();
            using (var zlib = new ZLibStream(new MemoryStream(idat.ToArray()), CompressionMode.Decompress))
                zlib.CopyTo(inflated);

            return (Unfilter(inflated.ToArray(), width, height), width, height);
        }

        private static byte[] Unfilter(byte[] filtered, int width, int height)
        {
            const int bpp = 3; // RGB, 8 bits per channel
            var stride = width * bpp;
            var raw = new byte[height * stride];
            var srcPos = 0;

            for (var row = 0; row < height; row++)
            {
                var filterType = filtered[srcPos++];
                var rowStart = row * stride;
                var prevRowStart = rowStart - stride;

                for (var i = 0; i < stride; i++)
                {
                    var x = filtered[srcPos++];
                    var a = i >= bpp ? raw[rowStart + i - bpp] : 0;
                    var b = row > 0 ? raw[prevRowStart + i] : 0;
                    var cc = row > 0 && i >= bpp ? raw[prevRowStart + i - bpp] : 0;

                    raw[rowStart + i] = filterType switch
                    {
                        0 => x,
                        1 => (byte)(x + a),
                        2 => (byte)(x + b),
                        3 => (byte)(x + (a + b) / 2),
                        4 => (byte)(x + PaethPredictor(a, b, cc)),
                        _ => throw new NotSupportedException($"Unsupported PNG filter type {filterType}.")
                    };
                }
            }

            return raw;
        }

        private static int PaethPredictor(int a, int b, int c)
        {
            var p = a + b - c;
            var pa = Math.Abs(p - a);
            var pb = Math.Abs(p - b);
            var pc = Math.Abs(p - c);
            if (pa <= pb && pa <= pc) return a;
            return pb <= pc ? b : c;
        }

        private static uint ReadUInt32BigEndian(byte[] data, int offset) =>
            (uint)((data[offset] << 24) | (data[offset + 1] << 16) | (data[offset + 2] << 8) | data[offset + 3]);
    }

    private sealed class PdfCanvas
    {
        private readonly StringBuilder _content = new();

        public void Text(string value, double x, double y, double size, string font, double r, double g, double b)
        {
            _content.AppendLine("BT");
            _content.AppendLine($"/{font} {F(size)} Tf");
            _content.AppendLine($"{F(r)} {F(g)} {F(b)} rg");
            _content.AppendLine($"{F(x)} {F(y)} Td");
            _content.AppendLine($"({Escape(value)}) Tj");
            _content.AppendLine("ET");
        }

        public void FillRect(double x, double y, double width, double height, double r, double g, double b)
        {
            _content.AppendLine($"q {F(r)} {F(g)} {F(b)} rg {F(x)} {F(y)} {F(width)} {F(height)} re f Q");
        }

        public void FillRoundedRect(double x, double y, double width, double height, double radius, double r, double g, double b)
        {
            FillRect(x, y, width, height, r, g, b);
        }

        public void StrokeRoundedRect(double x, double y, double width, double height, double radius, double r, double g, double b)
        {
            _content.AppendLine($"q {F(r)} {F(g)} {F(b)} RG 1 w {F(x)} {F(y)} {F(width)} {F(height)} re S Q");
        }

        public void Line(double x1, double y1, double x2, double y2, double r, double g, double b)
        {
            _content.AppendLine($"q {F(r)} {F(g)} {F(b)} RG 1 w {F(x1)} {F(y1)} m {F(x2)} {F(y2)} l S Q");
        }

        public void DrawImage(string xObjectName, double x, double y, double width, double height)
        {
            _content.AppendLine($"q {F(width)} 0 0 {F(height)} {F(x)} {F(y)} cm /{xObjectName} Do Q");
        }

        public override string ToString() => _content.ToString();

        private static string F(double value) => value.ToString("0.###", CultureInfo.InvariantCulture);

        private static string Escape(string value) =>
            value.Replace("\\", "\\\\").Replace("(", "\\(").Replace(")", "\\)");
    }
}
