#nullable enable
using System;

namespace SharpTalk
{

    public readonly struct PhonemeToken
    {
        public short Phon { get; init; }
        public long Ctrl { get; init; }  // kWord_Start, stress flags, etc.
        public short UserPitch { get; init; }
        public short UserDur { get; init; } // 0 = kDur_One (no scaling)
        public short UserNote { get; init; }
        public short UserRate { get; init; }
    }

    public sealed partial class AudioProcessor
    {
        // Phoneme IDs
        public const short _IY_ = 0; public const short _IH_ = 1;
        public const short _EH_ = 2; public const short _AE_ = 3;
        public const short _AA_ = 4; public const short _AH_ = 5;
        public const short _AO_ = 6; public const short _UH_ = 7;
        public const short _AX_ = 8; public const short _ER_ = 9;
        public const short _EY_ = 10; public const short _AY_ = 11;
        public const short _OY_ = 12; public const short _AW_ = 13;
        public const short _OW_ = 14; public const short _UW_ = 15;
        public const short _YU_ = 16; public const short _IR_ = 17;
        public const short _XR_ = 18; public const short _AR_ = 19;
        public const short _OR_ = 20; public const short _UR_ = 21;
        public const short _IX_ = 22; public const short _SIL_ = 23;
        public const short _RX_ = 24; public const short _LX_ = 25;
        public const short _EL_ = 26; public const short _EN_ = 27;
        public const short _w_ = 28; public const short _y_ = 29;
        public const short _r_ = 30; public const short _l_ = 31;
        public const short _h_ = 32; public const short _m_ = 33;
        public const short _n_ = 34; public const short _NG_ = 35;
        public const short _f_ = 36; public const short _v_ = 37;
        public const short _TH_ = 38; public const short _DH_ = 39;
        public const short _s_ = 40; public const short _z_ = 41;
        public const short _SH_ = 42; public const short _ZH_ = 43;
        public const short _p_ = 44; public const short _b_ = 45;
        public const short _t_ = 46; public const short _d_ = 47;
        public const short _k_ = 48; public const short _g_ = 49;
        public const short _CH_ = 50; public const short _JH_ = 51;
        public const short _TX_ = 52; public const short _DX_ = 53;
        public const short _QX_ = 54; public const short _DD_ = 55;
        public const short _Comma_ = 67;
        public const short _Period_ = 68;
        public const short _Quest_ = 69;
        public const short _Exclam_ = 70;

        // Ctrl-buffer flags (for PhonemeToken.Ctrl)
        public const long kSyllableTypeField = 0x0F;
        public const long kWord_End = 0x0001;
        public const long kPrep_End = 0x0002;
        public const long kVerb_End = 0x0004;
        public const long kTerm_End = 0x0008;
        public const long kWord_Initial_Consonant = 0x0080;
        public const long kSyllableOrderField = 0x0300;
        public const long kFirst_Syllable_In_Word = 0x0100;
        public const long kMid_Syllable_In_Word = 0x0200;
        public const long kLast_Syllable_In_Word = 0x0300;
        public const long kMore_Than_One_Syllable_In_Word = 0x0300;
        public const long kPrimaryStress = 0x0400;
        public const long kSecondaryStress = 0x0800;
        public const long kEmphaticStress = 0x1000;
        public const long kStressField = 0x1C00;
        public const long kIsStressed = 0x1C00;
        public const long kPrimOrEmphStress = 0x1400;
        public const long kContent_Word = 0x2000;
        public const long kBoundryTypeField = 0xF0000L;
        public const long kWord_Start = 0x10000L;
        public const long kPrep_Start = 0x20000L;
        public const long kVerb_Start = 0x40000L;
        public const long kTerm_Bound = 0x80000L;
        public const long kSilenceTypeField = 0x00F00000L;
        public const int kSilenceTypeShift = 20;
        public const long kSilenceDuration = 0x01000000L;
        public const long kSingingDuration = 0x40000000L;
        public const long kSingingPhon = 0x20000000L;
        public const long kSyllable_Start = 0x10000000L;
        public const long kPitchRise = 0x0020L;
        public const long kPitchFall = 0x0040L;
        public const long kPitchRise1 = 0x04000000L;
        public const long kPitchFall1 = 0x08000000L;
        public const long kLowVibrato = 0x10L;
        public const long kNoteDur = 0x0F00L;
        public const int kNoteDurShift = 8;
        public const long kNotePitch = 0x00FFL;
        public const long kCompoundNoun = 0x8000L;
        public const long kStressedWInitial = kIsStressed | kWord_Initial_Consonant;
        public const long kSampleMarker = 0x02000000L;

        // BND types for silence (index into BoundryDurTbl)
        public const int kBND_Pause = 1;
        public const int kBND_Decl = 2;
        public const int kBND_Quest = 3;
        public const int kBND_Emph = 4;

        // Private implementation constants
        private const int kFrameTime = 5;
        private const int kNormalPitch = 323;
        private const int kPhonBufSize = 512;
        private const int kPhonBuf_Red_Zone = kPhonBufSize - 10;
        private const int kPitchBufSize = kPhonBufSize * 6;
        private const int kMaxRamps = 16;
        private const int kStepSizeRes = 3;
        private const int kNeverHappens = -10000;
        private const int kDur_One = 0x100;
        private const int kDurStepRes = 8;
        private const int kNormal_Speech_Rate = 180;
        private const int kMinRate = 40;
        private const int k1pct = 655;
        private const int pct = 655;
        private const int kOneHalf = 0x8000;
        private const int k100percent = 0x10000;
        private const int k100pct_Dur = 128;

        // Hz-based pitch offsets from kNormalPitch
        private const int kHZ_4 = 335 - kNormalPitch;  // 12
        private const int kHZ_6 = 341 - kNormalPitch;  // 18
        private const int kHZ_7 = 344 - kNormalPitch;  // 21
        private const int kHZ_8 = 347 - kNormalPitch;  // 24
        private const int kHZ_9 = 350 - kNormalPitch;  // 27
        private const int kHZ_10 = 352 - kNormalPitch;  // 29
        private const int kHZ_12 = 358 - kNormalPitch;  // 35
        private const int kHZ_14 = 364 - kNormalPitch;  // 41
        private const int kHZ_18 = 374 - kNormalPitch;  // 51
        private const int kHZ_20 = 380 - kNormalPitch;  // 57
        private const int kHZ_25 = 393 - kNormalPitch;  // 70
        private const int kHZ_28 = 400 - kNormalPitch;  // 77

        // Phoneme flags (from phonFlags2 table)
        private const uint kVowelF = 1u << 0;
        private const uint kConsonantF = 1u << 1;
        private const uint kVoicedF = 1u << 2;
        private const uint kVowel1F = 1u << 3;
        private const uint kSonorantF = 1u << 4;
        private const uint kNasalF = 1u << 6;
        private const uint kSonorConsonF = 1u << 8;
        private const uint kPlosFricF = 1u << 10;
        private const uint kStopF = 1u << 12;
        private const uint kGStopF = 1u << 20;
        private const uint kAffricateF = 1u << 24;
        private const uint kVocLiq = 1u << 26;
        private const uint kFric = 1u << 27;

        // Pitch buffer event flags
        private const short kPitchStress_Flg = 0x1;
        private const short kPitchRiseFall_Flg = 0x2;
        private const short kPitchBoundry_Flg = 0x4;
        private const short kResetDecline = 0x8;
        private const short kPhraseReset = 0x10;
        private const short kPitchRiseFall1_Flg = 0x20;

        // Internal buffers
        private readonly short[] _phonBuf1 = new short[kPhonBufSize];
        private readonly long[] _phonCtrlBuf1 = new long[kPhonBufSize];
        private readonly short[] _userPitchBuf1 = new short[kPhonBufSize];
        private readonly short[] _userDurBuf1 = new short[kPhonBufSize];
        private readonly short[] _userNoteBuf1 = new short[kPhonBufSize];
        private readonly short[] _userRateBuf1 = new short[kPhonBufSize];

        private readonly short[] _phonBuf2 = new short[kPhonBufSize];
        private readonly long[] _phonCtrlBuf2 = new long[kPhonBufSize];
        private readonly short[] _userPitchBuf2 = new short[kPhonBufSize];
        private readonly short[] _userDurBuf2 = new short[kPhonBufSize];
        private readonly short[] _userNoteBuf2 = new short[kPhonBufSize];
        private readonly short[] _userRateBuf2 = new short[kPhonBufSize];

        private readonly short[] _durBuf = new short[kPhonBufSize];
        private readonly short[] _pitchBufFreq = new short[kPitchBufSize];
        private readonly short[] _pitchBufTime = new short[kPitchBufSize];
        private readonly short[] _pitchBufFlags = new short[kPitchBufSize];
        private readonly long[] _rampSteps = new long[kMaxRamps];

        // Voice params (set from VoiceData)
        private short _speechRate;
        private long _vpPitchRange;    // 16.16 fixed
        private long _vpStressGain;    // 16.16 fixed
        private short _vpRiseAmt;
        private short _vpFallAmt;
        private short _vpRiseAmt1;
        private short _vpFallAmt1;
        private int _vpAssertiveness; // 16.16 fixed
        private short _vpBaselineFall;
        private int _vpQuickness;
        private short _stressDurTime;   // frames (already >>1 from raw)
        private long _vibratoDepth1;
        private long _vibratoDepth2;
        private long _vibratoFreq;
        private long _vpIntonation;
        private short _voiceNaturalPitch;

        // State computed during pipeline
        private int _phonBuf1InIndex;
        private int _phonBuf2InIndex;
        private int _pitchBufInIndex;
        private int _scanIndex;
        private bool _isCompoundNoun;
        private short _endPunctuation;
        private bool _singing;

        // Rate params
        private long _rateRatio;
        private long _rateRatioLowGain;
        private short _stressDuration;

        // Pitch params
        private short _vpBaselinePitch;
        private short _baselineFallStart;
        private short _baselineFallEnd;
        private long _pFilterOut1;
        private long _pFilterOut2;
        private long _pFilterInGain;
        private long _pFilterFbGain;
        private short _pitchClauseStartTime;
        private short _pitchBoundry;

        // Fill_Pitch_Buf helpers
        private int _pitchTimeOffset;

        // Calc_Ramp_Steps result
        private short _curRamp;

        // StartNew_PitchClause output
        private short _baselineStartOffset;
        private short _baselineEndOffset;

        // Constructor

        public AudioProcessor(VoiceData voice)
        {
            InitFromVoice(voice);
        }

        private void InitFromVoice(VoiceData vd)
        {
            _speechRate = vd.Rate;
            _vpRiseAmt = vd.RiseAmt;
            _vpFallAmt = vd.FallAmt;
            _vpRiseAmt1 = vd.RiseAmt1;
            _vpFallAmt1 = vd.FallAmt1;
            _vpAssertiveness = vd.Assertiveness;
            _vpBaselineFall = vd.BaselineFall;
            _vpQuickness = vd.Quickness;
            _stressDurTime = vd.StressDurTime;
            _vpPitchRange = ((long)vd.PitchRange << 16) / 100;
            _vpStressGain = ((long)vd.StressGain << 16) / 100;
            _vibratoDepth1 = ((long)vd.VibratoDepth1Raw << 16) / 1000;
            _vibratoDepth2 = ((long)vd.VibratoDepth2Raw << 16) / 1000;
            long vf = ((long)vd.VibratoFreqRaw << 16) / 10;
            _vibratoFreq = (vf * 256) / 200;
            _vpIntonation = ((long)vd.Intonation << 16) / 100;
            _voiceNaturalPitch = HzToPitch(vd.PitchHz);
        }

        // Public entry point

        public SynthInputDump Process(PhonemeToken[] tokens, short endPunctuation = _Period_)
        {
            _endPunctuation = endPunctuation;
            _singing = false;

            ClearBuffers();
            InitRateParams();
            InitPitchParams();

            LoadPhonemes(tokens);
            FlagPhonBuf1();
            FillPhonBuf2();
            PitchRaiseAndFall();
            ModDuration();
            CalcRampSteps();
            FillPitchBuf();
            StartNewPitchClause();
            InsertPlosiveRelease();

            return BuildSynthInputDump();
        }

        // Pipeline setup helpers

        private void ClearBuffers()
        {
            for (int i = 0; i < kPhonBufSize; i++)
            {
                _phonBuf1[i] = _SIL_;
                _phonBuf2[i] = _SIL_;
                _phonCtrlBuf1[i] = 0;
                _phonCtrlBuf2[i] = 0;
                _userDurBuf1[i] = kDur_One;
                _userDurBuf2[i] = kDur_One;
                _userPitchBuf1[i] = 0;
                _userNoteBuf1[i] = 0;
                _userRateBuf1[i] = 0;
            }
        }

        private void InitRateParams()
        {
            if (_speechRate < kMinRate) _speechRate = kMinRate;
            _rateRatio = ((long)kNormal_Speech_Rate << 16) / _speechRate;
            long denominator = (((_speechRate - kNormal_Speech_Rate) * (long)(k1pct * 60)) >> 16) + kNormal_Speech_Rate;
            _rateRatioLowGain = ((long)kNormal_Speech_Rate << 16) / denominator;
            _stressDuration = (short)((_rateRatio * _stressDurTime) >> 16);
        }

        private void InitPitchParams()
        {
            _vpBaselinePitch = _voiceNaturalPitch;
            _baselineFallStart = (short)(kHZ_7 + _vpBaselineFall);
            _baselineFallEnd = (short)(kHZ_7 - _vpBaselineFall);
            _pFilterOut1 = (long)_baselineFallStart << kStepSizeRes;
            _pFilterOut2 = _pFilterOut1;
            _pFilterInGain = _vpQuickness;
            _pFilterFbGain = k100percent - _vpQuickness;
            _pitchClauseStartTime = (short)(10 / kFrameTime);
            _pitchBoundry = kNeverHappens;
        }

        private static short HzToPitch(short hz)
        {
            const int ratioK = 2621;
            if (hz <= 0) return 0;
            long freq, fk;
            if (hz < 100) { freq = hz << 3; fk = 0x000; }
            else if (hz < 200) { freq = hz << 2; fk = 0x100; }
            else if (hz < 400) { freq = hz << 1; fk = 0x200; }
            else if (hz < 800) { freq = hz; fk = 0x300; }
            else if (hz < 1600) { freq = hz >> 1; fk = 0x400; }
            else if (hz < 3200) { freq = hz >> 2; fk = 0x500; }
            else { freq = hz >> 3; fk = 0x600; }
            long ratio = ((freq - 400) * ratioK) >> 11;
            if (ratio < 0) ratio = 0;
            if (ratio >= Tables.logOf2Tbl.Length) ratio = Tables.logOf2Tbl.Length - 1;
            return (short)(Tables.logOf2Tbl[ratio] + fk);
        }

        // BuildSynthInputDump

        private SynthInputDump BuildSynthInputDump()
        {
            int count = _phonBuf2InIndex + 1; // +1 for lookahead SIL slot

            short[] phonBuf2 = new short[count];
            long[] controls = new long[count];
            short[] durBuf = new short[count];
            short[] userPitchBuf2 = new short[count];
            short[] userNoteBuf2 = new short[count];

            for (int i = 0; i < count; i++)
            {
                phonBuf2[i] = _phonBuf2[i];
                controls[i] = _phonCtrlBuf2[i];
                durBuf[i] = _durBuf[i];
                userPitchBuf2[i] = _userPitchBuf2[i];
                userNoteBuf2[i] = _userNoteBuf2[i];
            }

            int pitchCount = _pitchBufInIndex + 1;
            short[] pitchFreq = new short[pitchCount];
            short[] pitchTime = new short[pitchCount];
            short[] pitchFlags = new short[pitchCount];
            for (int i = 0; i < pitchCount; i++)
            {
                pitchFreq[i] = _pitchBufFreq[i];
                pitchTime[i] = _pitchBufTime[i];
                pitchFlags[i] = _pitchBufFlags[i];
            }

            long[] rampStepsCopy = new long[kMaxRamps];
            Array.Copy(_rampSteps, rampStepsCopy, kMaxRamps);

            var pitch = new PitchState
            {
                NextPitchBufTime = pitchCount > 0 ? pitchTime[0] : (short)0,
                PitchBufOutIndex = 0,
                CurPitchBufTime = (short)(_pitchClauseStartTime >> 1),
                CurPitchBufPitch = 0,
                CurPitchBufFlags = 0,

                PhonIndexTarg = -1,
                PhonIndexCp = -1,
                TimeIntoPhonTarg = _pitchClauseStartTime,
                TimeIntoPhonCp = 0,
                CurPhonDurCc = 0,
                CurPhonDurCp = 0,
                PhonDurDelay = 0,

                UvPhonPitchTarg = 0,
                PhonPitchOffset = 0,
                PhonPitchOffset1 = 0,

                FallRiseOffset = 0,
                FallRise1Offset = 0,
                StressTarget = 0,
                PunctOffset = 0,
                StressActiveTime = 0,
                StressDuration = _stressDuration,

                BaseLineOffset = 0,
                BasePitchOffset = 0,
                PitchBoundry = (short)_pitchBoundry,
                LowGainCp = 0,

                BaselineFallStart = _baselineFallStart,
                BaselineFallEnd = _baselineFallEnd,
                BaselineStartOffset = _baselineStartOffset,
                BaselineEndOffset = _baselineEndOffset,

                DownRampOffset = 0,
                DownRampStep = _rampSteps.Length > 0 ? _rampSteps[0] : 0,
                RampSteps = rampStepsCopy,
                CurRamp = _curRamp,

                PFilterOut1 = _pFilterOut1,
                PFilterOut2 = _pFilterOut2,
                PFilterInGain = _pFilterInGain,
                PFilterFbGain = _pFilterFbGain,

                VpIntonation = _vpIntonation,
                VpPitchRange = _vpPitchRange,
                VpBaselinePitch = _vpBaselinePitch,

                VibratoDepth1 = _vibratoDepth1,
                VibratoDepth2 = _vibratoDepth2,
                VibratoFreq = _vibratoFreq,
                VibratoPhase1 = 0,

                Singing = (short)(_singing ? 1 : 0),
                HzGlide = 0,
                MusicalNoteActive = 0,
                PortamentoAccum = 0,
                PortamentoStep = 0,
                NewPortaTarget = 0,
                NewSentence = 1,
                SpeechRate = _speechRate,
            };

            return SynthInputDump.Create(
                phonBuf2InIndex: _phonBuf2InIndex,
                phonBuf2: phonBuf2,
                controls: controls,
                durBuf: durBuf,
                userPitchBuf2: userPitchBuf2,
                userNoteBuf2: userNoteBuf2,
                pitchBufInIndex: (uint)_pitchBufInIndex,
                pitchBufFreq: pitchFreq,
                pitchBufTime: pitchTime,
                pitchBufFlags: pitchFlags,
                pitch: pitch
            );
        }

        // Inline helpers

        private short GetPhon2(int i)
        {
            if (i < 0 || i >= _phonBuf2InIndex) return _SIL_;
            return _phonBuf2[i];
        }

        private long GetCtrl2(int i)
        {
            if (i < 0 || i >= _phonBuf2InIndex) return 0;
            return _phonCtrlBuf2[i];
        }

        private uint GetPhonFlags1(int i)
        {
            if (i < 0 || i >= _phonBuf1InIndex) return 0;
            short p = _phonBuf1[i];
            if (p < 0 || p >= Tables.PhonFlags2.Length) return 0;
            return Tables.PhonFlags2[p];
        }
    }
}  // namespace
