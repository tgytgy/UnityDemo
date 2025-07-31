using System;

[AttributeUsage(AttributeTargets.Class, Inherited = false)]
public class NodeMenuAttribute : Attribute
{
    public string Path;
    public NodeMenuAttribute(string path)
    {
        Path = path;
    }
}