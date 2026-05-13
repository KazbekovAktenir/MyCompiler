using System.Collections.Generic;
using System.Text.RegularExpressions;

namespace MyCompiler.Lexing;

public class Lexer
{
    private readonly string _input;

    private static readonly Dictionary<string, TokenType> Keywords = new()
    {
        { "переменная", TokenType.Variable },
        { "если", TokenType.If },
        { "то", TokenType.Then },
        { "иначе", TokenType.Else },
        { "конец", TokenType.End },
        { "до", TokenType.To },
        { "вывести", TokenType.Print },
        { "функция", TokenType.Function },
        { "вернуть", TokenType.Return },
        { "ввести", TokenType.Input }
    };

    public Lexer(string input)
    {
        _input = input;
    }

    public List<Token> Tokenize()
    {
        var tokenRegex = new Regex(
            @"([а-яА-Яa-zA-Z_][а-яА-Яa-zA-Z0-9_]*)|(\d+)|(\=)|(\+)|(\<)|(\()|(\))|(\""[^""]*"")|(,)|(\r?\n)|([ \t]+)",
            RegexOptions.Compiled);

        var raw = new List<Token>();
        foreach (Match match in tokenRegex.Matches(_input))
        {
            string value = match.Value;
            if (value.Length > 0 && value[0] is ' ' or '\t')
                continue;

            if (value is "\n" or "\r\n")
            {
                raw.Add(new Token(TokenType.NewLine, ""));
                continue;
            }

            if (Keywords.TryGetValue(value, out var kw))
            {
                raw.Add(new Token(kw, value));
                continue;
            }

            if (char.IsDigit(value[0]))
            {
                raw.Add(new Token(TokenType.Number, value));
                continue;
            }

            if (value.StartsWith('"'))
            {
                raw.Add(new Token(TokenType.String, value.Trim('"')));
                continue;
            }

            if (Regex.IsMatch(value, @"^[а-яА-Яa-zA-Z_]"))
            {
                raw.Add(new Token(TokenType.Identifier, value));
                continue;
            }

            switch (value)
            {
                case "=":
                    raw.Add(new Token(TokenType.Assign, value));
                    break;
                case "+":
                    raw.Add(new Token(TokenType.Plus, value));
                    break;
                case "<":
                    raw.Add(new Token(TokenType.Less, value));
                    break;
                case "(":
                    raw.Add(new Token(TokenType.LPAREN, value));
                    break;
                case ")":
                    raw.Add(new Token(TokenType.RPAREN, value));
                    break;
                case ",":
                    raw.Add(new Token(TokenType.Comma, value));
                    break;
                default:
                    throw new LexerException($"Неизвестный символ или лексема: '{value}'");
            }
        }

        MergeLoopKeywords(raw);
        raw.Add(new Token(TokenType.EOF, ""));
        return raw;
    }

    /// <summary>Склеивает «цикл пока» и «цикл от» в составные токены.</summary>
    private static void MergeLoopKeywords(List<Token> tokens)
    {
        for (int i = 0; i < tokens.Count - 1; i++)
        {
            if (tokens[i].Type != TokenType.Identifier || tokens[i].Value != "цикл")
                continue;

            if (tokens[i + 1].Type == TokenType.Identifier && tokens[i + 1].Value == "пока")
            {
                tokens[i] = new Token(TokenType.LoopWhile, "цикл пока");
                tokens.RemoveAt(i + 1);
                continue;
            }

            if (tokens[i + 1].Type == TokenType.Identifier && tokens[i + 1].Value == "от")
            {
                tokens[i] = new Token(TokenType.LoopFrom, "цикл от");
                tokens.RemoveAt(i + 1);
            }
        }
    }
}
