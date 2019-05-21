﻿using Open.Threading.Tasks;
using System;
using System.Collections.Generic;
using System.Diagnostics.Contracts;
using System.IO;
using System.Threading;
using System.Threading.Tasks;
using System.Threading.Tasks.Dataflow;

namespace Open.Dataflow
{
    public static partial class DataFlowExtensions
    {
        public static ISourceBlock<T> Buffer<T>(this ISourceBlock<T> source, DataflowBlockOptions dataflowBlockOptions = null)
        {
            if (source == null)
                throw new NullReferenceException();
            Contract.EndContractBlock();

            var output = dataflowBlockOptions == null
                ? new BufferBlock<T>()
                : new BufferBlock<T>(dataflowBlockOptions);
            source.LinkToWithCompletion(output);
            return output;
        }

        public static ISourceBlock<T> BufferMany<T>(this ISourceBlock<T[]> source, ExecutionDataflowBlockOptions dataflowBlockOptions = null)
        {
            if (source == null)
                throw new NullReferenceException();
            Contract.EndContractBlock();

            var output = dataflowBlockOptions == null
                ? new TransformManyBlock<T[], T>(t => t)
                : new TransformManyBlock<T[], T>(t => t, dataflowBlockOptions); ;
            source.LinkToWithCompletion(output);
            return output;
        }

        public static ISourceBlock<T[]> Batch<T>(this ISourceBlock<T> source,
            int batchSize,
            GroupingDataflowBlockOptions dataflowBlockOptions = null)
        {
            if (source == null)
                throw new NullReferenceException();
            Contract.EndContractBlock();

            var batchBlock = dataflowBlockOptions == null
                ? new BatchBlock<T>(batchSize)
                : new BatchBlock<T>(batchSize, dataflowBlockOptions);
            source.LinkToWithCompletion(batchBlock);
            return batchBlock;
        }

        public static int ToTargetBlock<T>(this IEnumerable<T> source,
            ITargetBlock<T> target)
        {
            if (source == null)
                throw new NullReferenceException();
            if (target == null)
                throw new ArgumentNullException(nameof(target));
            Contract.EndContractBlock();

            var count = 0;
            foreach (var entry in source)
            {
                if (!target.Post(entry))
                    break;

                count++;
            }
            return count;
        }

        public static Task<int> ToTargetBlockAsync<T>(this IEnumerable<T> source,
            ITargetBlock<T> target,
            CancellationToken cancellationToken = default)
        {
            if (source == null)
                throw new NullReferenceException();
            if (target == null)
                throw new ArgumentNullException(nameof(target));
            Contract.EndContractBlock();

            if (source is ICollection<T> c && c.Count == 0)
                return Task.FromResult(0);

            return ToTargetBlockAsyncCore();

            async Task<int> ToTargetBlockAsyncCore()
            {
                var count = 0;
                foreach (var entry in source)
                {
                    if (cancellationToken.IsCancellationRequested
                        || !target.Post(entry) && !await target.SendAsync(entry))
                        break;

                    count++;
                }
                return count;
            }
        }

        public static async Task<List<T>> ToListAsync<T>(this IReceivableSourceBlock<T> source)
        {
            if (source == null)
                throw new NullReferenceException();
            Contract.EndContractBlock();

            var result = new List<T>();
            do
            {
                while (source.TryReceive(null, out var e))
                {
                    result.Add(e);
                }
            }
            while (await source.OutputAvailableAsync());

            return result;
        }

        public static ISourceBlock<T> AsBufferBlock<T>(this IEnumerable<T> source,
            int capacity = DataflowBlockOptions.Unbounded,
            CancellationToken cancellationToken = default)
        {
            if (source == null)
                throw new NullReferenceException();
            Contract.EndContractBlock();

            var buffer = new BufferBlock<T>(new DataflowBlockOptions()
            {
                BoundedCapacity = capacity,
                CancellationToken = cancellationToken
            });

            var result = ToTargetBlockAsync(source, buffer, cancellationToken);

            if (result.IsCompleted && !result.IsFaulted && !result.IsCanceled)
            {
                buffer.Complete();
            }
            else
            {
                _ = CallCompleteWhenFinished();

                async Task CallCompleteWhenFinished()
                {
                    await result;
                    buffer.Complete();
                }
            }

            return buffer;
        }

        public static async Task<int> AllLinesTo(this TextReader source,
            ITargetBlock<string> target)
        {
            if (source == null)
                throw new NullReferenceException();
            Contract.EndContractBlock();

            var count = 0;
            string line;
            while ((line = await source.ReadLineAsync()) != null)
            {
                if (!target.Post(line) && !await target.SendAsync(line))
                    break;

                count++;
            }
            return count;
        }

        public static async Task<int> AllLinesTo<T>(this TextReader source,
            ITargetBlock<T> target,
            Func<string, T> transform)
        {
            if (source == null)
                throw new NullReferenceException();
            Contract.EndContractBlock();

            var count = 0;
            string line;
            while ((line = await source.ReadLineAsync()) != null)
            {
                var e = transform(line);
                if (!target.Post(e) && !await target.SendAsync(e))
                    break;

                count++;
            }

            return count;
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
            => target.Fault(new Exception(message));

        public static void Fault(this IDataflowBlock target, string message, Exception innerException)
            => target.Fault(new Exception(message, innerException));

    }
}
