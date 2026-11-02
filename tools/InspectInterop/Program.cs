using System.Reflection.Metadata;
using System.Reflection.PortableExecutable;

if (args.Length == 0)
{
    Console.Error.WriteLine("Usage: InspectInterop <Assembly-CSharp.dll> [TypeName ...]");
    return 1;
}

var targets = args.Length > 1 ? args.Skip(1).ToHashSet(StringComparer.Ordinal) :
    new HashSet<string>(new[] { "uItemPickPanel", "MainGameManager", "PlayerCtrl", "PartnerCtrl",
        "StorageData", "PlayerData", "uFieldPanel", "CommonMessageWindowManager", "uCommonMessageWindow" });
var found = new HashSet<string>();
// Read PE metadata only: do not load or execute assemblies from the game.
using var stream = File.OpenRead(args[0]);
using var pe = new PEReader(stream);
var reader = pe.GetMetadataReader();
foreach (var handle in reader.TypeDefinitions)
{
    var type = reader.GetTypeDefinition(handle);
    var name = reader.GetString(type.Name);
    if (!targets.Contains(name)) continue;
    found.Add(name);
    Console.WriteLine($"TYPE {name}");
    foreach (var methodHandle in type.GetMethods())
    {
        var method = reader.GetMethodDefinition(methodHandle);
        var parameters = method.GetParameters().Select(reader.GetParameter)
            .Where(p => p.SequenceNumber > 0).Select(p => reader.GetString(p.Name));
        Console.WriteLine($"METHOD {reader.GetString(method.Name)}({string.Join(", ", parameters)})");
    }
}
foreach (var missing in targets.Except(found)) Console.Error.WriteLine($"Type absent: {missing}");
return targets.Except(found).Any() ? 2 : 0;
