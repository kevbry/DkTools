using System;

namespace DKX.Compilation
{
    public class TestApproachAttribute : Attribute
    {
        public TestApproach Approach { get; private set; }
        public bool IgnoreCase { get; set; }

        public TestApproachAttribute(TestApproach approach)
        {
            Approach = approach;
        }
    }

    public enum TestApproach
    {
        Normal,
        OpCodeValidator
    }
}
