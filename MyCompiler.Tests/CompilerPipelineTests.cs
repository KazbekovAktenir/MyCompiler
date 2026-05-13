using MyCompiler.Generation;
using MyCompiler.Lexing;
using MyCompiler.Parsing;
using MyCompiler.Semantic;
using Xunit;

namespace MyCompiler.Tests;

public class CompilerPipelineTests
{
    private const string Sample = """
        переменная x = 10
        переменная y = 20
        если (x < y) то
            вывести("ok")
        конец
        """;

    [Fact]
    public void Semantic_Allows_declared_variables_in_condition()
    {
        var root = Parse(Sample);
        var ex = Record.Exception(() => SemanticAnalyzer.Analyze(root));
        Assert.Null(ex);
    }

    [Fact]
    public void Python_emits_if_and_print()
    {
        var root = Parse(Sample);
        SemanticAnalyzer.Analyze(root);
        var py = new PythonCodeGenerator().Generate(root);
        Assert.Contains("if x < y:", py);
        Assert.Contains("print(", py);
    }

    [Fact]
    public void Java_emits_main_and_braces()
    {
        var root = Parse(Sample);
        SemanticAnalyzer.Analyze(root);
        var java = new JavaCodeGenerator().Generate(root);
        Assert.Contains("public static void main", java);
        Assert.Contains("System.out.println", java);
    }

    [Fact]
    public void Semantic_undefined_variable_throws()
    {
        var code = "переменная a = b + 1\n";
        var root = Parse(code);
        Assert.Throws<SemanticException>(() => SemanticAnalyzer.Analyze(root));
    }

    private static Parsing.AST.ProgramNode Parse(string code)
    {
        var lexer = new Lexer(code);
        var parser = new Parser(lexer.Tokenize());
        return parser.Parse();
    }
}
