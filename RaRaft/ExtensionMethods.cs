using System;
using System.Collections.Generic;
using System.IO;
using System.Runtime.Serialization.Formatters.Binary;

namespace RaRaft
{
    public static class ExtensionMethods
    {
        public static byte[] GetBytes(this object value)
        {
            if (value == null) throw new ArgumentNullException(nameof(value));

            var bf = new BinaryFormatter();
            using (var ms = new MemoryStream())
            {
                bf.Serialize(ms, value);
                return ms.ToArray();
            }
        }

        public static T GetObject<T>(this byte[] value)
        {
            if (value == null) throw new ArgumentNullException(nameof(value));

            var bf = new BinaryFormatter();
            using (var memStream = new MemoryStream())
            {
                memStream.Write(value, 0, value.Length);
                memStream.Seek(0, SeekOrigin.Begin);
                return (T) bf.Deserialize(memStream);
            }
        }

        

        public static int ReadInt(this Stream stream)
        {
            var buffer = new byte[sizeof(int)];
            stream.Read(buffer, 0, sizeof(int));
            return BitConverter.ToInt32(buffer,0);
        }

 

        public static T ReadObject<T>(this Stream stream, int length)
        {
            var buffer = new byte[length];
            stream.Read(buffer, 0, length);
            return buffer.GetObject<T>();
        }

        public static byte[] GetBuffer<T>(this LogEntry<T>[] entries)
        {
            var result = new List<byte>();
            foreach (var entry in entries)
            {
                // AddRange looks clumsy :¬(
                result.AddRange(entry.GetBuffer<T>());
            }
            return result.ToArray();
        }

        public static byte[] GetBuffer<T>(this LogEntry<T> entry)
        {
            var termBuffer = BitConverter.GetBytes(entry.Term);
            var indexBuffer = BitConverter.GetBytes(entry.Index);
            var valueBuffer = entry.Value.GetBytes();
            var valueSizeBuffer = BitConverter.GetBytes(valueBuffer.Length);
            
            var buffer = new byte[termBuffer.Length + indexBuffer.Length + valueSizeBuffer.Length + valueBuffer.Length];
            var index = 0;
            var append = new Action<byte[]>(bytes =>
            {
                Buffer.BlockCopy(bytes, 0, buffer, index, bytes.Length);
                index += bytes.Length;
            });

            append(termBuffer);
            append(indexBuffer);
            append(valueSizeBuffer);
            append(valueBuffer);
            return buffer;
        }
        

    }
}
