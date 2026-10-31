#!/usr/bin/env -S deno run --allow-run --allow-net --allow-read --allow-write --ext ts

const PROD_PF_PORT = 5433;
const PROD_POD = "postgres-statefulset-0";
const PROD_NS = "default";
const PROD_SECRET = "postgres-secrets";
const DB = "netptune";
const DEV_URL = "docker://postgres/18/dev?search_path=public";
const SERVER_DIR = new URL("../server/", import.meta.url).pathname;

const apply = Deno.args.includes("--apply");

async function run(cmd: string, args: string[], cwd?: string): Promise<string> {
  const proc = new Deno.Command(cmd, { args, cwd, stdout: "piped", stderr: "piped" });
  const { code, stdout, stderr } = await proc.output();
  if (code !== 0) {
    throw new Error(new TextDecoder().decode(stderr) || new TextDecoder().decode(stdout));
  }
  return new TextDecoder().decode(stdout).trim();
}

async function generateSchema(): Promise<string> {
  const dir = await Deno.makeTempDir({ prefix: "netptune-schema-" });
  const path = `${dir}/schema.sql`;

  console.log("Generating schema from the EF model...");
  await run("dotnet", ["ef", "dbcontext", "script", "-p", "Netptune.Entities", "-s", "Netptune.Entities", "-o", path], SERVER_DIR);

  const sql = await Deno.readTextFile(path);
  await Deno.writeTextFile(path, sql.replace(/^\uFEFF/, ""));

  return path;
}

async function getProdPassword(): Promise<string> {
  const encoded = await run("kubectl", [
    "get", "secret", PROD_SECRET, "-n", PROD_NS, "-o", "jsonpath={.data.POSTGRES_PASSWORD}",
  ]);
  return atob(encoded);
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

const schemaPath = await generateSchema();
const schemaUrl = `file://${schemaPath}`;
const prodPass = encodeURIComponent(await getProdPassword());
const prodUrl = `postgres://postgres:${prodPass}@localhost:${PROD_PF_PORT}/${DB}?sslmode=disable&search_path=public`;

console.log(`Port-forwarding ${PROD_POD} -> localhost:${PROD_PF_PORT}...`);
const pf = new Deno.Command("kubectl", {
  args: ["port-forward", PROD_POD, `${PROD_PF_PORT}:5432`, "-n", PROD_NS],
  stdout: "null",
  stderr: "null",
}).spawn();

let exitCode = 1;
try {
  await waitForPort(PROD_PF_PORT);
  console.log("Diffing: EF model -> prod\n");

  const atlasArgs = apply
    ? ["schema", "apply", "--url", prodUrl, "--to", schemaUrl, "--dev-url", DEV_URL]
    : ["schema", "diff", "--from", prodUrl, "--to", schemaUrl, "--dev-url", DEV_URL];

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
