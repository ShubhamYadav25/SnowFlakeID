# 🚀 Snowflake ID

## 📌 Introduction
This repository implements a **Twitter Snowflake ID Algorithm** in **C#**.  
It generates **globally unique, time-ordered, and high-performance IDs** suitable for distributed systems.

### ⚡ Key Features:
- **64-bit unique ID** generation.
- **Time-ordered** (helps with indexing & queries).
- **Thread-safe** and supports **high concurrency**.
- **Up to 4 million IDs per second per machine**.  

---

## 🔢 64-bit Snowflake ID Structure  
A Snowflake ID consists of 64 bits, structured as follows:

| Bits | Field | Description |
|------|-------------------|--------------------------------------------------|
| 1  | **Sign bit** | Always `0` (ensures positive IDs) |
| 41 | **Timestamp** | Milliseconds since epoch (custom start time) |
| 10 | **Machine ID** | Ensures uniqueness across nodes (max **1024** machines) |
| 12 | **Sequence Number** | Handles multiple requests in the same millisecond (max **4096** per ms) |
| **64** | **Total** | Compact, sortable, and unique |

📌 **Example ID Breakdown:**  
If the generated ID is **`68802193764575234`**, its binary representation might look like:
0101110101011010000000010000000000000000000001000000000010 (64 bits)

- **Timestamp:** `41 bits`
- **Machine ID:** `10 bits`
- **Sequence:** `12 bits`

---

## ✅ Pros of Snowflake over UUID
| Feature           | Snowflake (64-bit)  | UUID (128-bit) |
|------------------|-------------------|---------------|
| **Size**         | **8 bytes**        | 16 bytes |
| **Human Readable** | ✅ Yes (sortable)  | ❌ No (random) |
| **Performance** | ✅ Fast (bitwise ops) | ❌ Slower (string parsing) |
| **Database Indexing** | ✅ Efficient (numeric) | ❌ Inefficient (string-based) |
| **Use Case** | ✅ Distributed IDs | ✅ Cryptographic uniqueness |

## 🚀 Load Testing Performance

### 🔥 **How to Test Under Load**
To evaluate **performance, concurrency, and uniqueness**, we run a **multi-threaded test**:
```csharp
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
```

📊 **Expected Output**
---
Generated 1000000 unique IDs in 809 ms
Throughput: 1236093.9431396786 IDs/sec
