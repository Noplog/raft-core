using RaRaft;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace ConsoleApplication1
{
    /// <summary>
    /// Use this test connector to connect together nodes in the same process for testing
    /// </summary>
    public class TestConnector : INodeReference<TestMessage>
    {
        const int NETWORK_DELAY = 10;

        public Node<TestMessage> Node { get; private set; }

        public string Name
        {
            get
            {
                return this.Node.Name;
            }
        }

        public TestConnector(Node<TestMessage> node)
        {
            this.Node = node;
        }

        public async Task<RequestVoteResponse> RequestVote(RequestVoteRequest request)
        {
            await Task.Delay(NETWORK_DELAY);
            var result = this.Node.RequestVote(request);
            await Task.Delay(NETWORK_DELAY);
            Console.WriteLine($"T{request.Term} {this.Node.Name} voted {(result.VoteGranted ? "yes" : "no")} for candidate {request.Candidate}");
            return result;
        }

        public async Task<AppendResponse> Append(AppendRequest<TestMessage> request)
        {
            await Task.Delay(NETWORK_DELAY);
           /* if (request.Entries.Any())
            {
                Console.WriteLine($"T{request.Term} {request.Leader} -> {this.Node.Name} = {request.PreviousLogTerm}.{request.PreviousLogIndex}");
            }*/
            var result = this.Node.Append(request);
            //Console.WriteLine($"T{request.Term} {request.Leader} -> {this.Node.Name} = {result.Success}");
            await Task.Delay(NETWORK_DELAY);
            return result;
        }
    }
}
