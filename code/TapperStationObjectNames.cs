public static class TapperStationObjectNames
{
	public static readonly string[] ClaimFrameSuffixes =
	[
		" Claim Frame Front",
		" Claim Frame Back",
		" Claim Frame Left",
		" Claim Frame Right"
	];

	private static readonly string[] StationButtonSuffixes =
	[
		" Physical Tap Button",
		" Button Hitbox",
		..ClaimFrameSuffixes
	];

	public static bool TryParseStationIndex( string objectName, out int stationIndex )
	{
		stationIndex = -1;
		if ( string.IsNullOrWhiteSpace( objectName ) )
			return false;

		foreach ( var suffix in StationButtonSuffixes )
		{
			var suffixIndex = objectName.IndexOf( suffix );
			if ( suffixIndex <= "Station ".Length )
				continue;

			var prefix = objectName[..suffixIndex];
			if ( !prefix.StartsWith( "Station " ) )
				continue;

			if ( int.TryParse( prefix["Station ".Length..], out stationIndex ) )
				return true;

			stationIndex = -1;
		}

		return false;
	}
}
