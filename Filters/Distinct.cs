using System.Collections.Generic;
using System.Threading.Tasks.Dataflow;

namespace Open.Threading.Dataflow;

internal class DistinctFilter<T>(
	ITargetBlock<T> target,
	DataflowMessageStatus defaultResponseForDuplicate)
	: TargetBlockFilter<T>(target, defaultResponseForDuplicate, null)
{
    private readonly HashSet<T> _set = [];
#if NET9_0_OR_GREATER
	private readonly System.Threading.Lock _lock = new();
#else
    private readonly object _lock = new();
#endif

    protected override bool Accept(T messageValue)
	{
		bool didntHave;
		lock (_lock) // Assure order of acceptance.
			didntHave = _set.Add(messageValue);
		return didntHave;
	}
}

public static partial class DataFlowExtensions
{
	public static ITargetBlock<T> Distinct<T>(this ITargetBlock<T> target, DataflowMessageStatus defaultResponseForDuplicate) => new DistinctFilter<T>(target, defaultResponseForDuplicate);
}
