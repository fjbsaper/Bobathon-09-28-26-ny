import fs from "node:fs";
import path from "node:path";

let raw = "";
for await (const chunk of process.stdin) raw += chunk;
const payload = JSON.parse(raw);

if (!payload.last_assistant_message) process.exit(0);

const logDir = path.join(payload.cwd, ".bob", "logs");
const logFile = path.join(logDir, "conversation.log");
const timestamp = new Date().toISOString();

fs.mkdirSync(logDir, { recursive: true });
fs.appendFileSync(logFile, `${timestamp} [BOB]\n${payload.last_assistant_message}\n\n`);
