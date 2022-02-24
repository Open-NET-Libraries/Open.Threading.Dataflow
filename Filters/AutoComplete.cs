using System.Threading.Tasks.Dataflow;

namespace Open.Threading.Dataflow;

internal class AutoCompleteFilter<T> : TargetBlockFilterBase<T>
{
	public AutoCompleteFilter(int limit, ITargetBlock<T> target) : base(target) => Limit = limit;

	public int Limit { get; }
	public int AllowedCount { get; private set; }

	// The key here is to reject the message ahead of time.
	public override DataflowMessageStatus OfferMessage(DataflowMessageHeader messageHeader, T messageValue, ISourceBlock<T>? source, bool consumeToAccept)
	{
		var result = DataflowMessageStatus.DecliningPermanently;
		var completed = false;
		// There are multiple operations happening here that require synchronization to get right.
		ThreadSafety.LockConditional(Target,
			() => AllowedCount < Limit,
			() =>
			{
				AllowedCount++;
				result = Target.OfferMessage(messageHeader, messageValue, source, consumeToAccept);
				completed = AllowedCount == Limit;
			}
		);

		if (completed) Target.Complete();

		return result;
	}
}

public static partial class DataFlowExtensions
{
	public static ITargetBlock<T> AutoCompleteAfter<T>(this ITargetBlock<T> target, int limit) => new AutoCompleteFilter<T>(limit, target);
}
