using System.Collections.Generic;

namespace DKX.Compilation.Scopes
{
    class FlowTrace
    {
        private HashSet<string> _variablesInitialized;
        private bool _isEnded;

        public FlowTrace()
        {
        }

        public FlowTrace(FlowTrace clone)
        {
            if (clone._variablesInitialized != null) _variablesInitialized = new HashSet<string>(clone._variablesInitialized);
            _isEnded = clone._isEnded;
        }

        public bool IsEnded => _isEnded;

        public void OnVariableAssigned(string variableWbdkName)
        {
            if (_variablesInitialized == null) _variablesInitialized = new HashSet<string>();
            _variablesInitialized.Add(variableWbdkName);
        }

        public bool IsVariableInitialized(string variableWbdkName) => _variablesInitialized?.Contains(variableWbdkName) ?? false;

        public void OnBranchEnded() => _isEnded = true;

        public void MergeBranches(IEnumerable<FlowTrace> branches)
        {
            var numBranches = 0;
            var variableAssignments = new Dictionary<string, int>();
            var numEnded = 0;

            foreach (var branch in branches)
            {
                numBranches++;

                if (branch._variablesInitialized != null)
                {
                    foreach (var vi in branch._variablesInitialized)
                    {
                        if (variableAssignments.ContainsKey(vi)) variableAssignments[vi]++;
                        else variableAssignments[vi] = 1;
                    }
                }

                if (branch._isEnded) numEnded++;
            }

            foreach (var va in variableAssignments)
            {
                if (va.Value == numBranches)
                {
                    // Assigned in all branches
                    if (_variablesInitialized == null) _variablesInitialized = new HashSet<string>();
                    _variablesInitialized.Add(va.Key);
                }
            }

            if (!_isEnded && numEnded == numBranches) _isEnded = true;
        }
    }
}
