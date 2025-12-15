﻿// This is an independent project of an individual developer. Dear PVS-Studio, please check it.
// PVS-Studio Static Code Analyzer for C, C++, C#, and Java: https://pvs-studio.com

using System.Diagnostics;
using System.Text;
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
public class ParserConfigurationModuleImpl(ActionType actionType, string path = "ParserConfiguration.txt") : ICoreModule
{
    private bool _isInitialized;

    public void InitParser(IParser parser)
    {
        if (_isInitialized)
            return;

        _isInitialized = true;
        
        if (actionType == ActionType.DumpConfiguration)
        {
            DumpConfiguration(parser);
        }
        else
        {
            LoadConfiguration(parser);
        }
    }

    private void DumpConfiguration(IParser parser)
    {
        try
        {
            var configurationText = SerializeConfiguration(parser);
            File.WriteAllText(path, configurationText);
            Debug.WriteLine($"Parser configuration dumped to {Path.GetFullPath(path)}");
        }
        catch (Exception ex)
        {
            Debug.WriteLine($"Failed to dump parser configuration: {ex.Message}");
            throw;
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
            LoadConfigurationFromText(parser, configText);
            
            Debug.WriteLine($"Parser configuration loaded from {Path.GetFullPath(path)}");
        }
        catch (Exception ex)
        {
            Debug.WriteLine($"Failed to load parser configuration: {ex.Message}");
            throw new InvalidOperationException($"Failed to load parser configuration: {ex.Message}", ex);
        }
    }

    private string SerializeConfiguration(IParser parser)
    {
        var builder = new StringBuilder();
        builder.AppendLine("# Parser Configuration Dump");
        builder.AppendLine("# Generated: " + DateTime.Now.ToString("yyyy-MM-dd HH:mm:ss"));
        builder.AppendLine("# Format: <priority>|<type_full_name>|<instance_hash>|<ast_node_type>");
        builder.AppendLine("# Instance hash helps identify different instances of the same type");
        builder.AppendLine();
        
        // Собираем все существующие NodeCreators
        var allCreators = new List<(float Priority, IAstNodeCreator Creator)>();
        
        foreach (var level in parser.Configuration.NodeCreators)
        {
            foreach (var creator in level.Value)
            {
                allCreators.Add((level.Key, creator));
            }
        }
        
        // Группируем по типу для генерации уникальных идентификаторов
        var typeGroups = allCreators.GroupBy(x => x.Creator.GetType()).ToList();
        var instanceIdentifiers = new Dictionary<IAstNodeCreator, string>();
        
        foreach (var group in typeGroups)
        {
            var creators = group.ToList();
            if (creators.Count == 1)
            {
                instanceIdentifiers[creators[0].Creator] = "0";
            }
            else
            {
                for (int i = 0; i < creators.Count; i++)
                {
                    instanceIdentifiers[creators[i].Creator] = i.ToString();
                }
            }
        }
        
        // Записываем в файл
        foreach (var (priority, creator) in allCreators.OrderBy(x => x.Priority))
        {
            var type = creator.GetType();
            var instanceId = instanceIdentifiers[creator];
            var astNodeType = creator.AstNodeType?.ToString() ?? "null";
            
            builder.AppendLine($"{priority:F2}|{type.FullName}|{instanceId}|{astNodeType}");
        }
        
        return builder.ToString();
    }

    private void LoadConfigurationFromText(IParser parser, string configText)
    {
        // Собираем все существующие NodeCreators в список
        var allExistingCreators = new List<IAstNodeCreator>();
        var existingCreatorsByType = new Dictionary<Type, List<IAstNodeCreator>>();
        
        foreach (var level in parser.Configuration.NodeCreators)
        {
            foreach (var creator in level.Value)
            {
                allExistingCreators.Add(creator);
                var type = creator.GetType();
                if (!existingCreatorsByType.ContainsKey(type))
                    existingCreatorsByType[type] = new List<IAstNodeCreator>();
                existingCreatorsByType[type].Add(creator);
            }
        }
        
        // Парсим конфигурационный файл
        var lines = configText.Split('\n');
        var newConfiguration = new List<(float Priority, IAstNodeCreator Creator)>();
        var usedCreators = new HashSet<IAstNodeCreator>();
        
        foreach (var line in lines)
        {
            var trimmedLine = line.Trim();
            
            // Пропускаем пустые строки и комментарии
            if (string.IsNullOrEmpty(trimmedLine) || trimmedLine.StartsWith('#'))
                continue;
            
            var parts = trimmedLine.Split('|', 4);
            if (parts.Length < 3)
            {
                Debug.WriteLine($"Warning: Invalid format in line: {trimmedLine}");
                continue;
            }
            
            if (!float.TryParse(parts[0].Trim(), out float priority))
            {
                Debug.WriteLine($"Warning: Invalid priority in line: {trimmedLine}");
                continue;
            }
            
            var typeName = parts[1].Trim();
            var instanceId = parts.Length > 2 ? parts[2].Trim() : "0";
            var astNodeTypeStr = parts.Length > 3 ? parts[3].Trim() : "";
            
            // Находим соответствующий тип
            var type = Type.GetType(typeName);
            if (type == null)
            {
                // Пробуем найти тип в загруженных сборках
                type = AppDomain.CurrentDomain.GetAssemblies()
                    .SelectMany(a => a.GetTypes())
                    .FirstOrDefault(t => t.FullName == typeName);
            }
            
            if (type == null)
            {
                Debug.WriteLine($"Warning: Type not found: {typeName}");
                continue;
            }
            
            // Получаем список экземпляров этого типа
            if (!existingCreatorsByType.TryGetValue(type, out var typeCreators) || typeCreators.Count == 0)
            {
                Debug.WriteLine($"Warning: No instances found for type: {typeName}");
                continue;
            }
            
            // Выбираем конкретный экземпляр
            IAstNodeCreator? selectedCreator = null;
            
            if (typeCreators.Count == 1)
            {
                selectedCreator = typeCreators[0];
            }
            else
            {
                // Пытаемся сопоставить по instanceId
                if (int.TryParse(instanceId, out int index) && index >= 0 && index < typeCreators.Count)
                {
                    selectedCreator = typeCreators[index];
                }
                else
                {
                    // Если не нашли по индексу, пробуем найти по AstNodeType
                    if (!string.IsNullOrEmpty(astNodeTypeStr))
                    {
                        selectedCreator = typeCreators.FirstOrDefault(c => 
                            c.AstNodeType?.ToString() == astNodeTypeStr);
                    }
                    
                    // Если всё еще не нашли, берем первый доступный
                    if (selectedCreator == null)
                    {
                        selectedCreator = typeCreators.FirstOrDefault(c => !usedCreators.Contains(c));
                    }
                }
            }
            
            if (selectedCreator == null || usedCreators.Contains(selectedCreator))
            {
                Debug.WriteLine($"Warning: Could not find available creator for type: {typeName}");
                continue;
            }
            
            newConfiguration.Add((priority, selectedCreator));
            usedCreators.Add(selectedCreator);
        }
        
        // Добавляем оставшиеся (не упомянутые в файле) креаторы с их исходными приоритетами
        // Но сначала нужно восстановить исходные приоритеты
        var originalPriorities = new Dictionary<IAstNodeCreator, float>();
        foreach (var level in parser.Configuration.NodeCreators)
        {
            foreach (var creator in level.Value)
            {
                originalPriorities[creator] = level.Key;
            }
        }
        
        foreach (var creator in allExistingCreators)
        {
            if (!usedCreators.Contains(creator))
            {
                var priority = originalPriorities.GetValueOrDefault(creator, 0f);
                newConfiguration.Add((priority, creator));
            }
        }
        
        // Очищаем старую конфигурацию и добавляем новую
        parser.Configuration.NodeCreators.Clear();
        
        foreach (var (priority, creator) in newConfiguration)
        {
            parser.Configuration.NodeCreators.Add(priority, creator);
        }
    }

    /// <summary>
    /// Валидирует конфигурационный файл без применения изменений
    /// </summary>
    public bool ValidateConfigurationFile()
    {
        try
        {
            if (!File.Exists(path))
                return false;
            
            var configText = File.ReadAllText(path);
            
            // Просто проверяем, что файл можно распарсить
            var parser = new BasicParser.BasicParserImpl();
            LoadConfigurationFromText(parser, configText);
            return true;
        }
        catch
        {
            return false;
        }
    }
}