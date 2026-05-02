import fs from "node:fs";
import path from "node:path";

const repoRoot = process.cwd();
const tasksRoot = path.join( repoRoot, "tasks" );
const epicsRoot = path.join( tasksRoot, "epics" );
const itemsRoot = path.join( tasksRoot, "items" );
const validStatuses = new Set( [ "open", "in_progress", "blocked", "done" ] );

function readMarkdownFiles( dir ) {
	if ( !fs.existsSync( dir ) ) return [];
	return fs.readdirSync( dir )
		.filter( file => file.endsWith( ".md" ) )
		.sort()
		.map( file => {
			const fullPath = path.join( dir, file );
			return { file, fullPath, text: fs.readFileSync( fullPath, "utf8" ) };
		} );
}

function parseFrontmatter( text ) {
	if ( !text.startsWith( "---\n" ) ) return {};
	const end = text.indexOf( "\n---", 4 );
	if ( end === -1 ) return {};

	const data = {};
	let currentKey = null;
	for ( const line of text.slice( 4, end ).split( /\r?\n/ ) ) {
		const keyMatch = line.match( /^([A-Za-z0-9_-]+):(?:\s*(.*))?$/ );
		if ( keyMatch ) {
			currentKey = keyMatch[1];
			data[currentKey] = keyMatch[2] === "[]" ? [] : keyMatch[2] ?? "";
			continue;
		}

		const itemMatch = line.match( /^\s*-\s+(.+)$/ );
		if ( itemMatch && currentKey ) {
			if ( !Array.isArray( data[currentKey] ) ) data[currentKey] = [];
			data[currentKey].push( itemMatch[1] );
		}
	}

	return data;
}

const epics = readMarkdownFiles( epicsRoot ).map( entry => ( { ...entry, meta: parseFrontmatter( entry.text ) } ) );
const tasks = readMarkdownFiles( itemsRoot ).map( entry => ( { ...entry, meta: parseFrontmatter( entry.text ) } ) );
const errors = [];
const epicIds = new Set( epics.map( epic => epic.meta.id ) );
const taskIds = new Set();

for ( const epic of epics ) {
	if ( !epic.meta.id ) errors.push( `${epic.file}: missing id` );
	if ( epic.meta.type !== "epic" ) errors.push( `${epic.file}: type must be epic` );
	if ( !validStatuses.has( epic.meta.status ) ) errors.push( `${epic.file}: invalid status ${epic.meta.status}` );
	const children = Array.isArray( epic.meta.children ) ? epic.meta.children : [];
	for ( const child of children ) {
		if ( !tasks.some( task => task.meta.id === child ) ) errors.push( `${epic.file}: missing child task ${child}` );
	}
}

for ( const task of tasks ) {
	if ( !task.meta.id ) errors.push( `${task.file}: missing id` );
	if ( taskIds.has( task.meta.id ) ) errors.push( `${task.file}: duplicate id ${task.meta.id}` );
	taskIds.add( task.meta.id );
	if ( task.meta.type !== "task" ) errors.push( `${task.file}: type must be task` );
	if ( !validStatuses.has( task.meta.status ) ) errors.push( `${task.file}: invalid status ${task.meta.status}` );
	if ( !epicIds.has( task.meta.parent ) ) errors.push( `${task.file}: unknown parent ${task.meta.parent}` );
}

if ( errors.length > 0 ) {
	console.error( errors.join( "\n" ) );
	process.exit( 1 );
}

console.log( `Task inventory valid: ${epics.length} epics, ${tasks.length} tasks.` );
