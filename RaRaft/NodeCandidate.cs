using System;
using System.Threading;

namespace RaRaft
{

    public partial class Node<T>
    {

        int GetElectionPeriod()
        {
            return 300;
        }

        bool inElection { get; set; }
        Timer electionTimer { get; set; }

        void StartLeaderElection()
        {
            Console.WriteLine($"T{this.CurrentTerm} {this.Name} is standing for election");

            this.State = NodeState.Candidate;
            Interlocked.Increment(ref this.CurrentTerm);
            ClearElection();
            this.VotedFor = this.Name;
            inElection = true;
            electionTimer = new Timer(EndLeaderElection, null, GetElectionPeriod(), int.MaxValue);
            var votes = 1;
            var done = false;
            var sync = new object();
            foreach (var node in this.Nodes)
            {
                node.RequestVote(new RequestVoteRequest
                {
                    Candidate = this.Name,
                    Term = this.CurrentTerm
                }).ContinueWith(x => 
                {
                    lock(sync)
                    {
                        if (x.IsCompleted && null != x.Result)
                        {
                            if (!this.CheckTerm(x.Result.Term)) return;
                        }

                        if (x.IsCompleted && null != x.Result && x.Result.VoteGranted && inElection)
                        {
                            votes++;
                            if (votes > this.Quorum && !done)
                            {
                                done = true;
                                ClearElection();
                                StartLeadership();
                            }
                        }
                    }

                });
            }
        }

        void ClearElection()
        {
            inElection = false;
            if (electionTimer != null) electionTimer.Dispose();
            electionTimer = null;
        }

        void EndLeaderElection(object _)
        {
            if (this.State == NodeState.Candidate)
            {
                // we failed to get a quorum, restart the voting
                StartLeaderElection();
            }
        }

       
    }
}
