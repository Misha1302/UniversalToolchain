using UniversalToolchain.Dialects.Abstractions;

namespace UniversalToolchain.Dialects.Core;

internal static class DialectSemanticNormalization
{
    public static List<string> NormalizeActiveModules(
        IEnumerable<string> useModules,
        IEnumerable<string> excludeModules,
        List<DialectDiagnostic> diagnostics,
        string conflictCode)
    {
        var orderedUseModules = new List<string>();
        var useSet = new HashSet<string>(StringComparer.Ordinal);
        foreach (var module in useModules)
        {
            if (useSet.Add(module))
                orderedUseModules.Add(module);
        }

        var sortedExcludeModules = excludeModules
            .Distinct(StringComparer.Ordinal)
            .OrderBy(x => x, StringComparer.Ordinal)
            .ToList();
        var excludeSet = sortedExcludeModules.ToHashSet(StringComparer.Ordinal);

        foreach (var conflict in orderedUseModules.Where(excludeSet.Contains))
        {
            diagnostics.Add(new DialectDiagnostic(
                conflictCode,
                $"Module '{conflict}' cannot be both used and excluded.",
                DialectDiagnosticSeverity.Error));
        }

        return orderedUseModules.Where(x => !excludeSet.Contains(x)).ToList();
    }

    public static Dictionary<DialectBackendId, bool> NormalizeBackendRules<TDirective>(
        IReadOnlyList<TDirective> directives,
        Func<TDirective, DialectBackendId> targetSelector,
        Func<TDirective, bool> valueSelector,
        List<DialectDiagnostic> diagnostics,
        string contradictionCode)
    {
        var map = new Dictionary<DialectBackendId, bool>();

        foreach (var directive in directives)
        {
            var target = targetSelector(directive);
            var value = valueSelector(directive);

            if (map.TryGetValue(target, out var existing) && existing != value)
            {
                diagnostics.Add(new DialectDiagnostic(
                    contradictionCode,
                    $"Contradictory backend directives for '{DialectBackendSelectorText.ToText(target)}'.",
                    DialectDiagnosticSeverity.Error));
                continue;
            }

            if (!map.ContainsKey(target))
                map[target] = value;
        }

        return map;
    }

    public static List<IntrinsicBuildDirective> NormalizeIntrinsicRules<TDirective>(
        IReadOnlyList<TDirective> directives,
        Func<TDirective, string> nameSelector,
        Func<TDirective, DialectBackendSelector> targetSelector,
        Func<TDirective, bool> valueSelector,
        List<DialectDiagnostic> diagnostics,
        string contradictionCode)
    {
        var map = new Dictionary<(string Name, DialectBackendSelector Target), bool>();

        foreach (var directive in directives)
        {
            var key = (Name: nameSelector(directive), Target: targetSelector(directive));
            var value = valueSelector(directive);

            if (map.TryGetValue(key, out var existing) && existing != value)
            {
                diagnostics.Add(new DialectDiagnostic(
                    contradictionCode,
                    $"Contradictory intrinsic directives for '{key.Name}' on '{DialectBackendSelectorText.ToText(key.Target)}'.",
                    DialectDiagnosticSeverity.Error));
                continue;
            }

            if (!map.ContainsKey(key))
                map[key] = value;
        }

        return map.OrderBy(x => x.Key.Name, StringComparer.Ordinal)
            .ThenBy(x => x.Key.Target)
            .Select(x => new IntrinsicBuildDirective(x.Key.Name, x.Value, x.Key.Target))
            .ToList();
    }

    public static List<OptimizerBuildDirective> NormalizeOptimizerRules<TDirective>(
        IReadOnlyList<TDirective> directives,
        Func<TDirective, string> nameSelector,
        Func<TDirective, DialectBackendSelector> targetSelector,
        Func<TDirective, bool> valueSelector,
        List<DialectDiagnostic> diagnostics,
        string contradictionCode)
    {
        var map = new Dictionary<(string Name, DialectBackendSelector Target), bool>();

        foreach (var directive in directives)
        {
            var key = (Name: nameSelector(directive), Target: targetSelector(directive));
            var value = valueSelector(directive);

            if (map.TryGetValue(key, out var existing) && existing != value)
            {
                diagnostics.Add(new DialectDiagnostic(
                    contradictionCode,
                    $"Contradictory optimizer directives for '{key.Name}' on '{DialectBackendSelectorText.ToText(key.Target)}'.",
                    DialectDiagnosticSeverity.Error));
                continue;
            }

            if (!map.ContainsKey(key))
                map[key] = value;
        }

        return map.OrderBy(x => x.Key.Name, StringComparer.Ordinal)
            .ThenBy(x => x.Key.Target)
            .Select(x => new OptimizerBuildDirective(x.Key.Name, x.Value, x.Key.Target))
            .ToList();
    }

    public static List<string> ResolveOrder(
        IReadOnlyList<string> activeModules,
        IReadOnlyList<DialectOrderConstraint> constraints,
        List<DialectDiagnostic> diagnostics,
        string cycleCode,
        string cycleMessagePrefix,
        string? missingReferenceCode = null,
        string? missingReferenceMessagePrefix = null)
    {
        var nodes = new List<string>();
        var nodeSet = new HashSet<string>(StringComparer.Ordinal);
        foreach (var module in activeModules)
        {
            if (nodeSet.Add(module))
                nodes.Add(module);
        }

        var nodeIndices = nodes
            .Select((module, index) => new { module, index })
            .ToDictionary(x => x.module, x => x.index, StringComparer.Ordinal);

        var activeSet = nodes.ToHashSet(StringComparer.Ordinal);
        var edges = nodes.ToDictionary(x => x, _ => new HashSet<string>(StringComparer.Ordinal), StringComparer.Ordinal);
        var indegree = nodes.ToDictionary(x => x, _ => 0, StringComparer.Ordinal);

        foreach (var constraint in constraints)
        {
            if (!activeSet.Contains(constraint.SourceModule) || !activeSet.Contains(constraint.TargetModule))
            {
                if (!string.IsNullOrWhiteSpace(missingReferenceCode) && !string.IsNullOrWhiteSpace(missingReferenceMessagePrefix))
                    diagnostics.Add(new DialectDiagnostic(
                        missingReferenceCode,
                        $"{missingReferenceMessagePrefix}: '{constraint.SourceModule}' and '{constraint.TargetModule}'.",
                        DialectDiagnosticSeverity.Error));

                continue;
            }

            var (from, to) = constraint.Kind switch
            {
                DialectOrderConstraintKind.Before => (constraint.SourceModule, constraint.TargetModule),
                _ => (constraint.TargetModule, constraint.SourceModule)
            };

            if (!edges[from].Add(to))
                continue;

            indegree[to]++;
        }

        var queue = new SortedSet<(int Index, string Name)>();
        foreach (var node in nodes)
        {
            if (indegree[node] == 0)
                queue.Add((nodeIndices[node], node));
        }

        var ordered = new List<string>();

        while (queue.Count > 0)
        {
            var current = queue.Min;
            queue.Remove(current);
            ordered.Add(current.Name);

            foreach (var next in edges[current.Name])
            {
                indegree[next]--;
                if (indegree[next] == 0)
                    queue.Add((nodeIndices[next], next));
            }
        }

        if (ordered.Count == nodes.Count)
            return ordered;

        var cycleNodes = nodes.Except(ordered, StringComparer.Ordinal).OrderBy(x => x, StringComparer.Ordinal);
        diagnostics.Add(new DialectDiagnostic(
            cycleCode,
            $"{cycleMessagePrefix}: {string.Join(", ", cycleNodes)}.",
            DialectDiagnosticSeverity.Error));

        return [];
    }
}