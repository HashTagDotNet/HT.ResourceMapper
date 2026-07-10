// Playwright global teardown: wipes every E2E-prefixed row this suite could have touched.
// Never touches the shared domain TagDefinition or any other pre-existing app data (RD15).
const { execFileSync } = require('child_process');
const path = require('path');

const SQLCMD = 'C:\\Program Files\\Microsoft SQL Server\\Client SDK\\ODBC\\170\\Tools\\Binn\\SQLCMD.EXE';
const SERVER = '(localdb)\\MSSQLLocalDB';
const DATABASE = 'ResourceMapper';

module.exports = async () => {
  execFileSync(SQLCMD, ['-S', SERVER, '-d', DATABASE, '-i', path.join(__dirname, 'sql', 'cleanup.sql')], { stdio: 'inherit' });
};
