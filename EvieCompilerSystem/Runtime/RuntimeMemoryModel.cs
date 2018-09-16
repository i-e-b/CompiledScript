using System;
using System.Collections.Generic;
using System.Globalization;
using System.IO;
using System.Linq;
using System.Text;
using EvieCompilerSystem.InputOutput;

namespace EvieCompilerSystem.Runtime
{
    public class RuntimeMemoryModel
    {
        private readonly List<double> encodedTokens;

        public RuntimeMemoryModel(NanCodeWriter writer)
        {
            var ms = new MemoryStream(writer.OpCodeCount() * 16);
            writer.WriteToStream(ms);
            ms.Seek(0, SeekOrigin.Begin);

            encodedTokens = new List<double>((int)(ms.Length / 8));
            var raw = ms.ToArray();

            for (int i = 0; i < raw.Length; i+=8)
            {
                encodedTokens.Add(BitConverter.ToDouble(raw, i));
            }
        }

        public RuntimeMemoryModel(MemoryStream stream)
        {
            encodedTokens = new List<double>((int)(stream.Length / 8));
            var raw = stream.ToArray();

            for (int i = 0; i < raw.Length; i+=8)
            {
                encodedTokens.Add(BitConverter.ToDouble(raw, i));
            }
        }

        public string ToString(Dictionary<ulong, string> debugSymbols)
        {
            int index = 0;
            var sb = new StringBuilder();

            // Try to display static strings meaningfully
            if (NanTags.TypeOf(encodedTokens[0]) == DataType.Opcode) {
                index = 1;
                NanTags.DecodeLongOpCode(encodedTokens[0], out var c1, out var c2, out var count);
                if (c1 == 'c' && c2 == 's') {
                    sb.AppendLine("Data table: "+count+" tokens ("+(count*8)+" bytes)");
                    while (index < count)
                    {
                        var length = NanTags.DecodeUInt32(encodedTokens[index++]);
                        var chunkCount = (int)Math.Ceiling(length / 8.0d);
                        sb.Append(index + ": ("+length+") ");

                        for (var ch = 0; ch < chunkCount; ch++)
                        {
                            var raw = BitConverter.GetBytes(encodedTokens[index++]);
                            sb.Append(MakeSafe(Encoding.ASCII.GetString(raw)));
                        }
                        sb.AppendLine();
                    }
                }
            }

            // output remaining bytecodes
            for (; index < encodedTokens.Count; index++)
            {
                var token = encodedTokens[index];
                var type = NanTags.TypeOf(token);
                sb.Append(index.ToString());
                sb.Append(" ");
                sb.Append(type.ToString());
                sb.Append(": ");
                sb.AppendLine(Stringify(token, type, debugSymbols));
            }

            return sb.ToString();
        }

        private string MakeSafe(string raw)
        {
            return string.Join("", raw.ToCharArray().Select(c => 
                (c >= ' ' && c <= '~') ? c : '▒'
                ));
        }

        public override string ToString()
        {
            return ToString(null);
        }

        private string Stringify(double token, DataType type, Dictionary<ulong, string> debugSymbols)
        {
            switch (type){
                case DataType.Invalid: return "";
                case DataType.NoValue: return "";

                case DataType.VariableRef:
                    var rref = NanTags.DecodeVariableRef(token);
                    if (debugSymbols?.ContainsKey(rref) == true)
                    {
                        return "'" + debugSymbols[rref] + "' (" + rref.ToString("X") + ")";
                    }
                    return rref.ToString("X");

                case DataType.Opcode:
                    NanTags.DecodeOpCode(token, out var ccls, out var cact, out var p1, out var p2);
                    return ccls+""+cact+" ("+p1+", "+p2+")";

                case DataType.PtrString:
                case DataType.PtrHashtable:
                case DataType.PtrGrid:
                case DataType.PtrArray_Int32:
                case DataType.PtrArray_UInt32:
                case DataType.PtrArray_String:
                case DataType.PtrArray_Double:
                case DataType.PtrSet_String:
                case DataType.PtrSet_Int32:
                case DataType.PtrLinkedList:
                case DataType.PtrDiagnosticString:
                    NanTags.DecodePointer(token, out var targ, out _);
                    return " -> " + targ;

                case DataType.ValInt32: return NanTags.DecodeInt32(token).ToString();
                case DataType.ValUInt32: return NanTags.DecodeUInt32(token).ToString();
                case DataType.Number:  return token.ToString(CultureInfo.InvariantCulture);

                default:
                    throw new ArgumentOutOfRangeException(nameof(type), type, null);
            }
        }

        public List<double> Tokens()
        {
            return encodedTokens;
        }


        public string DereferenceString(long position) {
            // a string is [NanTag(UInt32): byte length] [string bytes, padded to 8 byte chunks]
            // The plan:
            //  1) read the byte length
            var header = encodedTokens[(int)position];
            if (NanTags.TypeOf(header) != DataType.ValUInt32) throw new Exception("String header was not a UInt32");
            var length = NanTags.DecodeUInt32(header);

            //  2) calculate chunk count and read the chunks
            var chunkCount = (int)Math.Ceiling(length / 8.0d);
            var rawBytes = encodedTokens.GetRange((int)position+1, chunkCount).SelectMany(BitConverter.GetBytes).ToArray();

            //  4) make string (ascii for now, then utf-8?)
            return Encoding.ASCII.GetString(rawBytes, 0, (int)length);
        }
        
        
        public bool CastBoolean(double encoded)
        {
            var type = NanTags.TypeOf(encoded);

            switch (type) {
                case DataType.Number:
                    return Math.Abs(encoded) > double.Epsilon;
               
                case DataType.ValInt32:
                    return NanTags.DecodeInt32(encoded) != 0;

                case DataType.ValUInt32:
                    return NanTags.DecodeUInt32(encoded) != 0;

                case DataType.PtrString:
                    { // null, empty, "false" or "0" are false. All other strings are true.
                        NanTags.DecodePointer(encoded, out var ptr, out _);
                        var strVal = DereferenceString(ptr);
                        if (string.IsNullOrEmpty(strVal)) return false;
                        if (string.Equals(strVal, "false", StringComparison.OrdinalIgnoreCase)) return false;
                        if (string.Equals(strVal, "0", StringComparison.OrdinalIgnoreCase)) return false;
                        return true;
                    }

                // All the things that can't be meaningfully cast are 'false'
                default: return false;
            }
        }

        public double CastDouble(double encoded)
        {
            var type = NanTags.TypeOf(encoded);

            switch (type) {
                case DataType.Number:
                    return encoded;
               
                case DataType.ValInt32:
                    return NanTags.DecodeInt32(encoded);

                case DataType.ValUInt32:
                    return NanTags.DecodeUInt32(encoded);

                // All the things that can't be meaningfully cast
                default: return double.NaN;
            }
        }

        /// <summary>
        /// Get a resonable string representation from a value.
        /// This should include stringifying non-string types (numbers, structures etc)
        /// </summary>
        public string CastString(double encoded)
        {
            var type = NanTags.TypeOf(encoded);
            switch (type){
                case DataType.Invalid: return "<invalid value>";
                case DataType.Number: return encoded.ToString(CultureInfo.InvariantCulture);
                case DataType.VariableRef: return "<Reference>";
                case DataType.Opcode: return "<Op Code>";
                case DataType.NoValue: return "";

                case DataType.PtrDiagnosticString:
                case DataType.PtrString:
                    NanTags.DecodePointer(encoded, out var target, out _);
                    return DereferenceString(target);

                case DataType.PtrHashtable:
                case DataType.PtrGrid:
                case DataType.PtrArray_Int32:
                case DataType.PtrArray_UInt32:
                case DataType.PtrArray_String:
                case DataType.PtrArray_Double:
                case DataType.PtrSet_String:
                case DataType.PtrSet_Int32:
                case DataType.PtrLinkedList:
                    return "<complex type>"; // TODO: add stringification later

                case DataType.ValInt32: return NanTags.DecodeInt32(encoded).ToString();
                case DataType.ValUInt32: return NanTags.DecodeUInt32(encoded).ToString();

                default:
                    throw new ArgumentOutOfRangeException();
            }
        }

        /// <summary>
        /// Cast a value to int. If not applicable, returns zero
        /// </summary>
        public int CastInt(double encoded)
        {
            var type = NanTags.TypeOf(encoded);
            switch (type){
                case DataType.Invalid:
                case DataType.VariableRef:
                case DataType.Opcode:
                case DataType.NoValue:
                    return 0;

                case DataType.PtrDiagnosticString:
                case DataType.PtrString:
                    NanTags.DecodePointer(encoded, out var target, out _);
                    int.TryParse(DereferenceString(target), out var result);
                    return result;

                case DataType.PtrHashtable:
                case DataType.PtrGrid:
                case DataType.PtrArray_Int32:
                case DataType.PtrArray_UInt32:
                case DataType.PtrArray_String:
                case DataType.PtrArray_Double:
                case DataType.PtrSet_String:
                case DataType.PtrSet_Int32:
                case DataType.PtrLinkedList:
                    return 0;

                case DataType.Number: return (int)encoded;
                case DataType.ValInt32: return NanTags.DecodeInt32(encoded);
                case DataType.ValUInt32: return (int)NanTags.DecodeUInt32(encoded);

                default:
                    throw new ArgumentOutOfRangeException();
            }
        }

        /// <summary>
        /// Store a new string at the end of memory, and return a string pointer token for it
        /// </summary>
        public double StoreStringAndGetReference(string str)
        {
            var location = encodedTokens.Count;

            var bytes = Encoding.ASCII.GetBytes(str);
            var headerOpCode = NanTags.EncodeUInt32((uint)bytes.Length);

            encodedTokens.Add(headerOpCode);

            unchecked
            {
                ulong pack = 0;
                int rem = 0;
                for (int i = 0; i < bytes.Length; i++)
                {
                    pack += ((ulong)bytes[i]) << rem;

                    rem += 8;
                    if (rem > 56)
                    {
                        encodedTokens.Add(BitConverter.Int64BitsToDouble((long)pack));
                        rem = 0;
                        pack = 0;
                    }
                }

                if (rem != 0) {
                    for (; rem < 64; rem += 8)
                    {
                        pack += ((ulong)'_') << rem;
                    }
                    encodedTokens.Add(BitConverter.Int64BitsToDouble((long)pack));
                }
            }
            return NanTags.EncodePointer(location, DataType.PtrString);
        }

        public string DiagnosticString(double token, Dictionary<ulong, string> symbols = null)
        {
            return Stringify(token, NanTags.TypeOf(token), symbols);
        }
    }
}