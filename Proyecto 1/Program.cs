using System;
using System.Collections.Generic;
using System.Text.RegularExpressions;

namespace CalculadoraTablasDeVerdad
{
    class Programa
    {
        static void Main()
        {
            Console.Write("Ingrese una proposición compuesta: ");
            Console.WriteLine("Puede usar caracteres como: →, ->, ↔, <->, ^, &, V, |, ¬, !");
            string expresionIngresada = Console.ReadLine()!;

            string expresion = expresionIngresada.Replace(" ", "")
                                                .Replace("→", "->")
                                                .Replace("↔", "<->")
                                                .Replace("∧", "&")
                                                .Replace("∨", "|")
                                                .Replace("¬", "!");

            var variables = ObtenerVariables(expresion);
            var subExpresiones = ObtenerSubExpresiones(expresion);

            int total = 1 << variables.Count;
            Console.WriteLine();

            foreach (var caracter in variables)
                Console.Write(caracter + "\t");
            foreach (var sub in subExpresiones)
                Console.Write(sub + "\t");
            Console.WriteLine();

            for (int i = 0; i < total; i++)
            {
                var asignacion = new Dictionary<char, bool>();
                for (int j = 0; j < variables.Count; j++)
                    asignacion[variables[j]] = ((i >> (variables.Count - j - 1)) & 1) == 1;

                foreach (var v in variables)
                    Console.Write((asignacion[v] ? "V" : "F") + "\t");

                var valoresSub = new Dictionary<string, bool>();
                foreach (var sub in subExpresiones)
                {
                    string evaluada = ReemplazarVariables(sub, asignacion);
                    bool resultado = EvaluarExpresion(evaluada);
                    valoresSub[sub] = resultado;
                    Console.Write((resultado ? "V" : "F") + "\t");
                }

                Console.WriteLine();
            }
        }

        static List<char> ObtenerVariables(string expresion)
        {
            HashSet<char> letras = new HashSet<char>();
            foreach (char c in expresion)
                if (char.IsLetter(c))
                    letras.Add(c);
            var lista = new List<char>(letras);
            lista.Sort();
            return lista;
        }
        static List<string> ObtenerSubExpresiones(string expr)
        {
            List<string> subs = new List<string>();
            Stack<int> pila = new Stack<int>();
            for (int i = 0; i < expr.Length; i++)
            {
                if (expr[i] == '(')
                    pila.Push(i);
                else if (expr[i] == ')')
                {
                    int ini = pila.Pop();
                    string sub = expr.Substring(ini, i - ini + 1);
                    if (!subs.Contains(sub))
                        subs.Add(sub);
                }
            }
            if (!subs.Contains(expr))
                subs.Add(expr);
            return subs;
        }
        static string ReemplazarVariables(string expr, Dictionary<char, bool> valores)
        {
            string resultado = expr;
            foreach (var kvp in valores)
                resultado = resultado.Replace(kvp.Key.ToString(), kvp.Value ? "1" : "0");
            return resultado;
        }
        static bool EvaluarExpresion(string expr)
        {
            while (expr.Contains("("))
            {
                int inicio = expr.LastIndexOf('(');
                int fin = expr.IndexOf(')', inicio);
                string sub = expr.Substring(inicio + 1, fin - inicio - 1);
                bool valor = Evaluar(sub);
                expr = expr.Substring(0, inicio) + (valor ? "1" : "0") + expr.Substring(fin + 1);
            }
            return Evaluar(expr);
        }
        static bool Evaluar(string expr)
        {
            expr = Regex.Replace(expr, @"!\s*1", "0");
            expr = Regex.Replace(expr, @"!\s*0", "1");

            while (Regex.IsMatch(expr, @"(1|0)&(1|0)"))
                expr = Regex.Replace(expr, @"(1|0)&(1|0)", m =>
                {
                    return (m.Groups[1].Value == "1" && m.Groups[2].Value == "1") ? "1" : "0";
                });

            while (Regex.IsMatch(expr, @"(1|0)\|(1|0)"))
                expr = Regex.Replace(expr, @"(1|0)\|(1|0)", m =>
                {
                    return (m.Groups[1].Value == "1" || m.Groups[2].Value == "1") ? "1" : "0";
                });

            while (Regex.IsMatch(expr, @"(1|0)->(1|0)"))
                expr = Regex.Replace(expr, @"(1|0)->(1|0)", m =>
                {
                    return (m.Groups[1].Value == "1" && m.Groups[2].Value == "0") ? "0" : "1";
                });

            while (Regex.IsMatch(expr, @"(1|0)<->(1|0)"))
                expr = Regex.Replace(expr, @"(1|0)<->(1|0)", m =>
                {
                    return (m.Groups[1].Value == m.Groups[2].Value) ? "1" : "0";
                });

            return expr.Trim() == "1";
        }
    }
}
