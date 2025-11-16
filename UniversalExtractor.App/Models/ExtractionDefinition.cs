// Copyright (c) 2024.
// This file is part of UniversalExtractor and is licensed under the GNU General Public License v3.0.
// See the LICENSE file distributed with this work for additional information.

using System;
using System.Collections.Generic;
using System.Text.RegularExpressions;

namespace UniversalExtractor.App.Models;

public class ExtractionDefinition
{
    private Regex? _regex;

    private ExtractionDefinition(string name, string pattern, RegexOptions? options = null, string? description = null)
    {
        Name = name;
        Pattern = pattern;
        Options = options ?? (RegexOptions.Compiled | RegexOptions.IgnoreCase | RegexOptions.Multiline);
        Description = description ?? string.Empty;
    }

    public string Name { get; }
    public string Pattern { get; }
    public RegexOptions Options { get; }
    public string Description { get; }

    public static IReadOnlyList<ExtractionDefinition> All { get; } = new[]
    {
        new ExtractionDefinition("E-mail address", @"\b[A-Z0-9._%+-]+@[A-Z0-9.-]+\.[A-Z]{2,}\b"),
        new ExtractionDefinition("Phone number", @"\b(?:\+?\d{1,3}[\s.-]?)?(?:\(?\d{2,4}\)?[\s.-]?){2,4}\d{2,4}\b", RegexOptions.Compiled | RegexOptions.Multiline),
        new ExtractionDefinition("Social network handles", @"(?<!\S)@[A-Za-z0-9._]{3,32}\b", RegexOptions.Compiled | RegexOptions.Multiline),
        new ExtractionDefinition("Dates", @"\b(?:\d{4}-\d{2}-\d{2}|\d{2}[\/.-]\d{2}[\/.-]\d{4})\b"),
        new ExtractionDefinition("Credit card number", @"\b(?:\d[ -]?){13,16}\b", RegexOptions.Compiled | RegexOptions.Multiline),
        new ExtractionDefinition("IBAN", @"\b[A-Z]{2}\d{2}[A-Z0-9]{8,30}\b"),
        new ExtractionDefinition("BIC/SWIFT", @"\b[A-Z]{4}[A-Z]{2}[A-Z0-9]{2}([A-Z0-9]{3})?\b"),
        new ExtractionDefinition("IPv4 addresses", @"\b(?:(?:25[0-5]|2[0-4]\d|[01]?\d\d?)\.){3}(?:25[0-5]|2[0-4]\d|[01]?\d\d?)\b", RegexOptions.Compiled | RegexOptions.Multiline),
        new ExtractionDefinition("IPv6", @"\b(?:[A-F0-9]{1,4}:){7}[A-F0-9]{1,4}\b|\b(?:[A-F0-9]{1,4}:){1,7}:|\b:(?:[A-F0-9]{1,4}:){1,7}[A-F0-9]{1,4}\b", RegexOptions.Compiled | RegexOptions.IgnoreCase | RegexOptions.Multiline),
        new ExtractionDefinition("MD5", @"\b[A-F0-9]{32}\b"),
        new ExtractionDefinition("SHA1", @"\b[A-F0-9]{40}\b"),
        new ExtractionDefinition("SHA256", @"\b[A-F0-9]{64}\b")
    };

    public IReadOnlyList<string> ExtractMatches(string input)
    {
        if (string.IsNullOrWhiteSpace(input))
        {
            return Array.Empty<string>();
        }

        var matches = Regex().Matches(input);
        var results = new List<string>(matches.Count);
        foreach (Match match in matches)
        {
            if (match.Success && !string.IsNullOrWhiteSpace(match.Value))
            {
                results.Add(match.Value.Trim());
            }
        }

        return results;
    }

    private Regex Regex() => _regex ??= new Regex(Pattern, Options);

    public override string ToString() => Name;
}
