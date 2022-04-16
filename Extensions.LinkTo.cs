using Open.Threading.Tasks;
using System;
using System.Linq;
using System.Threading.Tasks;
using System.Threading.Tasks.Dataflow;

namespace Open.Threading.Dataflow;

[System.Diagnostics.CodeAnalysis.SuppressMessage("Roslynator", "RCS1047:Non-asynchronous method name should not end with 'Async'.")]
public static partial class DataFlowExtensions
{
	public static IDisposable LinkTo<T>(this ISourceBlock<T> producer,
		Action<T> consumer)
		=> producer.LinkTo(new ActionBlock<T>(consumer));

	public static IDisposable LinkToAsync<T>(this ISourceBlock<T> producer,
		Func<T, Task> consumer)
		=> producer.LinkTo(new ActionBlock<T>(consumer));

	public static IDisposable LinkToWithCompletion<T>(this ISourceBlock<T> producer,
		ITargetBlock<T> consumer)
		=> producer.LinkTo(consumer, new DataflowLinkOptions() { PropagateCompletion = true });

	public static T PropagateFaultsTo<T>(this T source, params IDataflowBlock[] targets)
		where T : IDataflowBlock
	{
		_ = source.Completion.OnFaulted(ex =>
		{
			foreach (var target in targets.Where(t => t != null))
				target.Fault(ex.InnerException);
		});
		return source;
	}

	public static T PropagateCompletionTo<T>(this T source, params IDataflowBlock[] targets)
		where T : IDataflowBlock
	{
		_ = source.Completion.ContinueWith(task =>
		{
			foreach (var target in targets.Where(t => t != null))
			{
				if (task.IsFaulted)
					// ReSharper disable once PossibleNullReferenceException
					target.Fault(task.Exception.InnerException);
				else
					target.Complete();
			}
		});
		return source;
	}
}
