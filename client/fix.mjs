import { promises as fs } from 'fs';
import * as path from 'path';
import * as url from 'url';

const __dirname = url.fileURLToPath(new URL('.', import.meta.url));
const file = 'node_modules/@editorjs/editorjs/types/configs/index.d.ts';
const target = path.join(__dirname, file);
const data = await fs.readFile(target, { encoding: 'utf8' });


const remove = `import { fromCallback } from 'cypress/types/bluebird'\n\n`;
const result = data.replace(remove, '');

await fs.writeFile(target, result);
