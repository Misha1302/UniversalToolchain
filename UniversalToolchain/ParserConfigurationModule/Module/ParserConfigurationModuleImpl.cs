using System.Globalization;

namespace ParserConfigurationModule.Module;

/// <summary>
///     Dumps or reapplies parser creator priorities. Loading is strict and transactional:
///     malformed or stale entries fail before the live parser configuration is modified.
/// </summary>
/// <param name="actionType">Dump or read configuration.</param>
/// <param name="path">Configuration file path.</param>
public sealed class ParserConfigurationModuleImpl(ActionType actionType, string path = "ParserConfiguration.txt") : IFrontendCoreModule
{
    private const string FormatVersion = "2";
    public void InitParser(IParser parser)
    {
        parser = parser.ArgNotNull();
        switch (actionType)
        {
            case ActionType.DumpConfiguration:
                DumpConfiguration(parser);
                break;
            case ActionType.ReadConfiguration:
                LoadConfiguration(parser);
                break;
            default:
                Thrower.InvalidOpEx($"Unknown action type: {actionType}");
                break;
        }
    }

    private static IReadOnlyList<CreatorSnapshot> CollectCreators(IParser parser)
    {
        var snapshots = new List<CreatorSnapshot>();
        var indexesByType = new Dictionary<Type, int>();

        foreach (var level in parser.Configuration.NodeCreators)
        {
            foreach (var creator in level.Value)
            {
                var type = creator.GetType();
                var instanceIndex = indexesByType.GetValueOrDefault(type);
                indexesByType[type] = instanceIndex + 1;
                snapshots.Add(new CreatorSnapshot(level.Key, creator, GetTypeId(type), instanceIndex));
            }
        }

        return snapshots;
    }

    private static string GetTypeId(Type type) =>
        type.FullName ?? throw new InvalidOperationException($"Parser creator type '{type}' has no stable full name.");

    private void DumpConfiguration(IParser parser)
    {
        try
        {
            var configurationText = ConfigurationDumper.Dump(CollectCreators(parser));
            var fullPath = Path.GetFullPath(path);
            var directory = Path.GetDirectoryName(fullPath)!;
            Directory.CreateDirectory(directory);

            var temporaryPath = Path.Combine(directory, $".{Path.GetFileName(fullPath)}.{Guid.NewGuid():N}.tmp");
            try
            {
                File.WriteAllText(temporaryPath, configurationText, new UTF8Encoding(false));
                File.Move(temporaryPath, fullPath, true);
            }
            finally
            {
                if (File.Exists(temporaryPath))
                    File.Delete(temporaryPath);
            }

            Debug.WriteLine($"Parser configuration dumped to {fullPath}");
        }
        catch (Exception exception)
        {
            throw new InvalidOperationException($"Failed to dump parser configuration to '{path}'.", exception);
        }
    }

    private void LoadConfiguration(IParser parser)
    {
        try
        {
            if (!File.Exists(path))
            {
                Debug.WriteLine($"Configuration file not found: {path}. Using default configuration.");
                return;
            }

            var configText = File.ReadAllText(path);
            var current = CollectCreators(parser);
            var reordered = ConfigurationLoader.Build(configText, current);

            // Mutate the live parser only after the complete file has been validated.
            parser.Configuration.NodeCreators.Clear();
            foreach (var item in reordered)
                parser.Configuration.NodeCreators.Add(item.Priority, item.Creator);

            Debug.WriteLine($"Parser configuration loaded from {Path.GetFullPath(path)}");
        }
        catch (Exception exception)
        {
            throw new InvalidOperationException($"Failed to load parser configuration from '{path}'.", exception);
        }
    }

    private static class ConfigurationDumper
    {
        public static string Dump(IReadOnlyList<CreatorSnapshot> creators)
        {
            var lines = new List<string>
            {
                "# Parser Configuration Dump",
                $"# Format-Version: {FormatVersion}",
                "# Format: <priority>|<registered_type_full_name>|<instance_index>|<ast_node_type>",
                "# Entries are resolved only against creators already registered in this parser.",
                string.Empty
            };

            lines.AddRange(creators.Select(static item => string.Join(
                '|',
                item.Priority.ToString("R", CultureInfo.InvariantCulture),
                item.TypeId,
                item.InstanceIndex.ToString(CultureInfo.InvariantCulture),
                item.Creator.AstNodeType.ToString())));

            return string.Join(Environment.NewLine, lines);
        }
    }

    private static class ConfigurationLoader
    {
        public static IReadOnlyList<CreatorSnapshot> Build(
            string configText,
            IReadOnlyList<CreatorSnapshot> current)
        {
            configText = configText.ArgNotNull();
            current = current.ArgNotNull();

            var lines = ParseConfigurationLines(configText);
            var candidatesByType = current
                .GroupBy(static item => item.TypeId, StringComparer.Ordinal)
                .ToDictionary(static group => group.Key, static group => group.OrderBy(x => x.InstanceIndex).ToArray(), StringComparer.Ordinal);
            var usedCreators = new HashSet<IAstNodeCreator>(ReferenceEqualityComparer.Instance);
            var result = new List<CreatorSnapshot>(current.Count);

            foreach (var line in lines)
            {
                if (!candidatesByType.TryGetValue(line.TypeId, out var candidates))
                {
                    throw new FormatException(
                        $"Line {line.LineNumber} refers to parser creator type '{line.TypeId}', " +
                        "but no such creator is registered in the current parser composition.");
                }

                if (line.InstanceIndex < 0 || line.InstanceIndex >= candidates.Length)
                {
                    throw new FormatException(
                        $"Line {line.LineNumber} refers to instance {line.InstanceIndex} of '{line.TypeId}', " +
                        $"but the current composition contains {candidates.Length} instance(s).");
                }

                var selected = candidates[line.InstanceIndex];
                if (!usedCreators.Add(selected.Creator))
                    throw new FormatException($"Line {line.LineNumber} selects parser creator '{line.TypeId}' more than once.");

                var actualAstNodeType = selected.Creator.AstNodeType.ToString();
                if (!string.IsNullOrEmpty(line.AstNodeType) &&
                    !string.Equals(line.AstNodeType, actualAstNodeType, StringComparison.Ordinal))
                {
                    throw new FormatException(
                        $"Line {line.LineNumber} expects AST node type '{line.AstNodeType}' for '{line.TypeId}', " +
                        $"but the registered creator reports '{actualAstNodeType}'.");
                }

                result.Add(selected with { Priority = line.Priority });
            }

            // Partial order files remain supported: unmentioned creators retain their original priority and order.
            result.AddRange(current.Where(item => !usedCreators.Contains(item.Creator)));
            return result;
        }

        private static IReadOnlyList<ConfigLine> ParseConfigurationLines(string configText)
        {
            var result = new List<ConfigLine>();
            var lineNumber = 0;

            foreach (var rawLine in configText.Split('\n'))
            {
                lineNumber++;
                var line = rawLine.Trim();
                if (line.Length == 0 || line.StartsWith('#'))
                    continue;

                var parts = line.Split('|', 4);
                if (parts.Length != 4)
                    throw new FormatException($"Line {lineNumber} must contain exactly four pipe-separated fields.");

                if (!float.TryParse(parts[0].Trim(), NumberStyles.Float, CultureInfo.InvariantCulture, out var priority) ||
                    float.IsNaN(priority) ||
                    float.IsInfinity(priority))
                {
                    throw new FormatException($"Line {lineNumber} contains invalid invariant priority '{parts[0]}'.");
                }

                var typeId = parts[1].Trim();
                if (typeId.Length == 0)
                    throw new FormatException($"Line {lineNumber} contains an empty creator type identifier.");

                if (!int.TryParse(parts[2].Trim(), NumberStyles.None, CultureInfo.InvariantCulture, out var instanceIndex))
                    throw new FormatException($"Line {lineNumber} contains invalid instance index '{parts[2]}'.");

                result.Add(new ConfigLine(lineNumber, priority, typeId, instanceIndex, parts[3].Trim()));
            }

            return result;
        }
    }

    private sealed record CreatorSnapshot(
        float Priority,
        IAstNodeCreator Creator,
        string TypeId,
        int InstanceIndex);

    private sealed record ConfigLine(
        int LineNumber,
        float Priority,
        string TypeId,
        int InstanceIndex,
        string AstNodeType);
}
