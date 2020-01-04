using System;
using System.Threading;
using System.Threading.Tasks;
using System.Threading.Tasks.Dataflow;

namespace Open.Threading.Dataflow
{
	internal abstract class TargetBlockFilterBase<T> : ITargetBlock<T>
	{
		protected readonly ITargetBlock<T> Target;

		protected TargetBlockFilterBase(ITargetBlock<T> target)
		{
			Target = target ?? throw new ArgumentNullException(nameof(target));
		}

		protected int _state;
		const int ACCEPTING = 0;
		const int REJECTING = 1;

		protected bool Accepting => _state == ACCEPTING && !Target.Completion.IsCompleted;

		protected void CompleteInternal()
		{
			if (_state == ACCEPTING)
				Interlocked.CompareExchange(ref _state, REJECTING, ACCEPTING);
		}

		public void Complete()
		{
			CompleteInternal();
			Target.Complete();
		}

		public void Fault(Exception exception)
		{
			CompleteInternal();
			Target.Fault(exception);
		}

		public Task Completion => Target.Completion;

		// The key here is to reject the message ahead of time.
		public virtual DataflowMessageStatus OfferMessage(
			DataflowMessageHeader messageHeader, T messageValue, ISourceBlock<T> source, bool consumeToAccept)
			=> Accepting
				? Target.OfferMessage(messageHeader, messageValue, source, consumeToAccept)
				: DataflowMessageStatus.DecliningPermanently;
	}
}
