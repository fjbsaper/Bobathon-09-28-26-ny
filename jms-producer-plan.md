# JMS (Jakarta) MQ Producer Test Harness — Plan

## Overview

Build a standalone plain-Java 17, no-build-tool Jakarta JMS producer that acts as a test harness for
the MQ consumer. Multiple threads each send a configurable number of text messages to a queue.
Each thread owns its own JMS connection (same pattern as the consumer). The text body of every
message is formatted to encode the destination, the sending thread ID, and the message sequence
number. Configuration is driven by a separate `producer.properties` file. Logging uses
`java.util.logging` with the same log4j-style formatter as the consumer (classes copied; projects
are fully independent). MQ Jakarta JARs are at `C:\MQ\MQ10.0\java\lib\modules\jakarta`.

---

## Sub-Tasks

---

### Sub-Task 1 — Project layout and properties file

**Intent**
Establish the source directory structure and the `producer.properties` file that drives all runtime
behaviour. The harness is self-contained — no shared source with the consumer project.

**Expected Outcomes**
- `srcj/com/example/mqproducer/` directory structure
- `producer.properties` at the project root with all required keys documented

**Todo List**
1. Create `srcj/com/example/mqproducer/` directory structure
2. Create `producer.properties` with the following keys (with example values and comments):
   - `mq.ccdtUrl` — CCDT file URL (e.g. `file:///C:/MQ/ccdt.json`)
   - `mq.queueManager` — queue manager name
   - `mq.queue` — destination queue name
   - `producer.threads` — number of producer threads (e.g. `3`)
   - `producer.messagesPerThread` — messages each thread sends (e.g. `10`)
   - `log.file` — path to the output log file (e.g. `mqproducer.log`)
   - `mq.reconnectOption` — reconnect scope: `ANY` | `QMGR` | `NO` (e.g. `ANY`)
   - `mq.reconnectCount` — total reconnect timeout in seconds; `-1` = indefinite (e.g. `600`)
   - `mq.reconnectDelaySeconds` — approximate delay between retries (e.g. `5`)
   - `mq.ccsid` — CCSID for message encoding; `1208` = UTF-8 (e.g. `1208`)

**Relevant Context**
- Compilation uses `javac -cp "C:\MQ\MQ10.0\java\lib\modules\jakarta\*"` targeting an `out/` directory

**Status** — `[ ] pending`

---

### Sub-Task 2 — Configuration loader

**Intent**
Centralise all property reading in one class so no other class opens files or parses strings
directly.

**Expected Outcomes**
- `ProducerConfig.java` — loads and validates `producer.properties`; exposes typed getters
- Constructor throws `IllegalArgumentException` for missing or invalid values
- Getters: `getCcdtUrl()`, `getQueueManager()`, `getQueue()`, `getThreadCount()`,
  `getMessagesPerThread()`, `getLogFile()`

**Todo List**
1. Create `ProducerConfig.java` that reads a `Properties` object from a path passed at construction
2. Add typed getters; parse `threads` and `messagesPerThread` as positive ints
3. Validate that all required keys are present and numeric values are > 0

**Relevant Context**
- Follows the same pattern as `AppConfig` in the consumer project

**Status** — `[ ] pending`

---

### Sub-Task 3 — Logging setup

**Intent**
Configure `java.util.logging` with the same log4j-style format used by the consumer (thread ID,
not thread name). Classes are copied into this project — no dependency on the consumer project.

**Expected Outcomes**
- `Log4jStyleFormatter.java` — identical logic to the consumer's formatter (copy, not shared)
- `LoggingSetup.java` — static `init(String logFile)` method; removes console handler, adds
  `FileHandler` in append mode with `Log4jStyleFormatter`
- Log line format: `yyyy-MM-dd HH:mm:ss,SSS LEVEL [threadId] loggerName - message`

**Todo List**
1. Copy `Log4jStyleFormatter.java` into `srcj/com/example/mqproducer/`; update the package declaration
2. Copy `LoggingSetup.java` into `srcj/com/example/mqproducer/`; update the package declaration
3. Verify formatter produces the expected output format (same as consumer)

**Relevant Context**
- `LogRecord.getLongThreadID()` (Java 9+) provides the thread ID
- `FileHandler` is thread-safe; no extra synchronisation needed

**Status** — `[ ] pending`

---

### Sub-Task 4 — Message body format

**Intent**
Define the exact text content of every produced message so the consumer logs have enough
information to correlate messages back to their sender.

**Expected Outcomes**
- Message body is a plain string in the format:
  `destination=<queueName> threadId=<id> msg=<sequenceNumber>`
  Example: `destination=DEV.QUEUE.1 threadId=42 msg=3`
- `sequenceNumber` is 1-based, counting from 1 to `messagesPerThread` within each thread

**Todo List**
1. Document the message body format in a code comment in `ProducerWorker.java` (next sub-task)
2. No separate class is needed; format is assembled inline with `String.format`

**Relevant Context**
- Thread ID is obtained via `Thread.currentThread().threadId()` (Java 19+) or
  `Thread.currentThread().getId()` (Java 17 compatible)
- Queue name comes from `ProducerConfig.getQueue()`

**Status** — `[ ] pending`

---

### Sub-Task 5 — Producer worker thread

**Intent**
Implement the per-thread logic: open its own JMS connection using CCDT, send the configured number
of text messages, log each send (including destination, thread ID, and full message body), then
clean up and exit. Logging uses `Log4jStyleFormatter` via the shared `LoggingSetup` initialised in
`Main`.

**Expected Outcomes**
- `ProducerWorker.java` implements `Runnable`
- Uses `com.ibm.mq.jakarta.jms.MQConnectionFactory` configured with CCDT URL and queue manager
- Sends exactly `messagesPerThread` `TextMessage` objects to the configured queue
- After each successful send, logs at INFO level a single line containing:
  - Destination queue name (from `ProducerConfig.getQueue()`)
  - Sending thread ID (from `Thread.currentThread().getId()`)
  - Full message body text (the assembled `destination=… threadId=… msg=…` string)
  - Example log entry: `Sent destination=DEV.QUEUE.1 threadId=42 msg=3 body=[destination=DEV.QUEUE.1 threadId=42 msg=3]`
- Closes producer, session, and connection in a `finally` block; logs thread exit at INFO level

**Todo List**
1. Create `ProducerWorker.java` with constructor taking `ProducerConfig` and a thread identifier
2. Build `MQConnectionFactory`; set `CCDTURL` and `QMANAGER` via `WMQConstants`
3. Set reconnect and CCSID properties on the factory (see Relevant Context below)
4. In `run()`: create `Connection`, `Session`, `MessageProducer`, and `Destination`
5. Loop from 1 to `messagesPerThread`:
   - Obtain thread ID via `Thread.currentThread().getId()`
   - Build message body: `String.format("destination=%s threadId=%d msg=%d", queue, threadId, n)`
   - Call `session.createTextMessage(body)` and `producer.send(message)`
   - Log at INFO: `"Sent destination=%s threadId=%d msg=%d body=[%s]"` with queue, threadId, n, body
6. `finally` block: close producer, session, connection; log `"Producer thread %d finished"` at INFO

**Relevant Context**
- Logger is obtained via `Logger.getLogger(ProducerWorker.class.getName())` — picks up the handler
  registered by `LoggingSetup` automatically
- Jakarta JMS package: `jakarta.jms.*`
- IBM MQ Jakarta factory: `com.ibm.mq.jakarta.jms.MQConnectionFactory`
- `CCDTURL` property: `WMQConstants.WMQ_CCDTURL`
- `QMANAGER` property: `WMQConstants.WMQ_QUEUE_MANAGER`
- No reply-to destination is set (harness sends fire-and-forget)
- **WMQConstants class to import:** `com.ibm.msg.client.jakarta.wmq.WMQConstants`
  — bundled inside `com.ibm.mq.jakarta.client.jar` (the jakarta modules directory)
  — `WMQ_CCDTURL` resolves to the string `"XMSC_WMQ_CCDTURL"`;
    `WMQ_QUEUE_MANAGER` resolves to `"XMSC_WMQ_QUEUE_MANAGER"`
- **Do NOT use** `com.ibm.msg.client.wmq.WMQConstants` (that is the non-jakarta class in
  `com.ibm.mq.allclient.jar`) — it is not needed and should not be on the classpath
- **Do NOT use** `com.ibm.mq.constants.MQConstants` (also in the jakarta jar) — it does NOT
  contain `WMQ_CCDTURL` or `WMQ_QUEUE_MANAGER`
- **Automatic client reconnect (ANY)**
  - `factory.setIntProperty(WMQConstants.WMQ_CLIENT_RECONNECT_OPTIONS, WMQConstants.WMQ_CLIENT_RECONNECT_ANY)`
    — permits reconnect to any queue manager listed in the CCDT
  - `factory.setIntProperty(WMQConstants.WMQ_CLIENT_RECONNECT_TIMEOUT, config.getReconnectCount())`
    — total retry window in seconds; `-1` means retry indefinitely
  - Other option values: `WMQ_CLIENT_RECONNECT` (same QM only), `WMQ_CLIENT_RECONNECT_DISABLED` (off)
- **CCSID 1208 (UTF-8)**
  - `factory.setIntProperty(WMQConstants.WMQ_CCSID, config.getCcsId())`
    — default value in `producer.properties` is `1208` (UTF-8)

**Status** — `[x] complete`

---

### Sub-Task 6 — Main entry point

**Intent**
Wire everything together: load config, initialise logging, launch all producer threads, wait for
all to finish, then exit.

**Expected Outcomes**
- `Main.java` with a `main` method
- Accepts an optional command-line argument for the properties file path (defaults to
  `producer.properties`)
- Spawns `threadCount` threads each wrapping a `ProducerWorker`; threads named `producer-1`,
  `producer-2`, …
- Joins all threads; logs final summary (total messages expected = threads × messagesPerThread)

**Todo List**
1. Create `Main.java`
2. Resolve properties file path from args or default
3. Construct `ProducerConfig`, call `LoggingSetup.init()`
4. Spawn and name `threadCount` threads; start all, then join all
5. After all threads finish, log total messages sent at INFO level and exit

**Relevant Context**
- Naming threads `producer-N` distinguishes them clearly in the shared log file

**Status** — `[ ] pending`

---

### Sub-Task 7 — Compile and run instructions

**Intent**
Document exact `javac` and `java` commands to compile and run the harness on Windows with no
build tool, using the MQ JARs at `C:\MQ\MQ10.0\java\lib\modules\jakarta`.

**Expected Outcomes**
- A `BUILDING.md` (or section in `README.md`) with copy-paste commands for Windows
- Classpath uses the wildcard `jakarta\*` to include all JARs in the directory

**Todo List**
1. Write `javac` command compiling all `.java` sources under `srcj/` into `out/`
2. Write `java` command launching `com.example.mqproducer.Main`
3. Note optional argument for a custom properties file path
4. Note the JDK 17 requirement

**Relevant Context**
- Wildcard classpath works for `javac` and `java` when all required JARs are in one directory

**Status** — `[ ] pending`

---

## Class Diagram

```
Main
 └─ ProducerConfig       (reads producer.properties)
 └─ LoggingSetup         (configures java.util.logging)
 └─ Log4jStyleFormatter  (log4j-style format with thread ID)
 └─ ProducerWorker × N   (Runnable — one JMS connection each)
```

## Dependency Notes

- Sub-Tasks 1 and 2 must be completed before any other sub-task.
- Sub-Task 3 (logging) and Sub-Task 4 (message format) can proceed in parallel with each other.
- Sub-Task 5 (worker) depends on Sub-Tasks 2, 3, and 4.
- Sub-Task 6 (Main) depends on Sub-Tasks 2, 3, and 5.
- Sub-Task 7 (build instructions) can be done last.
