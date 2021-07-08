using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Lexxys.Tokenizer;

namespace State.Eval
{
    static class Test
    {

        public static void Go()
        {
            do
            {
                string x = Console.ReadLine();
                if (String.IsNullOrEmpty(x))
                {
                    Console.WriteLine("Press enter again to exit");
                    x = Console.ReadLine();
                    if (String.IsNullOrEmpty(x))
                        return;
                }
                var a = Parse(x);
                if (a.Statements.Count == 0)
                    Console.WriteLine("empty");
                else
                    a.Print(Console.Out);
            } while (true);
        }

        static Et Parse(string text)
        {
            return Parser.Parse(new CharStream(text), Defs());
        }

        static List<Function> Defs()
        {
            return new List<Function> {
                new Function("min", 
                    typeof(object), new Parameter { Name = "left" }, new Parameter { Name = "right" }),
                new Function("max",
                    typeof(object), new Parameter { Name = "left" }, new Parameter { Name = "right" }),
                new Function("print",
                    typeof(void), new Parameter { Name = "value", IsSequence = true }),
                new Function("if",
                    typeof(object), new Parameter { Name = "condition" }, new Parameter { Name = "then" }, new Parameter { Name = "else", IsOptional = true, ByNameOnly = true }),
            };
        }
    }
}
