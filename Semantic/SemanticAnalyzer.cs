using System.Collections.Generic;
using System.Linq;
using System.Text.RegularExpressions;
using MyCompiler.Parsing.AST;

namespace MyCompiler.Semantic;

public class SemanticException : Exception
{
    public SemanticException(string message) : base(message) { }
}

/// <summary>Проверка использования необъявленных переменных (по тексту выражений).</summary>
public static class SemanticAnalyzer
{
    private static readonly HashSet<string> PseudoKeywords =
    [
        "переменная", "если", "то", "иначе", "конец", "цикл", "пока", "от", "до",
        "вывести", "функция", "вернуть", "ввести"
    ];

    public static void Analyze(ProgramNode program)
    {
        var scope = new HashSet<string>(StringComparer.Ordinal);
        foreach (var node in program.Nodes)
        {
            if (node is FunctionNode f)
                scope.Add(f.Name);
        }
        Walk(program.Nodes, scope);
    }

    private static void Walk(IEnumerable<Node> nodes, HashSet<string> scope)
    {
        foreach (var node in nodes)
        {
            switch (node)
            {
                case VarDeclNode v:
                    foreach (var id in ExtractIdentifiers(v.Value))
                        EnsureDeclared(id, scope);
                    scope.Add(v.Name);
                    break;
                case PrintNode p:
                    foreach (var id in ExtractIdentifiers(p.Content))
                        EnsureDeclared(id, scope);
                    break;
                case InputNode i:
                    scope.Add(i.VariableName);
                    break;
                case ReturnNode r:
                    foreach (var id in ExtractIdentifiers(r.Expression))
                        EnsureDeclared(id, scope);
                    break;
                case IfNode iff:
                    foreach (var id in ExtractIdentifiers(iff.Condition))
                        EnsureDeclared(id, scope);
                    WalkBlock(iff.ThenBody, scope);
                    WalkBlock(iff.ElseBody, scope);
                    break;
                case WhileNode w:
                    foreach (var id in ExtractIdentifiers(w.Condition))
                        EnsureDeclared(id, scope);
                    WalkBlock(w.Body, scope);
                    break;
                case ForNode f:
                    foreach (var id in ExtractIdentifiers(f.From))
                        EnsureDeclared(id, scope);
                    foreach (var id in ExtractIdentifiers(f.To))
                        EnsureDeclared(id, scope);
                    WalkBlock(f.Body, ChildScope(scope, new[] { f.Iterator }));
                    break;
                case FunctionNode fn:
                    var fnScope = ChildScope(scope, fn.Parameters);
                    Walk(fn.Body, fnScope);
                    break;
            }
        }
    }

    private static void WalkBlock(List<Node> body, HashSet<string> parent)
    {
        var scope = new HashSet<string>(parent, StringComparer.Ordinal);
        Walk(body, scope);
    }

    private static HashSet<string> ChildScope(HashSet<string> parent, IEnumerable<string> extra)
    {
        var s = new HashSet<string>(parent, StringComparer.Ordinal);
        foreach (var e in extra)
            s.Add(e);
        return s;
    }

    private static void EnsureDeclared(string id, HashSet<string> scope)
    {
        if (char.IsDigit(id[0]))
            return;
        if (PseudoKeywords.Contains(id))
            return;
        if (!scope.Contains(id))
            throw new SemanticException($"Необъявленная переменная или функция: «{id}».");
    }

    private static IEnumerable<string> ExtractIdentifiers(string expr)
    {
        if (string.IsNullOrWhiteSpace(expr))
            yield break;

        var noStrings = Regex.Replace(expr, "\"[^\"]*\"", " ");
        foreach (Match m in Regex.Matches(noStrings, @"[а-яА-Яa-zA-Z_][а-яА-Яa-zA-Z0-9_]*"))
        {
            var id = m.Value;
            if (char.IsDigit(id[0]))
                continue;
            if (PseudoKeywords.Contains(id))
                continue;
            yield return id;
        }
    }
}
