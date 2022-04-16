using System;

namespace Open.Threading.Dataflow;

public static partial class DataFlowExtensions
{
	class Observer<T> : IObserver<T>, IDisposable
	{
		public static IObserver<T> New(
			Action<T>? onNext,
			Action<Exception>? onError,
			Action? onCompleted)
			=> new Observer<T>()
			{
				_onNext = onNext,
				_onError = onError,
				_onCompleted = onCompleted
			};

		Action? _onCompleted;
		Action<Exception>? _onError;
		Action<T>? _onNext;

		public void OnNext(T value) => _onNext?.Invoke(value);

		public void OnError(Exception error) => _onError?.Invoke(error);

		public void OnCompleted() => _onCompleted?.Invoke();

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
		Action? onCompleted = null)
		=> observable.Subscribe(Observer<T>.New(onNext, onError, onCompleted));

	public static IDisposable Subscribe<T>(this IObservable<T> observable,
		Action<T> onNext,
		Action? onCompleted = null)
		=> observable.Subscribe(Observer<T>.New(onNext, null, onCompleted));
}
