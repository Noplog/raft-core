# An Implementation of Raft

> Do Not Use (yet)

## Usage

The implementation doesn't make any assumptions about the type of messages you want to replicate on the log.
Therefore you need to create a poco class to represent your message:

```c#
[Serializable]
public class MyMessage
{
    public string Value { get; set; }
}
```

Then you need to create an implementation of `INodeReference<T>` which is used to pass messages between nodes:

```c#
// this is a pseudo code implementation which shows how you might
// send requests over http

public class HttpConnector : INodeReference<MyMessage>
{
    private string url;

    public TestConnector(string url)
    {
        this.url = url;
    }

    public string Name
    {
        get
        {
            return this.url;
        }
    }

    public Task<RequestVoteResponse> RequestVote(RequestVoteRequest request)
    {
        return SendRequestVote(url, request);
    }

    public Task<AppendResponse> Append(AppendRequest<MyMessage> request)
    {
        return SendAppend(url, request);
    }
}
```

Then create your node:

```c#
// This code sets up http://server1 which is connected to http://server2 and http://server3
// All three nodes will need setting up in a simliar way 

var connections = new HttpConnection[]{
    new HttpConnector("http://server2"),
    new HttpConnector("http://server3")
};

var log = new MonotonicLog<MyMessage>(new FileStream("node.log", FileMode.OpenOrCreate));

var node = new Node<MyMessage>(
    connections,
    "http://server1",
    log,
    myMessage => { /* write MyMessage to storage */ }
);

```

Then wire up the server  that's going to receive the calls made by your implementation
of `INodeReference`:

```c#
// this is a pseudo code implementation which shows how you might
// receive http requests in MVC, and pass them on to the node

public MyRaftController : Controller
{
    public ActionResult RequestVote(RequestVoteRequest request)
    {
        return Json(node.RequestVote(request));
    }

    public ActionResult Append(AppendRequest request)
    {
        return Json(node.Append(request));
    }
}
```

You can then start writing data to `node.Write(myMessage)` on the leading node, which will result in the
`myMessage => { /* write MyMessage to storage */ }` lambda being called on both the leader and follower nodes.

## Todo

* There's a bug somewhere, sometimes elections fail
* I think there's something wrong in my interpretation of the spec with regard to `nodeIndex` and `matchIndex`
* Each node needs to persist state (such as `term` and `votedFor`)
* The locking code is horrible. Should it be implemented in Akka (or similar) and the locks removed?
* Real implementations are required (like protobuf, HTTP etc...)
* Make parts of it more extendable (i.e. interfaces for the logs)
* Various improvements to the log (documented as todos in the `MonotonicLog.cs` file)
* Tests
* Logging
* Perf

## License

MIT
