using System.IO.Ports;
using System.Threading.Channels;

namespace SerialPortWrapper;

/// <summary>
///     A serial port wrapper with asynchronous support via channels
/// </summary>
public interface ISerialPortWrapper
{
    bool ConnectionState { get; }
    SerialPort SerialPort { get; }
    void Open();
    void Close();
    ValueTask SendMessage(byte[] message, CancellationToken cancellationToken = default);
    ValueTask<byte[]> ReceiveMessage(CancellationToken cancellationToken = default);
}

/// <summary>
///     A serial port wrapper with asynchronous support via channels
/// </summary>
public class SerialPortWrapper : ISerialPortWrapper, IDisposable
{
    private readonly PeriodicTimer _periodicTimer;
    private readonly Channel<byte[]> _readChannel;
    private readonly Channel<byte[]> _writeChannel;
    private CancellationTokenSource _cancellationTokenSource;
    private Task _readTask;
    private Task _writeTask;

    /// <summary>
    ///     Creates an instance of serial port wrapper
    /// </summary>
    /// <param name="readChannel"></param>
    /// <param name="writeChannel"></param>
    /// <param name="portName">
    ///     Name or path to the serial port file. On Windows it is "COM#", where # is a number. On Unix-like
    ///     systems, it is a path to a file that represents a serial port
    /// </param>
    /// <param name="waitReadWrite">A delay for checking of new data from serial port underlying stream</param>
    public SerialPortWrapper(
        Channel<byte[]> readChannel,
        Channel<byte[]> writeChannel,
        string portName,
        TimeSpan? waitReadWrite
    )
    {
        _readChannel = readChannel;
        _writeChannel = writeChannel;

        var serialPort = new SerialPort(portName);

        SerialPort = serialPort;

        _periodicTimer = new PeriodicTimer(waitReadWrite ?? TimeSpan.FromMilliseconds(10));
    }

    /// <summary>
    ///     Creates an instance of serial port wrapper
    /// </summary>
    /// <param name="readChannel"></param>
    /// <param name="writeChannel"></param>
    /// <param name="portName">
    ///     Name or path to the serial port file. On Windows it is "COM#", where # is a number. On Unix-like
    ///     systems, it is a path to a file that represents a serial port
    /// </param>
    /// <param name="baudRate">Rate of baud amount is transferred per second</param>
    /// <param name="waitReadWrite">A delay for checking of new data from serial port underlying stream</param>
    public SerialPortWrapper(
        Channel<byte[]> readChannel,
        Channel<byte[]> writeChannel,
        string portName,
        int baudRate,
        TimeSpan? waitReadWrite
    ) : this(readChannel, writeChannel, portName, waitReadWrite)
    {
        SerialPort.BaudRate = baudRate;
    }

    /// <summary>
    ///     Creates an instance of serial port wrapper
    /// </summary>
    /// <param name="readChannel"></param>
    /// <param name="writeChannel"></param>
    /// <param name="portName">
    ///     Name or path to the serial port file. On Windows it is "COM#", where # is a number. On Unix-like
    ///     systems, it is a path to a file that represents a serial port
    /// </param>
    /// <param name="baudRate">Rate of baud amount is transferred per second</param>
    /// <param name="parity">
    ///     Parity is an error-checking procedure in which the number of 1s must always be the same - either
    ///     even or odd - for each group of bits that is transmitted without error
    /// </param>
    /// <param name="waitReadWrite">A delay for checking of new data from serial port underlying stream</param>
    public SerialPortWrapper(
        Channel<byte[]> readChannel,
        Channel<byte[]> writeChannel,
        string portName,
        int baudRate,
        Parity parity,
        TimeSpan? waitReadWrite
    ) : this(readChannel, writeChannel, portName, baudRate, waitReadWrite)
    {
        SerialPort.Parity = parity;
    }

    /// <summary>
    ///     Creates an instance of serial port wrapper
    /// </summary>
    /// <param name="readChannel"></param>
    /// <param name="writeChannel"></param>
    /// <param name="portName">
    ///     Name or path to the serial port file. On Windows it is "COM#", where # is a number. On Unix-like
    ///     systems, it is a path to a file that represents a serial port
    /// </param>
    /// <param name="baudRate">Rate of baud amount is transferred per second</param>
    /// <param name="parity">
    ///     Parity is an error-checking procedure in which the number of 1s must always be the same - either
    ///     even or odd - for each group of bits that is transmitted without error
    /// </param>
    /// <param name="dataBits">Length of data bits per byte. Valid values are from 5 to 8</param>
    /// <param name="waitReadWrite">A delay for checking of new data from serial port underlying stream</param>
    public SerialPortWrapper(
        Channel<byte[]> readChannel,
        Channel<byte[]> writeChannel,
        string portName,
        int baudRate,
        Parity parity,
        int dataBits,
        TimeSpan? waitReadWrite
    ) : this(readChannel, writeChannel, portName, baudRate, parity, waitReadWrite)
    {
        SerialPort.DataBits = dataBits;
    }

    /// <summary>
    ///     Creates an instance of serial port wrapper
    /// </summary>
    /// <param name="readChannel"></param>
    /// <param name="writeChannel"></param>
    /// <param name="portName">
    ///     Name or path to the serial port file. On Windows it is "COM#", where # is a number. On Unix-like
    ///     systems, it is a path to a file that represents a serial port
    /// </param>
    /// <param name="baudRate">Rate of baud amount is transferred per second</param>
    /// <param name="parity">
    ///     Parity is an error-checking procedure in which the number of 1s must always be the same - either
    ///     even or odd - for each group of bits that is transmitted without error
    /// </param>
    /// <param name="dataBits">Length of data bits per byte. Valid values are from 5 to 8</param>
    /// <param name="stopBits">Number of stop bits per byte</param>
    /// <param name="waitReadWrite">A delay for checking of new data from serial port underlying stream</param>
    public SerialPortWrapper(
        Channel<byte[]> readChannel,
        Channel<byte[]> writeChannel,
        string portName,
        int baudRate,
        Parity parity,
        int dataBits,
        StopBits stopBits,
        TimeSpan? waitReadWrite
    ) : this(
        readChannel,
        writeChannel,
        portName,
        baudRate,
        parity,
        dataBits,
        waitReadWrite
    )
    {
        SerialPort.StopBits = stopBits;
    }

    public SerialPortWrapper(
        UnboundedChannelOptions readChannelOptions,
        UnboundedChannelOptions writeChannelOptions,
        string portName,
        TimeSpan? waitReadWrite
    ) : this(
        Channel.CreateUnbounded<byte[]>(readChannelOptions),
        Channel.CreateUnbounded<byte[]>(writeChannelOptions),
        portName,
        waitReadWrite
    )
    {
    }

    public SerialPortWrapper(
        UnboundedChannelOptions readChannelOptions,
        UnboundedChannelOptions writeChannelOptions,
        string portName,
        int baudRate,
        TimeSpan? waitReadWrite
    ) : this(
        Channel.CreateUnbounded<byte[]>(readChannelOptions),
        Channel.CreateUnbounded<byte[]>(writeChannelOptions),
        portName,
        baudRate,
        waitReadWrite
    )
    {
    }

    public SerialPortWrapper(
        UnboundedChannelOptions readChannelOptions,
        UnboundedChannelOptions writeChannelOptions,
        string portName,
        int baudRate,
        Parity parity,
        TimeSpan? waitReadWrite
    ) : this(
        Channel.CreateUnbounded<byte[]>(readChannelOptions),
        Channel.CreateUnbounded<byte[]>(writeChannelOptions),
        portName,
        baudRate,
        parity,
        waitReadWrite
    )
    {
    }

    public SerialPortWrapper(
        UnboundedChannelOptions readChannelOptions,
        UnboundedChannelOptions writeChannelOptions,
        string portName,
        int baudRate,
        Parity parity,
        int dataBits,
        TimeSpan? waitReadWrite
    ) : this(
        Channel.CreateUnbounded<byte[]>(readChannelOptions),
        Channel.CreateUnbounded<byte[]>(writeChannelOptions),
        portName,
        baudRate,
        parity,
        dataBits,
        waitReadWrite
    )
    {
    }

    public SerialPortWrapper(
        UnboundedChannelOptions readChannelOptions,
        UnboundedChannelOptions writeChannelOptions,
        string portName,
        int baudRate,
        Parity parity,
        int dataBits,
        StopBits stopBits,
        TimeSpan? waitReadWrite
    ) : this(
        Channel.CreateUnbounded<byte[]>(readChannelOptions),
        Channel.CreateUnbounded<byte[]>(writeChannelOptions),
        portName,
        baudRate,
        parity,
        dataBits,
        stopBits,
        waitReadWrite
    )
    {
    }

    public SerialPortWrapper(
        BoundedChannelOptions readChannelOptions,
        UnboundedChannelOptions writeChannelOptions,
        string portName,
        TimeSpan? waitReadWrite
    ) : this(
        Channel.CreateBounded<byte[]>(readChannelOptions),
        Channel.CreateUnbounded<byte[]>(writeChannelOptions),
        portName,
        waitReadWrite
    )
    {
    }

    public SerialPortWrapper(
        BoundedChannelOptions readChannelOptions,
        UnboundedChannelOptions writeChannelOptions,
        string portName,
        int baudRate,
        TimeSpan? waitReadWrite
    ) : this(
        Channel.CreateBounded<byte[]>(readChannelOptions),
        Channel.CreateUnbounded<byte[]>(writeChannelOptions),
        portName,
        baudRate,
        waitReadWrite
    )
    {
    }

    public SerialPortWrapper(
        BoundedChannelOptions readChannelOptions,
        UnboundedChannelOptions writeChannelOptions,
        string portName,
        int baudRate,
        Parity parity,
        TimeSpan? waitReadWrite
    ) : this(
        Channel.CreateBounded<byte[]>(readChannelOptions),
        Channel.CreateUnbounded<byte[]>(writeChannelOptions),
        portName,
        baudRate,
        parity,
        waitReadWrite
    )
    {
    }

    public SerialPortWrapper(
        BoundedChannelOptions readChannelOptions,
        UnboundedChannelOptions writeChannelOptions,
        string portName,
        int baudRate,
        Parity parity,
        int dataBits,
        TimeSpan? waitReadWrite
    ) : this(
        Channel.CreateBounded<byte[]>(readChannelOptions),
        Channel.CreateUnbounded<byte[]>(writeChannelOptions),
        portName,
        baudRate,
        parity,
        dataBits,
        waitReadWrite
    )
    {
    }

    public SerialPortWrapper(
        BoundedChannelOptions readChannelOptions,
        UnboundedChannelOptions writeChannelOptions,
        string portName,
        int baudRate,
        Parity parity,
        int dataBits,
        StopBits stopBits,
        TimeSpan? waitReadWrite
    ) : this(
        Channel.CreateBounded<byte[]>(readChannelOptions),
        Channel.CreateUnbounded<byte[]>(writeChannelOptions),
        portName,
        baudRate,
        parity,
        dataBits,
        stopBits,
        waitReadWrite
    )
    {
    }

    public SerialPortWrapper(
        UnboundedChannelOptions readChannelOptions,
        BoundedChannelOptions writeChannelOptions,
        string portName,
        TimeSpan? waitReadWrite
    ) : this(
        Channel.CreateUnbounded<byte[]>(readChannelOptions),
        Channel.CreateBounded<byte[]>(writeChannelOptions),
        portName,
        waitReadWrite
    )
    {
    }

    public SerialPortWrapper(
        UnboundedChannelOptions readChannelOptions,
        BoundedChannelOptions writeChannelOptions,
        string portName,
        int baudRate,
        TimeSpan? waitReadWrite
    ) : this(
        Channel.CreateUnbounded<byte[]>(readChannelOptions),
        Channel.CreateBounded<byte[]>(writeChannelOptions),
        portName,
        baudRate,
        waitReadWrite
    )
    {
    }

    public SerialPortWrapper(
        UnboundedChannelOptions readChannelOptions,
        BoundedChannelOptions writeChannelOptions,
        string portName,
        int baudRate,
        Parity parity,
        TimeSpan? waitReadWrite
    ) : this(
        Channel.CreateUnbounded<byte[]>(readChannelOptions),
        Channel.CreateBounded<byte[]>(writeChannelOptions),
        portName,
        baudRate,
        parity,
        waitReadWrite
    )
    {
    }

    public SerialPortWrapper(
        UnboundedChannelOptions readChannelOptions,
        BoundedChannelOptions writeChannelOptions,
        string portName,
        int baudRate,
        Parity parity,
        int dataBits,
        TimeSpan? waitReadWrite
    ) : this(
        Channel.CreateUnbounded<byte[]>(readChannelOptions),
        Channel.CreateBounded<byte[]>(writeChannelOptions),
        portName,
        baudRate,
        parity,
        dataBits,
        waitReadWrite
    )
    {
    }

    public SerialPortWrapper(
        UnboundedChannelOptions readChannelOptions,
        BoundedChannelOptions writeChannelOptions,
        string portName,
        int baudRate,
        Parity parity,
        int dataBits,
        StopBits stopBits,
        TimeSpan? waitReadWrite
    ) : this(
        Channel.CreateUnbounded<byte[]>(readChannelOptions),
        Channel.CreateBounded<byte[]>(writeChannelOptions),
        portName,
        baudRate,
        parity,
        dataBits,
        stopBits,
        waitReadWrite
    )
    {
    }

    public SerialPortWrapper(
        BoundedChannelOptions readChannelOptions,
        BoundedChannelOptions writeChannelOptions,
        string portName,
        TimeSpan? waitReadWrite
    ) : this(
        Channel.CreateBounded<byte[]>(readChannelOptions),
        Channel.CreateBounded<byte[]>(writeChannelOptions),
        portName,
        waitReadWrite
    )
    {
    }

    public SerialPortWrapper(
        BoundedChannelOptions readChannelOptions,
        BoundedChannelOptions writeChannelOptions,
        string portName,
        int baudRate,
        TimeSpan? waitReadWrite
    ) : this(
        Channel.CreateBounded<byte[]>(readChannelOptions),
        Channel.CreateBounded<byte[]>(writeChannelOptions),
        portName,
        baudRate,
        waitReadWrite
    )
    {
    }

    public SerialPortWrapper(
        BoundedChannelOptions readChannelOptions,
        BoundedChannelOptions writeChannelOptions,
        string portName,
        int baudRate,
        Parity parity,
        TimeSpan? waitReadWrite
    ) : this(
        Channel.CreateBounded<byte[]>(readChannelOptions),
        Channel.CreateBounded<byte[]>(writeChannelOptions),
        portName,
        baudRate,
        parity,
        waitReadWrite
    )
    {
    }

    public SerialPortWrapper(
        BoundedChannelOptions readChannelOptions,
        BoundedChannelOptions writeChannelOptions,
        string portName,
        int baudRate,
        Parity parity,
        int dataBits,
        TimeSpan? waitReadWrite
    ) : this(
        Channel.CreateBounded<byte[]>(readChannelOptions),
        Channel.CreateBounded<byte[]>(writeChannelOptions),
        portName,
        baudRate,
        parity,
        dataBits,
        waitReadWrite
    )
    {
    }

    public SerialPortWrapper(
        BoundedChannelOptions readChannelOptions,
        BoundedChannelOptions writeChannelOptions,
        string portName,
        int baudRate,
        Parity parity,
        int dataBits,
        StopBits stopBits,
        TimeSpan? waitReadWrite
    ) : this(
        Channel.CreateBounded<byte[]>(readChannelOptions),
        Channel.CreateBounded<byte[]>(writeChannelOptions),
        portName,
        baudRate,
        parity,
        dataBits,
        stopBits,
        waitReadWrite
    )
    {
    }

    /// <summary>
    ///     Releases all resources used by the Component.
    /// </summary>
    public void Dispose()
    {
        SerialPort.Dispose();
        _cancellationTokenSource?.Dispose();
        _periodicTimer.Dispose();
        GC.SuppressFinalize(this);
    }

    /// <summary>
    ///     Whether serial port is open or closed
    /// </summary>
    public bool ConnectionState => SerialPort.IsOpen;

    /// <summary>
    ///     Opens underlying serial port connection
    /// </summary>
    /// <returns>Whether the serial port connection successfully opened</returns>
    /// <exception cref="InvalidOperationException"></exception>
    public void Open()
    {
        if (SerialPort.IsOpen)
            throw new SerialPortWrapperConnectionStateException("Already open!");

        SerialPort.Open();

        _cancellationTokenSource = new CancellationTokenSource();

        _writeTask = Task.Factory.StartNew(async () => await Write(_cancellationTokenSource.Token).ConfigureAwait(false), TaskCreationOptions.LongRunning);
        _readTask = Task.Factory.StartNew(async () => await Read(_cancellationTokenSource.Token).ConfigureAwait(false), TaskCreationOptions.LongRunning);
    }

    /// <summary>
    ///     Closes underlying serial port connection
    /// </summary>
    /// <returns>Whether the serial port connection successfully closed</returns>
    /// <exception cref="InvalidOperationException"></exception>
    public void Close()
    {
        if (!SerialPort.IsOpen)
            throw new SerialPortWrapperConnectionStateException("Already closed!");

        _cancellationTokenSource?.Cancel();
        _writeTask.Wait();
        _readTask.Wait();

        SerialPort.Close();
    }

    /// <summary>
    ///     Puts data to the write queue to be sent in underlying serial port
    /// </summary>
    /// <param name="message"></param>
    /// <param name="cancellationToken"></param>
    public async ValueTask SendMessage(byte[] message, CancellationToken cancellationToken = default)
    {
        await _writeChannel.Writer.WriteAsync(message, CancellationTokenSource.CreateLinkedTokenSource(_cancellationTokenSource.Token, cancellationToken).Token).ConfigureAwait(false);
    }

    /// <summary>
    ///     Reads data from the read queue from underlying serial port
    /// </summary>
    /// <param name="cancellationToken"></param>
    /// <returns></returns>
    public async ValueTask<byte[]> ReceiveMessage(CancellationToken cancellationToken = default)
    {
        return await _readChannel.Reader.ReadAsync(CancellationTokenSource.CreateLinkedTokenSource(_cancellationTokenSource.Token, cancellationToken).Token).ConfigureAwait(false);
    }

    /// <summary>
    ///     Gets an underlying instance of serial port
    /// </summary>
    public SerialPort SerialPort { get; }

    private async ValueTask Write(CancellationToken cancellationToken = default)
    {
        while (!cancellationToken.IsCancellationRequested)
        {
            var buffer = await _writeChannel.Reader.ReadAsync(cancellationToken).ConfigureAwait(false);

            await WriteBufferAsync(buffer, cancellationToken).ConfigureAwait(false);
        }
    }

    private async ValueTask Read(CancellationToken cancellationToken = default)
    {
        while (!cancellationToken.IsCancellationRequested)
        {
            var bufferRead = await ReadBufferAsync(cancellationToken).ConfigureAwait(false);

            await _readChannel.Writer.WriteAsync(bufferRead, cancellationToken).ConfigureAwait(false);
        }
    }

    private async ValueTask<byte[]> ReadBufferAsync(CancellationToken cancellationToken)
    {
        try
        {
            while (SerialPort.BytesToRead == 0) await _periodicTimer.WaitForNextTickAsync(cancellationToken).ConfigureAwait(false);

            var bytesToRead = SerialPort.BytesToRead;

            var buffer = new byte[bytesToRead];

            _ = await SerialPort.BaseStream.ReadAsync(buffer.AsMemory(0, bytesToRead), cancellationToken).ConfigureAwait(false);

            return buffer;
        }
        catch (OperationCanceledException)
        {
            // Cancellation was requested
            return Array.Empty<byte>();
        }
    }

    private async ValueTask WriteBufferAsync(byte[] buffer, CancellationToken cancellationToken)
    {
        try
        {
            await SerialPort.BaseStream.WriteAsync(buffer, cancellationToken).ConfigureAwait(false);

            while (SerialPort.BytesToWrite > 0) await _periodicTimer.WaitForNextTickAsync(cancellationToken).ConfigureAwait(false);
        }
        catch (OperationCanceledException)
        {
            // Cancellation was requested
        }
    }

    public sealed class SerialPortWrapperConnectionStateException : Exception
    {
        public SerialPortWrapperConnectionStateException(string message) : base(message)
        {
        }
    }
}