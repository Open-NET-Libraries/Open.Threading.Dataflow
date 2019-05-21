using System;
using System.Diagnostics.Contracts;
using System.Threading;
using System.Threading.Tasks;
using System.Threading.Tasks.Dataflow;


namespace Open.Threading.Dataflow
{
    public static partial class DataFlowExtensions
    {
        /// <summary>
        /// Produces a source block that contains transformed results.
        /// </summary>
        /// <typeparam name="T">The input type.</typeparam>
        /// <param name="source">The source block to receive from.</param>
        /// <param name="transform">The transfrom function to apply.</param>
        /// <param name="options">Optional execution options.</param>
        /// <returns>The source block created.</returns>
        public static IReceivableSourceBlock<TOut> Pipe<TIn, TOut>(this ISourceBlock<TIn> source,
            Func<TIn, TOut> transform, ExecutionDataflowBlockOptions options = null)
        {
            if (source == null)
                throw new NullReferenceException();
            if (transform == null)
                throw new ArgumentNullException(nameof(transform));
            Contract.EndContractBlock();

            var output = TransformBlock.New(transform, options);
            source.LinkToWithCompletion(output);
            return output;
        }

        /// <summary>
        /// Produces a source block that contains transformed results.
        /// </summary>
        /// <typeparam name="T">The input type.</typeparam>
        /// <param name="source">The source block to receive from.</param>
        /// <param name="transform">The transfrom function to apply.</param>
        /// <param name="options">Optional execution options.</param>
        /// <returns>The source block created.</returns>
        public static IReceivableSourceBlock<TOut> PipeAsync<TIn, TOut>(this ISourceBlock<TIn> source,
            Func<TIn, Task<TOut>> transform, ExecutionDataflowBlockOptions options = null)
        {
            if (source == null)
                throw new NullReferenceException();
            if (transform == null)
                throw new ArgumentNullException(nameof(transform));
            Contract.EndContractBlock();

            var output = TransformBlock.NewAsync(transform, options);
            source.LinkToWithCompletion(output);
            return output;
        }

        /// <summary>
        /// Pipes the source data through a single producer constrained ActionBlock.
        /// </summary>
        /// <typeparam name="T">The input type.</typeparam>
        /// <param name="source">The source block to receive from.</param>
        /// <param name="handler">The handler function to apply.</param>
        /// <param name="options">Optional execution options.</param>
        /// <returns>The ActionBlock created.</returns>
        public static ActionBlock<T> Pipe<T>(this ISourceBlock<T> source,
            Action<T> handler,
            ExecutionDataflowBlockOptions options = null)
        {
            if (source == null)
                throw new NullReferenceException();
            if (handler == null)
                throw new ArgumentNullException(nameof(handler));
            Contract.EndContractBlock();

            var receiver = options == null
                ? new ActionBlock<T>(handler)
                : new ActionBlock<T>(handler, options);

            source.LinkToWithCompletion(receiver);
            return receiver;
        }

        /// <summary>
        /// Pipes the source data through a single producer constrained ActionBlock.
        /// </summary>
        /// <typeparam name="T">The input type.</typeparam>
        /// <param name="source">The source block to receive from.</param>
        /// <param name="handler">The handler function to apply.</param>
        /// <param name="options">Optional execution options.</param>
        /// <returns>The ActionBlock created.</returns>
        public static ActionBlock<T> PipeAsync<T>(this ISourceBlock<T> source,
            Func<T, Task> handler,
            ExecutionDataflowBlockOptions options = null)
        {
            if (source == null)
                throw new NullReferenceException();
            if (handler == null)
                throw new ArgumentNullException(nameof(handler));
            Contract.EndContractBlock();

            var receiver = options == null
                ? new ActionBlock<T>(handler)
                : new ActionBlock<T>(handler, options);

            source.LinkToWithCompletion(receiver);
            return receiver;
        }

        /// <summary>
        /// Pipes the source data through a single producer constrained ActionBlock with the specified max concurrency.
        /// </summary>
        /// <typeparam name="T">The input type.</typeparam>
        /// <param name="source">The source block to receive from.</param>
        /// <param name="maxConcurrency">The maximum concurrency of the action block</param>
        /// <param name="handler">The handler function to apply.</param>
        /// <param name="cancellationToken">An optional cancellation token.</param>
        /// <returns>The ActionBlock created.</returns>
        public static ActionBlock<T> PipeConcurrently<T>(this ISourceBlock<T> source,
            int maxConcurrency,
            Action<T> handler,
            CancellationToken cancellationToken = default)
            => source.Pipe(handler, new ExecutionDataflowBlockOptions
            {
                MaxDegreeOfParallelism = maxConcurrency,
                SingleProducerConstrained = true,
                CancellationToken = cancellationToken
            });

        /// <summary>
        /// Pipes the source data through a single producer constrained ActionBlock with the specified max concurrency.
        /// </summary>
        /// <typeparam name="T">The input type.</typeparam>
        /// <param name="source">The source block to receive from.</param>
        /// <param name="maxConcurrency">The maximum concurrency of the action block</param>
        /// <param name="handler">The async handler function to apply.</param>
        /// <param name="cancellationToken">An optional cancellation token.</param>
        /// <returns>The ActionBlock created.</returns>
        public static ActionBlock<T> PipeConcurrentlyAsync<T>(this ISourceBlock<T> source,
            int maxConcurrency,
            Func<T, Task> handler,
            CancellationToken cancellationToken = default)
            => source.PipeAsync(handler, new ExecutionDataflowBlockOptions
            {
                MaxDegreeOfParallelism = maxConcurrency,
                SingleProducerConstrained = true,
                CancellationToken = cancellationToken
            });

        /// <summary>
        /// Pipes the source data through a single producer constrained TransformBlock with the specified max concurrency.
        /// </summary>
        /// <typeparam name="TIn">The input type.</typeparam>
        /// <typeparam name="TOut">The output type.</typeparam>
        /// <param name="source">The source block to receive from.</param>
        /// <param name="maxConcurrency">The maximum concurrency of the transform block</param>
        /// <param name="transform">The transform function to apply.</param>
        /// <param name="cancellationToken">An optional cancellation token.</param>
        /// <returns>The TransformBlock created.</returns>
        public static IReceivableSourceBlock<TOut> PipeConcurrently<TIn, TOut>(this ISourceBlock<TIn> source,
            int maxConcurrency,
            Func<TIn, TOut> transform,
            CancellationToken cancellationToken = default)
            => source.Pipe(transform, new ExecutionDataflowBlockOptions
            {
                MaxDegreeOfParallelism = maxConcurrency,
                SingleProducerConstrained = true,
                CancellationToken = cancellationToken
            });

        /// <summary>
        /// Pipes the source data through a single producer constrained TransformBlock with the specified max concurrency.
        /// </summary>
        /// <typeparam name="TIn">The input type.</typeparam>
        /// <typeparam name="TOut">The output type.</typeparam>
        /// <param name="source">The source block to receive from.</param>
        /// <param name="maxConcurrency">The maximum concurrency of the transform block</param>
        /// <param name="transform">The async transform function to apply.</param>
        /// <param name="cancellationToken">An optional cancellation token.</param>
        /// <returns>The TransformBlock created.</returns>
        public static IReceivableSourceBlock<TOut> PipeConcurrentlyAsync<TIn, TOut>(this ISourceBlock<TIn> source,
            int maxConcurrency,
            Func<TIn, Task<TOut>> transform,
            CancellationToken cancellationToken = default)
            => source.PipeAsync(transform, new ExecutionDataflowBlockOptions
            {
                MaxDegreeOfParallelism = maxConcurrency,
                SingleProducerConstrained = true,
                CancellationToken = cancellationToken
            });

        /// <summary>
        /// Pipes the source data through a single producer constrained ActionBlock with the specified bounded capacity and max concurrency.
        /// </summary>
        /// <typeparam name="T">The input type.</typeparam>
        /// <param name="source">The source block to receive from.</param>
        /// <param name="capacity">The bounded capacity of the transform block</param>
        /// <param name="maxConcurrency">The maximum concurrency of the action block</param>
        /// <param name="handler">The handler function to apply.</param>
        /// <param name="cancellationToken">An optional cancellation token.</param>
        /// <returns>The ActionBlock created.</returns>
        public static ActionBlock<T> Pipe<T>(this ISourceBlock<T> source,
            int capacity,
            int maxConcurrency,
            Action<T> handler,
            CancellationToken cancellationToken = default)
            => source.Pipe(handler, new ExecutionDataflowBlockOptions
            {
                BoundedCapacity = capacity,
                MaxDegreeOfParallelism = maxConcurrency,
                SingleProducerConstrained = true,
                CancellationToken = cancellationToken
            });

        /// <summary>
        /// Pipes the source data through a single producer constrained ActionBlock with the specified bounded capacity and max concurrency.
        /// </summary>
        /// <typeparam name="T">The input type.</typeparam>
        /// <param name="source">The source block to receive from.</param>
        /// <param name="capacity">The bounded capacity of the transform block</param>
        /// <param name="maxConcurrency">The maximum concurrency of the action block</param>
        /// <param name="handler">The async handler function to apply.</param>
        /// <param name="cancellationToken">An optional cancellation token.</param>
        /// <returns>The ActionBlock created.</returns>
        public static ActionBlock<T> PipeAsync<T>(this ISourceBlock<T> source,
            int capacity,
            int maxConcurrency,
            Func<T, Task> handler,
            CancellationToken cancellationToken = default)
            => source.PipeAsync(handler, new ExecutionDataflowBlockOptions
            {
                BoundedCapacity = capacity,
                MaxDegreeOfParallelism = maxConcurrency,
                SingleProducerConstrained = true,
                CancellationToken = cancellationToken
            });

        /// <summary>
        /// Pipes the source data through a single producer constrained TransformBlock with the specified bounded capacity and max concurrency.
        /// </summary>
        /// <typeparam name="TIn">The input type.</typeparam>
        /// <typeparam name="TOut">The output type.</typeparam>
        /// <param name="source">The source block to receive from.</param>
        /// <param name="capacity">The bounded capacity of the transform block</param>
        /// <param name="maxConcurrency">The maximum concurrency of the transform block</param>
        /// <param name="transform">The transform function to apply.</param>
        /// <param name="cancellationToken">An optional cancellation token.</param>
        /// <returns>The TransformBlock created.</returns>
        public static IReceivableSourceBlock<TOut> Pipe<TIn, TOut>(this ISourceBlock<TIn> source,
            int capacity,
            int maxConcurrency,
            Func<TIn, TOut> transform,
            CancellationToken cancellationToken = default)
            => source.Pipe(transform, new ExecutionDataflowBlockOptions
            {
                BoundedCapacity = capacity,
                MaxDegreeOfParallelism = maxConcurrency,
                SingleProducerConstrained = true,
                CancellationToken = cancellationToken
            });


        /// <summary>
        /// Pipes the source data through a single producer constrained TransformBlock with the specified bounded capacity and max concurrency.
        /// </summary>
        /// <typeparam name="TIn">The input type.</typeparam>
        /// <typeparam name="TOut">The output type.</typeparam>
        /// <param name="source">The source block to receive from.</param>
        /// <param name="capacity">The bounded capacity of the transform block</param>
        /// <param name="maxConcurrency">The maximum concurrency of the transform block</param>
        /// <param name="transform">The async transform function to apply.</param>
        /// <param name="cancellationToken">An optional cancellation token.</param>
        /// <returns>The TransformBlock created.</returns>
        public static IReceivableSourceBlock<TOut> PipeAsync<TIn, TOut>(this ISourceBlock<TIn> source,
            int capacity,
            int maxConcurrency,
            Func<TIn, Task<TOut>> transform,
            CancellationToken cancellationToken = default)
            => source.PipeAsync(transform, new ExecutionDataflowBlockOptions
            {
                BoundedCapacity = capacity,
                MaxDegreeOfParallelism = maxConcurrency,
                SingleProducerConstrained = true,
                CancellationToken = cancellationToken
            });

        /// <summary>
        /// Pipes the source data through a single producer constrained ActionBlock with the specified bounded capacity.
        /// </summary>
        /// <typeparam name="T">The input type.</typeparam>
        /// <param name="source">The source block to receive from.</param>
        /// <param name="capacity">The bounded capacity of the transform block</param>
        /// <param name="handler">The handler function to apply.</param>
        /// <param name="cancellationToken">An optional cancellation token.</param>
        /// <returns>The ActionBlock created.</returns>
        public static ActionBlock<T> PipeLimited<T>(this ISourceBlock<T> source,
            int capacity,
            Action<T> handler,
            CancellationToken cancellationToken = default)
            => source.Pipe(handler, new ExecutionDataflowBlockOptions
            {
                BoundedCapacity = capacity,
                SingleProducerConstrained = true,
                CancellationToken = cancellationToken
            });

        /// <summary>
        /// Pipes the source data through a single producer constrained ActionBlock with the specified bounded capacity.
        /// </summary>
        /// <typeparam name="T">The input type.</typeparam>
        /// <param name="source">The source block to receive from.</param>
        /// <param name="capacity">The bounded capacity of the transform block</param>
        /// <param name="handler">The async handler function to apply.</param>
        /// <param name="cancellationToken">An optional cancellation token.</param>
        /// <returns>The ActionBlock created.</returns>
        public static ActionBlock<T> PipeLimitedAsync<T>(this ISourceBlock<T> source,
            int capacity,
            Func<T, Task> handler,
            CancellationToken cancellationToken = default)
            => source.PipeAsync(handler, new ExecutionDataflowBlockOptions
            {
                BoundedCapacity = capacity,
                SingleProducerConstrained = true,
                CancellationToken = cancellationToken
            });

        /// <summary>
        /// Pipes the source data through a single producer constrained TransformBlock with the specified bounded capacity.
        /// </summary>
        /// <typeparam name="TIn">The input type.</typeparam>
        /// <typeparam name="TOut">The output type.</typeparam>
        /// <param name="source">The source block to receive from.</param>
        /// <param name="capacity">The bounded capacity of the transform block</param>
        /// <param name="transform">The transform function to apply.</param>
        /// <param name="cancellationToken">An optional cancellation token.</param>
        /// <returns>The TransformBlock created.</returns>
        public static IReceivableSourceBlock<TOut> PipeLimited<TIn, TOut>(this ISourceBlock<TIn> source,
            int capacity,
            Func<TIn, TOut> transform,
            CancellationToken cancellationToken = default)
            => source.Pipe(transform, new ExecutionDataflowBlockOptions
            {
                BoundedCapacity = capacity,
                SingleProducerConstrained = true,
                CancellationToken = cancellationToken
            });

        /// <summary>
        /// Pipes the source data through a single producer constrained TransformBlock with the specified bounded capacity.
        /// </summary>
        /// <typeparam name="TIn">The input type.</typeparam>
        /// <typeparam name="TOut">The output type.</typeparam>
        /// <param name="source">The source block to receive from.</param>
        /// <param name="capacity">The bounded capacity of the transform block</param>
        /// <param name="transform">The async transform function to apply.</param>
        /// <param name="cancellationToken">An optional cancellation token.</param>
        /// <returns>The TransformBlock created.</returns>
        public static IReceivableSourceBlock<TOut> PipeLimitedAsync<TIn, TOut>(this ISourceBlock<TIn> source,
            int capacity,
            Func<TIn, Task<TOut>> transform,
            CancellationToken cancellationToken = default)
            => source.PipeAsync(transform, new ExecutionDataflowBlockOptions
            {
                BoundedCapacity = capacity,
                SingleProducerConstrained = true,
                CancellationToken = cancellationToken
            });
    }
}
