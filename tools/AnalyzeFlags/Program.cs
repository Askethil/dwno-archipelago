using System.Security.Cryptography;
using System.Text;
using System.Text.Json;
using Dwno.FlagAnalysis;

try
{
    var options = new Dictionary<string, string>(StringComparer.Ordinal);
    var check = false;
    for (var i = 0; i < args.Length; i++)
    {
        if (args[i] == "--check") { check = true; continue; }
        if (args[i] is not ("--input" or "--output" or "--report") || i + 1 >= args.Length)
            throw new ArgumentException("Usage: AnalyzeFlags [--input world.json] [--output result.json] [--report report.md] [--check]");
        if (!options.TryAdd(args[i], args[++i])) throw new ArgumentException("Duplicate option");
    }
    var root = new DirectoryInfo(Directory.GetCurrentDirectory());
    while (root is not null && !File.Exists(Path.Combine(root.FullName, "data", "world-catalog.en.json"))) root = root.Parent;
    if (root is null) throw new DirectoryNotFoundException("Run from the repository or a subdirectory.");
    string PathFor(string key, string fallback) => Path.GetFullPath(options.TryGetValue(key, out var value)
        ? value : Path.Combine(root.FullName, fallback));
    var input = PathFor("--input", "data/world-catalog.en.json");
    var output = PathFor("--output", "data/flag-conditions.en.json");
    var report = PathFor("--report", "docs/flag-conditions.en.md");
    if (new[] { input, output, report }.Distinct(StringComparer.OrdinalIgnoreCase).Count() != 3)
        throw new ArgumentException("Input, JSON output and report must be separate files.");
    var bytes = File.ReadAllBytes(input);
    using var document = JsonDocument.Parse(Encoding.UTF8.GetString(bytes).TrimStart('\uFEFF'));
    var analysis = FlagAnalyzer.Analyze(document.RootElement, Path.GetFileName(input), Convert.ToHexString(SHA256.HashData(bytes)));
    var json = JsonSerializer.Serialize(analysis, new JsonSerializerOptions {
        WriteIndented = true, PropertyNamingPolicy = JsonNamingPolicy.CamelCase
    }).Replace("\r\n", "\n") + "\n";
    var markdown = FlagAnalyzer.Report(analysis);
    foreach (var (path, content) in new[] { (output, json), (report, markdown) })
    {
        if (check)
        {
            if (!File.Exists(path) || File.ReadAllText(path).Replace("\r\n", "\n") != content)
                throw new InvalidDataException($"Generated file differs: {path}");
        }
        else
        {
            Directory.CreateDirectory(Path.GetDirectoryName(path)!);
            File.WriteAllText(path, content, new UTF8Encoding(false));
        }
    }
    Console.WriteLine($"{(check ? "Verified" : "Generated")}: {analysis.Summary.FlagSets} flag sets; " +
        $"{analysis.Summary.FlagSetsWithContextLabels} context-labeled; {analysis.Summary.FlagSetsWithSourceNames} source names; " +
        $"{analysis.Summary.ResolvedReferences} resolved / {analysis.Summary.UnresolvedReferences} unresolved reference slots.");
    return 0;
}
catch (Exception ex)
{
    Console.Error.WriteLine("Flag analysis failed: " + ex.Message);
    return 1;
}
