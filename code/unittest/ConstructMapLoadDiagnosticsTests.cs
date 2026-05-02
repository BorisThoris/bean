using Microsoft.VisualStudio.TestTools.UnitTesting;

[TestClass]
public sealed class ConstructMapLoadDiagnosticsTests
{
	[TestMethod]
	public void DisabledConstructUsesGeneratedVenueStatus()
	{
		var status = ConstructMapLoadDiagnostics.FormatWorldStatus( false, "facepunch.construct", false, false, "", "" );

		Assert.AreEqual( "GENERATED VENUE", status );
	}

	[TestMethod]
	public void EmptyConstructMapNameIsReportedAsSkipped()
	{
		var status = ConstructMapLoadDiagnostics.FormatWorldStatus( true, "", false, false, "", "" );

		Assert.AreEqual( "CONSTRUCT SKIPPED: EMPTY MAP", status );
	}

	[TestMethod]
	public void ValidLoadedMapReportsConstructMapName()
	{
		var status = ConstructMapLoadDiagnostics.FormatWorldStatus( true, "facepunch.construct", true, true, "construct", "" );

		Assert.AreEqual( "CONSTRUCT MAP: construct", status );
	}

	[TestMethod]
	public void FailedMapLoadReportsExceptionType()
	{
		var status = ConstructMapLoadDiagnostics.FormatWorldStatus( true, "facepunch.construct", false, false, "", "InvalidOperationException" );

		Assert.AreEqual( "CONSTRUCT FAILED: InvalidOperationException", status );
	}

	[TestMethod]
	public void LogLineContainsStableTapperConstructPrefixAndFields()
	{
		var diagnostics = new ConstructMapLoadDiagnostics(
			true,
			"facepunch.construct",
			"CreateAsync.Completed",
			true,
			true,
			"construct",
			"maps/construct",
			"bounds" );

		var line = diagnostics.ToLogLine();

		StringAssert.StartsWith( line, "[TapperConstruct]" );
		StringAssert.Contains( line, "phase=CreateAsync.Completed" );
		StringAssert.Contains( line, "requested='facepunch.construct'" );
		StringAssert.Contains( line, "status='CONSTRUCT MAP: construct'" );
	}

	[TestMethod]
	public void LogLineWithNullFieldsStillFormats()
	{
		var diagnostics = new ConstructMapLoadDiagnostics(
			true,
			null,
			null,
			false,
			false,
			null,
			null,
			null,
			null,
			null );

		var line = diagnostics.ToLogLine();

		StringAssert.StartsWith( line, "[TapperConstruct]" );
		StringAssert.Contains( line, "requested=''" );
		StringAssert.Contains( line, "status='CONSTRUCT SKIPPED: EMPTY MAP'" );
	}

	[TestMethod]
	public void FailureStatusDoesNotRequireMapMetadata()
	{
		var diagnostics = new ConstructMapLoadDiagnostics(
			true,
			"facepunch.construct",
			"CreateAsync.Failed",
			false,
			false,
			"",
			"",
			"",
			"NullReferenceException",
			"Object reference not set to an instance of an object." );

		Assert.AreEqual( "CONSTRUCT FAILED: NullReferenceException", diagnostics.WorldStatus );
		StringAssert.Contains( diagnostics.ToLogLine(), "message='Object reference not set to an instance of an object.'" );
	}
}
