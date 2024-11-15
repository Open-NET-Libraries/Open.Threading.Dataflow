using System;
using System.Threading;
using System.Threading.Tasks;
using System.Threading.Tasks.Dataflow;

namespace Open.Threading.Dataflow;

internal abstract class TargetBlockFilterBase<T>(ITargetBlock<T> target)
	: ITargetBlock<T>
{
#if NET9_0_OR_GREATER
    protected readonly System.Threading.Lock SyncLock = new();
#else
	protected readonly object SyncLock = new();
#endif

    protected readonly ITargetBlock<T> Target = target ?? throw new ArgumentNullException(nameof(target));

    protected int _state;
	const int ACCEPTING = 0;
	const int REJECTING = 1;

	protected bool Accepting => _state == ACCEPTING && !Target.Completion.IsCompleted;

	protected void CompleteInternal()
	{
		if (_state == ACCEPTING)
			_ = Interlocked.CompareExchange(ref _state, REJECTING, ACCEPTING);
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
		DataflowMessageHeader messageHeader,
		T messageValue, ISourceBlock<T>? source, bool consumeToAccept)
		=> Accepting
			? Target.OfferMessage(messageHeader, messageValue, source, consumeToAccept)
			: DataflowMessageStatus.DecliningPermanently;
}
