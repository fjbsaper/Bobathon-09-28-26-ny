import fs from "node:fs";
import path from "node:path";

let raw = "";
for await (const chunk of process.stdin) raw += chunk;
const payload = JSON.parse(raw);

const logDir = path.join(payload.cwd, ".bob", "logs");
const logFile = path.join(logDir, "conversation.log");
const timestamp = new Date().toISOString();

fs.mkdirSync(logDir, { recursive: true });
fs.appendFileSync(logFile, `${timestamp} [USER]\n${payload.prompt}\n\n`);
