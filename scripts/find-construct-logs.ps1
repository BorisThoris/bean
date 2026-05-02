$ErrorActionPreference = "SilentlyContinue"

$candidateRoots = @(
	(Join-Path $env:LOCALAPPDATA "sbox"),
	(Join-Path $env:LOCALAPPDATA "Sandbox"),
	(Join-Path $env:LOCALAPPDATA "Facepunch"),
	(Join-Path $env:APPDATA "sbox"),
	(Join-Path $env:APPDATA "Sandbox"),
	"E:\SteamLibrary\steamapps\common\sbox\logs",
	"E:\SteamLibrary\steamapps\common\sbox\.logs"
)

$roots = $candidateRoots | Where-Object { $_ -and (Test-Path $_) }

$cutoff = (Get-Date).AddHours( -8 )
$files = foreach ( $root in $roots ) {
	Get-ChildItem -Path $root -Recurse -Depth 4 -File -ErrorAction SilentlyContinue |
		Where-Object {
			$_.LastWriteTime -ge $cutoff
				-and ($_.Extension -in ".log", ".txt")
				-and ($_.Length -lt 20MB)
		}
}

$matches = $files |
	Sort-Object LastWriteTime -Descending |
	Select-Object -First 200 |
	ForEach-Object {
		Select-String -Path $_.FullName -Pattern "\[TapperConstruct\]" -SimpleMatch -ErrorAction SilentlyContinue
	}

if ( -not $matches ) {
	Write-Host "No [TapperConstruct] lines found in recent likely log files. Check the s&box editor console after launching the scene."
	exit 1
}

$matches | ForEach-Object {
	"{0}:{1}: {2}" -f $_.Path, $_.LineNumber, $_.Line
}
