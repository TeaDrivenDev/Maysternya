using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Globalization;
using System.Linq;
using System.Text;

namespace TeaDriven.Maysternya.SiiUnit.Parsing;

internal sealed class Lexer
{
    private static readonly ReadOnlyDictionary<string, TokenKind> Keywords;
    private static readonly ReadOnlyDictionary<char, TokenKind> Punctuation;
    private static readonly ReadOnlyCollection<char> HexDigits;

    private readonly string fileName;
    private readonly int length;
    private readonly ReadOnlyCollection<Lexeme> lexemes;
    internal readonly string Source;

    private readonly Stack<TextSpan> spans;
    private int column;
    private int index;
    private int line;

    static Lexer()
    {
        var punctuation =
            new Dictionary<char, TokenKind>
            {
                ['('] = TokenKind.LeftParen,
                [')'] = TokenKind.RightParen,
                ['['] = TokenKind.LeftSquare,
                [']'] = TokenKind.RightSquare,
                ['{'] = TokenKind.LeftBrace,
                ['}'] = TokenKind.RightBrace,
                [':'] = TokenKind.Colon,
                [';'] = TokenKind.SemiColon,
                ['.'] = TokenKind.Dot,
                [','] = TokenKind.Comma,
            };

        var keywords =
            new Dictionary<string, TokenKind>
            {
                ["true"] = TokenKind.True,
                ["false"] = TokenKind.False,
            };

        Keywords = new ReadOnlyDictionary<string, TokenKind>(keywords);
        Punctuation = new ReadOnlyDictionary<char, TokenKind>(punctuation);
        HexDigits = ['a', 'b', 'c', 'd', 'e', 'f', 'A', 'B', 'C', 'D', 'E', 'F'];
    }

    public Lexer(string source, string fileName)
    {
        this.fileName = fileName;
        this.Source = source;
        this.length = source.Length;
        this.spans = new Stack<TextSpan>();
        this.lexemes =
        [
            this.TryLexDirective,
            this.TryLexNumber,
            this.TryLexIdentifier,
            this.TryLexString,
            this.TryLexPunctuation,
        ];
    }

    private bool EndOfInput => this.index >= this.length;

    public ReadOnlyCollection<Token> Tokenize()
    {
        this.index = 0;
        this.line = 1;
        this.column = 1;

        var tokens = new List<Token>();
        while (!this.EndOfInput)
        {
            this.SkipWhile(Char.IsWhiteSpace);

            var current = this.Peek();
            if (this.SkipComments(current))
            {
                continue;
            }

            this.MarkStart();
            var token = default(Token);
            var success = this.lexemes.Any(lexeme => lexeme(current, out token));
            if (!success)
            {
                throw new SiiSyntaxException(
                    this.MarkEnd(),
                    $"Unexpected character '{current}' (0x{Convert.ToUInt16(current):X4})");
            }

            tokens.Add(token);
            this.spans.Pop();
        }

        this.MarkStart();
        var eof = this.MakeToken(TokenKind.EndOfInput, null);
        tokens.Add(eof);

        return tokens.AsReadOnly();
    }

    private void MarkStart()
    {
        var location = new Location(this.line, this.column, this.index);
        var span = new TextSpan(location, null);
        this.spans.Push(span);
    }

    private TextSpan MarkEnd()
    {
        var location = new Location(this.line, this.column, this.index);
        return this.spans.Pop().WithEnd(location);
    }

    private Token MakeToken(TokenKind kind, string text, object tag = null)
    {
        var span = this.MarkEnd();
        return new Token(text, kind, span, this.fileName, tag);
    }

    private bool SkipComments(char c)
    {
        return this.SkipLineComments(c) || this.SkipBlockComments(c);
    }

    private bool SkipLineComments(char c)
    {
        // skip both types of inline comments
        if (c != '#' && !this.IsNext("//"))
        {
            return false;
        }

        // Skip comment or directive until we hit a new line
        this.SkipWhile(ch => ch != '\n' && ch != '\r');
        return true;
    }

    private bool SkipBlockComments(char c)
    {
        if (!this.IsNext("/*"))
        {
            return false;
        }

        this.MarkStart();
        this.Skip(2);

        var closed = false;
        while (!this.EndOfInput)
        {
            if (this.TakeIfNext("*/"))
            {
                closed = true;
                break;
            }

            this.Take();
        }

        if (!closed)
        {
            throw new SiiSyntaxException(
                this.MarkEnd(),
                "Unexpected end-of-input (unclosed multi-line comment)");
        }

        this.spans.Pop();
        return true;
    }

    private bool TryLexDirective(char c, out Token token)
    {
        // If the next 3 characters are inc... its an enclude
        if (c is '@' && (this.Peek(-1) is '\r' || this.Peek(-1) is '\n'))
        {
            this.MarkStart();
            var text = this.TakeWhile(ch => ch != '\n' && ch != '\r');
            token = this.MakeToken(TokenKind.Directive, text);
            return true;
        }

        token = null;
        return false;
    }

    private bool TryLexIdentifier(char c, out Token token)
    {
        if (!Char.IsLetter(c) && c != '_' && c != '?')
        {
            token = null;
            return false;
        }

        this.MarkStart();
        var text = this.TakeWhile(ch => Char.IsLetterOrDigit(ch) || ch is '_' or '?');
        var kind = Keywords.GetValueOrDefault(text, TokenKind.Identifier);
        token = this.MakeToken(kind, text);
        return true;
    }

    private bool TryLexNumber(char c, out Token token)
    {
        if (c != '&' && c != '-' && !Char.IsDigit(c) && c != '_')
        {
            token = null;
            return false;
        }

        this.MarkStart();
        var text = default(string);
        if (c is '&')
        {
            this.Take();
            text = this.TakeWhile(ch => Char.IsDigit(ch) || HexDigits.Contains(ch));
            if (text.Length != 8)
            {
                throw new SiiSyntaxException(
                    this.MarkEnd(),
                    "Hexadecimal floating point numbers must be 8 characters");
            }

            token = this.MakeToken(TokenKind.Number, text, NumberFormat.HexFloat);
            return true;
        }

        var hasDecimal = false;
        var isNegative = false;
        var hasExponent = false;
        var forceTake = false;
        var format = NumberFormat.Integer;
        var tokenKind = TokenKind.Number;

        text =
            this.TakeWhile(
                delegate(char ch)
                {
                    if (forceTake)
                    {
                        forceTake = false;
                        return true;
                    }

                    var next = this.Peek(1);

                    if (tokenKind is TokenKind.Number)
                    {
                        if (Char.IsLetter(ch) || ch is '_' or '?')
                        {
                            if (ch is 'e' or 'E')
                            {
                                var second = this.Peek(2);
                                if (!Char.IsDigit(next)
                                    && !(next is '-' or '+' && Char.IsDigit(second)))
                                {
                                    // Try Identifier
                                    tokenKind = TokenKind.Identifier;
                                    return true;
                                }

                                if (hasExponent)
                                {
                                    throw new SiiSyntaxException(
                                        this.MarkEnd(),
                                        "Number already has exponent");
                                }

                                if (next is '-' or '+' && Char.IsDigit(second))
                                {
                                    forceTake = true;
                                }

                                format = NumberFormat.Float;
                                hasExponent = true;
                                return Char.IsDigit(next) || Char.IsDigit(second);
                            }

                            // Try Identifier
                            tokenKind = TokenKind.Identifier;
                            return true;
                        }

                        // Negative number?
                        if (ch is '-' && !isNegative && Char.IsDigit(next))
                        {
                            isNegative = true;
                            return true;
                        }

                        if (ch is '.' && Char.IsDigit(next))
                        {
                            if (hasDecimal)
                            {
                                throw new SiiSyntaxException(
                                    this.MarkEnd(),
                                    "Number already has a decimal point");
                            }

                            format = NumberFormat.Float;
                            hasDecimal = true;
                            return true;
                        }

                        return Char.IsDigit(ch);
                    }

                    return Char.IsLetterOrDigit(ch) || ch is '_' or '?';
                });

        token = this.MakeToken(tokenKind, text, format);
        return true;
    }

    private bool TryLexString(char c, out Token token)
    {
        if (c != '"')
        {
            token = null;
            return false;
        }

        this.MarkStart();
        this.Take();
        var closed = false;
        var builder = new StringBuilder();
        while (!this.EndOfInput)
        {
            var current = default(char);
            if ((current = this.Peek()) is '"')
            {
                this.Take();
                closed = true;
                break;
            }

            if (current is '\\')
            {
                if (this.EndOfInput)
                {
                    throw new SiiSyntaxException(
                        this.MarkEnd(),
                        "Unexpected end-of-input (unclosed string)");
                }

                var escape = this.HandleEscapeSequence(this.Take());
                builder.Append(escape);
                continue;
            }

            builder.Append(this.Take());
        }

        if (!closed)
        {
            throw new SiiSyntaxException(
                this.MarkEnd(),
                "Unexpected end-of-input (unclosed string)");
        }

        token = this.MakeToken(TokenKind.String, builder.ToString());
        return true;
    }

    private char HandleEscapeSequence(char seq)
    {
        if (this.EndOfInput)
        {
            throw new SiiSyntaxException(
                this.MarkEnd(),
                "Unexpected end-of-input (invalid escape sequence)");
        }

        switch (seq)
        {
            case 'a':
                return '\a';
            case 'b':
                return '\b';
            case 'f':
                return '\f';
            case 'n':
                return '\n';
            case 'r':
                return '\r';
            case 't':
                return '\t';
            case 'v':
                return '\v';
            case '0':
                return '\0';
            case '\'':
            case '"':
            case '\\':
            case '?':
                return seq;

            case 'x':
            {
                if (this.index + 2 >= this.length)
                {
                    throw new SiiSyntaxException(
                        this.MarkEnd(),
                        "Unexpected end-of-input (invalid \\x escape)");
                }

                var hex = this.Take(4);
                if (hex.Length != 2 && hex.Length != 4)
                {
                    throw new SiiSyntaxException(
                        this.MarkEnd(),
                        "Unexpected end-of-input (invalid \\x escape)");
                }

                var code = UInt16.Parse(hex, NumberStyles.AllowHexSpecifier);
                return Convert.ToChar(code);
            }

            default:
            {
                var octal = this.Take(3);
                if (octal.Length == 3)
                {
                    try
                    {
                        var code = Convert.ToUInt16(octal, 8);
                        return Convert.ToChar(code);
                    }
                    catch
                    {
                        throw new SiiSyntaxException(
                            this.MarkEnd(),
                            $"Invalid octal escape sequence \\{octal}");
                    }
                }

                throw new SiiSyntaxException(
                    this.MarkEnd(),
                    $"Unregocnized escape sequence \\{seq}");
            }
        }
    }

    private bool TryLexPunctuation(char c, out Token token)
    {
        foreach (var pair in Punctuation)
        {
            if (pair.Key == c)
            {
                this.MarkStart();
                this.Take();
                token = this.MakeToken(pair.Value, pair.Key.ToString());
                return true;
            }
        }

        token = null;
        return false;
    }

    private char Peek(int distance = 0)
    {
        var newIndex = this.index + distance;
        if (newIndex < 0 || newIndex >= this.length)
        {
            return '\0';
        }

        return this.Source[newIndex];
    }

    private bool IsNext(string search)
    {
        var len = search.Length;
        if (this.index + len >= this.length)
        {
            return false;
        }

        return this.Source.Substring(this.index, len) == search;
    }

    private char Take()
    {
        var current = this.Peek();
        var next = this.Peek(1);

        if (current is '\r')
        {
            ++this.line;
            this.column = 0;

            if (next is '\n')
            {
                ++this.index;
                current = next;
            }
        }
        else if (current is '\n')
        {
            ++this.line;
            this.column = 0;
        }

        ++this.column;
        ++this.index;

        return current;
    }

    private string Take(int amount)
    {
        var builder = new StringBuilder();
        for (var i = 0; i <= amount && !this.EndOfInput; ++i) builder.Append(this.Take());

        return builder.ToString();
    }

    private bool TakeIfNext(string search)
    {
        if (this.IsNext(search))
        {
            this.Skip(search.Length);
            return true;
        }

        return false;
    }

    private string TakeWhile(Predicate<char> predicate)
    {
        var builder = new StringBuilder();
        while (!this.EndOfInput && predicate(this.Peek())) builder.Append(this.Take());

        return builder.ToString();
    }

    private void Skip(int amount)
    {
        for (var i = 0; i <= amount; ++i) this.Take();
    }

    private void SkipWhile(Predicate<char> predicate)
    {
        while (!this.EndOfInput && predicate(this.Peek())) this.Take();
    }

    private delegate bool Lexeme(char c, out Token token);
}
