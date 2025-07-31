using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;

public static class NodeRegistry
{
    public class NodeEntry
    {
        public Type Type;
        public string MenuPath;
    }
    
    public static List<NodeEntry> LoadNodes(Type type)
    {
        return AppDomain.CurrentDomain.GetAssemblies()
            .SelectMany(a => a.GetTypes())
            .Where(t => type.IsAssignableFrom(t) && !t.IsAbstract)
            .Select(t =>
            {
                var attr = t.GetCustomAttribute<NodeMenuAttribute>();
                return new NodeEntry
                {
                    Type = t,
                    MenuPath = attr?.Path ?? $"Create/{t.Name}"
                };
            })
            .ToList();
    }
}