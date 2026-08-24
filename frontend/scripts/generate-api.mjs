#!/usr/bin/env node
import { execFileSync } from 'node:child_process'
import { mkdirSync, writeFileSync, existsSync } from 'node:fs'
import path from 'node:path'
import { fileURLToPath } from 'node:url'

const frontendRoot = path.resolve(path.dirname(fileURLToPath(import.meta.url)), '..')
const fallbackSpec = path.join(frontendRoot, 'openapi', 'swagger.json')
const outputDir = path.join(frontendRoot, 'app', 'api-client', 'generated')
const bin = path.join(frontendRoot, 'node_modules', '.bin', 'openapi')

const bases = [
  process.env.NUXT_PUBLIC_API_BASE,
  'http://localhost:5170',
  'http://localhost:8080'
].filter(Boolean)

async function resolveInput() {
  for (const base of bases) {
    const url = `${base.replace(/\/$/, '')}/swagger/v1/swagger.json`
    try {
      const response = await fetch(url)
      if (!response.ok)
        continue
      const spec = await response.text()
      mkdirSync(path.dirname(fallbackSpec), { recursive: true })
      writeFileSync(fallbackSpec, spec)
      console.log(`OpenAPI spec pulled from ${url}`)
      return fallbackSpec
    }
    catch {
      // API is optional; fall through to the committed snapshot.
    }
  }

  if (!existsSync(fallbackSpec)) {
    console.error('No OpenAPI spec found. Start the API or add openapi/swagger.json.')
    process.exit(1)
  }

  console.log(`Using committed spec at ${path.relative(frontendRoot, fallbackSpec)}`)
  return fallbackSpec
}

const input = await resolveInput()
mkdirSync(outputDir, { recursive: true })

if (!existsSync(bin)) {
  console.error('openapi-typescript-codegen is not installed. Run npm install.')
  process.exit(1)
}

execFileSync(bin, [
  '--input',
  input,
  '--output',
  outputDir,
  '--client',
  'fetch',
  '--useUnionTypes',
  '--exportCore',
  'true',
  '--exportServices',
  'true',
  '--exportModels',
  'true'
], {
  stdio: 'inherit',
  cwd: frontendRoot
})

console.log(`Generated client → ${path.relative(frontendRoot, outputDir)}`)
