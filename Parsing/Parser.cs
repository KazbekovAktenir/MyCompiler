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

    // Список всех ошибок
    public List<string> Errors { get; } = new();

    public Parser(List<Token> tokens)
    {
        _tokens = tokens;
    }

    private Token Current =>
        _position < _tokens.Count
            ? _tokens[_position]
            : _tokens[^1];

    private void SkipNewLines()
    {
        while (Current.Type == TokenType.NewLine)
            _position++;
    }

    // Безопасная проверка токена
    private bool Match(TokenType type)
    {
        SkipNewLines();

        if (Current.Type != type)
        {
            Errors.Add(
                $"Синтаксическая ошибка: ожидался {type}, " +
                $"получено {Current.Type} ('{Current.Value}')"
            );

            Synchronize();
            return false;
        }

        _position++;
        return true;
    }

    // Получение значения текущего токена
    private string Consume(TokenType type)
    {
        SkipNewLines();

        if (Current.Type != type)
        {
            Errors.Add(
                $"Синтаксическая ошибка: ожидался {type}, " +
                $"получено {Current.Type} ('{Current.Value}')"
            );

            Synchronize();
            return "";
        }

        string value = Current.Value;
        _position++;
        return value;
    }

    // Восстановление после ошибки
    private void Synchronize()
    {
        while (Current.Type != TokenType.NewLine &&
               Current.Type != TokenType.EOF)
        {
            _position++;
        }

        if (Current.Type == TokenType.NewLine)
            _position++;
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

            var stmt = ParseStatement();

            if (stmt != null)
                program.Nodes.Add(stmt);
        }

        return program;
    }

    private Node? ParseStatement()
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
            _ => HandleUnexpectedToken()
        };
    }

    private Node? HandleUnexpectedToken()
    {
        Errors.Add(
            $"Неожиданный токен: {Current.Type} ('{Current.Value}')"
        );

        Synchronize();
        return null;
    }

    private VarDeclNode? ParseVariableDeclaration()
    {
        if (!Match(TokenType.Variable))
            return null;

        string name = Consume(TokenType.Identifier);

        if (!Match(TokenType.Assign))
            return null;

        string value = ParseExpressionUntilLineEnd();

        return new VarDeclNode
        {
            Name = name,
            Value = value
        };
    }

    private PrintNode? ParsePrint()
    {
        if (!Match(TokenType.Print))
            return null;

        if (!Match(TokenType.LPAREN))
            return null;

        string content = ParseBalancedUntilClosingParen();

        return new PrintNode
        {
            Content = content
        };
    }

    private InputNode? ParseInput()
    {
        if (!Match(TokenType.Input))
            return null;

        if (!Match(TokenType.LPAREN))
            return null;

        string name = Consume(TokenType.Identifier);

        if (!Match(TokenType.RPAREN))
            return null;

        return new InputNode
        {
            VariableName = name
        };
    }

    private ReturnNode? ParseReturn()
    {
        if (!Match(TokenType.Return))
            return null;

        string expr = ParseExpressionUntilLineEnd();

        return new ReturnNode
        {
            Expression = expr
        };
    }

    private IfNode? ParseIf()
    {
        if (!Match(TokenType.If))
            return null;

        if (!Match(TokenType.LPAREN))
            return null;

        string condition = ParseBalancedUntilClosingParen();

        if (!Match(TokenType.Then))
            return null;

        var node = new IfNode
        {
            Condition = condition
        };

        SkipNewLines();

        while (Current.Type != TokenType.Else &&
               Current.Type != TokenType.End &&
               Current.Type != TokenType.EOF)
        {
            var stmt = ParseStatement();

            if (stmt != null)
                node.ThenBody.Add(stmt);

            SkipNewLines();
        }

        if (Current.Type == TokenType.Else)
        {
            Match(TokenType.Else);

            SkipNewLines();

            while (Current.Type != TokenType.End &&
                   Current.Type != TokenType.EOF)
            {
                var stmt = ParseStatement();

                if (stmt != null)
                    node.ElseBody.Add(stmt);

                SkipNewLines();
            }
        }

        if (!Match(TokenType.End))
            return null;

        return node;
    }

    private WhileNode? ParseWhile()
    {
        if (!Match(TokenType.LoopWhile))
            return null;

        if (!Match(TokenType.LPAREN))
            return null;

        string condition = ParseBalancedUntilClosingParen();

        var node = new WhileNode
        {
            Condition = condition
        };

        SkipNewLines();

        while (Current.Type != TokenType.End &&
               Current.Type != TokenType.EOF)
        {
            var stmt = ParseStatement();

            if (stmt != null)
                node.Body.Add(stmt);

            SkipNewLines();
        }

        if (!Match(TokenType.End))
            return null;

        return node;
    }

    private ForNode? ParseFor()
    {
        if (!Match(TokenType.LoopFrom))
            return null;

        string iterator = "i";
        string fromExpr = "";

        if (Current.Type == TokenType.Identifier &&
            _position + 1 < _tokens.Count &&
            _tokens[_position + 1].Type == TokenType.Assign)
        {
            iterator = Consume(TokenType.Identifier);

            if (!Match(TokenType.Assign))
                return null;

            fromExpr = ParseExpressionUntilTo();
        }
        else
        {
            fromExpr = ParseExpressionUntilTo();
        }

        if (!Match(TokenType.To))
            return null;

        string toExpr = ParseExpressionUntilLineEnd();

        var node = new ForNode
        {
            Iterator = iterator,
            From = fromExpr,
            To = toExpr
        };

        SkipNewLines();

        while (Current.Type != TokenType.End &&
               Current.Type != TokenType.EOF)
        {
            var stmt = ParseStatement();

            if (stmt != null)
                node.Body.Add(stmt);

            SkipNewLines();
        }

        if (!Match(TokenType.End))
            return null;

        return node;
    }

    private FunctionNode? ParseFunction()
    {
        if (!Match(TokenType.Function))
            return null;

        string name = Consume(TokenType.Identifier);

        if (!Match(TokenType.LPAREN))
            return null;

        var parameters = new List<string>();

        SkipNewLines();

        if (Current.Type != TokenType.RPAREN)
        {
            parameters.Add(
                Consume(TokenType.Identifier)
            );

            while (Current.Type == TokenType.Comma)
            {
                Match(TokenType.Comma);

                parameters.Add(
                    Consume(TokenType.Identifier)
                );
            }
        }

        if (!Match(TokenType.RPAREN))
            return null;

        var node = new FunctionNode
        {
            Name = name,
            Parameters = parameters
        };

        SkipNewLines();

        while (Current.Type != TokenType.End &&
               Current.Type != TokenType.EOF)
        {
            var stmt = ParseStatement();

            if (stmt != null)
                node.Body.Add(stmt);

            SkipNewLines();
        }

        if (!Match(TokenType.End))
            return null;

        return node;
    }

    private string ParseExpressionUntilTo()
    {
        var sb = new StringBuilder();

        while (Current.Type != TokenType.To &&
               Current.Type != TokenType.EOF)
        {
            sb.Append(FormatToken(Current)).Append(" ");
            _position++;
        }

        return sb.ToString().Trim();
    }

    private string ParseExpressionUntilLineEnd()
    {
        var sb = new StringBuilder();

        while (Current.Type != TokenType.NewLine &&
               Current.Type != TokenType.EOF)
        {
            sb.Append(FormatToken(Current)).Append(" ");
            _position++;
        }

        if (Current.Type == TokenType.NewLine)
            _position++;

        return sb.ToString().Trim();
    }

    private string ParseBalancedUntilClosingParen()
    {
        var sb = new StringBuilder();

        int depth = 1;

        while (Current.Type != TokenType.EOF)
        {
            if (Current.Type == TokenType.LPAREN)
            {
                depth++;
            }
            else if (Current.Type == TokenType.RPAREN)
            {
                depth--;

                if (depth == 0)
                {
                    _position++;
                    break;
                }
            }

            sb.Append(FormatToken(Current)).Append(" ");
            _position++;
        }

        if (depth != 0)
        {
            Errors.Add(
                "Синтаксическая ошибка: незакрытая скобка."
            );
        }

        return sb.ToString().Trim();
    }

    private static string FormatToken(Token token)
    {
        return token.Type switch
        {
            TokenType.String => $"\"{token.Value}\"",
            TokenType.Comma => ",",
            _ => token.Value
        };
    }
}
