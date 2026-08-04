using System;
using System.Buffers.Binary;
using System.IO;
using System.IO.Compression;
using System.Text;

namespace CardGeneration.Services;

internal sealed class StreamingPngWriter : IDisposable
{
    private static readonly byte[] PngSignature = [137, 80, 78, 71, 13, 10, 26, 10];
    private static readonly uint[] CrcTable = BuildCrcTable();

    private readonly string _outputPath;
    private readonly string _temporaryPath;
    private readonly FileStream _file;
    private readonly IdatChunkStream _idatStream;
    private readonly ZLibStream _compressor;
    private readonly byte[] _filteredRow;
    private readonly int _height;
    private int _rowsWritten;
    private bool _completed;
    private bool _disposed;

    public StreamingPngWriter(string outputPath, int width, int height, int dpi)
    {
        if (width <= 0 || height <= 0)
        {
            throw new ArgumentOutOfRangeException(nameof(width), "PNG dimensions must be positive.");
        }

        _outputPath = outputPath;
        _temporaryPath = $"{outputPath}.tmp-{Guid.NewGuid():N}";
        _height = height;
        _filteredRow = new byte[checked(width * 4 + 1)];
        Directory.CreateDirectory(Path.GetDirectoryName(outputPath) ?? ".");
        _file = new FileStream(_temporaryPath, FileMode.CreateNew, FileAccess.Write, FileShare.None, 1024 * 1024, FileOptions.SequentialScan);
        _file.Write(PngSignature);

        Span<byte> header = stackalloc byte[13];
        BinaryPrimitives.WriteInt32BigEndian(header[..4], width);
        BinaryPrimitives.WriteInt32BigEndian(header.Slice(4, 4), height);
        header[8] = 8;
        header[9] = 6;
        WriteChunk(_file, "IHDR", header);
        WriteChunk(_file, "sRGB", [0]);
        WritePhysicalResolutionChunk(_file, dpi);

        _idatStream = new IdatChunkStream(_file);
        _compressor = new ZLibStream(_idatStream, CompressionLevel.Fastest, leaveOpen: true);
    }

    public void WriteRow(ReadOnlySpan<byte> rgba)
    {
        ObjectDisposedException.ThrowIf(_disposed, this);
        if (_completed)
        {
            throw new InvalidOperationException("The PNG has already been completed.");
        }

        if (rgba.Length != _filteredRow.Length - 1)
        {
            throw new ArgumentException($"Expected {_filteredRow.Length - 1} RGBA bytes, received {rgba.Length}.", nameof(rgba));
        }

        if (_rowsWritten >= _height)
        {
            throw new InvalidOperationException("More rows were written than declared in the PNG header.");
        }

        // PNG Sub filtering keeps flat paper/card regions compact without retaining prior rows.
        _filteredRow[0] = 1;
        for (var index = 0; index < rgba.Length; index++)
        {
            var left = index >= 4 ? rgba[index - 4] : 0;
            _filteredRow[index + 1] = unchecked((byte)(rgba[index] - left));
        }

        _compressor.Write(_filteredRow);
        _rowsWritten++;
    }

    public void Complete()
    {
        ObjectDisposedException.ThrowIf(_disposed, this);
        if (_completed)
        {
            return;
        }

        if (_rowsWritten != _height)
        {
            throw new InvalidOperationException($"PNG requires {_height} rows, but {_rowsWritten} were written.");
        }

        _compressor.Dispose();
        _idatStream.Complete();
        WriteChunk(_file, "IEND", ReadOnlySpan<byte>.Empty);
        _file.Flush(flushToDisk: true);
        _file.Dispose();
        File.Move(_temporaryPath, _outputPath, overwrite: true);
        _completed = true;
        _disposed = true;
    }

    public void Dispose()
    {
        if (_disposed)
        {
            return;
        }

        _disposed = true;
        try
        {
            _compressor.Dispose();
            _idatStream.Dispose();
            _file.Dispose();
        }
        finally
        {
            if (!_completed && File.Exists(_temporaryPath))
            {
                File.Delete(_temporaryPath);
            }
        }
    }

    public static void SetDpi(string pngPath, int dpi)
    {
        var temporaryPath = $"{pngPath}.dpi-{Guid.NewGuid():N}";
        try
        {
            using (var input = new FileStream(pngPath, FileMode.Open, FileAccess.Read, FileShare.Read))
            using (var output = new FileStream(temporaryPath, FileMode.CreateNew, FileAccess.Write, FileShare.None))
            {
                Span<byte> header = stackalloc byte[33];
                input.ReadExactly(header);
                if (!header[..8].SequenceEqual(PngSignature)
                    || Encoding.ASCII.GetString(header.Slice(12, 4)) != "IHDR")
                {
                    throw new InvalidDataException($"File is not a supported PNG: {pngPath}");
                }

                output.Write(header);
                WritePhysicalResolutionChunk(output, dpi);
                input.CopyTo(output);
            }

            File.Move(temporaryPath, pngPath, overwrite: true);
        }
        finally
        {
            if (File.Exists(temporaryPath))
            {
                File.Delete(temporaryPath);
            }
        }
    }

    private static void WritePhysicalResolutionChunk(Stream output, int dpi)
    {
        if (dpi <= 0)
        {
            throw new ArgumentOutOfRangeException(nameof(dpi), "DPI must be positive.");
        }

        var pixelsPerMeter = (uint)Math.Round(dpi / 0.0254);
        Span<byte> physicalResolution = stackalloc byte[9];
        BinaryPrimitives.WriteUInt32BigEndian(physicalResolution[..4], pixelsPerMeter);
        BinaryPrimitives.WriteUInt32BigEndian(physicalResolution.Slice(4, 4), pixelsPerMeter);
        physicalResolution[8] = 1;
        WriteChunk(output, "pHYs", physicalResolution);
    }

    private static void WriteChunk(Stream output, string type, ReadOnlySpan<byte> data)
    {
        Span<byte> length = stackalloc byte[4];
        BinaryPrimitives.WriteInt32BigEndian(length, data.Length);
        output.Write(length);

        Span<byte> typeBytes = stackalloc byte[4];
        Encoding.ASCII.GetBytes(type, typeBytes);
        output.Write(typeBytes);
        output.Write(data);

        var crc = UpdateCrc(0xffffffff, typeBytes);
        crc = UpdateCrc(crc, data) ^ 0xffffffff;
        Span<byte> crcBytes = stackalloc byte[4];
        BinaryPrimitives.WriteUInt32BigEndian(crcBytes, crc);
        output.Write(crcBytes);
    }

    private static uint UpdateCrc(uint crc, ReadOnlySpan<byte> data)
    {
        foreach (var value in data)
        {
            crc = CrcTable[(crc ^ value) & 0xff] ^ (crc >> 8);
        }

        return crc;
    }

    private static uint[] BuildCrcTable()
    {
        var table = new uint[256];
        for (uint index = 0; index < table.Length; index++)
        {
            var value = index;
            for (var bit = 0; bit < 8; bit++)
            {
                value = (value & 1) != 0 ? 0xedb88320 ^ (value >> 1) : value >> 1;
            }

            table[index] = value;
        }

        return table;
    }

    private sealed class IdatChunkStream : Stream
    {
        private const int ChunkSize = 64 * 1024;
        private readonly Stream _output;
        private readonly byte[] _buffer = new byte[ChunkSize];
        private int _count;
        private bool _completed;

        public IdatChunkStream(Stream output)
        {
            _output = output;
        }

        public override bool CanRead => false;
        public override bool CanSeek => false;
        public override bool CanWrite => true;
        public override long Length => throw new NotSupportedException();
        public override long Position { get => throw new NotSupportedException(); set => throw new NotSupportedException(); }

        public override void Write(byte[] buffer, int offset, int count)
        {
            Write(buffer.AsSpan(offset, count));
        }

        public override void Write(ReadOnlySpan<byte> buffer)
        {
            if (_completed)
            {
                throw new InvalidOperationException("IDAT stream is complete.");
            }

            while (!buffer.IsEmpty)
            {
                var copyLength = Math.Min(_buffer.Length - _count, buffer.Length);
                buffer[..copyLength].CopyTo(_buffer.AsSpan(_count));
                _count += copyLength;
                buffer = buffer[copyLength..];
                if (_count == _buffer.Length)
                {
                    FlushChunk();
                }
            }
        }

        public void Complete()
        {
            if (_completed)
            {
                return;
            }

            FlushChunk();
            _completed = true;
        }

        protected override void Dispose(bool disposing)
        {
            if (disposing && !_completed)
            {
                Complete();
            }

            base.Dispose(disposing);
        }

        private void FlushChunk()
        {
            if (_count == 0)
            {
                return;
            }

            WriteChunk(_output, "IDAT", _buffer.AsSpan(0, _count));
            _count = 0;
        }

        public override void Flush() => _output.Flush();
        public override int Read(byte[] buffer, int offset, int count) => throw new NotSupportedException();
        public override long Seek(long offset, SeekOrigin origin) => throw new NotSupportedException();
        public override void SetLength(long value) => throw new NotSupportedException();
    }
}
