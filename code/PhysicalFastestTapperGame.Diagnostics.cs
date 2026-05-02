using System;

public readonly struct ConstructMapLoadDiagnostics
{
	public readonly bool Enabled;
	public readonly string RequestedMap;
	public readonly string Phase;
	public readonly bool Loaded;
	public readonly bool IsValid;
	public readonly string MapName;
	public readonly string MapFolder;
	public readonly string Bounds;
	public readonly string ExceptionType;
	public readonly string ExceptionMessage;

	public ConstructMapLoadDiagnostics(
		bool enabled,
		string requestedMap,
		string phase,
		bool loaded = false,
		bool isValid = false,
		string mapName = "",
		string mapFolder = "",
		string bounds = "",
		string exceptionType = "",
		string exceptionMessage = "" )
	{
		Enabled = enabled;
		RequestedMap = requestedMap ?? "";
		Phase = phase ?? "";
		Loaded = loaded;
		IsValid = isValid;
		MapName = mapName ?? "";
		MapFolder = mapFolder ?? "";
		Bounds = bounds ?? "";
		ExceptionType = exceptionType ?? "";
		ExceptionMessage = exceptionMessage ?? "";
	}

	public string WorldStatus => FormatWorldStatus( Enabled, RequestedMap, Loaded, IsValid, MapName, ExceptionType );

	public string ToLogLine()
	{
		return "[TapperConstruct] "
			+ $"phase={Phase} enabled={Enabled} requested='{RequestedMap}' loaded={Loaded} valid={IsValid} "
			+ $"map='{MapName}' folder='{MapFolder}' bounds='{Bounds}' status='{WorldStatus}' "
			+ $"exception='{ExceptionType}' message='{ExceptionMessage}'";
	}

	public static string FormatWorldStatus( bool enabled, string requestedMap, bool loaded, bool isValid, string mapName, string exceptionType )
	{
		if ( !enabled )
			return "GENERATED VENUE";

		if ( string.IsNullOrWhiteSpace( requestedMap ) )
			return "CONSTRUCT SKIPPED: EMPTY MAP";

		if ( !string.IsNullOrWhiteSpace( exceptionType ) )
			return $"CONSTRUCT FAILED: {exceptionType}";

		if ( loaded && isValid )
			return string.IsNullOrWhiteSpace( mapName ) ? "CONSTRUCT MAP" : $"CONSTRUCT MAP: {mapName}";

		return "CONSTRUCT UNAVAILABLE - GENERATED FALLBACK";
	}
}
