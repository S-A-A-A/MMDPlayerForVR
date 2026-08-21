using System;
using System.IO;
using System.Text;
using UnityEngine;

namespace MMDPlayerForVR.PmxPhysics
{
    internal sealed class PmxBinaryReader : IDisposable
    {
        private readonly BinaryReader _reader;
        private Encoding _textEncoding = Encoding.UTF8;

        public long Position => _reader.BaseStream.Position;
        public long Length => _reader.BaseStream.Length;

        public PmxBinaryReader(Stream stream)
        {
            _reader = new BinaryReader(stream);
        }

        public void Dispose()
        {
            _reader.Dispose();
        }

        public byte ReadByte()
        {
            return _reader.ReadByte();
        }

        public int ReadInt32()
        {
            return _reader.ReadInt32();
        }

        public ushort ReadUInt16()
        {
            return _reader.ReadUInt16();
        }

        public float ReadSingle()
        {
            return _reader.ReadSingle();
        }

        public byte[] ReadBytes(int count)
        {
            return _reader.ReadBytes(count);
        }

        public void SetTextEncoding(byte encodingKind)
        {
            _textEncoding = encodingKind == 0 ? Encoding.Unicode : Encoding.UTF8;
        }

        public string ReadText()
        {
            long textLengthPosition = Position;
            int length = ReadInt32();
            if (length < 0)
            {
                throw new InvalidDataException($"Invalid PMX text length: {length} at byte {textLengthPosition}.");
            }

            return _textEncoding.GetString(ReadBytes(length));
        }

        public Vector3 ReadVector3()
        {
            return new Vector3(ReadSingle(), ReadSingle(), ReadSingle());
        }

        public int ReadIndex(int size)
        {
            switch (size)
            {
                case 1:
                    return (sbyte)_reader.ReadByte();
                case 2:
                    return _reader.ReadInt16();
                case 4:
                    return _reader.ReadInt32();
                default:
                    throw new InvalidDataException($"Unsupported PMX index size: {size}");
            }
        }

        public void Skip(long byteCount)
        {
            if (byteCount < 0)
            {
                throw new InvalidDataException($"Invalid PMX skip length: {byteCount}");
            }

            long next = _reader.BaseStream.Position + byteCount;
            if (next > _reader.BaseStream.Length)
            {
                throw new EndOfStreamException("PMX data ended while skipping a chunk.");
            }

            _reader.BaseStream.Seek(byteCount, SeekOrigin.Current);
        }
    }
}
