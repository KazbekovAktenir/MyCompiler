using System;
using System.IO;
using MyCompiler.Generation;
using MyCompiler.Lexing;
using MyCompiler.Parsing;
using MyCompiler.Parsing.AST;
using MyCompiler.Semantic;

namespace MyCompiler;

internal static class Program
{
    private static void PrintUsage()
    {
        Console.WriteLine(
            "Псевдокод → Python / Java\n\n" +
            "Использование:\n" +
            "  dotnet run -- [файл.pc] [--lang python|java|all]\n\n" +
            "По умолчанию: examples/sample.pc и --lang all\n");
    }

    public static int Main(string[] args)
    {
        try
        {
            string inputPath = Path.Combine("examples", "sample.pc");
            var lang = "all";

            for (var i = 0; i < args.Length; i++)
            {
                if (args[i] == "--lang" && i + 1 < args.Length)
                {
                    lang = args[++i].ToLowerInvariant();
                    continue;
                }
                if (!args[i].StartsWith('-'))
                    inputPath = args[i];
            }

            if (!File.Exists(inputPath))
            {
                Console.Error.WriteLine($"Файл не найден: {inputPath}");
                PrintUsage();
                return 1;
            }

            var code = File.ReadAllText(inputPath);
            var lexer = new Lexer(code);
            var tokens = lexer.Tokenize();
            var parser = new Parser(tokens);
            var root = parser.Parse();

            if (parser.Errors.Count > 0)
            {
                Console.Error.WriteLine("Найдены синтаксические ошибки:");
                foreach (var error in parser.Errors)
                    Console.Error.WriteLine(error);
                return 1;
            }

            var semanticErrors = SemanticAnalyzer.AnalyzeAll(root);
            if (semanticErrors.Count > 0)
            {
                Console.Error.WriteLine("Найдены семантические ошибки:");
                foreach (var error in semanticErrors)
                    Console.Error.WriteLine(error);
                return 1;
            }

            var baseName = Path.GetFileNameWithoutExtension(inputPath);
            var outDir = Path.GetDirectoryName(Path.GetFullPath(inputPath)) ?? ".";

            void WriteLang(ICodeGenerator gen)
            {
                var text = gen.Generate(root);
                var fileName = gen is JavaCodeGenerator
                    ? "PseudocodeTranslator.java"
                    : $"{baseName}.{gen.FileExtension}";
                var outPath = Path.Combine(outDir, fileName);
                File.WriteAllText(outPath, text);
                Console.WriteLine($"Записано: {outPath}");
            }

            switch (lang)
            {
                case "python":
                    WriteLang(new PythonCodeGenerator());
                    break;
                case "java":
                    WriteLang(new JavaCodeGenerator());
                    break;
                case "all":
                    WriteLang(new PythonCodeGenerator());
                    WriteLang(new JavaCodeGenerator());
                    break;
                default:
                    Console.Error.WriteLine($"Неизвестный --lang: {lang}");
                    return 1;
            }

            return 0;
        }
        catch (LexerException ex)
        {
            Console.Error.WriteLine($"Лексическая ошибка: {ex.Message}");
            return 1;
        }
        catch (ParserException ex)
        {
            Console.Error.WriteLine($"Синтаксическая ошибка: {ex.Message}");
            return 1;
        }
        catch (SemanticException ex)
        {
            Console.Error.WriteLine($"Семантическая ошибка: {ex.Message}");
            return 1;
        }
        catch (Exception ex)
        {
            Console.Error.WriteLine($"Ошибка: {ex.Message}");
            return 1;
        }
    }
}
