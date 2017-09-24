using System.Collections.Generic;
using System.Threading.Tasks.Dataflow;

namespace Open.Dataflow
{

    internal class DistinctFilter<T> : TargetBlockFilter<T>
	{
		private readonly DataflowMessageStatus _defaultResponseForDuplicate;

		private readonly HashSet<T> _set = new HashSet<T>();

		public DistinctFilter(DataflowMessageStatus defaultResponseForDuplicate, ITargetBlock<T> target) : base(target)
		{
			_defaultResponseForDuplicate = defaultResponseForDuplicate;
		}

		// The key here is to reject the message ahead of time.
		public override DataflowMessageStatus OfferMessage(DataflowMessageHeader messageHeader, T messageValue, ISourceBlock<T> source, bool consumeToAccept)
		{
			bool didntHave;
			lock (_target) // Assure order of acceptance.
				didntHave = _set.Add(messageValue);
			if (didntHave)
				return _target.OfferMessage(messageHeader, messageValue, source, consumeToAccept);

			return _defaultResponseForDuplicate;
		}
	}

}