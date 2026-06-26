using System.Globalization;
using System.Text;
using SpeedClaim.Api.Models;

namespace SpeedClaim.Api.Services;

public static class PolicyDocumentGenerator
{
    public const string ContentType = "application/pdf";

    public static byte[] GenerateCertificatePdf(Policy policy, string customerName, string productName)
    {
        var lines = new[]
        {
            "SpeedClaim Insurance",
            "Policy Certificate",
            $"Policy Number: {policy.PolicyNumber}",
            $"Product: {productName}",
            $"Policyholder: {customerName}",
            $"Status: {policy.Status}",
            $"Coverage: {FormatMoney(policy.SumAssured)}",
            $"Premium: {FormatMoney(policy.PremiumAmount)} / {policy.PaymentFrequency}",
            $"Valid From: {policy.StartDate:dd MMM yyyy}",
            $"Valid To: {policy.EndDate:dd MMM yyyy}",
            policy.IssuedAt.HasValue ? $"Issued At: {policy.IssuedAt.Value:dd MMM yyyy HH:mm 'UTC'}" : "Issued At: Pending",
            "",
            "This certificate confirms that the policy listed above is active in SpeedClaim records.",
            "Please keep this document with your insurance records."
        };

        return BuildSimplePdf(lines);
    }

    private static string FormatMoney(decimal value) =>
        string.Create(CultureInfo.InvariantCulture, $"{value:0.00} USD");

    private static byte[] BuildSimplePdf(IEnumerable<string> lines)
    {
        var content = new StringBuilder();
        content.AppendLine("BT");
        content.AppendLine("/F1 18 Tf");
        content.AppendLine("72 760 Td");

        var first = true;
        foreach (var line in lines)
        {
            if (!first)
            {
                content.AppendLine("0 -24 Td");
            }

            var fontSize = line is "SpeedClaim Insurance" or "Policy Certificate" ? 18 : 11;
            content.AppendLine($"/F1 {fontSize} Tf");
            content.AppendLine($"({EscapePdfText(line)}) Tj");
            first = false;
        }

        content.AppendLine("ET");
        var stream = content.ToString();
        var objects = new[]
        {
            "<< /Type /Catalog /Pages 2 0 R >>",
            "<< /Type /Pages /Kids [3 0 R] /Count 1 >>",
            "<< /Type /Page /Parent 2 0 R /MediaBox [0 0 612 792] /Resources << /Font << /F1 4 0 R >> >> /Contents 5 0 R >>",
            "<< /Type /Font /Subtype /Type1 /BaseFont /Helvetica >>",
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
        {
            writer.WriteLine($"{offset:0000000000} 00000 n ");
        }

        writer.WriteLine("trailer");
        writer.WriteLine($"<< /Size {objects.Length + 1} /Root 1 0 R >>");
        writer.WriteLine("startxref");
        writer.WriteLine(xrefOffset);
        writer.WriteLine("%%EOF");
        writer.Flush();

        return output.ToArray();
    }

    private static string EscapePdfText(string value) =>
        value.Replace("\\", "\\\\").Replace("(", "\\(").Replace(")", "\\)");
}
