using Sandbox;
using System.Collections.Generic;

public sealed partial class PhysicalFastestTapperGame
{
	private enum RoundState
	{
		WaitingForPlayers,
		Countdown,
		Playing,
		Results,
		Intermission
	}

	private enum CameraMode
	{
		Bean,
		Station,
		Spectator
	}

	private enum TapperEventPhase
	{
		Warmup,
		ReadyCheck,
		Countdown,
		Live,
		PhotoFinish,
		Podium,
		NextModePreview
	}

	private enum StationCallout
	{
		None,
		Focus,
		Chain,
		Overheat,
		PhotoFinish,
		Winner,
		Spectating
	}

	public enum TapperGameMode
	{
		Classic10,
		Sprint5,
		Endurance30,
		Combo
	}

	private readonly struct ModeSettings
	{
		public readonly string Label;
		public readonly float Duration;
		public readonly float HeatGain;
		public readonly float HeatDecay;
		public readonly bool ComboScoring;

		public ModeSettings( string label, float duration, float heatGain, float heatDecay, bool comboScoring )
		{
			Label = label;
			Duration = duration;
			HeatGain = heatGain;
			HeatDecay = heatDecay;
			ComboScoring = comboScoring;
		}
	}

	private sealed class PlayerScore
	{
		public Connection Connection;
		public string ConnectionKey;
		public string Name = "PLAYER";
		public int StationIndex = -1;
		public GameObject Bean;
		public TapperPlayerBean BeanController;
		public TextRenderer BeanNameText;
		public int Score;
		public int BestScore;
		public int SessionWins;
		public int SessionTotalTaps;
		public int ConsecutiveWins;
		public int TournamentPoints;
		public int LastRoundPlacement;
		public int LastRoundPoints;
		public int FocusHits;
		public int LastRoundFocusHits;
		public int LastRoundScore;
		public int LastRoundMaxCombo;
		public float LastRoundPeakSpeed;
		public float RaceTrace;
		public bool FocusWindowActive;
		public readonly Dictionary<TapperGameMode, int> BestScoreByMode = new();
		public int Combo;
		public int MaxCombo;
		public float LastTapTime;
		public float LastSpeed;
		public float PeakSpeed;
		public float BestPeakSpeed;
		public float Heat;
		public float ComboPulse;
		public float LastSoundTime;
		public bool Ready;
		public bool Spectating;
		public string LastInteractionMessage = "";
		public float LastInteractionMessageTime;
	}

	private sealed class TapperStation
	{
		public int Index;
		public Vector3 Origin;
		public GameObject Root;
		public GameObject Button;
		public GameObject ButtonHitbox;
		public GameObject[] ClaimFrame;
		public GameObject ProgressFill;
		public GameObject HeatFill;
		public ModelRenderer ButtonRenderer;
		public ModelRenderer[] ClaimFrameRenderers;
		public ModelRenderer HeatFillRenderer;
		public Vector3 ButtonBaseScale;
		public Vector3[] ClaimFrameBaseScales;
		public Vector3 ProgressBaseScale;
		public Vector3 ProgressBasePosition;
		public Vector3 HeatBaseScale;
		public Vector3 HeatBasePosition;
		public float BarModelHalfExtentX;
		public float ButtonPunch;
		public float FinishFlash;
	}

	private sealed class ArenaWallFallbackText
	{
		public TextRenderer Title;
		public TextRenderer Debug;
		public TextRenderer Headline;
		public TextRenderer Mode;
		public TextRenderer Leaderboard;
		public TextRenderer Stations;
	}

}

public readonly struct TapperWallStationDisplay
{
	public readonly string Station;
	public readonly string Name;
	public readonly string SteamId;
	public readonly string Status;
	public readonly string Meta;
	public readonly string CssClass;
	public readonly int Score;
	public readonly float Speed;
	public readonly float Progress;
	public readonly float Heat;

	public TapperWallStationDisplay( string station, string name, string steamId, string status, string meta, string cssClass, int score = 0, float speed = 0f, float progress = 0f, float heat = 0f )
	{
		Station = station;
		Name = name;
		SteamId = steamId;
		Status = status;
		Meta = meta;
		CssClass = cssClass;
		Score = score;
		Speed = speed;
		Progress = progress;
		Heat = heat;
	}
}

public readonly struct TapperWallLeaderboardDisplay
{
	public readonly int Rank;
	public readonly string Name;
	public readonly string SteamId;
	public readonly int Score;
	public readonly float PeakSpeed;
	public readonly int Points;
	public readonly bool Winner;

	public TapperWallLeaderboardDisplay( int rank, string name, string steamId, int score, float peakSpeed, int points, bool winner )
	{
		Rank = rank;
		Name = name;
		SteamId = steamId;
		Score = score;
		PeakSpeed = peakSpeed;
		Points = points;
		Winner = winner;
	}
}

public readonly struct TapperWallScreenState
{
	public readonly string Title;
	public readonly string Headline;
	public readonly string Mode;
	public readonly string Debug;
	public readonly string RoundState;
	public readonly string EventPhase;
	public readonly string ModeName;
	public readonly int TournamentRound;
	public readonly int TournamentRounds;
	public readonly int ClaimedStations;
	public readonly float TimeRemaining;
	public readonly float RoundProgress;
	public readonly TapperWallLeaderboardDisplay[] Leaders;
	public readonly TapperWallStationDisplay[] Stations;

	public TapperWallScreenState( string title, string headline, string mode, string debug, string roundState, string eventPhase, string modeName, int tournamentRound, int tournamentRounds, int claimedStations, float timeRemaining, float roundProgress, TapperWallLeaderboardDisplay[] leaders, TapperWallStationDisplay[] stations )
	{
		Title = title;
		Headline = headline;
		Mode = mode;
		Debug = debug;
		RoundState = roundState;
		EventPhase = eventPhase;
		ModeName = modeName;
		TournamentRound = tournamentRound;
		TournamentRounds = tournamentRounds;
		ClaimedStations = claimedStations;
		TimeRemaining = timeRemaining;
		RoundProgress = roundProgress;
		Leaders = leaders;
		Stations = stations;
	}
}
