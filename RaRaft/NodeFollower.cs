using System;
using System.Linq;
using System.Threading;

namespace RaRaft
{
    public partial class Node<T>
    {
        Timer electionTimeoutTimer;
        object requestVoteSync = new object();
        public string VotedFor { get; private set; }
        public string CurrentLeader { get; private set; }

        public RequestVoteResponse RequestVote(RequestVoteRequest request)
        {
            lock(requestVoteSync)
            {
                this.CheckTerm(request.Term);

                if ((null == this.VotedFor || this.VotedFor == request.Candidate)
                    && (request.Term >= this.CurrentTerm)
                    && (request.LastLogIndex >= this.LastApplied))
                {
                    this.VotedFor = request.Candidate;
                    return new RequestVoteResponse
                    {
                        Term = this.CurrentTerm,
                        VoteGranted = true
                    };
                }

                return new RequestVoteResponse
                {
                    VoteGranted = false,
                    Term = this.CurrentTerm
                };
            }
        }

        object appendSync = new object();

        public AppendResponse Append(AppendRequest<T> request)
        {
            lock(appendSync)
            {
                this.CheckTerm(request.Term);
                ResetElectionTimeout();
                this.CurrentLeader = request.Leader;

                if (request.Term < this.CurrentTerm)
                {
                    return new AppendResponse { Success = false, Term = this.CurrentTerm };
                }

                if (request.PreviousLogIndex != 0 && request.PreviousLogTerm != 0)
                {
                    var previous = this.Log.Retrieve(request.PreviousLogIndex);
                    if (null == previous)
                    {
                        return new AppendResponse { Success = false, Term = this.CurrentTerm };
                    }

                    if (previous.Term != request.Term)
                    {
                        this.Log.WindBackToIndex(request.PreviousLogIndex);
                        return new AppendResponse { Success = false, Term = this.CurrentTerm };
                    }
                }

                if (request.Entries.Any())
                {
                    this.Log.Append(request.Entries);
                    this.CommitIndex = request.Entries.Select(x => x.Index).Max();
                }

                var nextAppliedIndex = Math.Min(request.LeaderCommitIndex, this.CommitIndex);
                if (nextAppliedIndex > this.LastApplied)
                {
                    var logsToCommit = this.Log.Scan(this.LastApplied + 1).Where(x => x.Index <= nextAppliedIndex).ToArray();
                    this.WriteToDurableStorage(logsToCommit.Select(x => x.Value).ToArray());
                    this.LastApplied = nextAppliedIndex;
                }

                return new AppendResponse
                {
                    Success = true,
                    Term = this.CurrentTerm
                };
            }
        }

        void ResetElectionTimeout()
        {
            if (null != electionTimeoutTimer) electionTimeoutTimer.Dispose();
            electionTimeoutTimer = new Timer(EndOfElectionTimeout, null, Rand.Next(150, 300), int.MaxValue);
        }

        void ClearElectionTimeout()
        {
            if (null != electionTimeoutTimer) electionTimeoutTimer.Dispose();
            electionTimeoutTimer = null;
        }

        void EndOfElectionTimeout(object _)
        {
            ClearElectionTimeout();
            StartLeaderElection();
        }



       
    }
}
