/**
 * Bob action logger — writes to .bob/log/<timestamp>.log
 * Handles PostToolUse and Stop events.
 *
 * The log file name is derived from the session_id so all events within a
 * single Bob session land in the same file.
 */

import { appendFileSync, mkdirSync } from "node:fs";
import { join } from "node:path";

let raw = "";
for await (const chunk of process.stdin) raw += chunk;

const payload = JSON.parse(raw);

const {
  session_id = "unknown",
  cwd = process.cwd(),
  hook_event_name,
  tool_name,
  tool_input,
  tool_response,
  last_assistant_message,
} = payload;

// Derive a filesystem-safe timestamp for the log filename from the session id.
// session_id is typically "task-<timestamp>-<random>"; we use the whole id
// sanitised of any characters that are invalid in filenames.
const safeSessionId = session_id.replace(/[^a-zA-Z0-9_-]/g, "_");
const logDir = join(cwd, ".bob", "log");
const logFile = join(logDir, `${safeSessionId}.log`);

mkdirSync(logDir, { recursive: true });

const ts = new Date().toISOString();

function truncate(value, max = 300) {
  if (value === undefined || value === null) return "";
  const str = typeof value === "string" ? value : JSON.stringify(value);
  return str.length > max ? str.slice(0, max) + " …(truncated)" : str;
}

let line;

if (hook_event_name === "PostToolUse") {
  const inputSummary = truncate(tool_input);
  const responseSummary = truncate(tool_response);
  line = `[${ts}] PostToolUse  tool=${tool_name}  input=${inputSummary}  response=${responseSummary}`;
} else if (hook_event_name === "Stop") {
  const lastMsg = truncate(last_assistant_message);
  line = `[${ts}] Stop  last_message=${lastMsg}`;
} else {
  // Fallback for any future event this script is wired to
  line = `[${ts}] ${hook_event_name}  payload=${truncate(raw)}`;
}

appendFileSync(logFile, line + "\n", "utf8");
