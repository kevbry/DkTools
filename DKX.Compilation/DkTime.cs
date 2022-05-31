namespace DKX.Compilation
{
    public struct DkTime
    {
        public static readonly DkTime MinValue = new DkTime(0);
        public static readonly DkTime MaxValue = new DkTime(0xffff);

        private ushort _ticks;

        public DkTime(ushort ticks)
        {
            _ticks = ticks;
        }

        public ushort Ticks => _ticks;

        public override string ToString()
        {
            var ticks = _ticks;
            var seconds = ticks % 30;   // 2-second increments
            ticks /= 30;
            var minutes = ticks % 60;
            ticks /= 60;
            var hours = ticks;

            return $"{hours:00}:{minutes:00}:{seconds:00}";
        }
    }
}
