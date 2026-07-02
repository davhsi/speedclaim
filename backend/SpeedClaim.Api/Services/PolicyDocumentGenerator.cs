using System.Globalization;
using System.Text;
using SpeedClaim.Api.Models;

namespace SpeedClaim.Api.Services;

public static class PolicyDocumentGenerator
{
    public const string ContentType = "application/pdf";

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

        return BuildPdf(content.ToString());
    }

    private static void DrawHeader(PdfCanvas c)
    {
        c.FillRect(0, 706, 612, 86, 0.06, 0.43, 0.55);
        c.FillRect(0, 706, 612, 10, 0.12, 0.62, 0.42);
        c.DrawSpeedClaimLogo(56, 742, 36);
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

    private static byte[] BuildPdf(string stream)
    {
        var objects = new[]
        {
            "<< /Type /Catalog /Pages 2 0 R >>",
            "<< /Type /Pages /Kids [3 0 R] /Count 1 >>",
            "<< /Type /Page /Parent 2 0 R /MediaBox [0 0 612 792] /Resources << /Font << /F1 4 0 R /F2 5 0 R >> >> /Contents 6 0 R >>",
            "<< /Type /Font /Subtype /Type1 /BaseFont /Helvetica >>",
            "<< /Type /Font /Subtype /Type1 /BaseFont /Helvetica-Bold >>",
            $"<< /Length {Encoding.ASCII.GetByteCount(stream)} >>\nstream\n{stream}endstream"
        };

        using var output = new MemoryStream();
        using var writer = new StreamWriter(output, Encoding.ASCII, leaveOpen: true);
        writer.WriteLine("%PDF-1.4");

        var offsets = new List<long> { 0 };
        for (var i = 0; i < objects.Length; i++)
        {
            writer.Flush();
            offsets.Add(output.Position);
            writer.WriteLine($"{i + 1} 0 obj");
            writer.WriteLine(objects[i]);
            writer.WriteLine("endobj");
        }

        writer.Flush();
        var xrefOffset = output.Position;
        writer.WriteLine("xref");
        writer.WriteLine($"0 {objects.Length + 1}");
        writer.WriteLine("0000000000 65535 f ");
        foreach (var offset in offsets.Skip(1))
            writer.WriteLine($"{offset:0000000000} 00000 n ");

        writer.WriteLine("trailer");
        writer.WriteLine($"<< /Size {objects.Length + 1} /Root 1 0 R >>");
        writer.WriteLine("startxref");
        writer.WriteLine(xrefOffset);
        writer.WriteLine("%%EOF");
        writer.Flush();

        return output.ToArray();
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

        public void DrawSpeedClaimLogo(double x, double y, double size)
        {
            FillRect(x, y, size, size, 0.04, 0.19, 0.25);
            _content.AppendLine(
                $"q 1 1 1 RG {F(size * .11)} w 1 J " +
                $"{F(x + size * .24)} {F(y + size * .36)} m " +
                $"{F(x + size * .32)} {F(y + size * .12)} {F(x + size * .80)} {F(y + size * .13)} {F(x + size * .73)} {F(y + size * .45)} c " +
                $"{F(x + size * .67)} {F(y + size * .69)} {F(x + size * .27)} {F(y + size * .53)} {F(x + size * .37)} {F(y + size * .78)} c " +
                $"{F(x + size * .44)} {F(y + size * .92)} {F(x + size * .64)} {F(y + size * .87)} {F(x + size * .73)} {F(y + size * .77)} c S Q");
            _content.AppendLine(
                $"q 0.13 0.78 0.53 RG {F(size * .09)} w 1 J 1 j " +
                $"{F(x + size * .23)} {F(y + size * .35)} m " +
                $"{F(x + size * .36)} {F(y + size * .20)} l " +
                $"{F(x + size * .73)} {F(y + size * .58)} l S Q");
            _content.AppendLine($"q 0.47 0.87 0.95 RG {F(size * .06)} w 1 J {F(x + size * .76)} {F(y + size * .83)} m {F(x + size * .92)} {F(y + size * .83)} l S Q");
            _content.AppendLine($"q 0.47 0.87 0.95 RG {F(size * .06)} w 1 J {F(x + size * .82)} {F(y + size * .70)} m {F(x + size * .94)} {F(y + size * .70)} l S Q");
        }

        public override string ToString() => _content.ToString();

        private static string F(double value) => value.ToString("0.###", CultureInfo.InvariantCulture);

        private static string Escape(string value) =>
            value.Replace("\\", "\\\\").Replace("(", "\\(").Replace(")", "\\)");
    }
}
