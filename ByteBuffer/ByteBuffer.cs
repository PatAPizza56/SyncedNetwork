using System;
using System.Collections.Generic;
using System.Text;

public class ByteBuffer : IDisposable
{
    List<byte> buffer;
    bool bufferUpdated;
    byte[] readBuffer;
    int readPosition;

    #region "Byte Buffer"

    public ByteBuffer()
    {
        buffer = new List<byte>();
        bufferUpdated = false;
        readPosition = 0;
    }
    public long GetReadPosition() { return readPosition; }
    public byte[] ToArray() { return buffer.ToArray(); }
    public int Count() { return buffer.Count; }
    public int Length() { return Count() - readPosition; }
    public void Clear() { buffer.Clear(); readPosition = 0; }

    #endregion

    #region "Byte Write"

    public void Write(byte[] value)
    {
        buffer.AddRange(value);
        bufferUpdated = true;
    }
    public void Write(int value)
    {
        buffer.AddRange(BitConverter.GetBytes(value));
        bufferUpdated = true;
    }
    public void Write(float value)
    {
        buffer.AddRange(BitConverter.GetBytes(value));
        bufferUpdated = true;
    }
    public void Write(short value)
    {
        buffer.AddRange(BitConverter.GetBytes(value));
        bufferUpdated = true;
    }
    public void Write(long value)
    {
        buffer.AddRange(BitConverter.GetBytes(value));
        bufferUpdated = true;
    }
    public void Write(string value)
    {
        buffer.AddRange(BitConverter.GetBytes(value.Length));
        buffer.AddRange(Encoding.ASCII.GetBytes(value));
        bufferUpdated = true;
    }

    #endregion

    #region "Byte Read"

    public byte[] ReadBytes(int length, bool peek = true)
    {
        if (bufferUpdated)
        {
            readBuffer = buffer.ToArray();
            bufferUpdated = false;
        }

        byte[] value = buffer.GetRange(readPosition, length).ToArray();

        if (buffer.Count > readPosition && peek)
        {
            readPosition += length;
        }

        return value;
    }
    public int ReadInt(bool peek = true)
    {
        if (buffer.Count > readPosition)
        {
            if (bufferUpdated)
            {
                readBuffer = buffer.ToArray();
                bufferUpdated = false;
            }

            int value = BitConverter.ToInt32(readBuffer, readPosition);

            if (buffer.Count > readPosition && peek)
            {
                readPosition += 4;
            }

            return value;
        }
        else
        {
            throw new Exception("Byte to int failed. Make sure you are reading the correct value!");
        }
    }
    public float ReadFloat(bool peek = true)
    {
        if (buffer.Count > readPosition)
        {
            if (bufferUpdated)
            {
                readBuffer = buffer.ToArray();
                bufferUpdated = false;
            }

            float value = BitConverter.ToSingle(readBuffer, readPosition);

            if (buffer.Count > readPosition && peek)
            {
                readPosition += 4;
            }

            return value;
        }
        else
        {
            throw new Exception("Byte to float failed. Make sure you are reading the correct value!");
        }
    }
    public short ReadShort(bool peek = true)
    {
        if (buffer.Count > readPosition)
        {
            if (bufferUpdated)
            {
                readBuffer = buffer.ToArray();
                bufferUpdated = false;
            }

            short value = BitConverter.ToInt16(readBuffer, readPosition);

            if (buffer.Count > readPosition && peek)
            {
                readPosition += 2;
            }

            return value;
        }
        else
        {
            throw new Exception("Byte to short failed. Make sure you are reading the correct value!");
        }
    }
    public long ReadLong(bool peek = true)
    {
        if (buffer.Count > readPosition)
        {
            if (bufferUpdated)
            {
                readBuffer = buffer.ToArray();
                bufferUpdated = false;
            }

            long value = BitConverter.ToInt64(readBuffer, readPosition);

            if (buffer.Count > readPosition && peek)
            {
                readPosition += 8;
            }

            return value;
        }
        else
        {
            throw new Exception("Byte to long failed. Make sure you are reading the correct value!");
        }
    }
    public string ReadString(bool peek = true)
    {
        int stringLength = ReadInt();

        if (bufferUpdated)
        {
            readBuffer = buffer.ToArray();
            bufferUpdated = false;
        }

        string value = Encoding.ASCII.GetString(readBuffer, readPosition, stringLength);

        if (buffer.Count > readPosition && peek)
        {
            readPosition += stringLength;
        }

        return value;
    }

    #endregion

    #region "IDisposable Interface"

    bool disposedValue = false;

    protected virtual void Dispose(bool disposing)
    {
        if (!disposedValue)
        {
            if (disposing)
            {
                buffer.Clear();

                readPosition = 0;
            }

            disposedValue = true;
        }
    }

    public void Dispose()
    {
        Dispose(true);

        GC.SuppressFinalize(this);
    }

    #endregion
}