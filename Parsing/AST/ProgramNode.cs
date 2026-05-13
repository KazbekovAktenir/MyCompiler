using System.Collections.Generic;

namespace MyCompiler.Parsing.AST
{
    public class ProgramNode : Node
    {
        public List<Node> Nodes { get; set; } = new List<Node>();
    }
}