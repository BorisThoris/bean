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
		Results,
		Spectator
	}

	private enum AmbientVenueRole
	{
		Light,
		Crowd,
		Sign,
		Celebration
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
		public GameObject ButtonTop;
		public GameObject ButtonHitbox;
		public GameObject FloorMarker;
		public GameObject ReadyLight;
		public GameObject WinnerGlow;
		public GameObject FocusRing;
		public GameObject RaceTraceFill;
		public GameObject ProgressFill;
		public GameObject HeatFill;
		public GameObject[] Sparks;
		public ModelRenderer ButtonRenderer;
		public ModelRenderer ButtonTopRenderer;
		public ModelRenderer FloorMarkerRenderer;
		public ModelRenderer ReadyLightRenderer;
		public ModelRenderer WinnerGlowRenderer;
		public ModelRenderer FocusRingRenderer;
		public ModelRenderer RaceTraceFillRenderer;
		public ModelRenderer HeatFillRenderer;
		public TextRenderer StationNumberText;
		public TextRenderer NameText;
		public TextRenderer ScoreText;
		public TextRenderer SpeedText;
		public TextRenderer ComboText;
		public TextRenderer RankText;
		public TextRenderer StatusText;
		public Vector3 ButtonBaseScale;
		public Vector3 ButtonTopBasePosition;
		public Vector3 ProgressBaseScale;
		public Vector3 ProgressBasePosition;
		public Vector3 HeatBaseScale;
		public Vector3 HeatBasePosition;
		public float ButtonPunch;
		public float FinishFlash;
	}

	private sealed class AmbientVenueObject
	{
		public GameObject GameObject;
		public ModelRenderer Renderer;
		public Vector3 BasePosition;
		public Vector3 BaseScale;
		public Color BaseColor;
		public AmbientVenueRole Role;
		public float Phase;
	}
}
