using System;
using System.Collections.Concurrent;
using System.Diagnostics;
using System.Threading;

/**
 *  Snowflake ID Structure (64-bit)
 *  1	Sign bit	Always 0 (since IDs are positive)
 *  41	Timestamp	Milliseconds since a custom epoch
 *  10	Machine ID	Ensures uniqueness across nodes (e.g., server ID)
 *  12	Sequence    Number	Ensures uniqueness within the same millisecond
 *  64	Total	    Compact and sortable unique ID
 */

public class Snowflake
{
    // Twitter epoch (Nov 4, 2010)
    private const long EPOCH = 1288834974657L;

    // 10-bit machine ID (supports 2^10 = 1024 machines)
    private const int MACHINE_ID_BITS = 10;

    // Max Machine ID = 1023 (2^10 -1)
    private const long MAX_MACHINE_ID = (1L << MACHINE_ID_BITS) - 1;

    // 12-bit sequence number (2^12 = 4096 IDs per ms)
    private const int SEQUENCE_BITS = 12;

    // Max Sequence = 4095 (2^12-1)
    private const long MAX_SEQUENCE = (1L << SEQUENCE_BITS) - 1;

    // MachineId can be server number ex: server1, server2, server...N
    private readonly long machineId;
    private long lastTimestamp = -1L;
    private long sequence = 0L;
    private readonly object lockObj = new object();

    /// <summary>
    /// Takes a machine ID (0-1023) to ensure uniqueness in distributed systems.
    /// </summary>
    /// <param name="machineId"></param>
    /// <exception cref="ArgumentException"></exception>
    public Snowflake(long machineId)
    {
        if (machineId > MAX_MACHINE_ID || machineId < 0)
            throw new ArgumentException("Machine ID out of range");

        this.machineId = machineId;
    }

    /// <summary>
    ///  Current UTC timestamp in milliseconds ex: 1707901845123
    /// </summary>
    /// <returns></returns>
    private long CurrentTimestamp()
    {
        return DateTimeOffset.UtcNow.ToUnixTimeMilliseconds();
    }

    /// <summary>
    /// 
    /// </summary>
    /// <param name="lastTimestamp"></param>
    /// <returns></returns>
    private long WaitNextMillis(long lastTimestamp)
    {
        long currentTimestamp = CurrentTimestamp();
        while (currentTimestamp <= lastTimestamp)
        {
            currentTimestamp = CurrentTimestamp();
        }
        return currentTimestamp;
    }

    public long NextId()
    {
        // thread safe
        lock (lockObj)
        {
            long timestamp = CurrentTimestamp();

            // Handling clock drift (if time moves backward, it prevents ID generation).
            if (timestamp < lastTimestamp)
            {
                throw new Exception("Clock moved backwards! Rejecting requests.");
            }

            // If multiple IDs are requested within the same millisecond, it increments the sequence number (0-4095).
            // If the sequence reaches 4096 (max per ms), it waits for the next millisecond
            if (timestamp == lastTimestamp)
            {
                sequence = (sequence + 1) & MAX_SEQUENCE;
                if (sequence == 0)
                {
                    timestamp = WaitNextMillis(lastTimestamp);
                }
            }
            else
            {
                sequence = 0;
            }

            // update the lastTimestamp for upcoming ids
            lastTimestamp = timestamp;

            /**
             * 64-bit ID
             * Left shift timestamp by (10 + 12) = 22 bits.
             * Left shift machine ID by 12 bits.
             * Bitwise OR (|) to merge all parts together.
             */
            return ((timestamp - EPOCH) << (MACHINE_ID_BITS + SEQUENCE_BITS)) |
                   (machineId << SEQUENCE_BITS) | sequence;
        }
    }
}

class Program
{
    /***
    * --------------------------  Load Test --------------------------------------------
    */
    public static void Main()
    {
        // Number of concurrent threads
        int threadCount = 10;

        // IDs generated per thread
        int idsPerThread = 100000; 

        // Initialize with Machine ID = 1
        var snowflake = new Snowflake(1);

        // To check for duplicates
        var idSet = new ConcurrentDictionary<long, bool>();

        var stopwatch = Stopwatch.StartNew();

        Parallel.For(0, threadCount, i =>
        {
            for (int j = 0; j < idsPerThread; j++)
            {
                long id = snowflake.NextId();

                if (!idSet.TryAdd(id, true))
                {
                    Console.WriteLine($"Duplicate ID detected: {id}");
                }
            }
        });

        stopwatch.Stop();

        long totalIds = threadCount * idsPerThread;
        Console.WriteLine($"Generated {totalIds} unique IDs in {stopwatch.ElapsedMilliseconds} ms");
        Console.WriteLine($"Throughput: {totalIds / (stopwatch.ElapsedMilliseconds / 1000.0)} IDs/sec");
    }
}

