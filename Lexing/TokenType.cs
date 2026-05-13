namespace MyCompiler.Lexing;

public enum TokenType
{
    Variable,
    If,
    Then,
    Else,
    End,
    LoopWhile,
    LoopFrom,
    To,
    Print,
    Function,
    Return,
    Input,

    Identifier,
    Number,
    String,
    Assign,
    Plus,
    LPAREN,
    RPAREN,
    Less,
    Comma,
    NewLine,
    EOF
}
