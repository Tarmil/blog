import { readdirSync } from 'fs'
import { build } from 'esbuild'

const file = 'all.js';

console.log("Bundling:", file);
build({
  entryPoints: ['./bin/html/Scripts/WebSharper/blog/' + file],
  bundle: true,
  minify: true,
  format: 'iife',
  outfile: 'bin/html/Scripts/WebSharper/' + file,
  loader: {
    '.jpg': 'file',
    '.png': 'file'
  },
  globalName: 'wsbundle'
});
