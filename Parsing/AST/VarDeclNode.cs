namespace MyCompiler.Parsing.AST
{
    public class VarDeclNode : Node
    {
        public string Name { get; set; }
        public string Value { get; set; } // Для простоты пока используем строку
    }
}