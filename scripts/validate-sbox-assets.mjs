import fs from "node:fs";
import path from "node:path";

const root = process.cwd();
const sourceExtensions = new Set([".cs", ".scene", ".prefab", ".json", ".md", ".mjs", ".vmdl"]);
const ignoredDirs = new Set([".git", ".sbox", ".vs", "bin", "obj"]);
const beanModelPath = ["models", "bean"].join("/");
const aiSourcePath = ["source_assets", "ai3d"].join("/");
const allowedImportedModelPaths = [
  path.normalize(path.join("Assets", "models", "quaternius")),
  path.normalize(path.join("Assets", "models", "frutiger_aero")),
];
const allowedImportedModelFiles = new Set([
  path.normalize(path.join("Assets", "models", "authored", "projection_sphere.vmdl")),
]);
const allowedPrimitiveModelFiles = new Set([
  path.normalize(path.join("Assets", "models", "authored", "pixel_grass_floor_slab.vmdl")),
]);
const importedMeshNode = ["Render", "Mesh", "File"].join("");
const renderPrimitiveNode = ["Render", "Primitive"].join("");
const devBoxModelPath = ["models", "dev", "box.vmdl"].join("/");
const devSphereModelPath = ["models", "dev", "sphere.vmdl"].join("/");
const removedAiPipelineNames = [
  ["cross", "ai", "3d"].join("-"),
  ["Hunyuan", "3D"].join(""),
  ["generate", "ai3d", "assets"].join("-"),
  ["blender", "convert", "glb", "to", "fbx"].join("-"),
];

const forbiddenPatterns = [
  { pattern: new RegExp(`${beanModelPath}/`), label: "removed bean model path reference" },
  { pattern: new RegExp(`${aiSourcePath}|${aiSourcePath.replaceAll("/", "\\\\")}`), label: "removed AI source path reference" },
  { pattern: new RegExp(`${devBoxModelPath}|${devSphereModelPath}`), label: "visible dev primitive model reference" },
  { pattern: new RegExp(removedAiPipelineNames.join("|")), label: "removed AI generation pipeline reference" },
];

let failed = false;

function fail(message) {
  failed = true;
  console.error(`ERROR: ${message}`);
}

function walk(dir) {
  const entries = fs.existsSync(dir) ? fs.readdirSync(dir, { withFileTypes: true }) : [];
  const files = [];

  for (const entry of entries) {
    const full = path.join(dir, entry.name);
    if (entry.isDirectory()) {
      if (!ignoredDirs.has(entry.name)) {
        files.push(...walk(full));
      }
      continue;
    }

    files.push(full);
  }

  return files;
}

const forbiddenDirs = [
  path.join(root, "Assets", "models", "bean"),
  path.join(root, "Assets", "source_assets", "ai3d"),
];

for (const dir of forbiddenDirs) {
  if (fs.existsSync(dir)) {
    fail(`Forbidden generated asset directory exists: ${path.relative(root, dir)}`);
  }
}

for (const file of walk(root)) {
  const relative = path.normalize(path.relative(root, file));
  const extension = path.extname(file);

  if (!sourceExtensions.has(extension)) {
    continue;
  }

  const text = fs.readFileSync(file, "utf8");
  for (const { pattern, label } of forbiddenPatterns) {
    if (pattern.test(text)) {
      fail(`${label} in ${relative}`);
    }
  }

  const isAllowedImportedModelPath = allowedImportedModelPaths.some((allowedPath) =>
    relative.startsWith(`${allowedPath}${path.sep}`)
  ) || allowedImportedModelFiles.has(relative);

  if (text.includes(importedMeshNode) && !isAllowedImportedModelPath) {
    fail(`imported ModelDoc mesh node reference outside approved model paths in ${relative}`);
  }

  if (text.includes(renderPrimitiveNode) && !allowedPrimitiveModelFiles.has(relative)) {
    fail(`ModelDoc render primitive node reference outside approved authored files in ${relative}`);
  }
}

if (failed) {
  process.exit(1);
}

console.log("Runtime world asset validation passed.");
