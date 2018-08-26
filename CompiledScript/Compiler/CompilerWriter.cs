﻿using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using CompiledScript.Utils;

namespace CompiledScript.Compiler
{
    class CompilerWriter
    {
        public static string Fill(int nb, char car)
        {
            var sb = new StringBuilder();
            for (int i = 0; i < nb * 4; i++)
            {
                sb.Append(car);
            }
            return sb.ToString();
        }

        public static string Compile(Node root, bool debug)
        {
            var sb = new StringBuilder();

            if (debug)
            {
                sb.Append("/*\r\n");
                sb.Append("CompiledScript - Intermediate Language\r\n");
                sb.Append("Date    : " + DateTime.Now + "\r\n");
                sb.Append("Version : 1.1\r\n");
                sb.Append("*/\r\n\r\n");
            }

            sb.Append(Compile(root, 0, debug));

            return sb.ToString();
        }

        /// <summary>
        /// Function/Program compiler. This is called recursively when subroutines are found
        /// </summary>
        public static string Compile(Node root, int indent, bool debug)
        {
		    var sb = new StringBuilder();

		    if (root.IsLeaf)
            {
                if (debug)
                {
                    sb.Append(Fill(indent - 1, ' '));
                    sb.Append("// Value : \"");
                    sb.Append(
                        StringEncoding.Decode(root.Text)
                            .Replace("\\", "\\\\")
                            .Replace("\t", "\\t")
                            .Replace("\n", "\\n")
                            .Replace("\r", ""));
                    sb.Append("\"\r\n");
                    sb.Append(Fill(indent - 1, ' '));
                }

			    sb.Append("v").Append(root.Text);
                sb.Append("\r\n");
		    }
            else
            {
                Node rootContainer = root;
                foreach (Node node in rootContainer.Children.ToList())
                {
				    node.Text = StringEncoding.Encode(node.Text);
				    if (!node.IsLeaf)
                    {
                        //node.Text = node.Text.ToLower();
					    var container = node;
					    if (IsMemoryFunction(node) )
					    {
					        CompileMemoryFunction(indent, debug, node, container, sb);
					    }
                        else if (IsFlowControl(node))
					    {
					        CompileConditionOrLoop(indent, debug, container, node, sb);
					    }
                        else if (IsFunctionDefinition(node))
                        {
                            CompileFunctionDefinition(indent, debug, node, sb);
                        }
                        else
					    {
					        CompileFunctionCall(indent, debug, sb, node);
					    }
				    }
                    else
                    {
                        sb.Append(Compile(node, indent + 1, debug));
				    }
			    }
		    }
		    return sb.ToString();
	    }
        
        private static bool IsFunctionDefinition(Node node)
        {
            switch (node.Text)
            {
                case "def":
                    return true;

                default:
                    return false;
            }
        }

        private static bool IsFlowControl(Node node)
        {
            switch (node.Text)
            {
                case "if":
                case "while":
                    return true;

                default:
                    return false;
            }
        }

        private static bool IsMemoryFunction(Node node)
        {
            switch (node.Text)
            {
                case "get":
                case "set":
                case "isset":
                case "unset":
                    return true;

                default:
                    return false;
            }
        }

        private static void CompileMemoryFunction(int level, bool debug, Node node, Node container, StringBuilder sb)
        {
            switch (node.Text)
            {
                case "set":
                    if (container.Children.Count != 2)
                    {
                        throw new Exception(node.Text + " required 2 parameters");
                    }

                    break;
                default:
                    if (container.Children.Count != 1)
                    {
                        throw new Exception(node.Text + " required 1 parameter");
                    }

                    break;
            }

            int nb = node.Text == "set" ? 1 : 0;
            Node child = new Node(false);
            child.Text = container.Children.ElementAt(0).Text;
            for (int i = nb; i >= 0; i--)
            {
                child.Children.AddLast(container.Children.ElementAt(i));
            }

            sb.Append(Compile(child, level + 1, debug));

            if (debug)
            {
                sb.Append(Fill(level, ' '));
                sb.Append("// Memory function : " + node.Text);
                sb.Append("\r\n");
                sb.Append(Fill(level, ' '));
            }

            sb.Append("m" + node.Text[0]);

            //builder.Append(" ");
            sb.Append("\r\n");
        }

        private static void CompileConditionOrLoop(int level, bool debug, Node container, Node node, StringBuilder sb)
        {
            if (container.Children.Count < 1)
            {
                throw new ArgumentException(node.Text + " required parameter(s)");
            }

            bool boucle = node.Text == "while";
            Node condition = new Node(false);
            condition.Children.AddLast(container.Children.ElementAt(0));
            condition.Text = "()";

            string compiledCondition = Compile(condition, level + 1, debug);

            if (debug)
            {
                sb.Append(Fill(level, ' '));
                sb.Append("// Condition for : " + node.Text);
                sb.Append("\r\n");
            }

            sb.Append(compiledCondition);

            Node body = new Node(false);
            for (int i = 1; i < container.Children.Count; i++)
            {
                body.Children.AddLast(container.Children.ElementAt(i));
            }

            body.Text = "()";
            string compileBody = Compile(body, level + 1, debug);

            string clean = FileReader.RemoveInlineComment(compileBody);


            List<string> elementsBody = new List<string>();
            foreach (string s in clean.Replace("\t", " ").Replace("\r", " ").Replace("\n", " ").Split(' '))
            {
                elementsBody.Add(s);
            }

            int nbElementBody = 0;
            foreach (string s in elementsBody)
            {
                if (s.Length != 0)
                {
                    nbElementBody++;
                }
            }

            int nbElementBack = nbElementBody;
            if (boucle)
            {
                clean = FileReader.RemoveInlineComment(compiledCondition);

                elementsBody = new List<string>();
                foreach (string s in clean.Replace("\t", " ").Replace("\r", " ").Replace("\n", " ").Split(' '))
                {
                    elementsBody.Add(s);
                }

                foreach (string s in elementsBody)
                {
                    if (s.Length != 0)
                    {
                        nbElementBack++;
                    }
                }

                nbElementBack += 2; // CMP + len body
                nbElementBody += 2;
            }

            if (debug)
            {
                sb.Append("\r\n");
                sb.Append(Fill(level, ' '));
                sb.Append("// Compare condition for : " + node.Text);
                sb.Append("\r\n");
                sb.Append(Fill(level, ' '));
            }

            sb.Append("ccmp");
            sb.Append(" ").Append(nbElementBody);

            if (debug)
            {
                sb.Append(" // If false, skip " + nbElementBody + " element(s)");
            }

            //builder.Append(" ");
            sb.Append("\r\n");

            sb.Append(compileBody);

            if (boucle)
            {
                if (debug)
                {
                    sb.Append(Fill(level, ' '));
                    sb.Append("// End : " + node.Text);
                    sb.Append("\r\n");
                    sb.Append(Fill(level, ' '));
                }

                sb.Append("cjmp");
                sb.Append(" ").Append(nbElementBack);
                //builder.Append(" ");
                sb.Append("\r\n");
                if (debug)
                {
                    sb.Append("\r\n");
                }
            }
            else
            {
                if (debug)
                {
                    sb.Append(Fill(level, ' '));
                    sb.Append("// End : " + node.Text);
                    sb.Append("\r\n");
                    sb.Append("\r\n");
                }
            }
        }

        private static void CompileFunctionCall(int level, bool debug, StringBuilder sb, Node node)
        {
            sb.Append(Compile(node, level + 1, debug));

            if (debug)
            {
                sb.Append(Fill(level, ' '));
                sb.Append("// Function : \"" + StringEncoding.Decode(node.Text));
                sb.Append("\" with " + node.Children.Count);
                sb.Append(" parameter(s)");
                sb.Append("\r\n");
                sb.Append(Fill(level, ' '));
            }

            sb.Append("f");
            sb.Append(node.Text).Append(" ");
            sb.Append(node.Children.Count); // parameter count
            sb.Append("\r\n");
        }

        private static void CompileFunctionDefinition(int level, bool debug, Node node, StringBuilder sb)
        {
            // 1) Compile the func to a temporary string
            // 2) Inject a new 'def' op-code, that names the function and does an unconditional jump over it.
            // 3) Inject the compiled func
            // 4) Inject a new 'return' op-code

            if (node.Children.Count != 2) throw new Exception("Function definition must have 3 parts: the name, the parameter list, and the definition.\r\n" +
                                                              "Call like `def (   myFunc ( param1 param2 ) ( ... statements ... )   )`");

            if (node.Children.Last.Value.Text != "()") throw new Exception("Bare functions not supported");

            var functionName = node.Children.First.Value.Text;
            var argCount = node.Children.First.Value.Children.Count;
            var subroutine = Compile(node.Children.Last.Value, level, debug);
            var tokenCount = subroutine.Split(new[] { '\t', '\n', '\r', ' ' }, StringSplitOptions.RemoveEmptyEntries).Length;

            Console.WriteLine(tokenCount + " subroutine opcodes for " + functionName + ":\r\n" + subroutine);

            if (debug)
            {
                sb.Append(Fill(level, ' '));
                sb.Append("// Function definition : \"" + functionName);
                sb.Append("\" with " + argCount);
                sb.Append(" parameter(s)");
                sb.Append("\r\n");
                sb.Append(Fill(level, ' '));
            }
            
            sb.Append("d");
            sb.Append(functionName);
            sb.Append(" ");
            sb.Append(argCount);
            sb.Append(" ");
            sb.Append(tokenCount); // relative jump to skip the function opcodes
            sb.Append("\r\n");

            // Write the actual function
            sb.Append(subroutine);
            sb.Append("\r\n");

            // Add the 'return' call
            sb.Append("cret\r\n");


            // Then the runner will need to interpret both the new op-codes
            // This would include a return-stack-push for calling functions,
            //   a jump-to-absolute-position by name
            //   a jump-to-absolute-position by return stack & pop
        }
    }
}
