using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading;
using System.Threading.Tasks;

namespace RaRaft
{
    public partial class Node<T>
    {
        Timer heartbeatTimer = null;
        object leaderSync = new object();

        void StartLeadership()
        {
            lock(leaderSync)
            {
                this.State = NodeState.Leader;
                this.CurrentLeader = this.Name;
                Console.WriteLine($"{this.Name} is elected leader");
                this.MatchIndex.Clear();
                this.NextIndex.Clear();
                foreach (var node in this.Nodes)
                {
                    this.MatchIndex.Add(node.Name, 0);
                    this.NextIndex.Add(node.Name, this.LastApplied + 1);
                }

                // start the heartbeat timer
                SendAppend();
            }
        }

        // not thread safe
        Task<bool> SendAppend()
        {
            ResetHeartbeat();
            var tcs = new TaskCompletionSource<bool>();
            var successCount = 0;
            var completedCount = 0;
            var sync = new object();
            foreach (var node in this.Nodes)
            {
                var nodeMatch = this.MatchIndex[node.Name];
                var nodeNext = this.NextIndex[node.Name];
             
                var entries = this.Log.Scan(nodeNext);
                LogEntry<T> previous = null;
                if (nodeNext - 1 > 0)
                {
                    previous = this.Log.Retrieve(nodeNext - 1);
                }

                node.Append(new AppendRequest<T>
                {
                    Entries = entries,
                    Leader = this.Name,
                    LeaderCommitIndex = this.CommitIndex,
                    Term = this.CurrentTerm,
                    PreviousLogIndex = previous == null ? 0 : previous.Index,
                    PreviousLogTerm = previous == null ? 0 : previous.Term
                }).ContinueWith(x => 
                {
                    lock(sync)
                    {
                        if (x.IsCompleted && null != x.Result)
                        {
                            completedCount++;
                            if (!CheckTerm(x.Result.Term)) return;
                        }

                        if (x.IsCompleted && null != x.Result && x.Result.Success)
                        {
                            if (entries.Length > 0)
                            {
                                this.NextIndex[node.Name] = entries.Select(y => y.Index).Max() + 1;
                                this.MatchIndex[node.Name] = entries.Select(y => y.Index).Max();
                            }
                            successCount++;
                        }

                        if (x.IsCompleted && null != x.Result && !x.Result.Success)
                        {
                            this.NextIndex[node.Name] = Math.Max(0, this.NextIndex[node.Name] -1);
                            // retry
                        }

                        if (successCount + 1 > this.Quorum && null != tcs)
                        {
                            // succeeded in getting a quorum write
                            tcs.TrySetResult(true);
                            tcs = null;
                            return;
                        }

                        if (completedCount == this.Nodes.Count && null != tcs)
                        {
                            // failed to get a quorum write
                            tcs.TrySetResult(false);
                        }
                    }

                });
            }
            return tcs.Task;
        }

        void ClearHeartbeat()
        {
            if (heartbeatTimer != null) heartbeatTimer.Dispose();
            heartbeatTimer = null;
        }

        void ResetHeartbeat()
        {
            if (heartbeatTimer != null) heartbeatTimer.Dispose();
            heartbeatTimer = new Timer(SendHeartbeat, null, 50, int.MaxValue);
        }

        void SendHeartbeat(object _)
        {
            lock(leaderSync)
            {
                this.SendAppend().ContinueWith(x => { });
            }
        }

        object writeToStorageSync = new object();

        public async Task<bool> Write(params T[] entries)
        {
            if (this.State != NodeState.Leader) throw new ApplicationException("The node is not the leader");
            Task<bool> appendResult = null;

            lock(leaderSync)
            {
                var logEntries = entries.Select(x =>
                {
                    Interlocked.Increment(ref this.CommitIndex);
                    return new LogEntry<T>(this.CurrentTerm, this.CommitIndex, x);
                }).ToArray();

                this.Log.Append(logEntries);
                appendResult = SendAppend();
            }

            var success = await appendResult; 
            if (success)
            {
                lock (writeToStorageSync)
                {
                    WriteToDurableStorage(entries);
                }
            }
            return success;
        }

       

    }
}
