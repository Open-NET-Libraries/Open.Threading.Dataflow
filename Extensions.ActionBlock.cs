using System;
using System.Threading.Tasks;
using System.Threading.Tasks.Dataflow;

namespace Open.Threading.Dataflow
{
	public static class ActionBlock
	{
		public static ActionBlock<T> New<T>(Action<T> action) => new(action);

		public static ActionBlock<T> New<T>(Action<T> action, ExecutionDataflowBlockOptions options) => new(action, options);

		public static ActionBlock<T> New<T>(Action<T> consumer, int maxParallel) => new(consumer, new ExecutionDataflowBlockOptions() { MaxDegreeOfParallelism = maxParallel });

		public static ActionBlock<T> NewAsync<T>(Func<T, Task> action) => new(action);

		public static ActionBlock<T> NewAsync<T>(Func<T, Task> action, ExecutionDataflowBlockOptions options) => new(action, options);

		public static ActionBlock<T> NewAsync<T>(Func<T, Task> consumer, int maxParallel) => new(consumer, new ExecutionDataflowBlockOptions() { MaxDegreeOfParallelism = maxParallel });
	}
}
