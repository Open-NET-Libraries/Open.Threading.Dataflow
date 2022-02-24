using System.Collections.Generic;
using System.Threading.Tasks.Dataflow;

namespace Open.Threading.Dataflow;

internal class DistinctFilter<T> : TargetBlockFilter<T>
{
	public DistinctFilter(ITargetBlock<T> target, DataflowMessageStatus defaultResponseForDuplicate)
		: base(target, defaultResponseForDuplicate, null)
	{
	}

	private readonly HashSet<T> _set = new();

	protected override bool Accept(T messageValue)
	{
		bool didntHave;
		lock (Target) // Assure order of acceptance.
			didntHave = _set.Add(messageValue);
		return didntHave;
	}
}

public static partial class DataFlowExtensions
{
	public static ITargetBlock<T> Distinct<T>(this ITargetBlock<T> target, DataflowMessageStatus defaultResponseForDuplicate) => new DistinctFilter<T>(target, defaultResponseForDuplicate);
}
