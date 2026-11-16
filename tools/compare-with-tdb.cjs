// Porownuje SQL ze sniffu z zywą bazą MySQL (wpp_tdb_base) i wypisuje rozjazdy.
// Uruchomienie: "$ABACUSAI_BOT_NODE" tools/compare-with-tdb.cjs <plik.sql> <tabela> <klucz...>
// Przyklad: ... out/forever_70938_new.sql creature_loot_template Entry,Item
const { execFileSync } = require('child_process');

const MYSQL = 'C:/Program Files/MySQL/MySQL Server 8.4/bin/mysql.exe';
const DSN = ['-h', '127.0.0.1', '-u', 'root', '-p!Taurus2277', '-N'];
const TDB = 'wpp_tdb_base';

function q(sql) {
    return execFileSync(MYSQL, [...DSN, '-e', sql], { encoding: 'utf8', maxBuffer: 256 * 1024 * 1024 })
        .split(/\r?\n/).filter(Boolean);
}

// INSERT IGNORE INTO `t` (`a`, `b`, `c`) VALUES ... ;
function readInserts(sqlFile, table) {
    const text = require('fs').readFileSync(sqlFile, 'utf8');
    const re = new RegExp('INSERT (?:IGNORE )?INTO `' + table + '` \\(([^)]*)\\) VALUES', 'g');
    const rows = [];
    let m;
    while ((m = re.exec(text)) !== null) {
        const cols = m[1].split(',').map((c) => c.trim().replace(/`/g, ''));
        let i = m.index + m[0].length;
        let depth = 0, quote = false;
        for (; i < text.length; i += 1) {
            const ch = text[i];
            if (ch === "'") quote = !quote;
            if (quote) continue;
            if (ch === '(') { if (depth === 0) rows.push({ cols, start: text.indexOf(')', i) }); depth += 1; }
            if (ch === ')') { depth -= 1; if (depth === 0) rows[rows.length - 1].end = i; }
            if (ch === ';' && depth === 0) break;
        }
        re.lastIndex = i;
    }
    return rows;
}

const [sqlFile, table, keySpec] = process.argv.slice(2);
const keys = keySpec.split(',').map((s) => s.trim());

const rows = readInserts(sqlFile, table);
const out = [`${table}: ${rows.length} INSERT-ow w ${sqlFile}`];

const tdbCols = q(`SHOW COLUMNS FROM ${TDB}.${table};`).map((l) => l.split('\t')[0]);
out.push(`kolumny TDB (${tdbCols.length}): ${tdbCols.join(', ')}`);
if (rows.length) out.push(`kolumny SQL: ${rows[0].cols.join(', ')}`);
out.push('');

const missingCols = rows.length ? tdbCols.filter((c) => !rows[0].cols.includes(c)) : [];
if (missingCols.length) out.push(`UWAGA: SQL nie wypelnia kolumn TDB: ${missingCols.join(', ')}\n`);

// ile wierszy TDB ma dla tych samych kluczy
const keyIdx = keys.map((k) => rows[0].cols.indexOf(k));
if (keyIdx.some((i) => i < 0)) {
    out.push(`Klucze ${keys.join(',')} nie wystepuja w INSERT (sa: ${rows[0].cols.join(',')})`);
} else {
    const sample = q(`SELECT ${keys.join(',')} FROM ${TDB}.${table};`);
    out.push(`TDB ma ${sample.length} wierszy w tej tabeli (klucz: ${keys.join(', ')})`);
}
require('fs').writeFileSync('out/tmp/compare-' + table + '.txt', out.join('\n') + '\n');
