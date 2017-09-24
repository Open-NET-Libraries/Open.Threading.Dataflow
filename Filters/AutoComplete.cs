using System.Threading.Tasks.Dataflow;
using Open.Threading;

namespace Open.Dataflow
{

    internal class AutoCompleteFilter<T> : TargetBlockFilter<T>
	{
		public AutoCompleteFilter(int limit, ITargetBlock<T> target) : base(target)
		{
			_limit = limit;
		}

		private readonly int _limit;
		public int Limit
		{
			get { return _limit; }
		}

		private int _allowed = 0;
		public int AllowedCount
		{
			get { return _allowed; }
		}



		// The key here is to reject the message ahead of time.
		public override DataflowMessageStatus OfferMessage(DataflowMessageHeader messageHeader, T messageValue, ISourceBlock<T> source, bool consumeToAccept)
		{
			var result = DataflowMessageStatus.DecliningPermanently;
			var completed = false;
			// There are multiple operations happening here that require synchronization to get right.
			ThreadSafety.LockConditional(_target,
				() => _allowed < _limit,
				() =>
				{
					_allowed++;
					result = _target.OfferMessage(messageHeader, messageValue, source, consumeToAccept);
					completed = _allowed == _limit;
				}
			);

			if (completed) _target.Complete();

			return result;
		}
	}

}