using System.Threading.Tasks.Dataflow;

namespace Open.Threading.Dataflow
{
	internal class ChangedFilter<T> : TargetBlockFilter<T>
	{
		public ChangedFilter(ITargetBlock<T> target, DataflowMessageStatus defaultResponseForDuplicate)
			: base(target, defaultResponseForDuplicate, null)
		{
		}

		readonly object SyncLock = new object();
		T _last = default!;

		protected override bool Accept(T messageValue)
			=> ThreadSafety.LockConditional(
				SyncLock,
				() => !(messageValue is null ? _last is null : messageValue.Equals(_last)),
				() => _last = messageValue);
	}

	public static partial class DataFlowExtensions
	{
		public static ITargetBlock<T> OnlyIfChanged<T>(this ITargetBlock<T> target, DataflowMessageStatus defaultResponseForDuplicate)
			=> new ChangedFilter<T>(target, defaultResponseForDuplicate);
	}
}
