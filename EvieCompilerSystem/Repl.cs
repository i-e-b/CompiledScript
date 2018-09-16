using System;
using System.IO;
using EvieCompilerSystem.InputOutput;
using EvieCompilerSystem.Runtime;

namespace EvieCompilerSystem
{
    // ReSharper disable once UnusedMember.Global
    public static class Repl
    {
        private static string baseDirectory = "";

        // ReSharper disable once UnusedMember.Global
        public static void BuildAndRun(string languageInput, TextReader input, TextWriter output, bool trace, bool printIL) {
            
            var contents = SourceCodeReader.ReadContent(languageInput, true);
            
            // Compile
            var sourceCodeReader = new Compiler.SourceCodeTokeniser();
            var program = sourceCodeReader.Read(contents);
            Compiler.Compiler.BaseDirectory = baseDirectory;
            var compiledOutput = Compiler.Compiler.CompileRoot(program, debug: false);

            var stream = new MemoryStream();
            compiledOutput.WriteToStream(stream);
            stream.Seek(0,SeekOrigin.Begin);
            var byteCodeReader = new RuntimeMemoryModel(stream);

            if (printIL)
            {
                output.WriteLine("======= BYTE CODE SUMMARY ==========");
                compiledOutput.AddSymbols(ByteCodeInterpreter.BuildFunctionSymbols());
                output.WriteLine(value: byteCodeReader.ToString(compiledOutput.GetSymbols()));
                output.WriteLine("====================================");
            }

            // Execute
            try
            {
                var interpreter = new ByteCodeInterpreter();

                // Init the interpreter.
                interpreter.Init(byteCodeReader, input, output, debugSymbols: compiledOutput.GetSymbols());

                /* TODO later: ability to pass in args
                foreach (var pair in argsVariables.ToArray())
                {
                    interpreter.Variables.SetValue(pair.Key, pair.Value);
                }
                */

                interpreter.Execute(false, trace);
            }
            catch (Exception e)
            {
                output.WriteLine("Exception : " + e.Message);
                output.WriteLine("\r\n\r\n" + e.StackTrace);
            }
        }

        /// <summary>
        /// Set the directory used to resolve includes
        /// </summary>
        public static void SetBasePath(string path)
        {
            baseDirectory = path;
        }
    }
}
