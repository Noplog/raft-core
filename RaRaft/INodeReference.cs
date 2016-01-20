using System.Threading.Tasks;

namespace RaRaft
{
    /// <summary>
    /// Implement this interface to connect nodes together with whatever serializers and protocols you want
    /// </summary>
    public interface INodeReference<T>
    {
        string Name { get; }

        Task<RequestVoteResponse> RequestVote(RequestVoteRequest request);

        Task<AppendResponse> Append(AppendRequest<T> request);

    }
}
