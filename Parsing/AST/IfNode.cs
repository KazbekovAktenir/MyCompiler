using System.Collections.Generic;

namespace MyCompiler.Parsing.AST
{
    public class IfNode : Node
    {
        public string Condition { get; set; } // Например, "x < 20"
        public List<Node> ThenBody { get; set; } = new List<Node>();
        public List<Node> ElseBody { get; set; } = new List<Node>();
    }
}