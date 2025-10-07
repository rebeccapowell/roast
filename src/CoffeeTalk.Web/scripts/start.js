#!/usr/bin/env node

const { existsSync } = require('node:fs');
const { join } = require('node:path');
const { spawnSync } = require('node:child_process');

const projectRoot = join(__dirname, '..');
const npmExecutable = process.platform === 'win32' ? 'npm.cmd' : 'npm';
const nextExecutable = join(
  projectRoot,
  'node_modules',
  '.bin',
  process.platform === 'win32' ? 'next.cmd' : 'next'
);
const nextPackageJson = join(projectRoot, 'node_modules', 'next', 'package.json');

if (!existsSync(nextPackageJson)) {
  console.log('Node modules missing for CoffeeTalk.Web. Installing dependencies...');
  const installResult = spawnSync(npmExecutable, ['install'], {
    cwd: projectRoot,
    stdio: 'inherit'
  });

  if (installResult.status !== 0) {
    process.exit(installResult.status ?? 1);
  }
} else {
  console.log('CoffeeTalk.Web dependencies already installed. Skipping npm install.');
}

if (!existsSync(nextExecutable)) {
  console.error('Unable to locate the Next.js executable after dependency installation.');
  process.exit(1);
}

const startResult = spawnSync(nextExecutable, ['start'], {
  cwd: projectRoot,
  stdio: 'inherit'
});

process.exit(startResult.status ?? 1);
