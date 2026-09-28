# Building and Running

> **Requirement:** JDK 19 or later (JDK 17 compatible except `Thread.threadId()` requires JDK 19+).
> IBM MQ 10.0 client JARs must be installed at `C:\MQ\MQ10.0\java\lib\modules\jakarta\`.

**Classpath required at compile and run time — the jakarta wildcard is sufficient:**

| JAR | Location | Purpose |
|-----|----------|---------|
| `jakarta\*` (wildcard) | `C:\MQ\MQ10.0\java\lib\modules\jakarta\` | All required JARs in one directory: `com.ibm.mq.jakarta.client.jar` (includes `WMQConstants`), `jakarta.jms-api.jar`, Bouncy Castle TLS JARs, JNDI JARs |

> **Note:** `com.ibm.mq.allclient.jar` (in `C:\MQ\MQ10.0\java\lib\`) is **not** needed.
> The `WMQConstants` class used by both projects (`com.ibm.msg.client.jakarta.wmq.WMQConstants`)
> is already bundled inside `com.ibm.mq.jakarta.client.jar`.
> The similarly named `com.ibm.mq.constants.MQConstants` (also in `jakarta.client.jar`) does
> **not** contain `WMQ_CCDTURL` or `WMQ_QUEUE_MANAGER` — use `WMQConstants` only.

---

## MQ Consumer (`com.example.mqconsumer`)

### 1 – Compile

Run from the project root (the directory that contains `srcj/` and `consumer.properties`):

```cmd
javac -cp "C:\MQ\MQ10.0\java\lib\modules\jakarta\*" ^
      -d out ^
      srcj\com\example\mqconsumer\AppConfig.java ^
      srcj\com\example\mqconsumer\Log4jStyleFormatter.java ^
      srcj\com\example\mqconsumer\LoggingSetup.java ^
      srcj\com\example\mqconsumer\ConsumerWorker.java ^
      srcj\com\example\mqconsumer\Main.java
```

### 2 – Run (default properties file)

```cmd
java -cp "out;C:\MQ\MQ10.0\java\lib\modules\jakarta\*" com.example.mqconsumer.Main
```

### 3 – Run (custom properties file)

```cmd
java -cp "out;C:\MQ\MQ10.0\java\lib\modules\jakarta\*" com.example.mqconsumer.Main C:\path\to\my-consumer.properties
```

---

## MQ Producer Test Harness (`com.example.mqproducer`)

### 1 – Compile

```cmd
javac -cp "C:\MQ\MQ10.0\java\lib\modules\jakarta\*" ^
      -d out ^
      srcj\com\example\mqproducer\ProducerConfig.java ^
      srcj\com\example\mqproducer\Log4jStyleFormatter.java ^
      srcj\com\example\mqproducer\LoggingSetup.java ^
      srcj\com\example\mqproducer\ProducerWorker.java ^
      srcj\com\example\mqproducer\Main.java
```

### 2 – Run (default properties file)

```cmd
java -cp "out;C:\MQ\MQ10.0\java\lib\modules\jakarta\*" com.example.mqproducer.Main
```

### 3 – Run (custom properties file)

```cmd
java -cp "out;C:\MQ\MQ10.0\java\lib\modules\jakarta\*" com.example.mqproducer.Main C:\path\to\my-producer.properties
```

---

## Compiling both projects together

Because the two packages share an `out/` directory but have no cross-package dependencies,
you can compile everything in one pass:

```cmd
javac -cp "C:\MQ\MQ10.0\java\lib\modules\jakarta\*" ^
      -d out ^
      srcj\com\example\mqconsumer\*.java ^
      srcj\com\example\mqproducer\*.java
```

---

## Typical workflow

1. Edit `consumer.properties` and `producer.properties` with your CCDT URL, queue manager
   name, and queue name.
2. Compile (one-time, or after any source change).
3. Start the **consumer** first — it will wait for up to `consumer.timeoutSeconds` seconds.
4. Start the **producer** — it sends `producer.threads × producer.messagesPerThread` messages.
5. Check `mqconsumer.log` and `mqproducer.log` for the log4j-style output.

---

# C# / .NET Implementation (`srccs/`)

> **Requirement:** .NET 8 SDK or later.  
> IBM MQ .NET client is fetched automatically via NuGet (`IBMXMSDotnetClient`).  
> Pin the package version in the `.csproj` files to one that matches your MQ server.

The C# projects mirror the Java packages exactly:

| C# project | Equivalent Java package |
|------------|------------------------|
| `srccs/MqConsumer/` | `com.example.mqconsumer` |
| `srccs/MqProducer/` | `com.example.mqproducer` |

Both projects use the same `consumer.properties` / `producer.properties` files at the project root.

---

## C# MQ Consumer (`MqConsumer`)

### 1 – Restore dependencies

```cmd
dotnet restore srccs\MqConsumer\MqConsumer.csproj
```

### 2 – Build

```cmd
dotnet build srccs\MqConsumer\MqConsumer.csproj -c Release
```

### 3 – Run (default properties file)

```cmd
dotnet run --project srccs\MqConsumer\MqConsumer.csproj
```

### 4 – Run (custom properties file)

```cmd
dotnet run --project srccs\MqConsumer\MqConsumer.csproj -- C:\path\to\my-consumer.properties
```

### 5 – Publish self-contained executable (Windows x64)

```cmd
dotnet publish srccs\MqConsumer\MqConsumer.csproj -c Release -r win-x64 --self-contained -o out\cs\consumer
```

Then run directly:

```cmd
out\cs\consumer\MqConsumer.exe [properties-file]
```

---

## C# MQ Producer Test Harness (`MqProducer`)

### 1 – Restore dependencies

```cmd
dotnet restore srccs\MqProducer\MqProducer.csproj
```

### 2 – Build

```cmd
dotnet build srccs\MqProducer\MqProducer.csproj -c Release
```

### 3 – Run (default properties file)

```cmd
dotnet run --project srccs\MqProducer\MqProducer.csproj
```

### 4 – Run (custom properties file)

```cmd
dotnet run --project srccs\MqProducer\MqProducer.csproj -- C:\path\to\my-producer.properties
```

### 5 – Publish self-contained executable (Windows x64)

```cmd
dotnet publish srccs\MqProducer\MqProducer.csproj -c Release -r win-x64 --self-contained -o out\cs\producer
```

Then run directly:

```cmd
out\cs\producer\MqProducer.exe [properties-file]
```

---

## Typical C# workflow

1. Edit `consumer.properties` and `producer.properties` with your CCDT URL, queue manager name, and queue name.
2. Restore NuGet packages (`dotnet restore`) once.
3. Start the **consumer** first — it waits up to `consumer.timeoutSeconds` seconds.
4. Start the **producer** — it sends `producer.threads × producer.messagesPerThread` messages.
5. Check `mqconsumer.log` and `mqproducer.log` for log4j-style output.
