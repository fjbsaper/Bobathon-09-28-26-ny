package com.example.mqproducer;

import java.io.PrintWriter;
import java.io.StringWriter;
import java.time.Instant;
import java.time.ZoneId;
import java.time.format.DateTimeFormatter;
import java.util.logging.Formatter;
import java.util.logging.LogRecord;

/**
 * A {@link Formatter} that mimics the classic log4j layout:
 * <pre>
 *   yyyy-MM-dd HH:mm:ss,SSS LEVEL [threadId] loggerName - message
 * </pre>
 * The level field is right-padded to 5 characters (e.g. {@code INFO }, {@code WARN }).
 * The comma before milliseconds matches the log4j ISO8601 date pattern.
 * Thread ID is the long value from {@link LogRecord#getLongThreadID()} (Java 9+).
 */
public class Log4jStyleFormatter extends Formatter {

    private static final DateTimeFormatter TIMESTAMP_FMT = DateTimeFormatter
            .ofPattern("yyyy-MM-dd HH:mm:ss,SSS")
            .withZone(ZoneId.systemDefault());

    @Override
    public String format(LogRecord record) {
        String timestamp = TIMESTAMP_FMT.format(Instant.ofEpochMilli(record.getMillis()));
        String level     = String.format("%-5s", record.getLevel().getName());
        long   threadId  = record.getLongThreadID();
        String logger    = record.getLoggerName();
        String message   = formatMessage(record);

        StringBuilder sb = new StringBuilder(128);
        sb.append(timestamp)
          .append(' ').append(level)
          .append(" [").append(threadId).append(']')
          .append(' ').append(logger)
          .append(" - ").append(message)
          .append(System.lineSeparator());

        Throwable thrown = record.getThrown();
        if (thrown != null) {
            StringWriter sw = new StringWriter();
            thrown.printStackTrace(new PrintWriter(sw, true));
            sb.append(sw);
        }

        return sb.toString();
    }
}
