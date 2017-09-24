using System;
using System.Threading.Tasks;
using System.Threading.Tasks.Dataflow;

namespace Open.Dataflow
{

    internal abstract class TargetBlockFilter<T> : ITargetBlock<T>
	{
		protected readonly ITargetBlock<T> _target;

		public TargetBlockFilter(ITargetBlock<T> target)
		{
			_target = target;
		}

		public Task Completion
		{
			get
			{
				return _target.Completion;
			}
		}

		public void Complete()
		{
			_target.Complete();
		}

		public void Fault(Exception exception)
		{
			_target.Fault(exception);
		}

		// The key here is to reject the message ahead of time.
		public abstract DataflowMessageStatus OfferMessage(DataflowMessageHeader messageHeader, T messageValue, ISourceBlock<T> source, bool consumeToAccept);
	}

}