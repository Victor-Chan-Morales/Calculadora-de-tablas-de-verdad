using System;
using System.Collections.Generic;
using System.Linq;

public enum TokenType {
    Variable,
    Not,
    And,
    Or,
    Imply,
    Equiv,
    LParen,
    RParen
}

public class Token {
    public TokenType Type;
    public string Value;

    public Token(TokenType type, string value = "")
    {
        Type = type;
        Value = value;
    }
}

public class Lexer
{
    private string input;
    private int position = 0;

    public Lexer(string input)
    {
        this.input = input.Replace(" ", "");
    }

    public List<Token> Tokenize()
    {
        List<Token> tokens = new List<Token>();

        while (position < input.Length)
        {
            char current = input[position];

            // Ignorar espacios
            if (char.IsWhiteSpace(current))
            {
                position++;
                continue;
            }

            // Operadores de dos o más caracteres
            if (position + 2 < input.Length && input.Substring(position, 3) == "<->")
            {
                tokens.Add(new Token(TokenType.Equiv));
                position += 3;
                continue;
            }
            if (position + 1 < input.Length && input.Substring(position, 2) == "->")
            {
                tokens.Add(new Token(TokenType.Imply));
                position += 2;
                continue;
            }

            switch (current)
            {
                case '¬':
                case '!':
                case '-':
                case '~':
                    tokens.Add(new Token(TokenType.Not));
                    position++;
                    break;
                case '∧':
                case '^':
                case '&':
                    tokens.Add(new Token(TokenType.And));
                    position++;
                    break;
                case '∨':
                case 'v':
                case '|':
                    tokens.Add(new Token(TokenType.Or));
                    position++;
                    break;
                case '→':
                    tokens.Add(new Token(TokenType.Imply));
                    position++;
                    break;
                case '↔':
                    tokens.Add(new Token(TokenType.Equiv));
                    position++;
                    break;
                case '(':
                    tokens.Add(new Token(TokenType.LParen));
                    position++;
                    break;
                case ')':
                    tokens.Add(new Token(TokenType.RParen));
                    position++;
                    break;
                default:
                    // Variables: letras y números (p, q1, r2, etc.)
                    if (char.IsLetter(current))
                    {
                        int start = position;
                        position++;
                        while (position < input.Length && (char.IsLetterOrDigit(input[position]) || input[position] == '_'))
                            position++;
                        string varName = input.Substring(start, position - start);
                        tokens.Add(new Token(TokenType.Variable, varName));
                    }
                    else
                    {
                        throw new Exception($"Carácter inválido: {current}");
                    }
                    break;
            }
        }

        return tokens;
    }
}

public abstract class Expr
{
    public abstract bool Evaluate(Dictionary<string, bool> vars);
    public abstract List<string> GetVariables();
}

public class VariableExpr : Expr
{
    public string Name;

    public VariableExpr(string name)
    {
        Name = name;
    }

    public override bool Evaluate(Dictionary<string, bool> vars)
    {
        return vars[Name];
    }

    public override List<string> GetVariables()
    {
        return new List<string> { Name };
    }
}

public class NotExpr : Expr
{
    public Expr Operand;

    public NotExpr(Expr operand)
    {
        Operand = operand;
    }

    public override bool Evaluate(Dictionary<string, bool> vars)
    {
        return !Operand.Evaluate(vars);
    }

    public override List<string> GetVariables()
    {
        return Operand.GetVariables();
    }
}

public enum BinaryOp
{
    And, Or, Imply, Equiv
}

public class BinaryExpr : Expr
{
    public Expr Left;
    public Expr Right;
    public BinaryOp Op;

    public BinaryExpr(Expr left, Expr right, BinaryOp op)
    {
        Left = left;
        Right = right;
        Op = op;
    }

    public override bool Evaluate(Dictionary<string, bool> vars)
    {
        bool a = Left.Evaluate(vars);
        bool b = Right.Evaluate(vars);

        return Op switch
        {
            BinaryOp.And => a && b,
            BinaryOp.Or => a || b,
            BinaryOp.Imply => !a || b,
            BinaryOp.Equiv => a == b,
            _ => throw new Exception("Operador desconocido")
        };
    }

    public override List<string> GetVariables()
    {
        return Left.GetVariables().Union(Right.GetVariables()).ToList();
    }
}

public class Parser
{
    private List<Token> tokens;
    private int position = 0;

    public Parser(List<Token> tokens)
    {
        this.tokens = tokens;
    }

    public Expr Parse()
    {
        return ParseEquiv();
    }

    private Expr ParseEquiv()
    {
        Expr left = ParseImply();
        while (Match(TokenType.Equiv))
        {
            Expr right = ParseImply();
            left = new BinaryExpr(left, right, BinaryOp.Equiv);
        }
        return left;
    }

    private Expr ParseImply()
    {
        Expr left = ParseOr();
        while (Match(TokenType.Imply))
        {
            Expr right = ParseOr();
            left = new BinaryExpr(left, right, BinaryOp.Imply);
        }
        return left;
    }

    private Expr ParseOr()
    {
        Expr left = ParseAnd();
        while (Match(TokenType.Or))
        {
            Expr right = ParseAnd();
            left = new BinaryExpr(left, right, BinaryOp.Or);
        }
        return left;
    }

    private Expr ParseAnd()
    {
        Expr left = ParseNot();
        while (Match(TokenType.And))
        {
            Expr right = ParseNot();
            left = new BinaryExpr(left, right, BinaryOp.And);
        }
        return left;
    }

    private Expr ParseNot()
    {
        if (Match(TokenType.Not))
        {
            return new NotExpr(ParseNot());
        }
        return ParsePrimary();
    }

    private Expr ParsePrimary()
    {
        if (Match(TokenType.LParen))
        {
            Expr expr = ParseEquiv();
            Expect(TokenType.RParen);
            return expr;
        }

        if (Match(TokenType.Variable, out Token token))
        {
            return new VariableExpr(token.Value);
        }

        throw new Exception("Expresión inválida");
    }

    private bool Match(TokenType type)
    {
        if (position < tokens.Count && tokens[position].Type == type)
        {
            position++;
            return true;
        }
        return false;
    }

    private bool Match(TokenType type, out Token token)
    {
        if (position < tokens.Count && tokens[position].Type == type)
        {
            token = tokens[position];
            position++;
            return true;
        }
        token = null!;
        return false;
    }

    private void Expect(TokenType type)
    {
        if (!Match(type))
        {
            throw new Exception($"Se esperaba: {type}");
        }
    }
}

public static class TruthTableGenerator
{
    public static void Generate(Expr expr)
    {
        var variables = expr.GetVariables().Distinct().ToList();
        int rows = 1 << variables.Count;

        Console.WriteLine(string.Join(" ", variables) + " | Resultado");

        for (int i = 0; i < rows; i++)
        {
            var values = new Dictionary<string, bool>();
            for (int j = 0; j < variables.Count; j++)
            {
                bool val = (i & (1 << (variables.Count - j - 1))) != 0;
                values[variables[j]] = val;
            }

            bool result = expr.Evaluate(values);
            string line = string.Join(" ", variables.Select(v => values[v] ? "V" : "F"));
            Console.WriteLine($"{line} | {(result ? "V" : "F")}");
        }
    }
}

class Program
{
    static void Main()
    {
        while (true)
        {
            Console.WriteLine("*-*-*-*-* CALCULADORA DE PROPOSICIONES *-*-*-*-*");
            Console.WriteLine("Caracteres válidos: ^, v, ->, <->, ¬, a,b,c...z");
            Console.WriteLine("¿Desea ingresar una nueva proposición? (s/n): ");
            string operar = Console.ReadLine()!.ToUpper();
            if (operar == "S")
            {
                Console.WriteLine("Ingrese la expresión lógica:");
                string input = Console.ReadLine()!;

                try
                {
                    var lexer = new Lexer(input);
                    var tokens = lexer.Tokenize();

                    var parser = new Parser(tokens);
                    var expr = parser.Parse();

                    TruthTableGenerator.Generate(expr);
                }
                catch (Exception ex)
                {
                    Console.WriteLine($"Error: {ex.Message}");
                }
            }
            else
            {
                break;
            }
        }
    }
}
