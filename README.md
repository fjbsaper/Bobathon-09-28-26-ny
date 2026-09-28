# Bobathon-09-28-26-ny

## Purpose

Have IBM Bob create a viable program to consume an MQ message from a Uniform cluster.

---

## Projects

### MQ Consumer (`com.example.mqconsumer`)

A plain-Java 19+, no-build-tool Jakarta JMS consumer that reads text messages from an IBM MQ queue.
Multiple threads each own their own JMS connection. A thread exits when its receive timeout
expires; once all threads have exited the program terminates.

| File | Description |
|------|-------------|
| `consumer.properties` | Runtime configuration (CCDT URL, queue manager, queue, threads, timeout, log file) |
| `srcj/com/example/mqconsumer/AppConfig.java` | Loads and validates `consumer.properties` |
| `srcj/com/example/mqconsumer/Log4jStyleFormatter.java` | log4j-style `java.util.logging` formatter |
| `srcj/com/example/mqconsumer/LoggingSetup.java` | Wires the file handler to the root logger |
| `srcj/com/example/mqconsumer/ConsumerWorker.java` | Per-thread consumer logic |
| `srcj/com/example/mqconsumer/Main.java` | Entry point — launches threads, joins all |

### MQ Producer Test Harness (`com.example.mqproducer`)

A standalone Jakarta JMS producer that sends configurable batches of text messages to a queue.
Designed to exercise the consumer — multiple threads each own their own JMS connection.

| File | Description |
|------|-------------|
| `producer.properties` | Runtime configuration (CCDT URL, queue manager, queue, threads, messages per thread, log file) |
| `srcj/com/example/mqproducer/ProducerConfig.java` | Loads and validates `producer.properties` |
| `srcj/com/example/mqproducer/Log4jStyleFormatter.java` | log4j-style `java.util.logging` formatter |
| `srcj/com/example/mqproducer/LoggingSetup.java` | Wires the file handler to the root logger |
| `srcj/com/example/mqproducer/ProducerWorker.java` | Per-thread producer logic |
| `srcj/com/example/mqproducer/Main.java` | Entry point — launches threads, joins all |

See **[BUILDING.md](BUILDING.md)** for compile and run instructions.

---

## WMQConstants — classpath investigation findings

Investigation confirmed that `WMQ_CCDTURL` and `WMQ_QUEUE_MANAGER` are **not** in
`com.ibm.mq.constants.MQConstants`. The correct class and its location are:

| Class | Package | JAR | `WMQ_CCDTURL` value | `WMQ_QUEUE_MANAGER` value |
|-------|---------|-----|---------------------|--------------------------|
| `WMQConstants` | `com.ibm.msg.client.jakarta.wmq` | `com.ibm.mq.jakarta.client.jar` ✅ | `XMSC_WMQ_CCDTURL` | `XMSC_WMQ_QUEUE_MANAGER` |
| `MQConstants` | `com.ibm.mq.constants` | `com.ibm.mq.jakarta.client.jar` ❌ | not present | not present |
| `WMQConstants` | `com.ibm.msg.client.wmq` | `com.ibm.mq.allclient.jar` ❌ | same values | same values |

**Conclusion:** both projects import `com.ibm.msg.client.jakarta.wmq.WMQConstants` from inside
`com.ibm.mq.jakarta.client.jar`. The `jakarta\*` wildcard classpath is sufficient —
`com.ibm.mq.allclient.jar` is not required.

---

## Prompts
### Prompts made by Bob
Asked Bob to record the prompts to the user and their answers
### Prompts made by the user
rules requested:
create a log of all bob actions in .bob/log directory, create it if it does not exist and have the log file use a timestamp

create a rule to have bob log its prompts to the user and the replies from the user

create a plan for a jms (jakarta) consumer, with CCDTURL and qmgr name in properties as well as number of threads in properties, MQ jar files are in C:\MQ\MQ10.0\java\lib\modules\jakarta, time out in properties for duration without message. when time out reached exit program, log destination, reply to destination, message content, messages are text messages

update the plan to add a java.util.logging formatter that gives the same output as log4J but replace the thread name with the thread id

create a plan for the testing harness, multi-threaded with properties (number of threads, number of messages per thread) content of the text message is destination, thread ID # of message

add to the @jms-producer-plan.md the logging using the same formatter and logging the destination, theadID and message content


Execute both plans

update the plans to check if the contants referred by WMQ Constants are also part of the MQConstants class in com.ibm.mq.jakarta.client.jar and update the readme.md

apply the changes if not already done so

using plans @jms-consumer-plan.md  and @jms-producer-plan.md use folder srccs to create the code in c#

change the plan to add the retry optionns any on the connection factory and ccid 1208

also apply the changes to java in srcj
