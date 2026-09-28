package com.example.mqconsumer;

import java.io.IOException;
import java.util.logging.FileHandler;
import java.util.logging.Handler;
import java.util.logging.Level;
import java.util.logging.Logger;

/**
 * Configures {@code java.util.logging} for the entire application.
 * Call {@link #init(String)} once from {@code Main} before any threads start.
 * <p>
 * Behaviour:
 * <ul>
 *   <li>Removes all default console handlers from the root logger.</li>
 *   <li>Attaches a {@link FileHandler} in append mode targeting {@code logFile}.</li>
 *   <li>Formats every record using {@link Log4jStyleFormatter}.</li>
 * </ul>
 */
public class LoggingSetup {

    private LoggingSetup() { /* utility class */ }

    /**
     * Initialise logging. Must be called exactly once before any worker threads are started.
     *
     * @param logFile path to the log file (will be created or appended to)
     * @throws IOException if the {@link FileHandler} cannot open the file
     */
    public static void init(String logFile) throws IOException {
        Logger root = Logger.getLogger("");

        // Remove default console handlers so nothing goes to stderr
        for (Handler handler : root.getHandlers()) {
            root.removeHandler(handler);
        }

        // Append mode: second arg true = append
        FileHandler fileHandler = new FileHandler(logFile, true);
        fileHandler.setFormatter(new Log4jStyleFormatter());
        fileHandler.setLevel(Level.ALL);

        root.addHandler(fileHandler);
        root.setLevel(Level.ALL);
    }
}
