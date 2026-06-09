#!/usr/bin/env -S deno run --allow-run --allow-net --ext ts

const PROD_PF_PORT = 5433;
const PROD_POD = "postgres-statefulset-0";
const PROD_NS = "default";
const PROD_PASS = "postgresx";
const DB = "netptune";

const apply = Deno.args.includes("--apply");

async function run(cmd: string, args: string[]): Promise<string> {
  const proc = new Deno.Command(cmd, { args, stdout: "piped", stderr: "piped" });
  const { code, stdout, stderr } = await proc.output();
  if (code !== 0) {
    throw new Error(new TextDecoder().decode(stderr));
  }
  return new TextDecoder().decode(stdout).trim();
}

async function getLocalPostgresContainer(): Promise<string> {
  const ps = await run("podman", ["ps", "--format", "{{.Names}}"]);
  const container = ps.split("\n").find((n) => n.startsWith("postgres-"));
  if (!container) throw new Error("No local postgres container found (is Aspire running?)");
  return container;
}

async function getLocalPassword(container: string): Promise<string> {
  const raw = await run("podman", ["inspect", container]);
  const [info] = JSON.parse(raw);
  const envEntry = info.Config.Env.find((e: string) => e.startsWith("POSTGRES_PASSWORD="));
  if (!envEntry) throw new Error("POSTGRES_PASSWORD not found in container env");
  return envEntry.split("=").slice(1).join("=");
}

async function getLocalPort(container: string): Promise<number> {
  const port = await run("podman", ["port", container, "5432/tcp"]);
  const match = port.match(/:(\d+)$/);
  if (!match) throw new Error(`Could not determine local postgres port from: ${port}`);
  return Number(match[1]);
}

async function waitForPort(port: number, retries = 20): Promise<void> {
  for (let i = 0; i < retries; i++) {
    try {
      const conn = await Deno.connect({ port });
      conn.close();
      return;
    } catch {
      await new Promise((r) => setTimeout(r, 500));
    }
  }
  throw new Error(`Port ${port} did not become available`);
}

async function displayDiffWithBat(atlasArgs: string[]): Promise<number> {
  const atlas = new Deno.Command("atlas", {
    args: atlasArgs,
    stdout: "piped",
    stderr: "inherit",
  }).spawn();

  const bat = new Deno.Command("bat", {
    args: ["--language", "sql"],
    stdin: "piped",
  }).spawn();

  await atlas.stdout.pipeTo(bat.stdin);
  const [atlasStatus, batStatus] = await Promise.all([atlas.status, bat.status]);
  return atlasStatus.code || batStatus.code;
}

const localContainer = await getLocalPostgresContainer();
const localPass = await getLocalPassword(localContainer);
const localPort = await getLocalPort(localContainer);
const localUrl = `postgres://postgres:${localPass}@localhost:${localPort}/${DB}?sslmode=disable`;
const prodUrl = `postgres://postgres:${PROD_PASS}@localhost:${PROD_PF_PORT}/${DB}?sslmode=disable`;

console.log(`Port-forwarding ${PROD_POD} -> localhost:${PROD_PF_PORT}...`);
const pf = new Deno.Command("kubectl", {
  args: ["port-forward", PROD_POD, `${PROD_PF_PORT}:5432`, "-n", PROD_NS],
  stdout: "null",
  stderr: "null",
}).spawn();

let exitCode = 1;
try {
  await waitForPort(PROD_PF_PORT);
  console.log("Diffing: local -> prod\n");

  const atlasArgs = apply
    ? ["schema", "apply", "--url", prodUrl, "--to", localUrl, "--dev-url", "docker://postgres/16/dev", "--auto-approve"]
    : ["schema", "diff", "--from", prodUrl, "--to", localUrl];

  if (apply) {
    const atlas = new Deno.Command("atlas", { args: atlasArgs }).spawn();
    const { code } = await atlas.status;
    exitCode = code;
  } else {
    exitCode = await displayDiffWithBat(atlasArgs);
  }
} finally {
  pf.kill("SIGTERM");
}

Deno.exit(exitCode);
