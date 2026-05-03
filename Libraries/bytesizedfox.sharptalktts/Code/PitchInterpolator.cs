#nullable enable
using System;

namespace SharpTalk
{

    public sealed class PitchInterpolator
    {
        private readonly SynthInputDump _dump;

        // Mutable state mirroring vv fields touched by Interpolate_Pitch / Phon_Boundry_Pitch
        private short _nextPitchBufTime;
        private int _pitchBufOutIndex;
        private int _curPitchBufTime;
        private short _curPitchBufPitch;
        private short _curPitchBufFlags;

        private int _phonIndexTarg;
        private int _timeIntoPhonTarg;
        private int _curPhonDurCc;
        private int _phonDurDelay;

        private int _phonIndexCp;
        private int _timeIntoPhonCp;
        private int _curPhonDurCp;

        private int _uvPhonPitchTarg;
        private int _phonPitchOffset1;

        private int _fallRiseOffset;
        private int _fallRise1Offset;
        private int _stressTarget;
        private int _punctOffset;
        private int _stressActiveTime;

        private int _baselineStartOffset;
        private int _baselineEndOffset;
        private long _downRampOffset;
        private long _downRampStep;
        private long[] _rampSteps;
        private int _curRamp;

        private long _pFilterOut1;
        private long _pFilterOut2;
        private long _pFilterInGain;
        private long _pFilterFbGain;

        private long _vpIntonation;
        private long _vpPitchRange;
        private int _vpBaselinePitch;

        private long _vibratoDepth1;
        private long _vibratoDepth2;
        private long _vibratoFreq;
        private int _vibratoPhase1;

        private bool _singing;
        private bool _hzGlide;
        private bool _musicalNoteActive;
        private long _portamentoAccum;
        private long _portamentoStep;
        private bool _newPortaTarget;
        private bool _newSentence;
        private int _speechRate;

        private int _pitchBoundry;
        private bool _lowGainCp;
        private int _pbHold;
        private bool _pbLowGain;

        // Constants from mt4.h
        private const int kStepSizeRes = 3;
        private const int kNeverHappens = -10000;
        private const int kFrameTime = 5;
        private const int pct = 655;
        private const int k100percent = 0x10000;

        // Pitch buffer event flags
        private const int kResetDecline = 0x8;
        private const int kPhraseReset = 0x10;
        private const int kPitchRiseFall_Flg = 0x2;
        private const int kPitchRiseFall1_Flg = 0x20;
        private const int kPitchStress_Flg = 0x1;

        // Phoneme flags
        private const uint kVoicedF = (1 << 2);
        private const uint kVowelF = (1 << 0);
        private const uint kVowel1F = (1 << 3);
        private const uint kGStopF = (1 << 20);
        private const uint kStopF = (1 << 12);

        // PhonCtrl field masks
        private const long kSyllableTypeField = 0x0F;
        private const long kWord_End = 0x0001;
        private const long kPrep_End = 0x0002;
        private const long kMid_Syllable_In_Word = 0x0200;
        private const long kPrimOrEmphStress = 0x1400;

        // _SIL_ phoneme index
        private const int _SIL_ = 23;
        // _YU_ phoneme index
        private const int _YU_ = 16;

        public PitchInterpolator(SynthInputDump dump)
        {
            _dump = dump;
            PitchState s = dump.Pitch;

            _nextPitchBufTime = s.NextPitchBufTime;
            _pitchBufOutIndex = s.PitchBufOutIndex;
            _curPitchBufTime = s.CurPitchBufTime;
            _curPitchBufPitch = s.CurPitchBufPitch;
            _curPitchBufFlags = s.CurPitchBufFlags;

            _phonIndexTarg = s.PhonIndexTarg;
            _timeIntoPhonTarg = s.TimeIntoPhonTarg;
            _curPhonDurCc = s.CurPhonDurCc;
            _phonDurDelay = s.PhonDurDelay;

            _phonIndexCp = s.PhonIndexCp;
            _timeIntoPhonCp = s.TimeIntoPhonCp;
            _curPhonDurCp = s.CurPhonDurCp;

            _uvPhonPitchTarg = s.UvPhonPitchTarg;
            _phonPitchOffset1 = s.PhonPitchOffset1;

            _fallRiseOffset = s.FallRiseOffset;
            _fallRise1Offset = s.FallRise1Offset;
            _stressTarget = s.StressTarget;
            _punctOffset = s.PunctOffset;
            _stressActiveTime = s.StressActiveTime;

            _baselineStartOffset = s.BaselineStartOffset;
            _baselineEndOffset = s.BaselineEndOffset;
            _downRampOffset = s.DownRampOffset;
            _downRampStep = s.DownRampStep;
            _rampSteps = s.RampSteps;
            _curRamp = s.CurRamp;

            _pFilterOut1 = s.PFilterOut1;
            _pFilterOut2 = s.PFilterOut2;
            _pFilterInGain = s.PFilterInGain;
            _pFilterFbGain = s.PFilterFbGain;

            _vpIntonation = s.VpIntonation;
            _vpPitchRange = s.VpPitchRange;
            _vpBaselinePitch = s.VpBaselinePitch;

            _vibratoDepth1 = s.VibratoDepth1;
            _vibratoDepth2 = s.VibratoDepth2;
            _vibratoFreq = s.VibratoFreq;
            _vibratoPhase1 = s.VibratoPhase1;

            _singing = s.Singing != 0;
            _hzGlide = s.HzGlide != 0;
            _musicalNoteActive = s.MusicalNoteActive != 0;
            _portamentoAccum = s.PortamentoAccum;
            _portamentoStep = s.PortamentoStep;
            _newPortaTarget = s.NewPortaTarget != 0;
            _newSentence = s.NewSentence != 0;
            _speechRate = s.SpeechRate;

            _pitchBoundry = s.PitchBoundry;
            _lowGainCp = s.LowGainCp != 0;

            _voiceNaturalPitch = s.VpBaselinePitch;

            _pbHold = kNeverHappens;
            _pbLowGain = false;
        }

        private int _controlF0;
        private int _voiceNaturalPitch; // initialized to vpBaselinePitch at construction
        private long _curPhonCtrlSinging; // ctrl of phoneme currently being rendered (singing path)

        public short Step()
        {
            Interpolate_Pitch();
            return (short)_controlF0;
        }

        private const long kLowVibrato = 0x10L;

        // Called by SpeechRenderer at the start of each phoneme (equivalent to StartNewPhon/DoNote in C)
        public void DoNote(int phonIndex)
        {
            _hzGlide = false;
            _curPhonCtrlSinging = GetPhonCtrl(phonIndex);

            long ctrl = (phonIndex >= 0 && phonIndex < _dump.PhonCtrlBuf2.Length)
                        ? _dump.PhonCtrlBuf2[phonIndex] : 0;

            // If outside a singing block, musical note context ends
            if ((ctrl & kSingingPhon) == 0)
                _musicalNoteActive = false;

            short note = (phonIndex >= 0 && phonIndex < _dump.UserNoteBuf2.Length)
                         ? _dump.UserNoteBuf2[phonIndex] : (short)0;

            if (note != 0 && (ctrl & kSilenceDuration) == 0)
            {
                if ((ctrl & kSingingPhon) != 0)
                {
                    if (note < 0)
                    {
                        // note > 37: raw Hz glide
                        int targetPitch = HzToPitch(-note);
                        int curPitch = (int)(_portamentoAccum >> 16);
                        int frames = (phonIndex < _dump.DurBuf.Length) ? _dump.DurBuf[phonIndex] : 1;
                        if (frames < 1) frames = 1;
                        _vpBaselinePitch = targetPitch;
                        _portamentoStep = ((long)(targetPitch - curPitch) << 16) / frames;
                        _newPortaTarget = true;
                        _hzGlide = true;
                    }
                    else
                    {
                        // note <= 37: constant musical note (IIR convergence)
                        _vpBaselinePitch = note;
                        _portamentoStep = 0;
                        _newPortaTarget = true;
                        _musicalNoteActive = true;
                    }
                }
                else
                {
                    // EC_note: semitone offset above voiceNaturalPitch
                    int n = (note & 0xFF) << 8;
                    if (n != 0x7F00)
                    {
                        _vpBaselinePitch = _voiceNaturalPitch + ((n * 0x1555) >> 16);
                        if (_vpBaselinePitch < 0) _vpBaselinePitch = 0;
                    }
                }
            }
        }

        private static int HzToPitch(int hz)
        {
            if (hz <= 0) return 0;
            int freq, fk;
            if (hz < 100) { freq = hz << 3; fk = 0x000; }
            else if (hz < 200) { freq = hz << 2; fk = 0x100; }
            else if (hz < 400) { freq = hz << 1; fk = 0x200; }
            else if (hz < 800) { freq = hz; fk = 0x300; }
            else if (hz < 1600) { freq = hz >> 1; fk = 0x400; }
            else if (hz < 3200) { freq = hz >> 2; fk = 0x500; }
            else { freq = hz >> 3; fk = 0x600; }
            int ratio = ((freq - 400) * 2621) >> 11;
            if (ratio < 0) ratio = 0;
            if (ratio >= Tables.logOf2Tbl.Length) ratio = Tables.logOf2Tbl.Length - 1;
            return Tables.logOf2Tbl[ratio] + fk;
        }

        private const long kSingingDuration = 0x40000000L;
        private const long kSingingPhon = 0x20000000L;
        private const long kSilenceDuration = 0x01000000L;

        private short GetPhon(int index)
        {
            if (index >= 0 && index < _dump.PhonBuf2InIndex)
                return _dump.PhonBuf2[index];
            return _SIL_;
        }

        private long GetPhonCtrl(int index)
        {
            if (index >= 0 && index < _dump.PhonBuf2InIndex)
                return _dump.PhonCtrlBuf2[index];
            return 0;
        }

        private void Phon_Boundry_Pitch()
        {
            if (_timeIntoPhonCp >= _curPhonDurCp)
            {
                _timeIntoPhonCp -= _curPhonDurCp;
                _phonIndexCp++;
                _curPhonDurCp = (_phonIndexCp < _dump.DurBuf.Length) ? _dump.DurBuf[_phonIndexCp] : 0;

                int curPhon = GetPhon(_phonIndexCp);
                uint curFlags = Tables.PhonFlags2[curPhon];
                long curCtrl = GetPhonCtrl(_phonIndexCp + 1);

                int nextPhon = GetPhon(_phonIndexCp + 1);
                uint nextFlags = Tables.PhonFlags2[nextPhon];
                long nextCtrl = GetPhonCtrl(_phonIndexCp + 1);

                if (_pitchBoundry == 0)
                    _pitchBoundry = kNeverHappens;
                if (_pitchBoundry > 0)
                    _pitchBoundry = 0;

                _pbHold = kNeverHappens;
                _pbLowGain = false;

                if ((curFlags & kVowel1F) != 0
                    && (nextCtrl & kMid_Syllable_In_Word) == 0
                    && ((curCtrl & kSyllableTypeField) >= kWord_End)
                    && nextPhon != _YU_)
                {
                    if ((curFlags & kVowelF) != 0)
                    {
                        if (curPhon == nextPhon && (nextCtrl & kPrimOrEmphStress) != 0)
                        {
                            _pbHold = _curPhonDurCp;
                        }
                        else if ((curCtrl & kSyllableTypeField) >= kPrep_End)
                        {
                            _pbHold = _curPhonDurCp;
                            _pbLowGain = true;
                        }
                    }
                    else
                    {
                        if ((curFlags & kStopF) == 0
                            && curPhon != 53 // _DX_
                            && (nextCtrl & kPrimOrEmphStress) != 0)
                        {
                            _pbHold = _curPhonDurCp;
                        }
                    }
                }

                if ((nextFlags & kGStopF) != 0)
                    _pbHold = _curPhonDurCp;

                if ((curFlags & kGStopF) != 0)
                {
                    _pbHold = _curPhonDurCp;
                    return; // goto Exit
                }
            }

            int timeAt50 = 50 / kFrameTime;  // = 10
            int lastFrame = _curPhonDurCp - 1;
            if (_timeIntoPhonCp == timeAt50 || _timeIntoPhonCp == lastFrame)
            {
                _pitchBoundry = _pbHold;
                _lowGainCp = _pbLowGain;
            }
        }

        private void Interpolate_Pitch()
        {
            // --- Pitch buffer event collection loop ---
            bool collect = true;
            do
            {
                if (_curPitchBufTime >= _nextPitchBufTime
                    && _pitchBufOutIndex < (int)_dump.PitchBufInIndex)
                {
                    _curPitchBufPitch = _dump.PitchBufFreq[_pitchBufOutIndex];
                    _curPitchBufFlags = _dump.PitchBufFlags[_pitchBufOutIndex];

                    _curPitchBufTime -= _nextPitchBufTime;
                    _pitchBufOutIndex++;

                    _nextPitchBufTime = _dump.PitchBufTime[_pitchBufOutIndex];

                    if ((_curPitchBufFlags & kResetDecline) != 0)
                    {
                        _downRampOffset = 0;
                    }
                    else if ((_curPitchBufFlags & kPhraseReset) != 0)
                    {
                        _downRampOffset = (long)(_baselineStartOffset - _baselineEndOffset) << 14;
                        if (_curRamp < _rampSteps.Length - 1)
                            _curRamp++;
                        _downRampStep = _rampSteps[_curRamp];
                        _fallRiseOffset = 0;
                        _stressTarget = 0;
                        _punctOffset = 0;
                    }
                    else if ((_curPitchBufFlags & kPitchRiseFall_Flg) != 0)
                    {
                        _fallRiseOffset += _curPitchBufPitch;
                        if (_curPitchBufPitch < 0)
                        {
                            if (_stressTarget > 0) _stressTarget = 0;
                        }
                        else
                        {
                            if (_stressTarget < 0) _stressTarget = 0;
                        }
                    }
                    else if ((_curPitchBufFlags & kPitchRiseFall1_Flg) != 0)
                    {
                        _fallRise1Offset += _curPitchBufPitch;
                    }
                    else if ((_curPitchBufFlags & kPitchStress_Flg) != 0)
                    {
                        _stressTarget = _curPitchBufPitch;
                        _stressActiveTime = (int)_dump.Pitch.StressDuration;
                    }
                    else
                    {
                        _punctOffset = _curPitchBufPitch << 1;
                    }
                }
                else
                {
                    collect = false;
                }
            }
            while (collect);

            if (!_singing)
            {
                // --- Baseline fall ---
                int userPitch = (_phonIndexTarg >= 0 && _phonIndexTarg < _dump.UserPitchBuf2.Length)
                                ? _dump.UserPitchBuf2[_phonIndexTarg] : 0;
                int baseLineOffset = _baselineStartOffset - (int)(_downRampOffset >> 16) + userPitch;

                if (baseLineOffset > _baselineEndOffset)
                    _downRampOffset += _downRampStep;

                // --- Stress timer ---
                _stressActiveTime--;
                if (_stressActiveTime < 0)
                    _stressTarget = 0;

                // --- Phoneme target advance ---
                if (_timeIntoPhonTarg > _curPhonDurCc + _phonDurDelay
                    && _phonIndexTarg < _dump.PhonBuf2InIndex)
                {
                    _timeIntoPhonTarg -= _curPhonDurCc;
                    _phonIndexTarg++;
                    _curPhonDurCc = (_phonIndexTarg < _dump.DurBuf.Length) ? _dump.DurBuf[_phonIndexTarg] : 0;
                    _phonDurDelay = 0;

                    int curPhon = GetPhon(_phonIndexTarg);
                    long curCtrl = GetPhonCtrl(_phonIndexTarg);
                    uint curFlags = Tables.PhonFlags2[curPhon];
                    int nextPhon = GetPhon(_phonIndexTarg + 1);
                    uint nextFlags = Tables.PhonFlags2[nextPhon];

                    int phonPitchOffset = Tables.PhonPitchTbl[curPhon];
                    phonPitchOffset >>= 1; // always 50%

                    if ((nextFlags & kVoicedF) == 0)
                        _phonDurDelay = 25 / kFrameTime; // = 5

                    if ((curFlags & kVoicedF) != 0)
                    {
                        _phonPitchOffset1 = phonPitchOffset << 1;
                        _uvPhonPitchTarg = 0;
                    }
                    else
                    {
                        _uvPhonPitchTarg = phonPitchOffset << kStepSizeRes;
                        _phonPitchOffset1 = 0;
                        if ((curFlags & kStopF) != 0)
                            _phonDurDelay = 30 / kFrameTime; // = 6
                        else
                            _phonDurDelay = 0;
                    }
                }

                Phon_Boundry_Pitch();

                // --- Pitch target ---
                int phonPitchTarget = (int)(((long)(_stressTarget + _fallRiseOffset + _punctOffset + baseLineOffset) * _vpIntonation) >> 16);
                phonPitchTarget = (short)phonPitchTarget; // C truncates to short here
                phonPitchTarget = (phonPitchTarget + _phonPitchOffset1) << kStepSizeRes;

                // --- IIR filter init on new sentence ---
                if (_newSentence)
                {
                    _pFilterOut1 = _vpBaselinePitch;
                    _pFilterOut2 = _vpBaselinePitch;
                    _newSentence = false;
                }

                // --- IIR filter ---
                _pFilterOut1 = ((_pFilterInGain * phonPitchTarget) + (_pFilterFbGain * _pFilterOut1)) >> 16;
                _pFilterOut2 = ((_pFilterInGain * (_pFilterOut1 + _uvPhonPitchTarg)) + (_pFilterFbGain * _pFilterOut2)) >> 16;

                int basePitchOffset = (int)(_pFilterOut2 >> kStepSizeRes);

                // --- Phoneme boundary envelope ---
                int pbIndex = _timeIntoPhonCp - _pitchBoundry;
                if (pbIndex < 0) pbIndex = -pbIndex;

                const int kPbWindow = 45 / kFrameTime; // 9
                if (pbIndex <= kPbWindow)
                {
                    if (_lowGainCp)
                        basePitchOffset += pbIndex * (10 / kPbWindow) - 10;
                    else
                        basePitchOffset += pbIndex * (80 / kPbWindow) - 80;
                }

                // --- controlF0 ---
                _phonPitchOffset1 = (int)(((long)_phonPitchOffset1 * 98 * pct) >> 16);
                _controlF0 = (int)((((long)basePitchOffset * _vpPitchRange) >> 16) + _vpBaselinePitch);

                // --- Vibrato ---
                _vibratoPhase1 = (int) (_vibratoPhase1 + _vibratoFreq) & 0x00FFFFFF;

                double phaseNorm = (double)_vibratoPhase1 / 16777216.0;
                double angle = phaseNorm * 2.0 * Math.PI;
                int vibrato = (int)(Math.Sin(angle) * 128.0);

                if (_speechRate >= 100)
                    _controlF0 += (int)((vibrato * _vibratoDepth1) >> 16);
                else
                    _controlF0 += (int)((vibrato * _vibratoDepth2) >> 16);
            }
            else
            {
                // --- Singing mode ---
                if (_newSentence)
                {
                    _portamentoAccum = (long)_vpBaselinePitch << 16;
                    _newSentence = false;
                    _newPortaTarget = false;
                }
                else if (_newPortaTarget)
                {
                    if (_portamentoStep > 0)
                    {
                        _portamentoAccum += _portamentoStep;
                        if ((_portamentoAccum >> 16) >= _vpBaselinePitch)
                        {
                            _portamentoAccum = (long)_vpBaselinePitch << 16;
                            _newPortaTarget = false;
                        }
                    }
                    else if (_portamentoStep < 0)
                    {
                        _portamentoAccum += _portamentoStep;
                        if ((_portamentoAccum >> 16) < _vpBaselinePitch)
                        {
                            _portamentoAccum = (long)_vpBaselinePitch << 16;
                            _newPortaTarget = false;
                        }
                    }
                    else if (_singing)
                    {
                        long target = (long)_vpBaselinePitch << 16;
                        long diff = target - _portamentoAccum;
                        _portamentoAccum += diff >> 2;
                        if (diff > -0x10000L && diff < 0x10000L)
                        {
                            _portamentoAccum = target;
                            _newPortaTarget = false;
                        }
                    }
                    else
                    {
                        _portamentoAccum = (long)_vpBaselinePitch << 16;
                        _newPortaTarget = false;
                    }
                }

                _controlF0 = (int)(_portamentoAccum >> 16);

                // advance 24-bit phase accumulator
                _vibratoPhase1 = (int)((_vibratoPhase1 + _vibratoFreq) & 0xFFFFFF);

                // convert phase → radians
                double phaseNorm = (double)_vibratoPhase1 / 16777216.0; // 2^24
                double angle = phaseNorm * 2.0 * Math.PI;

                // generate vibrato in same range as table (-128..127)
                int vibrato = (int)(Math.Sin(angle) * 128.0);

                if (!_hzGlide && _musicalNoteActive) {
                    long depth = (_curPhonCtrlSinging & kLowVibrato) != 0 ? _vibratoDepth2: _vibratoDepth1;
                    _controlF0 += (int)((vibrato * depth) >> 16);
                }


            }

            if (_controlF0 < 0) _controlF0 = 0;

            _curPitchBufTime++;
            _timeIntoPhonTarg++;
            _timeIntoPhonCp++;
        }
    }
}  // namespace
