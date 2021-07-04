using System;
using System.Threading.Tasks.Dataflow;

namespace Open.Threading.Dataflow
{
	internal class TargetBlockFilter<T> : TargetBlockFilterBase<T>
	{
		private readonly DataflowMessageStatus _filterDeclineStatus;
		private readonly Func<T, bool>? _filter;

		public TargetBlockFilter(
			ITargetBlock<T> target,
			DataflowMessageStatus filterDeclineStatus,
			Func<T, bool>? filter) : base(target)
		{
			_filter = filter;
			switch (filterDeclineStatus)
			{
				case DataflowMessageStatus.Postponed:
				case DataflowMessageStatus.NotAvailable:
					throw new ArgumentException("Block filter does not support: " + Enum.GetName(typeof(DataflowMessageStatus), filterDeclineStatus));

			}

			_filterDeclineStatus = filterDeclineStatus;

		}

		private static readonly ITargetBlock<T> NullTarget = DataflowBlock.NullTarget<T>();

		protected virtual bool Accept(T messageValue)
			=> _filter!(messageValue);

		public override DataflowMessageStatus OfferMessage(
			DataflowMessageHeader messageHeader, T messageValue, ISourceBlock<T>? source, bool consumeToAccept)
		{
			if (!Accepting)
				return DataflowMessageStatus.DecliningPermanently;

			if (Accept(messageValue))
				return base.OfferMessage(messageHeader, messageValue, source, consumeToAccept);

			if (_filterDeclineStatus == DataflowMessageStatus.Accepted)
				return NullTarget.OfferMessage(messageHeader, messageValue, source, consumeToAccept);

			return _filterDeclineStatus;
		}
	}


	public static partial class DataFlowExtensions
	{
		/// <summary>
		/// Synchronously filters out incomming messages before they arrive to the target.
		/// Passes the message to the target if the filter function returns true.
		/// If the filter function returns false, then the message will either be accepted and disappear (default), or will be declined if the decli
		/// </summary>
		/// <typeparam name="T">The message type.</typeparam>
		/// <param name="target">The Target-Block to pass messages too.</param>
		/// <param name="filter">Passes the message to the target if the filter function returns true.</param>
		/// <param name="decline">If true, and the filter returns false, the message will be declined, allowing it to continue elsewhere.  If false (default) the message is accepted even though it is not being passed to the target.</param>
		/// <returns>A filter block that preceeds the target..</returns>
		public static ITargetBlock<T> Filter<T>(this ITargetBlock<T> target,
			Func<T, bool> filter,
			bool decline = false)
			=> new TargetBlockFilter<T>(
				target,
				decline ? DataflowMessageStatus.Declined : DataflowMessageStatus.Accepted,
				filter);


		/// <summary>
		/// Processes an item through an acceptor function.
		/// If the acceptor returns true, then it was accepted and subsequently received/taken from the source (no longer available to downstream targets).
		/// </summary>
		/// <typeparam name="T">The message type</typeparam>
		/// <param name="source">The source block to receive from.</param>
		/// <param name="acceptor">The function to process the item and decide if accepted.</param>
		/// <returns>The original source block to allow for more acceptors or filters to be applied.</returns>
		public static ISourceBlock<T> TakeOrContinue<T>(this ISourceBlock<T> source,
			Func<T, bool> acceptor)
		{
			var receiver = DataflowBlock
				.NullTarget<T>() // If the acceptor returns true, the message is dropped.
				.Filter(acceptor, true); // Else it's declined and continues on to the next target.

			source.LinkToWithCompletion(receiver);
			return source;
		}
	}

}
