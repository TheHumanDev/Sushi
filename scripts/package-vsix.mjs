import console from "node:console";
import { execSync } from "node:child_process";
import { mkdirSync, readFileSync, rmSync } from "node:fs";
import { dirname, join } from "node:path";
import { fileURLToPath } from "node:url";

const root = dirname(dirname(fileURLToPath(import.meta.url)));
const packageVersion = JSON.parse(readFileSync(join(root, "package.json"), "utf8")).version;
const projectProperties = readFileSync(join(root, "Directory.Build.props"), "utf8");
const assemblyVersion = projectProperties.match(/<Version>\s*([^<\s]+)\s*<\/Version>/)?.[1];

if (!assemblyVersion) {
    throw new Error("Cannot determine Sushi's assembly version from Directory.Build.props.");
}

for (const relativePath of ["server", "client/out", "syntaxes/sushi.tmLanguage.json", "vsix"]) {
    rmSync(join(root, relativePath), { recursive: true, force: true });
}

mkdirSync(join(root, "vsix"), { recursive: true });

run("dotnet publish Sushi/Sushi.csproj -c Release -r win-x64 --self-contained true -p:PublishSingleFile=true -o server");
run(`npm exec -- vsce package --pre-release --target win32-x64 --no-dependencies --githubBranch v${assemblyVersion} --out vsix/sushi-win32-x64-${packageVersion}.vsix`);

function run(command) {
    console.log(`\n> ${command}`);
    execSync(command, { cwd: root, stdio: "inherit" });
}
