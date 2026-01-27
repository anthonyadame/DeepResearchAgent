#!/usr/bin/env node

const { execSync } = require('child_process');
const fs = require('fs');
const path = require('path');

function isReadme(file) {
  const base = path.basename(file).toLowerCase();
  return (
    base === 'readme.md' ||
    base === 'license' ||
    base === 'license.md' ||
    base === 'contributing.md' ||
    base === 'security.md' ||
    base === 'code_of_conduct.md'
  );
}

function main() {
  // Find all markdown files tracked by git
  const stdout = execSync("git ls-files \"**/*.md\"", { encoding: 'utf8' });
  const files = stdout.split('\n').filter(Boolean);

  const violations = [];
  let hasLegacyBuildDoc = false;

  for (const file of files) {
    // Skip internal docs folders
    if (file.startsWith('BuildDocs/')) continue;
    if (file.startsWith('BuildDoc/')) { hasLegacyBuildDoc = true; continue; }

    // Skip .github docs and templates
    if (file.startsWith('.github/')) continue;

    // Allow consumer-facing READMEs in any project/package root
    const base = path.basename(file).toLowerCase();
    if (isReadme(file)) continue;

    // Temporarily allow UI development guide until migrated
    if (file === 'DeepResearchAgent.UI/DEVELOPMENT.md') continue;

    // Any other .md should live under BuildDocs
    violations.push(file);
  }

  if (hasLegacyBuildDoc) {
    console.log('Note: Legacy BuildDoc/ detected. Consider migrating to BuildDocs/.');
  }

  if (violations.length) {
    console.error('Documentation placement violations found. Move these files to BuildDocs/ or rename to README.md if consumer-facing:');
    for (const v of violations) console.error(' - ' + v);
    process.exit(1);
  }

  console.log('Docs guardrail passed: internal docs are under BuildDocs/ (or legacy BuildDoc/).');
}

main();
