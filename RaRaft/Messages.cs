namespace RaRaft
{
    public class AppendRequest<T>
    {
        public int Term { get; set; }
        public string Leader { get; set; }
        public int PreviousLogIndex { get; set; }
        public int PreviousLogTerm { get; set; }
        /// <summary>
        /// Empty for heartbeat
        /// </summary>
        public LogEntry<T>[] Entries { get; set; }
        public int LeaderCommitIndex { get; set; }
    }

    public class AppendResponse
    {
        public int Term { get; set; }
        public bool Success { get; set; }
    }

    public class RequestVoteRequest
    {
        public int Term { get; set; }
        public string Candidate { get; set; }
        public int LastLogIndex { get; set; }
        public int LastLogTerm { get; set; }
    }

    public class RequestVoteResponse
    {
        public int Term { get; set; }
        public bool VoteGranted { get; set; }
    }

   

}
