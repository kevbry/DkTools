using System.Collections.Generic;

namespace DKX.Compilation.Tokens
{
    public class TokenUseTracker
    {
        private HashSet<int> _indices = new HashSet<int>();

        public void Use(params DkxToken[] tokens)
        {
            foreach (var token in tokens)
            {
                if (!_indices.Contains(token.Position)) _indices.Add(token.Position);
            }
        }

        public bool IsUsed(DkxToken token) => _indices.Contains(token.Position);
    }
}
