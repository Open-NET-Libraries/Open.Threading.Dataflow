using System.Threading.Tasks.Dataflow;

namespace Open.Threading.Dataflow;

internal class ChangedFilter<T>(ITargetBlock<T> target, DataflowMessageStatus defaultResponseForDuplicate)
	: TargetBlockFilter<T>(target, defaultResponseForDuplicate, null)
{
	T _last = default!;

	protected override bool Accept(T messageValue)
		=> ThreadSafety.LockConditional(
			SyncLock,
			() => messageValue is not null ? messageValue.Equals(_last) : _last is null,
			() => _last = messageValue);
}

public static partial class DataFlowExtensions
{
	public static ITargetBlock<T> OnlyIfChanged<T>(this ITargetBlock<T> target, DataflowMessageStatus defaultResponseForDuplicate) => new ChangedFilter<T>(target, defaultResponseForDuplicate);
}
