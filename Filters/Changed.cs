using System.Threading.Tasks.Dataflow;
using Open.Threading;

namespace Open.Dataflow
{
    internal class ChangedFilter<T> : TargetBlockFilter<T>
	{
		readonly DataflowMessageStatus _defaultResponseForDuplicate;

		T _last;

		public ChangedFilter(DataflowMessageStatus defaultResponseForDuplicate, ITargetBlock<T> target) : base(target)
		{
			_defaultResponseForDuplicate = defaultResponseForDuplicate;
		}

		// The key here is to reject the message ahead of time.
		public override DataflowMessageStatus OfferMessage(DataflowMessageHeader messageHeader, T messageValue, ISourceBlock<T> source, bool consumeToAccept)
		{
			return ThreadSafety.LockConditional(
					_target,
					() => !messageValue.Equals(_last),
					() => _last = messageValue)
				? _target.OfferMessage(messageHeader, messageValue, source, consumeToAccept)
				: _defaultResponseForDuplicate;
		}
	}
}