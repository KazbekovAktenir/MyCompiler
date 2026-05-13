using System.Collections.Generic;

namespace MyCompiler.Parsing.AST;

public class FunctionNode : Node
{
    public string Name { get; set; } = "";
    public List<string> Parameters { get; set; } = new List<string>();
    public List<Node> Body { get; set; } = new List<Node>();
}