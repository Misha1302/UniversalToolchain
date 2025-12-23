using System.Diagnostics;
using BasicCore;
using BasicCore.ParserWrapper;
using ExceptionsManager;

namespace ParserConfigurationModule;

/// <summary>
///     <para> You can fix parser-modules execution order like this: </para>
///     1. Dump parser configuration to file <br />
///     2. Rearrange modules by hand <br />
///     3. Read configuration from file <br />
///     <para> For correct work this module have to execute InitParser after all others plugins </para>
/// </summary>
/// <param name="actionType">dump or read configuration</param>
/// <param name="path">path to file</param>
public class ParserConfigurationModuleImpl(ActionType actionType, string path = "ParserConfiguration.txt") : IFrontendCoreModule
{
    private bool _isInitialized;

    public void InitParser(IParser parser)
    {
        if (_isInitialized) return;
        _isInitialized = true;

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

    private void DumpConfiguration(IParser parser)
    {
        try
        {
            var configurationText = new ConfigurationDumper(parser).Dump();
            File.WriteAllText(path, configurationText);
            Debug.WriteLine($"Parser configuration dumped to {Path.GetFullPath(path)}");
        }
        catch (Exception ex)
        {
            Debug.WriteLine($"Failed to dump parser configuration: {ex.Message}");
            Thrower.InvalidOpEx($"Failed to dump parser configuration: {ex.Message}");
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
            new ConfigurationLoader(parser).Load(configText);
            Debug.WriteLine($"Parser configuration loaded from {Path.GetFullPath(path)}");
        }
        catch (Exception ex)
        {
            Debug.WriteLine($"Failed to load parser configuration: {ex.Message}");
            Thrower.InvalidOpEx($"Failed to load parser configuration: {ex.Message}");
        }
    }

    // Вспомогательные классы для инкапсуляции логики
    private class ConfigurationDumper(IParser parser)
    {
        public string Dump()
        {
            var creators = CollectAllCreators();
            return GenerateDumpContent(creators);
        }

        private List<(float Priority, IAstNodeCreator Creator)> CollectAllCreators()
        {
            var result = new List<(float, IAstNodeCreator)>();

            foreach (var level in parser.Configuration.NodeCreators)
            {
                foreach (var creator in level.Value)
                {
                    result.Add((level.Key, creator));
                }
            }

            return result;
        }

        private string GenerateDumpContent(List<(float Priority, IAstNodeCreator Creator)> creators)
        {
            var instanceIdentifiers = CreateInstanceIdentifiers(creators);

            var lines = new List<string>
            {
                "# Parser Configuration Dump",
                $"# Generated: {DateTime.Now:yyyy-MM-dd HH:mm:ss}",
                "# Format: <priority>|<type_full_name>|<instance_hash>|<ast_node_type>",
                "# Instance hash helps identify different instances of the same type",
                ""
            };

            lines.AddRange(creators
                .OrderBy(x => x.Priority)
                .Select(x => FormatCreatorLine(x, instanceIdentifiers)));

            return string.Join(Environment.NewLine, lines);
        }

        private Dictionary<IAstNodeCreator, string> CreateInstanceIdentifiers(
            List<(float Priority, IAstNodeCreator Creator)> creators)
        {
            var result = new Dictionary<IAstNodeCreator, string>();

            foreach (var group in creators.GroupBy(x => x.Creator.GetType()))
            {
                var items = group.ToList();
                for (var i = 0; i < items.Count; i++)
                {
                    result[items[i].Creator] = i.ToString();
                }
            }

            return result;
        }

        private string FormatCreatorLine(
            (float Priority, IAstNodeCreator Creator) item,
            Dictionary<IAstNodeCreator, string> instanceIdentifiers)
        {
            var type = item.Creator.GetType();
            var instanceId = instanceIdentifiers[item.Creator];
            var astNodeType = item.Creator.AstNodeType.ToString();

            return $"{item.Priority:F2}|{type.FullName}|{instanceId}|{astNodeType}";
        }
    }

    private class ConfigurationLoader
    {
        private readonly IParser _parser;

        public ConfigurationLoader(IParser parser)
        {
            _parser = parser;
        }

        public void Load(string configText)
        {
            var lines = ParseConfigurationLines(configText);
            var newConfiguration = BuildNewConfiguration(lines);
            ApplyNewConfiguration(newConfiguration);
        }

        private List<ConfigLine> ParseConfigurationLines(string configText)
        {
            var lines = new List<ConfigLine>();
            var lineNumber = 0;

            foreach (var rawLine in configText.Split('\n'))
            {
                lineNumber++;
                var line = rawLine.Trim();

                if (string.IsNullOrEmpty(line) || line.StartsWith('#'))
                    continue;

                var parts = line.Split('|', 4);
                if (parts.Length < 3)
                {
                    Debug.WriteLine($"Warning: Invalid format in line {lineNumber}: {line}");
                    continue;
                }

                if (!float.TryParse(parts[0].Trim(), out var priority))
                {
                    Debug.WriteLine($"Warning: Invalid priority in line {lineNumber}: {line}");
                    continue;
                }

                lines.Add(new ConfigLine(
                    priority,
                    parts[1].Trim(),
                    parts.Length > 2 ? parts[2].Trim() : "0",
                    parts.Length > 3 ? parts[3].Trim() : ""
                ));
            }

            return lines;
        }

        private List<(float Priority, IAstNodeCreator Creator)> BuildNewConfiguration(List<ConfigLine> lines)
        {
            var existingCreators = CollectExistingCreators();
            var usedCreators = new HashSet<IAstNodeCreator>();
            var result = new List<(float Priority, IAstNodeCreator Creator)>();

            foreach (var line in lines)
            {
                var creator = FindMatchingCreator(line, existingCreators, usedCreators);
                if (creator != null)
                {
                    result.Add((line.Priority, creator));
                    usedCreators.Add(creator);
                }
            }

            // Add remaining creators with their original priorities
            AddRemainingCreators(result, usedCreators, existingCreators);
            return result;
        }

        private Dictionary<Type, List<IAstNodeCreator>> CollectExistingCreators()
        {
            var result = new Dictionary<Type, List<IAstNodeCreator>>();

            foreach (var level in _parser.Configuration.NodeCreators)
            {
                foreach (var creator in level.Value)
                {
                    var type = creator.GetType();
                    if (!result.ContainsKey(type))
                        result[type] = new List<IAstNodeCreator>();
                    result[type].Add(creator);
                }
            }

            return result;
        }

        private IAstNodeCreator? FindMatchingCreator(
            ConfigLine line,
            Dictionary<Type, List<IAstNodeCreator>> existingCreators,
            HashSet<IAstNodeCreator> usedCreators)
        {
            var type = FindType(line.TypeName);
            if (type == null || !existingCreators.TryGetValue(type, out var typeCreators))
                return null;

            return SelectCreator(line, typeCreators, usedCreators);
        }

        private Type? FindType(string typeName)
        {
            return Type.GetType(typeName) ??
                   AppDomain.CurrentDomain.GetAssemblies()
                       .SelectMany(a => a.GetTypes())
                       .FirstOrDefault(t => t.FullName == typeName);
        }

        private IAstNodeCreator? SelectCreator(
            ConfigLine line,
            List<IAstNodeCreator> typeCreators,
            HashSet<IAstNodeCreator> usedCreators)
        {
            // Try by instance ID
            if (int.TryParse(line.InstanceId, out var index) && index >= 0 && index < typeCreators.Count)
            {
                var creator = typeCreators[index];
                if (!usedCreators.Contains(creator))
                    return creator;
            }

            // Try by AstNodeType
            if (!string.IsNullOrEmpty(line.AstNodeType))
            {
                var creator = typeCreators.FirstOrDefault(c =>
                    !usedCreators.Contains(c) &&
                    c.AstNodeType.ToString() == line.AstNodeType);
                if (creator != null)
                    return creator;
            }

            // Fallback to first available
            return typeCreators.FirstOrDefault(c => !usedCreators.Contains(c));
        }

        private void AddRemainingCreators(
            List<(float Priority, IAstNodeCreator Creator)> result,
            HashSet<IAstNodeCreator> usedCreators,
            Dictionary<Type, List<IAstNodeCreator>> existingCreators)
        {
            var originalPriorities = GetOriginalPriorities();

            foreach (var typeCreators in existingCreators.Values)
            {
                foreach (var creator in typeCreators)
                {
                    if (!usedCreators.Contains(creator))
                    {
                        var priority = originalPriorities.GetValueOrDefault(creator, 0f);
                        result.Add((priority, creator));
                    }
                }
            }
        }

        private Dictionary<IAstNodeCreator, float> GetOriginalPriorities()
        {
            var result = new Dictionary<IAstNodeCreator, float>();

            foreach (var level in _parser.Configuration.NodeCreators)
            {
                foreach (var creator in level.Value)
                {
                    result[creator] = level.Key;
                }
            }

            return result;
        }

        private void ApplyNewConfiguration(List<(float Priority, IAstNodeCreator Creator)> newConfiguration)
        {
            _parser.Configuration.NodeCreators.Clear();

            foreach (var (priority, creator) in newConfiguration)
            {
                _parser.Configuration.NodeCreators.Add(priority, creator);
            }
        }

        private record ConfigLine(float Priority, string TypeName, string InstanceId, string AstNodeType);
    }
}