// Playwright global setup: cleanup-then-seed (RD16) so a crashed prior run's residue can't
// contaminate this run. Shells out to sqlcmd (RD18) — the mssql npm client's localdb named-pipe
// handling is unreliable; sqlcmd against (localdb)\MSSQLLocalDB is the proven path in this repo.
const { execFileSync } = require('child_process');
const path = require('path');

const SQLCMD = 'C:\\Program Files\\Microsoft SQL Server\\Client SDK\\ODBC\\170\\Tools\\Binn\\SQLCMD.EXE';
const SERVER = '(localdb)\\MSSQLLocalDB';
const DATABASE = 'ResourceMapper';

function runSql(file) {
  execFileSync(SQLCMD, ['-S', SERVER, '-d', DATABASE, '-i', file], { stdio: 'inherit' });
}

module.exports = async () => {
  const sqlDir = path.join(__dirname, 'sql');
  runSql(path.join(sqlDir, 'cleanup.sql'));
  runSql(path.join(sqlDir, 'seed.sql'));
};
