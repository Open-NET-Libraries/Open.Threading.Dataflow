using System;
using System.Threading.Tasks;
using System.Threading.Tasks.Dataflow;

namespace Open.Threading.Dataflow
{
    internal sealed class AcceptOrPassBlock<T> : ITargetBlock<T>
    {
        private readonly Func<T, bool> _handler;
        private readonly ITargetBlock<T> _completer;

        private AcceptOrPassBlock()
        {
            _completer = new ActionBlock<T>(e => { });
            Completion = _completer.Completion;
        }

        public AcceptOrPassBlock(Func<T, bool> handler) : this()
        {
            _handler = handler ?? throw new ArgumentNullException(nameof(handler));
        }

        public DataflowMessageStatus OfferMessage(
            DataflowMessageHeader messageHeader,
            T messageValue, ISourceBlock<T> source,
            bool consumeToAccept)
        {
            if (_completer.Completion.IsCompleted)
                return DataflowMessageStatus.DecliningPermanently;

            return _handler(messageValue)
                ? DataflowMessageStatus.Accepted
                : DataflowMessageStatus.Declined;
        }

        public void Complete()
            => _completer.Complete();

        public void Fault(Exception exception)
            => _completer.Fault(exception);

        public Task Completion { get; }
    }

    public static partial class DataFlowExtensions
    {
        /// <summary>
        /// Processes an item through an acceptor function.
        /// If the acceptor returns true, then it was accepted and subsequently received/taken from the source.
        /// </summary>
        /// <typeparam name="T"></typeparam>
        /// <param name="source">The source block to receive from.</param>
        /// <param name="acceptor">The function to process the item and decide if accepted.</param>
        /// <returns>The original source block to allow for more acceptors or filters to be applied.</returns>
        public static ISourceBlock<T> AcceptOrPass<T>(this ISourceBlock<T> source,
            Func<T, bool> acceptor)
        {
            var receiver = new AcceptOrPassBlock<T>(acceptor);
            source.LinkToWithCompletion(receiver);
            return source;
        }
    }
}
