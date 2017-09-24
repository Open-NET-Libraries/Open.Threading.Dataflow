using System;
using System.Linq;
using System.Threading.Tasks;
using System.Threading.Tasks.Dataflow;
using Open.Threading;

namespace System.Threading.Tasks.Dataflow
{
	public static class TransformBlock
	{
		public static TransformBlock<Tin, Tout> New<Tin, Tout>(Func<Tin, Tout> pipe)
		{
			return new TransformBlock<Tin, Tout>(pipe);
		}

		public static TransformBlock<Tin, Tout> New<Tin, Tout>(Func<Tin, Tout> pipe, ExecutionDataflowBlockOptions options)
		{
			return new TransformBlock<Tin, Tout>(pipe, options);
		}

		public static TransformBlock<Tin, Tout> New<Tin, Tout>(Func<Tin, Tout> pipe, int maxParallel)
		{
			return new TransformBlock<Tin, Tout>(pipe, new ExecutionDataflowBlockOptions() { MaxDegreeOfParallelism = maxParallel });
		}

		public static TransformBlock<Tin, Task<Tout>> NewAsync<Tin, Tout>(Func<Tin, Task<Tout>> pipe)
		{
			return new TransformBlock<Tin, Task<Tout>>(pipe);
		}

		public static TransformBlock<Tin, Task<Tout>> NewAsync<Tin, Tout>(Func<Tin, Task<Tout>> pipe, ExecutionDataflowBlockOptions options)
		{
			return new TransformBlock<Tin, Task<Tout>>(pipe, options);
		}

		public static TransformBlock<Tin, Task<Tout>> NewAsync<Tin, Tout>(Func<Tin, Task<Tout>> pipe, int maxParallel)
		{
			return new TransformBlock<Tin, Task<Tout>>(pipe, new ExecutionDataflowBlockOptions() { MaxDegreeOfParallelism = maxParallel });
		}
	}

	public static class ActionBlock
	{
		public static ActionBlock<T> New<T>(Action<T> action)
		{
			return new ActionBlock<T>(action);
		}

		public static ActionBlock<T> New<T>(Action<T> action, ExecutionDataflowBlockOptions options)
		{
			return new ActionBlock<T>(action, options);
		}

		public static ActionBlock<T> New<T>(Action<T> consumer, int maxParallel)
		{
			return new ActionBlock<T>(consumer, new ExecutionDataflowBlockOptions() { MaxDegreeOfParallelism = maxParallel });
		}

		public static ActionBlock<T> NewAsync<T>(Func<T, Task> action)
		{
			return new ActionBlock<T>(action);
		}

		public static ActionBlock<T> NewAsync<T>(Func<T, Task> action, ExecutionDataflowBlockOptions options)
		{
			return new ActionBlock<T>(action, options);
		}

		public static ActionBlock<T> NewAsync<T>(Func<T, Task> consumer, int maxParallel)
		{
			return new ActionBlock<T>(consumer, new ExecutionDataflowBlockOptions() { MaxDegreeOfParallelism = maxParallel });
		}
	}
}

namespace Open.Dataflow
{

	public static class DataFlowExtensions
	{
		public static ITargetBlock<T> AutoCompleteAfter<T>(this ITargetBlock<T> target, int limit)
		{
			return new AutoCompleteFilter<T>(limit, target);
		}

		public static ITargetBlock<T> Distinct<T>(this ITargetBlock<T> target, DataflowMessageStatus defaultResponseForDuplicate)
		{
			return new DistinctFilter<T>(defaultResponseForDuplicate, target);
		}

		public static ITargetBlock<T> OnlyIfChanged<T>(this ITargetBlock<T> target, DataflowMessageStatus defaultResponseForDuplicate)
		{
			return new ChangedFilter<T>(defaultResponseForDuplicate, target);
		}

		public static TransformBlock<Tin, Tout> Pipe<Tin, Tout>(this ISourceBlock<Tin> source, Func<Tin, Tout> pipe)
		{
			var output = TransformBlock.New(pipe);
			source.LinkToWithExceptions(output);
			return output;
		}

		public static TransformBlock<Tin, Tout> Pipe<Tin, Tout>(this ISourceBlock<Tin> source, Func<Tin, Tout> pipe, ExecutionDataflowBlockOptions options)
		{
			var output = TransformBlock.New(pipe, options);
			source.LinkToWithExceptions(output);
			return output;
		}

		public static TransformBlock<Tin, Tout> Pipe<Tin, Tout>(this ISourceBlock<Tin> source, Func<Tin, Tout> pipe, int maxParallel)
		{
			var output = TransformBlock.New(pipe, new ExecutionDataflowBlockOptions() { MaxDegreeOfParallelism = maxParallel });
			source.LinkToWithExceptions(output);
			return output;
		}

		public static IDisposable LinkTo<T>(this ISourceBlock<T> producer, Action<T> consumer)
		{
			return producer.LinkTo(new ActionBlock<T>(consumer));
		}

		public static IDisposable LinkToWithExceptions<T>(this ISourceBlock<T> producer, ITargetBlock<T> consumer)
		{
			return producer.LinkTo(consumer, new DataflowLinkOptions() { PropagateCompletion = true });
		}

		public static ISourceBlock<T> Buffer<T>(this ISourceBlock<T[]> source)
		{
			var output = new BufferBlock<T>();
			var input = new ActionBlock<T[]>(array =>
			{
				foreach (var value in array)
					output.Post(value);
			});
			source.LinkToWithExceptions(input);

			return output;
		}

		public static T PropagateFaultsTo<T>(this T source, params IDataflowBlock[] targets)
		where T : IDataflowBlock
		{
			source.Completion.OnFaulted(ex =>
			{
				foreach (var target in targets.Where(t => t != null))
					target.Fault(ex.InnerException);
			});
			return source;
		}

		public static T PropagateCompletionTo<T>(this T source, params IDataflowBlock[] targets)
			where T : IDataflowBlock
		{
			source.Completion.ContinueWith(task =>
			{
				foreach (var target in targets.Where(t => t != null))
				{
					if (task.IsFaulted)
						target.Fault(task.Exception.InnerException);
					else
						target.Complete();
				}
			});
			return source;
		}
		public static T OnComplete<T>(this T source, Action oncomplete)
			where T : IDataflowBlock
		{
			source.Completion.ContinueWith(task => oncomplete());
			return source;
		}

		public static T OnComplete<T>(this T source, Action<Task> oncomplete)
			where T : IDataflowBlock
		{
			source.Completion.ContinueWith(oncomplete);
			return source;
		}

		public static T OnFault<T>(this T source, Action<Exception> onfault)
			where T : IDataflowBlock
		{
			source.Completion.OnFaulted(task => onfault(task.InnerException));
			return source;
		}

		public static void Fault(this IDataflowBlock target, string message)
		{
			target.Fault(new Exception(message));
		}

		public static void Fault(this IDataflowBlock target, string message, Exception innerException)
		{
			target.Fault(new Exception(message, innerException));
		}


		class Observer<T> : IObserver<T>, IDisposable
		{
			public static IObserver<T> New(
				Action<T> onNext,
				Action<Exception> onError,
				Action onCompleted
			)
			{
				return new Observer<T>()
				{
					_onNext = onNext,
					_onError = onError,
					_onCompleted = onCompleted
				};
			}

			Action _onCompleted;
			Action<Exception> _onError;
			Action<T> _onNext;


			public void OnNext(T value)
			{
				if (_onNext != null) _onNext(value);
			}

			public void OnError(Exception error)
			{
				if (_onError != null) _onError(error);
			}

			public void OnCompleted()
			{
				if (_onCompleted != null) _onCompleted();
			}


			public void Dispose()
			{
				_onNext = null;
				_onError = null;
				_onCompleted = null;
			}
		}

		public static IDisposable Subscribe<T>(this IObservable<T> observable,
			Action<T> onNext,
			Action<Exception> onError,
			Action onCompleted = null)
		{
			return observable.Subscribe(Observer<T>.New(onNext, onError, onCompleted));
		}

		public static IDisposable Subscribe<T>(this IObservable<T> observable,
			Action<T> onNext,
			Action onCompleted = null)
		{
			return observable.Subscribe(Observer<T>.New(onNext, null, onCompleted));
		}

	}
}
