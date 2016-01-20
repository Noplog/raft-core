namespace RaRaft
{
    public class LogEntry<T>
    {
        public LogEntry(int term, int index, T value)
        {
            this.Term = term;
            this.Index = index;
            this.Value = value;
        }
        
        public int Index { get; private set; }
        public int Term { get; private set; }
        public T Value { get; private set; }
    }
}
