using System.Collections.Generic;

namespace MyCompiler.Parsing.AST;

public class WhileNode : Node
{
    public string Condition { get; set; } = "";
    public List<Node> Body { get; set; } = new List<Node>();
}