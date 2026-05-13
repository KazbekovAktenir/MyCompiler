using System;
using System.Collections.Generic;
using System.Text;
using MyCompiler.Lexing;
using MyCompiler.Parsing.AST;

namespace MyCompiler.Parsing;

public class ParserException : Exception
{
    public ParserException(string message) : base(message) { }
}

public class Parser
{
    private readonly List<Token> _tokens;
    private int _position;

    public Parser(List<Token> tokens)
    {
        _tokens = tokens;
    }

    private Token Current => _position < _tokens.Count ? _tokens[_position] : _tokens[^1];

    private void SkipNewLines()
    {
        while (Current.Type == TokenType.NewLine)
            _position++;
    }

    private Token Match(TokenType type)
    {
        SkipNewLines();
        if (Current.Type != type)
            throw new ParserException($"Ожидался {type}, получено {Current.Type} ('{Current.Value}').");
        var t = Current;
        _position++;
        return t;
    }

    public ProgramNode Parse()
    {
        var program = new ProgramNode();
        SkipNewLines();
        while (Current.Type != TokenType.EOF)
        {
            SkipNewLines();
            if (Current.Type == TokenType.EOF)
                break;
            var node = ParseStatement();
            program.Nodes.Add(node);
        }
        return program;
    }

    private Node ParseStatement()
    {
        SkipNewLines();
        return Current.Type switch
        {
            TokenType.Variable => ParseVariableDeclaration(),
            TokenType.Print => ParsePrint(),
            TokenType.If => ParseIf(),
            TokenType.LoopWhile => ParseWhile(),
            TokenType.LoopFrom => ParseFor(),
            TokenType.Function => ParseFunction(),
            TokenType.Return => ParseReturn(),
            TokenType.Input => ParseInput(),
            _ => throw new ParserException($"Неожиданный токен: {Current.Type} ('{Current.Value}').")
        };
    }

    private VarDeclNode ParseVariableDeclaration()
    {
        Match(TokenType.Variable);
        var name = Match(TokenType.Identifier).Value;
        Match(TokenType.Assign);
        var value = ParseExpressionUntilLineEnd();
        return new VarDeclNode { Name = name, Value = value };
    }

    private PrintNode ParsePrint()
    {
        Match(TokenType.Print);
        Match(TokenType.LPAREN);
        var content = ParseBalancedUntilClosingParen();
        return new PrintNode { Content = content };
    }

    private InputNode ParseInput()
    {
        Match(TokenType.Input);
        Match(TokenType.LPAREN);
        var name = Match(TokenType.Identifier).Value;
        Match(TokenType.RPAREN);
        return new InputNode { VariableName = name };
    }

    private ReturnNode ParseReturn()
    {
        Match(TokenType.Return);
        var expr = ParseExpressionUntilLineEnd();
        return new ReturnNode { Expression = expr };
    }

    private IfNode ParseIf()
    {
        Match(TokenType.If);
        Match(TokenType.LPAREN);
        var cond = ParseBalancedUntilClosingParen();
        Match(TokenType.Then);

        var node = new IfNode { Condition = cond };
        SkipNewLines();
        while (Current.Type is not (TokenType.Else or TokenType.End))
        {
            if (Current.Type == TokenType.NewLine)
            {
                _position++;
                continue;
            }
            node.ThenBody.Add(ParseStatement());
            SkipNewLines();
        }

        if (Current.Type == TokenType.Else)
        {
            Match(TokenType.Else);
            SkipNewLines();
            while (Current.Type != TokenType.End)
            {
                if (Current.Type == TokenType.NewLine)
                {
                    _position++;
                    continue;
                }
                node.ElseBody.Add(ParseStatement());
                SkipNewLines();
            }
        }

        Match(TokenType.End);
        return node;
    }

    private WhileNode ParseWhile()
    {
        Match(TokenType.LoopWhile);
        Match(TokenType.LPAREN);
        var cond = ParseBalancedUntilClosingParen();
        var node = new WhileNode { Condition = cond };
        SkipNewLines();
        while (Current.Type != TokenType.End)
        {
            if (Current.Type == TokenType.NewLine)
            {
                _position++;
                continue;
            }
            node.Body.Add(ParseStatement());
            SkipNewLines();
        }
        Match(TokenType.End);
        return node;
    }

    private ForNode ParseFor()
    {
        Match(TokenType.LoopFrom);
        SkipNewLines();

        string iterator;
        string fromExpr;

        if (Current.Type == TokenType.Identifier &&
            _position + 1 < _tokens.Count &&
            _tokens[_position + 1].Type == TokenType.Assign)
        {
            iterator = Match(TokenType.Identifier).Value;
            Match(TokenType.Assign);
            fromExpr = ParseExpressionUntilTo();
        }
        else
        {
            iterator = "i";
            fromExpr = ParseExpressionUntilTo();
        }

        Match(TokenType.To);
        var toExpr = ParseExpressionUntilLineEnd();
        var node = new ForNode { Iterator = iterator, From = fromExpr, To = toExpr };
        SkipNewLines();
        while (Current.Type != TokenType.End)
        {
            if (Current.Type == TokenType.NewLine)
            {
                _position++;
                continue;
            }
            node.Body.Add(ParseStatement());
            SkipNewLines();
        }
        Match(TokenType.End);
        return node;
    }

    private FunctionNode ParseFunction()
    {
        Match(TokenType.Function);
        var name = Match(TokenType.Identifier).Value;
        Match(TokenType.LPAREN);
        var parameters = new List<string>();
        SkipNewLines();
        if (Current.Type != TokenType.RPAREN)
        {
            parameters.Add(Match(TokenType.Identifier).Value);
            while (Current.Type == TokenType.Comma)
            {
                Match(TokenType.Comma);
                SkipNewLines();
                parameters.Add(Match(TokenType.Identifier).Value);
            }
        }
        Match(TokenType.RPAREN);

        var node = new FunctionNode { Name = name, Parameters = parameters };
        SkipNewLines();
        while (Current.Type != TokenType.End)
        {
            if (Current.Type == TokenType.NewLine)
            {
                _position++;
                continue;
            }
            node.Body.Add(ParseStatement());
            SkipNewLines();
        }
        Match(TokenType.End);
        return node;
    }

    /// <summary>Выражение до ключевого слова «до» (для цикла).</summary>
    private string ParseExpressionUntilTo()
    {
        var sb = new StringBuilder();
        int depth = 0;
        while (Current.Type != TokenType.EOF)
        {
            if (depth == 0 && Current.Type == TokenType.To)
                break;
            AppendToken(sb, ref depth);
        }
        return sb.ToString().Trim();
    }

    private string ParseExpressionUntilLineEnd()
    {
        var sb = new StringBuilder();
        int depth = 0;
        while (Current.Type != TokenType.EOF)
        {
            if (depth == 0 && Current.Type == TokenType.NewLine)
                break;
            if (depth == 0 && Current.Type == TokenType.EOF)
                break;
            AppendToken(sb, ref depth);
        }
        if (Current.Type == TokenType.NewLine)
            _position++;
        return sb.ToString().Trim();
    }

    /// <summary>Содержимое в скобках с учётом вложенности (условие, аргументы вывести).</summary>
    private string ParseBalancedUntilClosingParen()
    {
        var sb = new StringBuilder();
        int depth = 1;
        while (Current.Type != TokenType.EOF)
        {
            if (Current.Type == TokenType.LPAREN)
            {
                depth++;
                sb.Append('(');
                _position++;
                continue;
            }
            if (Current.Type == TokenType.RPAREN)
            {
                depth--;
                _position++;
                if (depth == 0)
                    break;
                sb.Append(')');
                continue;
            }
            AppendToken(sb, ref depth);
        }
        if (depth != 0)
            throw new ParserException("Незакрытая скобка в выражении.");
        return sb.ToString().Trim();
    }

    private void AppendToken(StringBuilder sb, ref int depth)
    {
        switch (Current.Type)
        {
            case TokenType.LPAREN:
                depth++;
                sb.Append('(');
                _position++;
                break;
            case TokenType.RPAREN:
                depth--;
                sb.Append(')');
                _position++;
                break;
            case TokenType.String:
                sb.Append('"').Append(Current.Value).Append('"');
                _position++;
                break;
            case TokenType.Comma:
                sb.Append(", ");
                _position++;
                break;
            default:
                if (sb.Length > 0 && sb[^1] is not ('(' or ' ' or ','))
                    sb.Append(' ');
                sb.Append(Current.Value);
                _position++;
                break;
        }
    }
}
