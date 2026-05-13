using System.Collections.Generic;

namespace MyCompiler.Parsing.AST;

public class ForNode : Node
{
    public string Iterator { get; set; } = "";
    public string From { get; set; } = "";
    public string To { get; set; } = "";
    public List<Node> Body { get; set; } = new List<Node>();
}