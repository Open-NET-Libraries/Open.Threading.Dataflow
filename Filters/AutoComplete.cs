using Open.Threading;
using System.Threading.Tasks.Dataflow;

namespace Open.Dataflow
{

	internal class AutoCompleteFilter<T> : TargetBlockFilter<T>
	{
		public AutoCompleteFilter(int limit, ITargetBlock<T> target) : base(target)
		{
			Limit = limit;
		}

		public int Limit { get; }
		public int AllowedCount { get; private set; }


		// The key here is to reject the message ahead of time.
		public override DataflowMessageStatus OfferMessage(DataflowMessageHeader messageHeader, T messageValue, ISourceBlock<T> source, bool consumeToAccept)
		{
			var result = DataflowMessageStatus.DecliningPermanently;
			var completed = false;
			// There are multiple operations happening here that require synchronization to get right.
			ThreadSafety.LockConditional(_target,
				() => AllowedCount < Limit,
				() =>
				{
					AllowedCount++;
					result = _target.OfferMessage(messageHeader, messageValue, source, consumeToAccept);
					completed = AllowedCount == Limit;
				}
			);

			if (completed) _target.Complete();

			return result;
		}
	}

}
