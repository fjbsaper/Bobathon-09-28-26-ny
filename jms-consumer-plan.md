# JMS (Jakarta) MQ Consumer — Plan

## Overview

Build a plain-Java 17, no-build-tool Jakarta JMS consumer that reads text messages from an IBM MQ queue.
Configuration is loaded from a properties file. Each consumer thread gets its own JMS connection.
When a thread receives no message within the configured timeout it exits; once all threads have exited the
program terminates. All logging (destination, reply-to, message content) goes to a single shared
`java.util.logging` file.

MQ Jakarta JARs are located at `C:\MQ\MQ10.0\java\lib\modules\jakarta`.

---

## Sub-Tasks

---

### Sub-Task 1 — Project layout and properties file

**Intent**  
Establish the source directory structure and the properties file that drives all runtime behaviour.
Nothing is hard-coded; every tuneable value lives in one file.

**Expected Outcomes**
- `srcj/` directory with package `com.example.mqconsumer`
- `consumer.properties` file at the project root with all required keys documented

**Todo List**
1. Create `srcj/com/example/mqconsumer/` directory structure
2. Create `consumer.properties` with the following keys (with example values and comments):
   - `mq.ccdtUrl` — CCDT file URL (e.g. `file:///C:/MQ/ccdt.json`)
   - `mq.queueManager` — queue manager name
   - `mq.queue` — destination queue name
   - `consumer.threads` — number of consumer threads (e.g. `3`)
   - `consumer.timeoutSeconds` — seconds to wait for a message before the thread exits (e.g. `30`)
   - `log.file` — path to the output log file (e.g. `mqconsumer.log`)
   - `mq.reconnectOption` — reconnect scope: `ANY` | `QMGR` | `NO` (e.g. `ANY`)
   - `mq.reconnectCount` — total reconnect timeout in seconds; `-1` = indefinite (e.g. `600`)
   - `mq.reconnectDelaySeconds` — approximate delay between retries (e.g. `5`)
   - `mq.ccsid` — CCSID for message encoding; `1208` = UTF-8 (e.g. `1208`)

**Relevant Context**
- No build tool; compilation will use `javac` with `-cp` pointing at the Jakarta JARs

**Status** — `[ ] pending`

---

### Sub-Task 2 — Configuration loader

**Intent**  
Centralise all property reading in one class so no other class calls `System.getProperty` or opens
files directly.

**Expected Outcomes**
- `AppConfig.java` — loads and validates `consumer.properties`; exposes typed getters
- Constructor throws `IllegalArgumentException` for missing or invalid values
- Properties: `getCcdtUrl()`, `getQueueManager()`, `getQueue()`, `getThreadCount()`,
  `getTimeoutMillis()`, `getLogFile()`

**Todo List**
1. Create `AppConfig.java` that reads a `Properties` object from a path passed at construction
2. Add typed getters; parse `threads` as int, `timeoutSeconds` as int (converted to ms)
3. Validate that required keys are present and values are positive where applicable

**Relevant Context**
- Used by `Main` and passed into each worker thread

**Status** — `[ ] pending`

---

### Sub-Task 3 — Logging setup

**Intent**
Configure `java.util.logging` programmatically so that all threads write to a single rotating file
handler. The format must match the classic log4j pattern
`%d{ISO8601} %-5p [<threadId>] %c - %m%n`
but with the thread **ID** (a long integer) in place of the thread name.

**Expected Outcomes**
- `Log4jStyleFormatter.java` — a custom `java.util.logging.Formatter` subclass
- Output line format (one line per record, plus the exception stack trace if present):
  `2024-05-01 12:34:56,123 INFO  [42] com.example.mqconsumer.ConsumerWorker - Message text here`
  - Date/time: `yyyy-MM-dd HH:mm:ss,SSS` (log4j ISO8601 style)
  - Level right-padded to 5 characters (e.g. `INFO `, `WARN `, `ERROR`)
  - Thread ID in square brackets: `[<id>]`
  - Logger name (class name) after the bracket
  - Message after ` - `
  - Appended throwable stack trace when present
- `LoggingSetup.java` — one static `init(String logFile)` method
- Configures a `FileHandler` in append mode on the root logger using `Log4jStyleFormatter`
- Called once from `Main` before any threads start

**Todo List**
1. Create `Log4jStyleFormatter.java` extending `java.util.logging.Formatter`
2. Override `format(LogRecord record)`:
   - Format timestamp using `java.time.Instant` + `DateTimeFormatter` with pattern `yyyy-MM-dd HH:mm:ss,SSS`
   - Pad the level name to 5 chars with `String.format("%-5s", level)`
   - Obtain thread ID via `record.getLongThreadID()`
   - Assemble the line: `"timestamp LEVEL [threadId] loggerName - message\n"`
   - Append formatted throwable via `formatThrowable(record)` (or `super.formatMessage`) if present
3. Create `LoggingSetup.java`
4. Remove default console handler from root logger
5. Add a `FileHandler` in append mode targeting `AppConfig.getLogFile()`, set formatter to `Log4jStyleFormatter`

**Relevant Context**
- `LogRecord.getLongThreadID()` is available from Java 9+; safe to use with Java 17
- `FileHandler` is thread-safe; no additional synchronisation needed for logging
- log4j ISO8601 date pattern uses a comma before milliseconds (`ss,SSS`), not a dot

**Status** — `[ ] pending`

---

### Sub-Task 4 — Consumer worker thread

**Intent**  
Implement the per-thread logic: open its own JMS connection using CCDT, loop receiving messages
until the timeout is reached with no message, log each message's destination, reply-to, and text
content, then clean up and exit.

**Expected Outcomes**
- `ConsumerWorker.java` implements `Runnable`
- Uses `com.ibm.mq.jakarta.jms.MQConnectionFactory` configured with CCDT URL and queue manager
- Calls `connection.createSession()` → `session.createConsumer(queue)` → `consumer.receive(timeoutMs)`
- Logs on each received message: destination, reply-to destination, message text
- Exits the run loop when `receive()` returns `null` (timeout); closes consumer, session, connection
- Logs a final INFO entry on exit

**Todo List**
1. Create `ConsumerWorker.java` with constructor taking `AppConfig` and a thread identifier
2. Build `MQConnectionFactory`, set `CCDTURL` property and `QMANAGER` property
3. Set reconnect and CCSID properties on the factory (see Relevant Context below)
4. Create `Connection`, `Session`, `Destination`, and `MessageConsumer` in `run()`
5. Loop: call `consumer.receive(timeoutMs)`; if null break; else log and continue
6. Log: destination name from `message.getJMSDestination()`, reply-to from `message.getJMSReplyTo()`,
   body from casting to `TextMessage` and calling `getText()`
7. In a `finally` block: close consumer, session, connection; log thread exit

**Relevant Context**
- Jakarta JMS package: `jakarta.jms.*`
- IBM MQ Jakarta factory class: `com.ibm.mq.jakarta.jms.MQConnectionFactory`
- CCDT is set via `factory.setStringProperty(WMQConstants.WMQ_CCDTURL, url)`
- Queue manager via `factory.setStringProperty(WMQConstants.WMQ_QUEUE_MANAGER, qmgr)`
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
    — default value in `consumer.properties` is `1208` (UTF-8)

**Status** — `[x] complete`

---

### Sub-Task 5 — Main entry point

**Intent**  
Wire everything together: load config, initialise logging, launch the configured number of worker
threads, wait for all of them to finish, then exit.

**Expected Outcomes**
- `Main.java` with a `main` method
- Accepts an optional command-line argument for the properties file path (defaults to `consumer.properties`)
- Creates and starts `N` threads wrapping `ConsumerWorker`
- Calls `thread.join()` on each thread; exits after all have returned

**Todo List**
1. Create `Main.java`
2. Resolve properties file path from args or default
3. Construct `AppConfig`, call `LoggingSetup.init()`
4. Spawn `AppConfig.getThreadCount()` threads, each with a new `ConsumerWorker`; name threads `worker-1`, `worker-2`, …
5. Start all threads, then join all threads
6. Log final program-exit message at INFO level

**Relevant Context**
- Thread naming aids log readability when all threads share one file

**Status** — `[ ] pending`

---

### Sub-Task 6 — Compile and run instructions

**Intent**  
Document the exact `javac` and `java` commands needed to compile and run the program without a
build tool, so any developer can reproduce it with the MQ JARs at `C:\MQ\MQ10.0\java\lib\modules\jakarta`.

**Expected Outcomes**
- `README.md` section (or standalone `BUILDING.md`) with copy-paste compile and run commands for Windows
- Classpath lists all required JARs from the Jakarta modules directory

**Todo List**
1. Identify the required JARs from `C:\MQ\MQ10.0\java\lib\modules\jakarta` (at minimum:
   `com.ibm.mq.jakarta.client.jar` and `jakarta.jms-api.jar`)
2. Write `javac` command compiling all `.java` sources into an `out/` directory
3. Write `java` command launching `com.example.mqconsumer.Main`
4. Note the JDK 17 requirement

**Relevant Context**
- Wildcard classpath (`jakarta\*`) can be used if all JARs in the directory should be included

**Status** — `[ ] pending`

---

## Class Diagram

```
Main
 └─ AppConfig          (reads consumer.properties)
 └─ LoggingSetup       (configures java.util.logging)
 └─ ConsumerWorker × N (Runnable — one JMS connection each)
```

## Dependency Notes

- Sub-Tasks 1 and 2 must be completed before any other sub-task.
- Sub-Task 3 (logging) can be done in parallel with Sub-Task 4 (worker).
- Sub-Task 5 (Main) depends on Sub-Tasks 2, 3, and 4.
- Sub-Task 6 (build instructions) can be done last.
