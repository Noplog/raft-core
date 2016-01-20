using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Threading;

namespace RaRaft
{
    // Lets do some serious perf testing before we optimise this
    // TODO: Add a memory cache?
    // TODO: Add bookmarks for term/index positions in the file?
    // TODO: Compaction?


    /// <summary>
    /// Stores a log of T items, with a term and index
    /// </summary>
    /// <typeparam name="T"></typeparam>
    public class MonotonicLog<T> : IDisposable
    {
        Stream logStream;

        public MonotonicLog(Stream stream)
        {
            logStream = stream;
        }

        /// <summary>
        /// Get the highest index in the log
        /// </summary>
        /// <returns></returns>
        public int GetHighestIndex()
        {
            try
            {
                Monitor.Enter(logStream);
                var highestIndex = 0;
                logStream.Position = 0;
                while (logStream.Position < logStream.Length)
                {
                    var term = logStream.ReadInt();
                    var index = logStream.ReadInt();
                    var valueSize = logStream.ReadInt();
                    highestIndex = Math.Max(index, highestIndex);
                    logStream.Position += valueSize;
                }
                return highestIndex;
            }
            finally
            {
                Monitor.Exit(logStream);
            }
        }

      

        /// <summary>
        /// Add an entry to the log. Note that the index for the entry is controlled by the caller
        /// </summary>
        /// <param name="entries"></param>
        public void Append(params LogEntry<T>[] entries)
        {
            var buffer = entries.GetBuffer();
            
            try
            {
                Monitor.Enter(logStream);
                logStream.Position = logStream.Length;
                logStream.Write(buffer, 0, buffer.Length);
                logStream.Flush();
            }
            finally
            {
                Monitor.Exit(logStream);
            }
        }

        /// <summary>
        /// Retrieve a page of log entries 
        /// </summary>
        /// <param name="from"></param>
        /// <param name="pageSize"></param>
        /// <returns></returns>
        public LogEntry<T>[] Scan(int from = 0, int pageSize = 500)
        {
            try
            {
                Monitor.Enter(logStream);
                return this.InternalScan(from).Take(pageSize).ToArray();
            }
            finally
            {
                Monitor.Exit(logStream);
            }
        }

        // not thread safe
        IEnumerable<LogEntry<T>> InternalScan(int from = 0)
        {
            logStream.Position = 0;
            while (logStream.Position < logStream.Length)
            {
                var term = logStream.ReadInt();
                var index = logStream.ReadInt();
                var valueSize = logStream.ReadInt();
                if (index < from)
                {
                    logStream.Position += valueSize;
                    continue;
                }
                var value = logStream.ReadObject<T>(valueSize);
                yield return new LogEntry<T>(term, index, value);
            }
        }

        /// <summary>
        /// Retreive a log entry by index
        /// </summary>
        /// <param name="index"></param>
        /// <returns></returns>
        public LogEntry<T> Retrieve(int index)
        {
            try
            {
                Monitor.Enter(logStream);
                return this.InternalScan(index).FirstOrDefault();
            }
            finally
            {
                Monitor.Exit(logStream);
            }
            
        }

        /// <summary>
        /// Winds back the log to the entry before the supplied index
        /// </summary>
        /// <param name="to"></param>
        public void WindBackToIndex(int to)
        {
            try
            {
                Monitor.Enter(logStream);
                logStream.Position = 0;
                while (logStream.Position < logStream.Length)
                {
                    var startOfCurrent = logStream.Position;

                    var term = logStream.ReadInt();
                    var index = logStream.ReadInt();
                    var valueSize = logStream.ReadInt();
                    if (index < to)
                    {
                        logStream.Position += valueSize;
                        continue;
                    }

                    logStream.Position = 0;
                    logStream.SetLength(startOfCurrent);
                    logStream.Flush();
                    return;
                }
            }
            finally
            {
                Monitor.Exit(logStream);
            }
        }


        public void Dispose()
        {
            this.logStream.Dispose();
        }
    }
}
