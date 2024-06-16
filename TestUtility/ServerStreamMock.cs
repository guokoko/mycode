using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using Grpc.Core;

namespace TestUtility
{
    public class ServerStreamMock<T> : IServerStreamWriter<T>, IAsyncStreamReader<T>
    {
        public List<T> Messages { get; } = new List<T>();

        public T Current { get; set; }

        int index;

        public ServerStreamMock()
        {
            WriteOptions = null!;
            Current = Messages.FirstOrDefault();
            index = 0;
        }

        public ServerStreamMock(List<T> messages) : this()
        {
            Messages = messages;
        }

        public Task WriteAsync(T message)
        {
            Messages.Add(message);
            return Task.CompletedTask;
        }

        public WriteOptions WriteOptions { get; set; }
        public Task<bool> MoveNext(CancellationToken cancellationToken)
        {
            ++index;
            if (Messages.Count >= index)
            {
                Current = Messages[index - 1];
                return Task.FromResult(true);
            }
            else
            {
                return Task.FromResult(false);
            }
        }
    }
}