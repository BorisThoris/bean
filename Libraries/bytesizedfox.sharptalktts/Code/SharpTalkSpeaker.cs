using System;
using System.Threading.Tasks;
using Sandbox;
using SharpTalk;

namespace SharpTalk;

public enum VoicePreset { Baseline, Whisper, Custom }

public sealed class SharpTalkSpeaker : Component
{
	[Property] public string SpeakOnStart { get; set; } = "";

	[Property, Range( 40, 600 )] public int Rate    { get; set; } = 200;
	[Property, Range( 40, 500 )] public int PitchHz { get; set; } = 122;
	[Property, Range( 0f, 2f  )] public float AudioVolume { get; set; } = 1f;

	// ── Voice Definition ─────────────────────────────────────────────────────

	bool _applyingPreset;

	VoicePreset _preset = VoicePreset.Baseline;
	[Property, Group( "1. Voice Definition" )]
	public VoicePreset Preset
	{
		get => _preset;
		set
		{
			_preset = value;
			if ( value == VoicePreset.Custom ) return;

			_applyingPreset = true;
			var v = value == VoicePreset.Whisper ? VoiceData.WhisperVoice : VoiceData.BaselineVoice;
			Female          = v.VoiceType == 1;
			VoicingGain     = v.VGain;
			AspirationGain  = v.AGain;
			AspirationCycle = v.ACycle;
			F4Freq          = v.F4Freq;
			F4BW            = v.F4BW;
			F4pFreq         = v.F4pFreq;
			F4pBW           = v.F4pBW;
			F5pFreq         = v.F5pFreq;
			F5pBW           = v.F5pBW;
			F6pFreq         = v.F6pFreq;
			F6pBW           = v.F6pBW;
			BwGain1         = v.BwGain1;
			BwGain2         = v.BwGain2;
			BwGain3         = v.BwGain3;
			NasalBase       = v.NasalBase;
			NasalTarg       = v.NasalTarg;
			NasalBW         = v.NasalBW;
			NGain           = v.NGain;
			PitchRange      = v.PitchRange;
			StressGain      = v.StressGain;
			Intonation      = v.Intonation;
			RiseAmt         = v.RiseAmt;
			FallAmt         = v.FallAmt;
			BaselineFall    = v.BaselineFall;
			_applyingPreset = false;
		}
	}

	bool _female = false;
	[Property, Group( "1. Voice Definition" ), Description( "Shifts the vocal tract character towards a female voice." )]
	public bool Female { get => _female; set { _female = value; if ( !_applyingPreset ) _preset = VoicePreset.Custom; } }

	int _voicingGain = 100;
	[Property, Group( "1. Voice Definition" ), Range( 0, 100 ), Description( "Strength of vocal cord vibration. Lower values make the voice sound weaker or breathier." )]
	public int VoicingGain     { get => _voicingGain;     set { _voicingGain     = value; if ( !_applyingPreset ) _preset = VoicePreset.Custom; } }

	int _aspirationGain = 0;
	[Property, Group( "1. Voice Definition" ), Range( 0, 500 ), Description( "Amount of breathy air noise in the voice. High values produce a whisper-like effect." )]
	public int AspirationGain  { get => _aspirationGain;  set { _aspirationGain  = value; if ( !_applyingPreset ) _preset = VoicePreset.Custom; } }

	int _aspirationCycle = 192;
	[Property, Group( "1. Voice Definition" ), Range( 0, 255 ), Description( "Rhythm of the breathiness. Lower values make the breath noise more continuous and even." )]
	public int AspirationCycle { get => _aspirationCycle; set { _aspirationCycle = value; if ( !_applyingPreset ) _preset = VoicePreset.Custom; } }

	// ── Formants ─────────────────────────────────────────────────────────────

	int _f4Freq = 3000;
	[Property, Group( "2. Formants" ), Range( 1000, 6000 ), Description( "Brightness or 'ring' of the voice. Higher values sound sharper and more present." )]
	public int F4Freq  { get => _f4Freq;  set { _f4Freq  = value; if ( !_applyingPreset ) _preset = VoicePreset.Custom; } }

	int _f4BW = 200;
	[Property, Group( "2. Formants" ), Range( 10, 1000 ), Description( "Focus of the brightness peak. Lower values sound more resonant; higher values are softer and more diffuse." )]
	public int F4BW    { get => _f4BW;    set { _f4BW    = value; if ( !_applyingPreset ) _preset = VoicePreset.Custom; } }

	int _f4pFreq = 3600;
	[Property, Group( "2. Formants" ), Range( 1000, 6000 ), Description( "High-frequency 'sheen' of the voice. Shapes the upper brightness character." )]
	public int F4pFreq { get => _f4pFreq; set { _f4pFreq = value; if ( !_applyingPreset ) _preset = VoicePreset.Custom; } }

	int _f4pBW = 150;
	[Property, Group( "2. Formants" ), Range( 10, 500 ), Description( "Focus of the upper brightness sheen." )]
	public int F4pBW   { get => _f4pBW;   set { _f4pBW   = value; if ( !_applyingPreset ) _preset = VoicePreset.Custom; } }

	int _f5pFreq = 3750;
	[Property, Group( "2. Formants" ), Range( 1000, 6000 ), Description( "Airy high-frequency resonance. Contributes to openness and air in the upper range." )]
	public int F5pFreq { get => _f5pFreq; set { _f5pFreq = value; if ( !_applyingPreset ) _preset = VoicePreset.Custom; } }

	int _f5pBW = 100;
	[Property, Group( "2. Formants" ), Range( 10, 500 ), Description( "Focus of the airy high-frequency resonance." )]
	public int F5pBW   { get => _f5pBW;   set { _f5pBW   = value; if ( !_applyingPreset ) _preset = VoicePreset.Custom; } }

	int _f6pFreq = 4500;
	[Property, Group( "2. Formants" ), Range( 1000, 8000 ), Description( "Very high resonance that adds subtle air and presence at the top of the spectrum." )]
	public int F6pFreq { get => _f6pFreq; set { _f6pFreq = value; if ( !_applyingPreset ) _preset = VoicePreset.Custom; } }

	int _f6pBW = 150;
	[Property, Group( "2. Formants" ), Range( 10, 500 ), Description( "Focus of the very high resonance." )]
	public int F6pBW   { get => _f6pBW;   set { _f6pBW   = value; if ( !_applyingPreset ) _preset = VoicePreset.Custom; } }

	int _bwGain1 = 150;
	[Property, Group( "2. Formants" ), Range( 0, 300 ), Description( "Resonance damping in the low range. Affects how open and warm low vowels sound." )]
	public int BwGain1 { get => _bwGain1; set { _bwGain1 = value; if ( !_applyingPreset ) _preset = VoicePreset.Custom; } }

	int _bwGain2 = 100;
	[Property, Group( "2. Formants" ), Range( 0, 300 ), Description( "Resonance damping in the mid range. Affects clarity of mid vowels." )]
	public int BwGain2 { get => _bwGain2; set { _bwGain2 = value; if ( !_applyingPreset ) _preset = VoicePreset.Custom; } }

	int _bwGain3 = 100;
	[Property, Group( "2. Formants" ), Range( 0, 300 ), Description( "Resonance damping in the upper-mid range." )]
	public int BwGain3 { get => _bwGain3; set { _bwGain3 = value; if ( !_applyingPreset ) _preset = VoicePreset.Custom; } }

	// ── Nasal ─────────────────────────────────────────────────────────────────

	int _nasalBase = 330;
	[Property, Group( "3. Nasal" ), Range( 100, 600 ), Description( "Starting character of nasal sounds (m, n, ng). Shapes how nasals begin." )]
	public int NasalBase { get => _nasalBase; set { _nasalBase = value; if ( !_applyingPreset ) _preset = VoicePreset.Custom; } }

	int _nasalTarg = 400;
	[Property, Group( "3. Nasal" ), Range( 100, 600 ), Description( "Target character of nasal sounds. Shapes how fully-developed nasals feel." )]
	public int NasalTarg { get => _nasalTarg; set { _nasalTarg = value; if ( !_applyingPreset ) _preset = VoicePreset.Custom; } }

	int _nasalBW = 60;
	[Property, Group( "3. Nasal" ), Range( 10, 200 ), Description( "How sharp or diffuse the nasal resonance sounds." )]
	public int NasalBW   { get => _nasalBW;   set { _nasalBW   = value; if ( !_applyingPreset ) _preset = VoicePreset.Custom; } }

	int _nGain = 100;
	[Property, Group( "3. Nasal" ), Range( 0, 500 ), Description( "Overall nasal character of the voice. Higher values make the voice sound more nasal." )]
	public int NGain     { get => _nGain;     set { _nGain     = value; if ( !_applyingPreset ) _preset = VoicePreset.Custom; } }

	// ── Intonation ───────────────────────────────────────────────────────────

	int _pitchRange = 100;
	[Property, Group( "4. Intonation" ), Range( 0, 200 ), Description( "How much the pitch varies while speaking. 0 = monotone; higher = more expressive and dynamic." )]
	public int PitchRange   { get => _pitchRange;   set { _pitchRange   = value; if ( !_applyingPreset ) _preset = VoicePreset.Custom; } }

	int _stressGain = 60;
	[Property, Group( "4. Intonation" ), Range( 0, 100 ), Description( "How strongly stressed syllables stand out from unstressed ones." )]
	public int StressGain   { get => _stressGain;   set { _stressGain   = value; if ( !_applyingPreset ) _preset = VoicePreset.Custom; } }

	int _intonation = 100;
	[Property, Group( "4. Intonation" ), Range( 0, 200 ), Description( "Overall strength of sentence-level pitch patterns." )]
	public int Intonation   { get => _intonation;   set { _intonation   = value; if ( !_applyingPreset ) _preset = VoicePreset.Custom; } }

	int _riseAmt = 29;
	[Property, Group( "4. Intonation" ), Range( -100, 100 ), Description( "How much pitch rises at the start of a stressed syllable." )]
	public int RiseAmt      { get => _riseAmt;      set { _riseAmt      = value; if ( !_applyingPreset ) _preset = VoicePreset.Custom; } }

	int _fallAmt = -29;
	[Property, Group( "4. Intonation" ), Range( -100, 0 ), Description( "How much pitch drops at the end of a stressed syllable." )]
	public int FallAmt      { get => _fallAmt;      set { _fallAmt      = value; if ( !_applyingPreset ) _preset = VoicePreset.Custom; } }

	int _baselineFall = 51;
	[Property, Group( "4. Intonation" ), Range( 0, 100 ), Description( "How much the overall pitch drifts downward towards the end of a sentence." )]
	public int BaselineFall { get => _baselineFall; set { _baselineFall = value; if ( !_applyingPreset ) _preset = VoicePreset.Custom; } }

	// ── Events ────────────────────────────────────────────────────────────────

	public event Action<PhonemeEvent> OnPhoneme;

	// ── Internals ─────────────────────────────────────────────────────────────

	TtsEngine _engine;
	SoundHandle _handle;
	bool _speaking;
	PhonemeEvent[] _phonemeEvents = Array.Empty<PhonemeEvent>();
	int _nextPhonemeIndex;
	float _speakStartTime = -1f;

	public bool IsSpeaking => _speaking;

	protected override void OnStart()
	{
		try { InitEngine(); }
		catch ( Exception e ) { Log.Error( $"SharpTalkSpeaker: OnStart threw — {e}" ); }
	}

	void InitEngine()
	{
		var dict    = LibraryData.EnglishLex;
		var symbols = LibraryData.Symbols;

		_engine = new TtsEngine( BuildVoice(), dict, symbols );
		Log.Info( $"SharpTalkSpeaker: engine initialized (dict={dict.Length}b, symbols={symbols.Length}b)" );

		if ( !string.IsNullOrWhiteSpace( SpeakOnStart ) )
			_ = Speak( SpeakOnStart );
	}

	VoiceData BuildVoice()
	{
		VoiceData v = _preset switch
		{
			VoicePreset.Whisper => VoiceData.WhisperVoice,
			VoicePreset.Custom  => new VoiceData
			{
				VGain        = (short)VoicingGain,
				AGain        = (short)AspirationGain,
				ACycle       = (short)AspirationCycle,
				F4Freq       = (short)F4Freq,
				F4BW         = (short)F4BW,
				F4pFreq      = (short)F4pFreq,
				F4pBW        = (short)F4pBW,
				F5pFreq      = (short)F5pFreq,
				F5pBW        = (short)F5pBW,
				F6pFreq      = (short)F6pFreq,
				F6pBW        = (short)F6pBW,
				BwGain1      = (short)BwGain1,
				BwGain2      = (short)BwGain2,
				BwGain3      = (short)BwGain3,
				NasalBase    = (short)NasalBase,
				NasalTarg    = (short)NasalTarg,
				NasalBW      = (short)NasalBW,
				NGain        = (short)NGain,
				PitchRange   = (short)PitchRange,
				StressGain   = (short)StressGain,
				Intonation   = (short)Intonation,
				RiseAmt      = (short)RiseAmt,
				FallAmt      = (short)FallAmt,
				BaselineFall = (short)BaselineFall,
			},
			_ => VoiceData.BaselineVoice,
		};
		v.Rate      = (short)Rate;
		v.PitchHz   = (short)PitchHz;
		v.VoiceType = (short)( Female ? 1 : 0 );
		return v;
	}

	public async Task Speak( string text )
	{
		if ( _engine is null ) { Log.Error( "SharpTalkSpeaker: Speak() called but engine is null — was OnStart run?" ); return; }

		Stop();
		_speaking = true;

		short[] samples;
		PhonemeEvent[] events;
		try
		{
			(samples, events) = await GameTask.RunInThreadAsync( () => _engine.SpeakWithEvents( text ) );
		}
		catch ( Exception e )
		{
			Log.Error( $"SharpTalkSpeaker: synthesis threw — {e}" );
			_speaking = false;
			return;
		}
		_phonemeEvents = events;
		_nextPhonemeIndex = 0;
		_speakStartTime = -1f;

		using var stream = new SoundStream( TtsEngine.SampleRate, 1 );
		_handle = stream.Play( AudioVolume, 1f );
		_handle.SetParent( GameObject );
		_handle.FollowParent = true;
		_handle.Update();
		_speakStartTime = Time.Now;

		int offset = 0;
		while ( offset < samples.Length )
		{
			int space = stream.MaxWriteSampleCount - stream.QueuedSampleCount;
			if ( space <= 0 ) { await GameTask.Delay( 5 ); continue; }
			int count = Math.Min( space, samples.Length - offset );
			stream.WriteData( samples.AsSpan( offset, count ) );
			offset += count;
		}

		stream.Close();
		_speaking = false;
	}

	protected override void OnUpdate()
	{
		if ( OnPhoneme is null || _nextPhonemeIndex >= _phonemeEvents.Length ) return;
		if ( _speakStartTime < 0f ) return;

		float t = Time.Now - _speakStartTime;
		while ( _nextPhonemeIndex < _phonemeEvents.Length && _phonemeEvents[_nextPhonemeIndex].TimeSeconds <= t )
			OnPhoneme?.Invoke( _phonemeEvents[_nextPhonemeIndex++] );
	}

	public void Stop()
	{
		if ( _handle != null && _handle.IsValid && _handle.IsPlaying )
			_handle.Stop( 0f );
		_speaking = false;
		_speakStartTime = -1f;
		_nextPhonemeIndex = _phonemeEvents.Length;
	}

	public void SetVoice( VoiceData voice )
	{
		if ( _engine is null ) return;
		voice.Rate    = (short)Rate;
		voice.PitchHz = (short)PitchHz;
		_engine.Voice = voice;
	}

	public void ApplyVoice()
	{
		if ( _engine is null ) return;
		var v = _engine.Voice;
		v.Rate    = (short)Rate;
		v.PitchHz = (short)PitchHz;
		_engine.Voice = v;
	}
}
