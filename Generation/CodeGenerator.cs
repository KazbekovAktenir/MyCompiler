using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Text.RegularExpressions;
using MyCompiler.Parsing.AST;

namespace MyCompiler.Generation;

public interface ICodeGenerator
{
    string FileExtension { get; }
    string Generate(ProgramNode program);
}

public sealed class PythonCodeGenerator : ICodeGenerator
{
    public string FileExtension => "py";

    public string Generate(ProgramNode program)
    {
        var (functions, rest) = Partition(program);
        var sb = new StringBuilder();
        foreach (var fn in functions)
            sb.Append(GenerateFunction(fn, 0));
        foreach (var node in rest)
            sb.Append(GenerateNode(node, 0));
        return sb.ToString();
    }

    private static (List<FunctionNode> functions, List<Node> rest) Partition(ProgramNode program)
    {
        var functions = program.Nodes.OfType<FunctionNode>().ToList();
        var rest = program.Nodes.Where(n => n is not FunctionNode).ToList();
        return (functions, rest);
    }

    private string GenerateFunction(FunctionNode func, int indent)
    {
        var sb = new StringBuilder();
        var ind = Indent(indent);
        sb.AppendLine($"{ind}def {func.Name}({string.Join(", ", func.Parameters)}):");
        foreach (var child in func.Body)
            sb.Append(GenerateNode(child, indent + 1));
        return sb.ToString();
    }

    private string GenerateNode(Node node, int indent)
    {
        var ind = Indent(indent);
        return node switch
        {
            VarDeclNode v => $"{ind}{v.Name} = {v.Value}\n",
            PrintNode p => $"{ind}print({PythonPrintExpression(p.Content)})\n",
            InputNode i => $"{ind}{i.VariableName} = input()\n",
            ReturnNode r => $"{ind}return {r.Expression}\n",
            IfNode iff => GenerateIf(iff, indent),
            ForNode f => GenerateFor(f, indent),
            WhileNode w => GenerateWhile(w, indent),
            FunctionNode => "", // уже сверху
            _ => ""
        };
    }

    private string GenerateIf(IfNode i, int indent)
    {
        var sb = new StringBuilder();
        var ind = Indent(indent);
        sb.AppendLine($"{ind}if {i.Condition}:");
        foreach (var c in i.ThenBody)
            sb.Append(GenerateNode(c, indent + 1));
        if (i.ElseBody.Count > 0)
        {
            sb.AppendLine($"{ind}else:");
            foreach (var c in i.ElseBody)
                sb.Append(GenerateNode(c, indent + 1));
        }
        return sb.ToString();
    }

    private string GenerateFor(ForNode f, int indent)
    {
        var sb = new StringBuilder();
        var ind = Indent(indent);
        var toExpr = TryParseInt(f.To, out var toInt)
            ? $"{toInt + 1}"
            : $"({f.To}) + 1";
        sb.AppendLine($"{ind}for {f.Iterator} in range({f.From}, {toExpr}):");
        foreach (var c in f.Body)
            sb.Append(GenerateNode(c, indent + 1));
        return sb.ToString();
    }

    private string GenerateWhile(WhileNode w, int indent)
    {
        var sb = new StringBuilder();
        var ind = Indent(indent);
        sb.AppendLine($"{ind}while {w.Condition}:");
        foreach (var c in w.Body)
            sb.Append(GenerateNode(c, indent + 1));
        return sb.ToString();
    }

    private static string Indent(int level) => new string(' ', level * 4);

    private static bool TryParseInt(string s, out int value) =>
        int.TryParse(s.Trim(), out value);

    /// <summary>Оборачивает операнды в str() для конкатенации со строкой в Python 3.</summary>
    private static string PythonPrintExpression(string expr)
    {
        if (!expr.Contains('+'))
            return expr;
        var parts = expr.Split(new[] { '+' }, StringSplitOptions.None);
        var sb = new StringBuilder();
        for (var i = 0; i < parts.Length; i++)
        {
            if (i > 0)
                sb.Append(" + ");
            var p = parts[i].Trim();
            if (p.StartsWith('"'))
                sb.Append(p);
            else if (Regex.IsMatch(p, @"^-?\d+$"))
                sb.Append(p);
            else
                sb.Append($"str({p})");
        }
        return sb.ToString();
    }
}

public sealed class JavaCodeGenerator : ICodeGenerator
{
    public string FileExtension => "java";

    public string Generate(ProgramNode program)
    {
        var (functions, rest) = Partition(program);
        var sb = new StringBuilder();
        sb.AppendLine("public class PseudocodeTranslator {");
        sb.AppendLine("    public static void main(String[] args) {");
        foreach (var node in rest)
            sb.Append(GenerateStatement(node, 2));
        sb.AppendLine("    }");
        foreach (var fn in functions)
            sb.Append(GenerateMethod(fn));
        sb.AppendLine("}");
        return sb.ToString();
    }

    private static (List<FunctionNode> functions, List<Node> rest) Partition(ProgramNode program)
    {
        var functions = program.Nodes.OfType<FunctionNode>().ToList();
        var rest = program.Nodes.Where(n => n is not FunctionNode).ToList();
        return (functions, rest);
    }

    private string GenerateMethod(FunctionNode func)
    {
        var sb = new StringBuilder();
        var plist = string.Join(", ", func.Parameters.Select(p => $"int {p}"));
        sb.AppendLine($"    public static int {func.Name}({plist}) {{");
        foreach (var child in func.Body)
            sb.Append(GenerateStatement(child, 2));
        sb.AppendLine("    }");
        sb.AppendLine();
        return sb.ToString();
    }

    private string GenerateStatement(Node node, int indent)
    {
        var ind = new string(' ', indent * 4);
        return node switch
        {
            VarDeclNode v => $"{ind}{JavaVarDecl(v)};\n",
            PrintNode p => $"{ind}System.out.println({p.Content});\n",
            InputNode i => $"{ind}// ввести не отображено в консольной версии Java — используйте Scanner\n",
            ReturnNode r => $"{ind}return {r.Expression};\n",
            IfNode iff => GenerateIf(iff, indent),
            ForNode f => GenerateFor(f, indent),
            WhileNode w => GenerateWhile(w, indent),
            _ => ""
        };
    }

    private static string JavaVarDecl(VarDeclNode v)
    {
        var t = Regex.IsMatch(v.Value.Trim(), @"^-?\d+$") ? "int" : "var";
        return $"{t} {v.Name} = {v.Value}";
    }

    private string GenerateIf(IfNode i, int indent)
    {
        var sb = new StringBuilder();
        var ind = new string(' ', indent * 4);
        sb.AppendLine($"{ind}if ({i.Condition}) {{");
        foreach (var c in i.ThenBody)
            sb.Append(GenerateStatement(c, indent + 1));
        if (i.ElseBody.Count > 0)
        {
            sb.AppendLine($"{ind}}} else {{");
            foreach (var c in i.ElseBody)
                sb.Append(GenerateStatement(c, indent + 1));
        }
        sb.AppendLine($"{ind}}}");
        return sb.ToString();
    }

    private string GenerateFor(ForNode f, int indent)
    {
        var sb = new StringBuilder();
        var ind = new string(' ', indent * 4);
        sb.AppendLine($"{ind}for (int {f.Iterator} = {f.From}; {f.Iterator} <= {f.To}; {f.Iterator}++) {{");
        foreach (var c in f.Body)
            sb.Append(GenerateStatement(c, indent + 1));
        sb.AppendLine($"{ind}}}");
        return sb.ToString();
    }

    private string GenerateWhile(WhileNode w, int indent)
    {
        var sb = new StringBuilder();
        var ind = new string(' ', indent * 4);
        sb.AppendLine($"{ind}while ({w.Condition}) {{");
        foreach (var c in w.Body)
            sb.Append(GenerateStatement(c, indent + 1));
        sb.AppendLine($"{ind}}}");
        return sb.ToString();
    }
}
