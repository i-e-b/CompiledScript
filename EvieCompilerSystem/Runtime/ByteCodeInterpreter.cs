﻿using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using EvieCompilerSystem.Compiler;
using EvieCompilerSystem.InputOutput;
using EvieCompilerSystem.Utils;

namespace EvieCompilerSystem.Runtime
{
    public class ByteCodeInterpreter
    {
        private List<double> program;
        public Dictionary<ulong, FunctionDefinition>  Functions;
        public static Dictionary<ulong, string> BuiltInFunctions;
        public static Dictionary<ulong, string> DebugSymbols;
        public Scope Variables;
        public static Random rnd = new Random();
       
        private int stepsTaken;
        private TextReader _input;
        private TextWriter _output;
        private RuntimeMemoryModel _memory;
        private bool runningVerbose;

        public void Init(RuntimeMemoryModel bin, TextReader input, TextWriter output, Scope importVariables = null, Dictionary<ulong, string> debugSymbols = null)
        {
            _memory = bin;

            if (BuiltInFunctions == null) {
                BuiltInFunctions = BuiltInFunctionSymbols();
            }

            program = bin.Tokens();

            Variables = new Scope(importVariables);
            Functions = new Dictionary<ulong, FunctionDefinition>();
            DebugSymbols = debugSymbols;

            _input  = input;
            _output = output;
        }

        /// <summary>
        /// Symbol mapping for built-in functions
        /// </summary>
        public static Dictionary<ulong, string> BuiltInFunctionSymbols()
        {
            var tmp = new Dictionary<ulong, string>();
            Action<string> add = s => tmp.Add(NanTags.GetCrushedName(s), s);

            add("="); add("equals"); add(">"); add("<"); add("<>"); add("not-equal");
            add("assert"); add("random"); add("eval"); add("call"); add("not"); add("or");
            add("and"); add("readkey"); add("readline"); add("print"); add("substring");
            add("length"); add("replace"); add("concat"); add("return"); add("+"); add("-");
            add("*"); add("/"); add("%");
            add("()"); // empty value marker
            return tmp;
        }

        public double Execute(bool resetVars, bool verbose)
        {
		    double evalResult;
            runningVerbose = verbose;

            if (resetVars) { Variables.Clear(); }

		    var valueStack = new Stack<double>();
            var returnStack = new Stack<int>(); // absolute position for call and return
		    
            var param = new LinkedList<double>();
            int position = 0; // the PC. This is in TOKEN positions, not bytes
		    int programCount = program.Count;
		    
		    while (position < programCount)
            {
			    stepsTaken++;
                //if (stepsTaken > 1000) throw new Exception("trap");
                // TODO: add a hook for a step event here?
                
                // Prevent stackoverflow.
                // Ex: if(true 1 10 20)
			    if ((stepsTaken & 127) == 0 && valueStack.Count > 100) // TODO: improve this mess. Might be able to use void returns (and add them to loops?)
                {
                    var oldValues = valueStack.ToArray();
                    valueStack = new Stack<double>(oldValues.Skip(oldValues.Length - 100));
                }

                double word = program[position];

                if (verbose)
                {
                    _output.WriteLine("          stack :"+string.Join(", ",valueStack.ToArray().Select(t=> _memory.DiagnosticString(t, DebugSymbols))));
                    _output.WriteLine("          #" + stepsTaken + "; p="+position
                                      +"; w="+_memory.DiagnosticString(word, DebugSymbols) );
                }

                var type = NanTags.TypeOf(word);
                switch (type) {
                    case DataType.Invalid:
                        throw new Exception("Unknown code point at " + position);

                    case DataType.Opcode:
                        // decode opcode and do stuff
                        NanTags.DecodeOpCode(word, out var codeClass, out var codeAction, out var p1, out var p2);
                        ProcessOpCode(codeClass, codeAction, p1, p2, ref position, param, valueStack, returnStack, word);
                        break;

                    default:
                        valueStack.Push(word); // these could be raw doubles, encoded real values, or references/pointers
                        break;
                }

                
                position++;
		    }

		    if (valueStack.Count != 0)
            {
                evalResult = valueStack.Pop();
		    }
            else
            {
			    evalResult = 0;
		    }

		    valueStack.Clear();

		    return evalResult;
	    }

        private void ProcessOpCode(char codeClass, char codeAction, ushort p1, ushort p2, ref int position, LinkedList<double> param, Stack<double> valueStack, Stack<int> returnStack, double word)
        {
            switch (codeClass)
            {
                case 'f': // Function *CALLS*
                    if (codeAction == 'c') {
                        position = PrepareFunctionCall(position, param, p1, valueStack, returnStack);
                    }
                    else if (codeAction == 'd') {
                        position = HandleFunctionDefinition(p1, p2, position, valueStack);
                    }
                    break;

                case 'c': // flow Control -- conditions, jumps etc
                    {
                        int opCodeCount = p2 + (p1 << 16); // we use 31 bit jumps, in case we have lots of static data
                        position = HandleControlSignal(codeAction, opCodeCount, valueStack, returnStack, position, param);
                    }
                    break;

                case 'm': // Memory access - get|set|isset|unset
                    if (valueStack.Count < 1) throw new Exception("Empty stack before memory access at position " + position);
                    HandleMemoryAccess(codeAction, valueStack, position);
                    break;

                case 's': // reserved for System operation.
                    break;

                default:
                    throw new Exception("Unexpected op code at " + position + " : " + _memory.DiagnosticString(word, DebugSymbols));
            }
        }

        private int HandleFunctionDefinition(ushort argCount, ushort tokenCount, int position, Stack<double> valueStack)
        {
            var functionNameHash = NanTags.DecodeVariableRef(valueStack.Pop());
            if (Functions.ContainsKey(functionNameHash)) throw new Exception("Function '" + functionNameHash + "' redefined at " + position + ". Original at " + Functions[functionNameHash]);

            Functions.Add(functionNameHash, new FunctionDefinition{
                StartPosition = position,
                ParamCount = argCount
            });

            return position + tokenCount + 1; // + definition length + terminator
        }

        private void HandleMemoryAccess(char action, Stack<double> valueStack, int position)
        {
            var varName = NanTags.DecodeVariableRef(valueStack.Pop());
            double value;

            switch (action)
            {
                case 'g': // get (adds a value to the stack, false if not set)
                    value = Variables.Resolve(varName);
                    valueStack.Push(value);

                    break;

                case 's': // set
                    if (valueStack.Count < 1) throw new Exception("There were no values to save. Did you forget a `return` in a function? Position: " + position);
                    value = valueStack.Pop();
                    Variables.SetValue(varName, value);
                    break;

                case 'i': // is set? (adds a bool to the stack)
                    valueStack.Push(NanTags.EncodeBool(Variables.CanResolve(varName)));
                    break;

                case 'u': // unset
                    Variables.Remove(varName);
                    break;
            }
        }

        private int HandleControlSignal(char action, int opCodeCount, Stack<double> valueStack, Stack<int> returnStack, int position, LinkedList<double> param)
        {
            switch (action)
            {
                // cmp - relative jump *DOWN* if top of stack is false
                case 'c':
                    var condition = _memory.CastBoolean(valueStack.Pop());

                    //position++;
                    if (condition == false) { position += opCodeCount; }
                    break;

                // jmp - unconditional relative jump *UP*
                case 'j':
                    position++;
                    int jmpLength = opCodeCount;
                    position -= 1; // this opcode
                    position -= jmpLength;
                    break;
                    

                // skip - unconditional relative jump *DOWN*
                case 's':
                    //position++;
                    position += opCodeCount;
                    break;

                // ct - call term - a function that returns values ended without returning
                case 't':
                    throw new Exception("A function returned without setting a value. Did you miss a 'return' in a function?");

                // ret - pop return stack and jump to absolute position
                case 'r':
                    HandleReturn(ref position, valueStack, returnStack, Variables, opCodeCount);
                    break;
            }

            return position;
        }

        private void HandleReturn(ref int position, Stack<double> valueStack, Stack<int> returnStack, Scope Variables, int returnParamCount)
        {
            if (returnStack.Count < 1) throw new Exception("Return stack empty. Check program logic");
            Variables.DropScope();
            position = returnStack.Pop();
        }

        private int PrepareFunctionCall(int position, LinkedList<double> param, ushort nbParams, Stack<double> valueStack, Stack<int> returnStack)
        {
            param.Clear();

            var functionNameHash = NanTags.DecodeVariableRef(valueStack.Pop());

            // Pop values from stack.
            for (int i = 0; i < nbParams; i++)
            {
                try
                {
                    param.AddFirst(valueStack.Pop());
                }
                catch (Exception ex)
                {
                    throw new Exception("Stack underflow. Ran out of values before function call at " + position, ex);
                }
            }

            // Evaluate function.
            var evalResult = EvaluateFunctionCall(ref position, functionNameHash, nbParams, param, returnStack, valueStack);

            // Add result on stack as a value.
            if (NanTags.TypeOf(evalResult) != DataType.NoValue)
            {
                valueStack.Push(evalResult);
            } else if (NanTags.DecodeNonValue(evalResult) == NonValueType.Unit) {
                valueStack.Push(evalResult);
            }

            return position;
        }

        // Evaluate a function call
	    public double EvaluateFunctionCall(ref int position, ulong functionNameHash, int nbParams, LinkedList<double> param, Stack<int> returnStack, Stack<double> valueStack)
        {
            if (BuiltInFunctions.ContainsKey(functionNameHash))
            {
                return EvaluateBuiltInFunction(ref position, functionNameHash, nbParams, param, returnStack, valueStack);
            }

            if (Functions.ContainsKey(functionNameHash))
            {
                // handle functions that are defined in the program
                Variables.PushScope(param); // write parameters into new scope
                returnStack.Push(position); // set position for 'cret' call
                position = Functions[functionNameHash].StartPosition; // move pointer to start of function
                return NanTags.VoidReturn(); // return no value, continue execution elsewhere
            }

            // TODO: use a symbols file to get source names back?
            throw new Exception("Tried to call an undefined function '"
                                + DbgStr(functionNameHash) 
                                + "' at position " + position
                                + "\r\nKnown functions: " + string.Join(", ", Functions.Keys.Select(DbgStr))
                                + "\r\nAs a string: " + TryDeref(functionNameHash) + "?"
                                + "\r\nKnown symbols: " + string.Join(", ", DebugSymbols.Keys.Select(DbgStr)));
        }

        private string TryDeref(ulong functionNameHash)
        {
            try
            {
                return _memory.DereferenceString((long)functionNameHash);
            }
            catch
            {
                return "<not a known string>";
            }
        }

        private string DbgStr(ulong hash)
        {
            if (DebugSymbols == null) return hash.ToString("X");
            if ( ! DebugSymbols.ContainsKey(hash)) return "<unknown> " + hash.ToString("X");

            return DebugSymbols[hash] + " ("+hash.ToString("X")+")";
        }

        private double EvaluateBuiltInFunction(ref int position, ulong functionNameHash, int nbParams, LinkedList<double> param, Stack<int> returnStack, Stack<double> valueStack)
        {
            var functionName = BuiltInFunctions[functionNameHash];

            if (IsMathFunc(functionName))
            {
                // handle math functions
                try
                {
                    return EvalMath(functionName[0], param);
                }
                catch (Exception e)
                {
                    throw new Exception("Math Error : " + e.Message);
                }
            }

            switch (functionName)
            {
                // each element equal to the last
                case "=":
                case "equals":
                    if (nbParams < 2) throw new Exception("equals ( = ) must have at least two things to compare");
                    return FoldInequality(param, (a, b) => Math.Abs(a - b) <= double.Epsilon);

                // Each element smaller than the last
                case ">":
                    if (nbParams < 2) throw new Exception("greater than ( > ) must have at least two things to compare");
                    return FoldInequality(param, (a, b) => a > b);

                // Each element larger than the last
                case "<":
                    if (nbParams < 2) throw new Exception("less than ( < ) must have at least two things to compare");
                    return FoldInequality(param, (a, b) => a < b);

                // Each element DIFFERENT TO THE LAST (does not check set uniqueness!)
                case "<>":
                case "not-equal":
                    if (nbParams < 2) throw new Exception("not-equal ( <> ) must have at least two things to compare");
                    return FoldInequality(param, (a, b) => Math.Abs(a - b) > double.Epsilon);

                case "assert":
                    if (nbParams < 1) return NanTags.VoidReturn(); // assert nothing passes
                    var condition = param.ElementAt(0);
                    if (_memory.CastBoolean(condition) == false)
                    {
                        var msg = ConcatLinkedList(param.First.Next);
                        throw new Exception("Assertion failed: " + msg);
                    }

                    break;

                case "random":
                    if (nbParams < 1) return rnd.NextDouble(); // 0 params - any size
                    if (nbParams < 2) return rnd.Next(_memory.CastInt(param.ElementAt(0))); // 1 param  - max size
                    return rnd.Next(_memory.CastInt(param.ElementAt(0)), _memory.CastInt(param.ElementAt(1))); // 2 params - range

                case "eval":
                    var reader = new SourceCodeTokeniser();
                    var statements = _memory.CastString(param.ElementAt(0));
                    var programTmp = reader.Read(statements);
                    var bin = Compiler.Compiler.CompileRoot(programTmp, false);
                    var interpreter = new ByteCodeInterpreter();
                    interpreter.Init(new RuntimeMemoryModel(bin), _input, _output, Variables, DebugSymbols); // todo: optional other i/o for eval?
                    return interpreter.Execute(false, runningVerbose);

                case "call":
                    NanTags.DecodePointer(param.ElementAt(0), out var target, out var type);
                    if (type != DataType.PtrString) throw new Exception("Tried to call a function by name, but passed a '" + type + "' at " + position);
                    // this should be a string, but we need a function name hash -- so calculate it:
                    var strName = _memory.DereferenceString(target);
                    functionNameHash = NanTags.GetCrushedName(strName);
                    nbParams--;
                    param.RemoveFirst();
                    return EvaluateFunctionCall(ref position, functionNameHash, nbParams, param, returnStack, valueStack);

                case "not" when nbParams == 1:
                    var bval = _memory.CastBoolean(param.ElementAt(0));
                    return NanTags.EncodeBool(!bval);

                case "or":
                {
                    bool more = nbParams > 0;
                    int i = 0;
                    while (more)
                    {
                        var bresult = _memory.CastBoolean(param.ElementAt(i));
                        if (bresult) return NanTags.EncodeBool(true);

                        i++;
                        more = i < nbParams;
                    }

                    return NanTags.EncodeBool(false);
                }

                case "and":
                {
                    bool more = nbParams > 0;
                    int i = 0;
                    while (more)
                    {
                        var bresult = _memory.CastBoolean(param.ElementAt(i));
                        if (!bresult) return NanTags.EncodeBool(false);

                        i++;
                        more = i < nbParams;
                    }

                    return NanTags.EncodeBool(true);
                }

                case "readkey":
                    return _memory.StoreStringAndGetReference(((char) _input.Read()).ToString());

                case "readline":
                    return _memory.StoreStringAndGetReference(_input.ReadLine());

                case "print":
                {
                    string lastStr = null;
                    foreach (var v in param)
                    {
                        lastStr = _memory.CastString(v);
                        _output.Write(lastStr);
                    }

                    if (lastStr != "") _output.WriteLine();
                }
                    break;

                case "substring" when nbParams == 2:
                {
                    var newString = _memory.CastString(param.ElementAt(0)).Substring(_memory.CastInt(param.ElementAt(1)));
                    return _memory.StoreStringAndGetReference(newString);
                }

                case "substring":
                    if (nbParams == 3)
                    {
                        int start = _memory.CastInt(param.ElementAt(1));
                        int length = _memory.CastInt(param.ElementAt(2));

                        try
                        {
                            string s = _memory.CastString(param.ElementAt(0)).Substring(start, length);
                            return _memory.StoreStringAndGetReference(s);
                        }
                        catch (Exception ex)
                        {
                            _output.WriteLine("Runner error: " + ex);
                        }
                    }

                    break;

                case "length" when nbParams == 1:
                    return _memory.CastString(param.ElementAt(0)).Length; // TODO: lengths of other things

                case "replace" when nbParams == 3:
                    string exp = _memory.CastString(param.ElementAt(0));
                    string oldValue = _memory.CastString(param.ElementAt(1));
                    string newValue = _memory.CastString(param.ElementAt(2));
                    exp = exp.Replace(oldValue, newValue);
                    return _memory.StoreStringAndGetReference(exp);

                case "concat":
                    var builder = new StringBuilder();

                    foreach (var v in param)
                    {
                        builder.Append(_memory.CastString(v));
                    }

                    return _memory.StoreStringAndGetReference(builder.ToString());

                case "()":
                    { // valueless marker (like an empty object)
                        return NanTags.EncodeNonValue(NonValueType.Unit);
                    }
            }

            return NanTags.VoidReturn();
        }

        private string ConcatLinkedList(LinkedListNode<double> list)
        {
            var sb = new StringBuilder();
            while (list != null) {
                sb.Append(_memory.CastString(list.Value));
                list = list.Next;
            }
            return sb.ToString();
        }

        private double FoldInequality(IEnumerable<double> list, Func<double, double, bool> comparitor)
        {
            bool first = true;
            double prev = 0;
            foreach (var encoded in list)
            {
                if (first) {
                    prev = _memory.CastDouble(encoded);
                    first = false;
                    continue;
                }
                var current = _memory.CastDouble(encoded);
                if ( ! comparitor(prev, current)) return NanTags.EncodeBool(false);
                prev = current;
            }
            return NanTags.EncodeBool(true);
        }

        private static bool IsMathFunc(string functionName)
        {
            if (functionName.Length != 1) return false;
            switch (functionName[0])
            {
                case '+':
                case '-':
                case '*':
                case '/':
                case '%':
                    return true;
                default:
                    return false;
            }
        }

	    private static double EvalMath(char op, LinkedList<double> args)
        {
            var argCount = args.Count;

            if (argCount == 0) throw new Exception("Math funtion called with no arguments");

            var arg0 = args.First();

            if (argCount == 1) {
                switch (op) {
                    case '+': // uniary plus: no-op
                        return arg0;

                    case '-': // uniary minus: value negation
                        return -arg0;

                    case '%': // uniary remainder: common case, odd/even
                        return arg0 % 2;

                    default:
                        throw new Exception("Uniary '"+op+"' is not supported");
                }
            }

		    switch (op)
            {
		        case '+':
			        return args.Sum();

		        case '-':
			        return args.ChainDifference();

		        case '*':
		            return args.ChainProduct();

		        case '/':
		            return args.ChainDivide();

		        case '%':
		            return args.ChainRemainder();
		    }

            throw new Exception("Error Math : unknown op : " + op); // this should never happen. It would be an error in the interpreter
	    }
    }
}
