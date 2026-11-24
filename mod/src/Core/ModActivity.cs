using System;
using System.Collections.Generic;

namespace BigWalkArchipelago.Core
{
    // What the mod is doing right now, so a check fired by the mod's own hands is never a solve.
    internal static class ModActivity
    {
        private static readonly List<string> Names = new();

        // Null when the mod is not acting, i.e. the game is.
        internal static string Current => Names.Count == 0 ? null : string.Join(" > ", Names);

        internal static IDisposable Enter(string name)
        {
            Names.Add(name);
            return new Scope();
        }

        private sealed class Scope : IDisposable
        {
            private bool _done;

            public void Dispose()
            {
                if (_done)
                    return;

                _done = true;
                if (Names.Count > 0)
                    Names.RemoveAt(Names.Count - 1);
            }
        }
    }
}
