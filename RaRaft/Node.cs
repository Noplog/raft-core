using System;
using System.Collections.Generic;

namespace RaRaft
{
    public enum NodeState
    {
        Follower,
        Candidate,
        Leader
    }

    public partial class Node<T>
    {
        // TODO: Load durable state on startup (i.e. currentTerm, votedFor)


        public Node(
            INodeReference<T>[] nodes, 
            string name, 
            MonotonicLog<T> log,
            Action<T[]> writeToDurableStorage, 
            Random rand = null)
        {
            this.State = NodeState.Follower;
            this.Nodes = new List<INodeReference<T>>(nodes);
            this.Log = log;
            this.Name = name;
            this.Rand = rand ?? new Random();
            this.MatchIndex = new Dictionary<string, int>();
            this.NextIndex = new Dictionary<string, int>();
            this.WriteToDurableStorage = writeToDurableStorage;

            this.CommitIndex = this.Log.GetHighestIndex();
            ResetElectionTimeout();

        }

        MonotonicLog<T> Log { get; set; }
        int CurrentTerm;
        int CommitIndex;
        int LastApplied { get; set; }
        Dictionary<string, int> NextIndex { get; set; }
        Dictionary<string, int> MatchIndex { get; set; }

        Random Rand { get; set; }
        public NodeState State { get; private set; }
        public List<INodeReference<T>> Nodes { get; private set; }
        public string Name { get; private set; }
        Action<T[]> WriteToDurableStorage { get; set; }
        int Quorum
        {
            get
            {
                return (this.Nodes.Count + 1) / 2;
            }
        }

        bool CheckTerm(int newTerm)
        {
            if (newTerm > this.CurrentTerm)
            {
                CurrentTerm = newTerm;
                VotedFor = null;
                State = NodeState.Follower;
                ClearElection();
                ClearHeartbeat();
                ResetElectionTimeout();
                return false;
            }
            return true;
        }

    }
}
