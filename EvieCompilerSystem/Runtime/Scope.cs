using System;
using System.Collections.Generic;
using EvieCompilerSystem.InputOutput;

namespace EvieCompilerSystem.Runtime
{
    /// <summary>
    /// Simple variable scope storage and resolver.
    /// Values are found in the most recently defined scope first
    /// </summary>
    public class Scope
    {
        readonly LinkedList<Dictionary<ulong, double>> scopes;
        private readonly object _lock = new object();

        /// <summary>
        /// Create a new empty value scope
        /// </summary>
        public Scope()
        {
            scopes = new LinkedList<Dictionary<ulong, double>>();
            scopes.AddLast(new Dictionary<ulong, double>()); // global scope
        }

        /// <summary>
        /// Create a new scope, copying values (read-only) from another scope stack.
        /// If there are name conflicts in the scope, only the visible values will be copied
        /// </summary>
        /// <param name="importVariables">Variables to copy. All will be added to the global level.</param>
        public Scope(Scope importVariables)
        {
            scopes = new LinkedList<Dictionary<ulong, double>>();
            var global = new Dictionary<ulong, double>();

            if (importVariables != null) {
                foreach (var value in importVariables.ListAllVisible()){
                    global.Add(value.Key, value.Value);
                }
            }

            scopes.AddLast(global);
        }

        /// <summary>
        /// List all values in the scope
        /// </summary>
        private IEnumerable<KeyValuePair<ulong, double>> ListAllVisible()
        {
            lock (_lock)
            {
                var seen = new HashSet<ulong>();
                var scope = scopes.Last;
                while (scope != null)
                {
                    foreach (var pair in scope.Value)
                    {
                        if (seen.Contains(pair.Key)) continue;
                        seen.Add(pair.Key);
                        yield return pair;
                    }
                    scope = scope.Previous;
                }
            }
        }

        /// <summary>
        /// Start a new inner-most scope.
        /// Parameters are specially named by index (like "__p0", "__p1"). The compiler must match this.
        /// </summary>
        public void PushScope(ICollection<double> parameters = null) {
            var sd = new Dictionary<ulong, double>();
            var i = 0;
            if (parameters != null)
            {
                foreach (var parameter in parameters)
                {
                    sd.Add(NameFor(i), parameter);
                    i++;
                }
            }
            lock (_lock)
            {
                scopes.AddLast(sd);
            }
        }

        /// <summary>
        /// Remove innermost scope, and drop back to the previous one
        /// </summary>
        public void DropScope() {
            lock (_lock)
            {
                if (scopes.Last == scopes.First) throw new Exception("Tried to drop the global scope");
                scopes.RemoveLast();
            }
        }

        /// <summary>
        /// Read a value by name
        /// </summary>
        public double Resolve(ulong crushedName){
            lock (_lock)
            {
                var current = scopes.Last;
                while (current != null)
                {
                    if (current.Value.ContainsKey(crushedName)) return current.Value[crushedName];
                    current = current.Previous;
                }

                throw new Exception("Could not resolve '" + crushedName.ToString("X") + "', check program logic");
            }
        }

        /// <summary>
        /// Set a value by name. If no scope has it, then it will be defined in the innermost scope
        /// </summary>
        public void SetValue(ulong crushedName, double value) {
            lock (_lock)
            {
                var current = scopes.Last;
                while (current != null)
                {
                    if (current.Value.ContainsKey(crushedName))
                    {
                        current.Value[crushedName] = value;
                        return;
                    }
                    current = current.Previous;
                }

                scopes.Last.Value.Add(crushedName, value);
            }
        }

        /// <summary>
        /// Clear all scopes and restore empty globals
        /// </summary>
        public void Clear()
        {
            lock (_lock)
            {
                scopes.Clear();
                scopes.AddLast(new Dictionary<ulong, double>()); // global scope
            }
        }

        /// <summary>
        /// Does this name exist in any scopes?
        /// </summary>
        public bool CanResolve(ulong crushedName)
        {
            lock (_lock)
            {
                var current = scopes.Last;
                while (current != null)
                {
                    if (current.Value.ContainsKey(crushedName)) return true;
                    current = current.Previous;
                }
                return false;
            }
        }

        /// <summary>
        /// Remove this variable.
        /// <para>
        /// NOTE: this will only work in the local or global scopes, but not any intermediaries.
        /// </para>
        /// It's expected that this will be used mostly on globals. They will be removed in preference to locals.
        /// </summary>
        public void Remove(ulong crushedName)
        {
            lock (_lock)
            {
                if (scopes.First.Value.Remove(crushedName)) return;
                scopes.Last.Value.Remove(crushedName);
            }
        }

        /// <summary>
        /// Get the name for a positional argument
        /// </summary>
        public static ulong NameFor(int i)
        {
            return NanTags.GetCrushedName("__p" + i);
        }

        /// <summary>
        /// Does this name exist in the top level scope?
        /// Will ignore other scopes, including global.
        /// </summary>
        public bool InScope(ulong crushedName)
        {
            lock (_lock)
            {
                return scopes.Last.Value.ContainsKey(crushedName);
            }
        }

        public int Depth()
        {
            return scopes.Count;
        }
    }
}