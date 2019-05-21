﻿using Open.Threading;
using System.Threading.Tasks.Dataflow;

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

    public static partial class DataFlowExtensions
    {
        public static ITargetBlock<T> OnlyIfChanged<T>(this ITargetBlock<T> target, DataflowMessageStatus defaultResponseForDuplicate)
            => new ChangedFilter<T>(defaultResponseForDuplicate, target);
    }
}
