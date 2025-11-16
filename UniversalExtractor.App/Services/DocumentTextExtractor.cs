// Copyright (c) 2024.
// This file is part of UniversalExtractor and is licensed under the GNU General Public License v3.0.
// See the LICENSE file distributed with this work for additional information.

using System;
using System.Collections.Generic;
using System.IO;
using System.IO.Compression;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Documents;
using DocumentFormat.OpenXml.Packaging;
using DocumentFormat.OpenXml.Wordprocessing;
using UglyToad.PdfPig;
using XText = System.Xml.Linq.XText;
using XDocument = System.Xml.Linq.XDocument;

namespace UniversalExtractor.App.Services;

public static class DocumentTextExtractor
{
    private static readonly HashSet<string> SupportedExtensions = new(StringComparer.OrdinalIgnoreCase)
    {
        ".pdf",
        ".docx",
        ".docm",
        ".dotx",
        ".dotm",
        ".txt",
        ".csv",
        ".odt",
        ".rtf",
        ".html",
        ".htm",
        ".md",
        ".markdown"
    };

    public static IReadOnlyCollection<string> AllowedExtensions => SupportedExtensions;

    public static bool IsSupported(string filePath)
    {
        var extension = Path.GetExtension(filePath);
        return !string.IsNullOrEmpty(extension) && SupportedExtensions.Contains(extension);
    }

    public static async Task<string> ReadAsTextAsync(string filePath)
    {
        if (!IsSupported(filePath))
        {
            throw new NotSupportedException("Unsupported file type.");
        }

        var extension = Path.GetExtension(filePath).ToLowerInvariant();
        return extension switch
        {
            ".txt" or ".csv" or ".md" or ".markdown" or ".html" or ".htm" => await File.ReadAllTextAsync(filePath),
            ".rtf" => ReadRtf(filePath),
            ".pdf" => await Task.Run(() => ReadPdf(filePath)),
            ".docx" or ".docm" or ".dotx" or ".dotm" => await Task.Run(() => ReadWordOpenXml(filePath)),
            ".odt" => await ReadOdtAsync(filePath),
            _ => throw new NotSupportedException($"The extension \"{extension}\" is not supported.")
        };
    }

    private static string ReadRtf(string filePath)
    {
        var flowDocument = new FlowDocument();
        var range = new TextRange(flowDocument.ContentStart, flowDocument.ContentEnd);
        using var fileStream = File.OpenRead(filePath);
        range.Load(fileStream, DataFormats.Rtf);
        return range.Text;
    }

    private static string ReadPdf(string filePath)
    {
        var builder = new StringBuilder();
        using var document = PdfDocument.Open(filePath);
        foreach (var page in document.GetPages())
        {
            builder.AppendLine(page.Text);
        }

        return builder.ToString();
    }

    private static string ReadWordOpenXml(string filePath)
    {
        using var document = WordprocessingDocument.Open(filePath, false);
        var body = document.MainDocumentPart?.Document?.Body;
        if (body == null)
        {
            return string.Empty;
        }

        var texts = body.Descendants<Text>().Select(t => t.Text);
        return string.Join(Environment.NewLine, texts);
    }

    private static async Task<string> ReadOdtAsync(string filePath)
    {
        await using var fileStream = File.OpenRead(filePath);
        using var archive = new ZipArchive(fileStream, ZipArchiveMode.Read, leaveOpen: false);
        var entry = archive.GetEntry("content.xml") ?? throw new InvalidOperationException("content.xml not found inside ODT archive.");
        await using var entryStream = entry.Open();
        using var reader = new StreamReader(entryStream, Encoding.UTF8, leaveOpen: false);
        var xml = await reader.ReadToEndAsync();
        var document = XDocument.Parse(xml);
        var builder = new StringBuilder();

        foreach (var textNode in document.DescendantNodes().OfType<XText>())
        {
            var value = textNode.Value;
            if (!string.IsNullOrWhiteSpace(value))
            {
                builder.AppendLine(value.Trim());
            }
        }

        return builder.ToString();
    }
}
