using System;
using System.IO;
using System.IO.Pipelines;
using System.Threading;
using System.Threading.Tasks;

namespace jaytwo.Ergonomics.Images;

/// <summary>
/// Bridges async source/destination streams to sync NetVips I/O with pipes on both ends.
/// </summary>
internal static class AsyncStreamBridge
{
    public static async Task<ImageSize> RunAsync(
        Stream source,
        Stream destination,
        PipelineOp[] ops,
        AlphaBehavior sourceAlpha,
        ImageEncoding encoding,
        CancellationToken cancellationToken)
    {
        cancellationToken.ThrowIfCancellationRequested();

        var inputPipe = new Pipe();
        var outputPipe = new Pipe();

        var inputPump = PumpSourceAsync(source, inputPipe.Writer, cancellationToken);
        var outputPump = PumpDestinationAsync(outputPipe.Reader, destination, cancellationToken);
        var producer = Task.Run(
            () => Produce(
                inputPipe,
                outputPipe,
                ops,
                sourceAlpha,
                encoding,
                cancellationToken),
            CancellationToken.None);

        try
        {
            await Task.WhenAll(inputPump, outputPump, producer).ConfigureAwait(false);
            return await producer.ConfigureAwait(false);
        }
        catch (Exception) when (cancellationToken.IsCancellationRequested)
        {
            throw new OperationCanceledException(cancellationToken);
        }
        finally
        {
            await Task.WhenAll(
                Observe(inputPump),
                Observe(outputPump),
                Observe(producer)).ConfigureAwait(false);
        }
    }

    private static ImageSize Produce(
        Pipe inputPipe,
        Pipe outputPipe,
        PipelineOp[] ops,
        AlphaBehavior sourceAlpha,
        ImageEncoding encoding,
        CancellationToken cancellationToken)
    {
        using var inputStream = inputPipe.Reader.AsStream(leaveOpen: true);
        using var outputStream = outputPipe.Writer.AsStream(leaveOpen: true);
        try
        {
            var size = ImageEngine.ExecutePipeline(
                inputStream,
                outputStream,
                ops,
                sourceAlpha,
                encoding,
                cancellationToken);
            inputPipe.Reader.Complete();
            outputPipe.Writer.Complete();
            return size;
        }
        catch (Exception ex)
        {
            try
            {
                inputPipe.Reader.Complete(ex);
            }
            catch (Exception)
            {
                // Already completed.
            }

            try
            {
                outputPipe.Writer.Complete(ex);
            }
            catch (Exception)
            {
                // Already completed.
            }

            throw;
        }
    }

    private static async Task PumpSourceAsync(
        Stream source,
        PipeWriter writer,
        CancellationToken cancellationToken)
    {
        Exception? error = null;
        try
        {
            while (true)
            {
                var memory = writer.GetMemory();
                var read = await source.ReadAsync(memory, cancellationToken).ConfigureAwait(false);
                if (read == 0)
                {
                    break;
                }

                writer.Advance(read);
                var flushed = await writer.FlushAsync(cancellationToken).ConfigureAwait(false);
                if (flushed.IsCompleted)
                {
                    break;
                }
            }
        }
        catch (Exception ex)
        {
            error = ex;
            throw;
        }
        finally
        {
            await writer.CompleteAsync(error).ConfigureAwait(false);
        }
    }

    private static async Task PumpDestinationAsync(
        PipeReader reader,
        Stream destination,
        CancellationToken cancellationToken)
    {
        Exception? error = null;
        try
        {
            while (true)
            {
                var result = await reader.ReadAsync(cancellationToken).ConfigureAwait(false);
                var buffer = result.Buffer;
                try
                {
                    foreach (var segment in buffer)
                    {
                        await destination.WriteAsync(segment, cancellationToken).ConfigureAwait(false);
                    }
                }
                finally
                {
                    reader.AdvanceTo(buffer.End);
                }

                if (result.IsCompleted)
                {
                    break;
                }
            }
        }
        catch (Exception ex)
        {
            error = ex;
            throw;
        }
        finally
        {
            await reader.CompleteAsync(error).ConfigureAwait(false);
        }
    }

    private static async Task Observe(Task task)
    {
        try
        {
            await task.ConfigureAwait(false);
        }
        catch (Exception)
        {
            // Observed so a secondary fault is not left unobserved.
        }
    }
}
