using System.IO.Ports;
using System.Threading.Channels;

namespace SerialPortWrapper;

/// <summary>
///     A serial port wrapper with asynchronous support via channels
/// </summary>
public interface ISerialPortWrapper
{
    bool ConnectionState { get; }
    void Open(CancellationToken cancellationToken = default);
    void Close(CancellationToken cancellationToken = default);
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

    private readonly SerialPort _serialPort;

    private readonly Channel<byte[]> _writeChannel;

    private CancellationTokenSource? _cancellationTokenSource;

    private TaskCompletionSource? _readTaskCompletionSource;
    private TaskCompletionSource? _writeTaskCompletionSource;

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

        _serialPort = serialPort;

        _periodicTimer = new PeriodicTimer(waitReadWrite ?? TimeSpan.FromMilliseconds(10));

        _serialPort.ErrorReceived += OnSerialPortErrorReceived;
        _serialPort.PinChanged += OnSerialPortPinChanged;
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
        _serialPort.BaudRate = baudRate;
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
        _serialPort.Parity = parity;
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
        _serialPort.DataBits = dataBits;
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
        _serialPort.StopBits = stopBits;
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
        _serialPort.Dispose();
        _cancellationTokenSource?.Dispose();
        _periodicTimer.Dispose();
        GC.SuppressFinalize(this);
    }

    /// <summary>
    ///     Whether serial port is open or closed
    /// </summary>
    public bool ConnectionState => _serialPort.IsOpen;

    /// <summary>
    ///     Opens underlying serial port connection
    /// </summary>
    /// <param name="cancellationToken"></param>
    /// <returns>Whether the serial port connection successfully opened</returns>
    /// <exception cref="InvalidOperationException"></exception>
    public void Open(CancellationToken cancellationToken = default)
    {
        if (_serialPort.IsOpen)
            throw new SerialPortWrapperConnectionStateException("Already open!");

        _writeTaskCompletionSource = new TaskCompletionSource();
        _readTaskCompletionSource = new TaskCompletionSource();

        _serialPort.Open();
        _cancellationTokenSource = CancellationTokenSource.CreateLinkedTokenSource(cancellationToken);

        Task.Factory.StartNew(() => Write(_cancellationTokenSource.Token), TaskCreationOptions.LongRunning);
        Task.Factory.StartNew(() => Read(_cancellationTokenSource.Token), TaskCreationOptions.LongRunning);
    }

    /// <summary>
    ///     Closes underlying serial port connection
    /// </summary>
    /// <param name="cancellationToken"></param>
    /// <returns>Whether the serial port connection successfully closed</returns>
    /// <exception cref="InvalidOperationException"></exception>
    public void Close(CancellationToken cancellationToken = default)
    {
        if (!_serialPort.IsOpen)
            throw new SerialPortWrapperConnectionStateException("Already closed!");

        _cancellationTokenSource?.Cancel();

        _writeTaskCompletionSource?.Task.WaitAsync(cancellationToken);
        _readTaskCompletionSource?.Task.WaitAsync(cancellationToken);

        _serialPort.Close();
    }

    /// <summary>
    ///     Puts data to the write queue to be sent in underlying serial port
    /// </summary>
    /// <param name="message"></param>
    /// <param name="cancellationToken"></param>
    public async ValueTask SendMessage(byte[] message, CancellationToken cancellationToken = default)
    {
        await _writeChannel.Writer.WriteAsync(message, cancellationToken).ConfigureAwait(false);
    }

    /// <summary>
    ///     Reads data from the read queue from underlying serial port
    /// </summary>
    /// <param name="cancellationToken"></param>
    /// <returns></returns>
    public async ValueTask<byte[]> ReceiveMessage(CancellationToken cancellationToken = default)
    {
        return await _readChannel.Reader.ReadAsync(cancellationToken).ConfigureAwait(false);
    }

    public event EventHandler<SerialPinChangedEventArgs> SerialPortPinChanged;

    private void OnSerialPortPinChanged(object sender, SerialPinChangedEventArgs args)
    {
        SerialPortPinChanged?.Invoke(sender, args);
    }

    private void OnSerialPortErrorReceived(object sender, SerialErrorReceivedEventArgs args)
    {
        throw new InvalidOperationException($"SerialPort error: {Enum.GetName(typeof(SerialError), args.EventType)}");
    }

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
            var (bufferRead, bytesRead) = await ReadBufferAsync(cancellationToken).ConfigureAwait(false);

            await _readChannel.Writer.WriteAsync(bufferRead, cancellationToken).ConfigureAwait(false);
        }
    }

    private async ValueTask<(byte[] buffer, int bytesRead)> ReadBufferAsync(CancellationToken cancellationToken)
    {
        try
        {
            while (_serialPort.BytesToRead == 0) await _periodicTimer.WaitForNextTickAsync(cancellationToken).ConfigureAwait(false);

            var bytesToRead = _serialPort.BytesToRead;

            var buffer = new byte[bytesToRead];

            var bytesRead = await _serialPort.BaseStream.ReadAsync(buffer.AsMemory(0, bytesToRead), cancellationToken).ConfigureAwait(false);

            return (buffer, bytesRead);
        }
        catch (OperationCanceledException)
        {
            // Cancellation was requested
            return (Array.Empty<byte>(), -1);
        }
    }

    private async ValueTask WriteBufferAsync(byte[] buffer, CancellationToken cancellationToken)
    {
        try
        {
            await _serialPort.BaseStream.WriteAsync(buffer, cancellationToken).ConfigureAwait(false);

            while (_serialPort.BytesToWrite > 0) await _periodicTimer.WaitForNextTickAsync(cancellationToken).ConfigureAwait(false);
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