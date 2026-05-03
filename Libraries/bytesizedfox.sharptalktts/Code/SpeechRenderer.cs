#nullable enable
using System;
using System.Collections.Generic;

namespace SharpTalk
{

    public class SpeechRenderer
    {
        private VoiceData _voice;
        private SynthInputDump _dump = null!; // set at start of Render()

        private class ControlBlock
        {
            public short curP_START_Targ;
            public short curP_END_Targ;
            public short prevP_END_Targ;
            public short nextP_START_Targ;
            public int curTarget_TIME;
            public int curTarget_STEP;
            public int curTarget_OFFS;
            public int HEAD_offs;
            public int HEAD_step;
            public int TAIL_offs;
            public int TAIL_step;
            public int TAIL_START_time;
            public int onset_END_TIME;
            public short onset_VAL;
            public int ptrToTargetList; // index into _diphEntries; -1 = no list
        }

        private ControlBlock[] _cb = new ControlBlock[15];
        private short[] _controlData = new short[15];
        private short[] _diphEntries = new short[400];
        private int _nextDiphEntryIdx;

        // Current-phoneme context
        private int _curPhon, _prevPhon, _nextPhon, _prev2Phon;
        private uint _curPhonFlags, _prevPhonFlags, _nextPhonFlags, _prev2PhonFlags;
        private int _curPhonCtrl, _prevPhonCtrl, _nextPhonCtrl, _prev2PhonCtrl;
        private int _curPhonDur;
        private int _curPhonMaxDur;
        private long _curPhonPctOfMaxDur, _curPhonPctOfMaxDur1, _curPhonPctOfMaxDur2;

        // Shared during InitCtrlsForNewPhon iteration
        private int _transLevel, _transTime;
        private int _curBlockIndex;

        private int _durDoneInPhon;
        private int _curPhonBufIndex;
        private bool _startingNewPhon;
        private bool _bigBang = true;

        // Block index constants
        public const int kF1 = 0; public const int kF2 = 1; public const int kF3 = 2;
        public const int kBW1 = 3; public const int kBW2 = 4; public const int kBW3 = 5;
        public const int kFNZ = 6; public const int kAV = 7; public const int kAF = 8;
        public const int kAp2 = 9; public const int kAp3 = 10; public const int kAp4 = 11;
        public const int kAp5 = 12; public const int kAp6 = 13; public const int kAB = 14;
        private const int kNumOfBlocks = 15;

        // Block type constants (match Tables.CtrlBlockTypeTbl)
        public const int kFreqType = 0; public const int kBWType = 1; public const int kFNZType = 2;
        public const int kSourceAmpType = 3; public const int kResonAmpType = 4;

        // Phoneme numbers
        private const int _IY_ = 0; private const int _ER_ = 9; private const int _AY_ = 11;
        private const int _OY_ = 12; private const int _UW_ = 15; private const int _YU_ = 16;
        private const int _SIL_ = 23; private const int _LX_ = 25; private const int _EL_ = 26;
        private const int _EN_ = 27; private const int _w_ = 28; private const int _y_ = 29;
        private const int _r_ = 30; private const int _l_ = 31; private const int _h_ = 32;
        private const int _m_ = 33; private const int _n_ = 34; private const int _NG_ = 35;
        private const int _f_ = 36; private const int _v_ = 37; private const int _TH_ = 38;
        private const int _DH_ = 39; private const int _s_ = 40; private const int _z_ = 41;
        private const int _SH_ = 42; private const int _ZH_ = 43; private const int _p_ = 44;
        private const int _b_ = 45; private const int _t_ = 46; private const int _d_ = 47;
        private const int _k_ = 48; private const int _g_ = 49; private const int _CH_ = 50;
        private const int _JH_ = 51; private const int _TX_ = 52; private const int _DX_ = 53;
        private const int _QX_ = 54; private const int _DD_ = 55;

        // Phoneme flag constants
        private const uint kVowelF = 1 << 0; private const uint kConsonantF = 1 << 1;
        private const uint kVoicedF = 1 << 2; private const uint kVowel1F = 1 << 3;
        private const uint kSonorantF = 1 << 4; private const uint kSonorant1F = 1 << 5;
        private const uint kNasalF = 1 << 6; private const uint kLiqGlideF = 1 << 7;
        private const uint kSonorConsonF = 1 << 8; private const uint kPlosiveF = 1 << 9;
        private const uint kPlosFricF = 1 << 10; private const uint kObstF = 1 << 11;
        private const uint kStopF = 1 << 12; private const uint kAlveolarF = 1 << 13;
        private const uint kVelar = 1 << 14; private const uint kLabialF = 1 << 15;
        private const uint kDentalF = 1 << 16; private const uint kPalatalF = 1 << 17;
        private const uint kYGlideStartF = 1 << 18; private const uint kYGlideEndF = 1 << 19;
        private const uint kGStopF = 1 << 20; private const uint kFrontF = 1 << 21;
        private const uint kDiphthongF = 1 << 22; private const uint kAffricateF = 1 << 24;
        private const uint kLiqGlide2F = 1 << 25; private const uint kVocLiq = 1 << 26;
        private const uint kFric = 1 << 27;

        // Ctrl field masks
        private const int kPlosive_Release = 0x4000;
        private const int kPrimOrEmphStress = 0x1400;
        private const int kStressField = 0x1C00;
        private const int kSyllableTypeField = 0x000F;

        private const int kNoValue = -1;
        private const int kMaxBandWidth = 1000;
        private const int C_V_type = 0;
        private const int V_C_type = 1;
        private const int kFrontR = 0; private const int kMiddleR = 1;
        private const int kBackR = 2; private const int kRoundR = 3;
        private const int kConsonantR = 4;
        private const int kStepSizeRes = 3;
        private const int k1pct = 655;
        private const int kFrameTime = 5;
        private const int kSizeOf1xTbl = 100;
        private const int kOneHalf = 0x8000;

        // Voice data
        private short[] _envelopeListTbl;
        private short[] _lociTbl;
        private short[] _voiceAvTbl;
        private short[] _voiceNoiseAmpTbl;
        private int _nasalTargFreq, _nasalBaseFreq, _locusOffset;
        private int _voiceBWgain1, _voiceBWgain2, _voiceBWgain3;

        public SpeechRenderer(VoiceData voice)
        {
            _voice = voice;
            for (int i = 0; i < _cb.Length; i++) _cb[i] = new ControlBlock();
            bool male = voice.VoiceType == 0;
            _envelopeListTbl = male ? Tables.MaleEnvTbl : Tables.FemaleEnvTbl;
            _lociTbl = male ? Tables.Male_Loci_Tbl : Tables.Female_Loci_Tbl;
            _voiceAvTbl = male ? Tables.avVolTblM : Tables.avVolTblF;
            _voiceNoiseAmpTbl = male ? Tables.Male_NoiseAmpTbl : Tables.Female_NoiseAmpTbl;
            _nasalTargFreq = voice.NasalTarg;
            _nasalBaseFreq = voice.NasalBase;
            _locusOffset = voice.Locus;
            _voiceBWgain1 = (voice.BwGain1 << 16) / 100;
            _voiceBWgain2 = (voice.BwGain2 << 16) / 100;
            _voiceBWgain3 = (voice.BwGain3 << 16) / 100;
        }

        public Frame[] Render(SynthInputDump dump)
        {
            _dump = dump;
            var frames = new List<Frame>();
            _curPhonBufIndex = 0;
            _durDoneInPhon = 0;
            _startingNewPhon = true;

            // Big-Bang: seed curP_END_Targ from the first phoneme's target (once per renderer)
            if (_bigBang)
            {
                _bigBang = false;
                SetPhonContext(0);
                for (_curBlockIndex = 0; _curBlockIndex < kNumOfBlocks; _curBlockIndex++)
                    _cb[_curBlockIndex].curP_END_Targ = (short)GetFirstTarget(0);
            }

            var pitchInterp = new PitchInterpolator(dump);
            int totalFrames = 0;
            for (int i = 0; i < dump.PhonBuf2InIndex; i++) totalFrames += dump.DurBuf[i];

            for (int i = 0; i < totalFrames; i++)
            {
                if (_durDoneInPhon >= _dump.DurBuf[_curPhonBufIndex])
                {
                    _curPhonBufIndex++;
                    _durDoneInPhon = 0;
                    _startingNewPhon = true;
                }
                if (_startingNewPhon) { InitCtrlsForNewPhon(); pitchInterp.DoNote(_curPhonBufIndex); _startingNewPhon = false; }

                short f0 = pitchInterp.Step();
                InterpolateFormants();
                frames.Add(SaveFrame(f0, (byte)_dump.PhonCtrlBuf2[_curPhonBufIndex]));
                _durDoneInPhon++;
            }
            return frames.ToArray();
        }

        private void SetPhonContext(int index)
        {
            _curPhon = GP(index); _curPhonFlags = PF(_curPhon); _curPhonCtrl = PC(index);
            _nextPhon = GP(index + 1); _nextPhonFlags = PF(_nextPhon); _nextPhonCtrl = PC(index + 1);
            _prevPhon = GP(index - 1); _prevPhonFlags = PF(_prevPhon); _prevPhonCtrl = PC(index - 1);
            _prev2Phon = GP(index - 2); _prev2PhonFlags = PF(_prev2Phon); _prev2PhonCtrl = PC(index - 2);
            _curPhonDur = (index >= 0 && index < _dump.DurBuf.Length) ? _dump.DurBuf[index] : 0;
        }

        private void FillPhonTargets()
        {
            for (int i = 0; i < kNumOfBlocks; i++) _cb[i].onset_END_TIME = 0;
            _nextDiphEntryIdx = 0;
            if ((_curPhonFlags & kPlosFricF) == 0 && _curPhon != _SIL_ && _curPhon >= 0 && _curPhon < Tables.MaxDurTbl.Length)
            {
                int maxDur = Tables.MaxDurTbl[_curPhon] / kFrameTime;
                _curPhonMaxDur = maxDur > 0 ? maxDur : 1;
                _curPhonPctOfMaxDur = ((long)_curPhonDur << 16) / _curPhonMaxDur;
                _curPhonPctOfMaxDur1 = (_curPhonPctOfMaxDur >> 1) + kOneHalf;
                _curPhonPctOfMaxDur2 = _curPhonPctOfMaxDur1 - (10L * k1pct);
            }
        }

        private void InitCtrlsForNewPhon()
        {
            SetPhonContext(_curPhonBufIndex);
            FillPhonTargets();

            for (_curBlockIndex = 0; _curBlockIndex < kNumOfBlocks; _curBlockIndex++)
            {
                var cb = _cb[_curBlockIndex];
                int bt = Tables.CtrlBlockTypeTbl[_curBlockIndex];

                cb.prevP_END_Targ = cb.curP_END_Targ;
                cb.nextP_START_Targ = (short)GetFirstTarget(_curPhonBufIndex + 1);
                cb.curTarget_OFFS = 0;
                cb.ptrToTargetList = -1;

                short rawTarg = GetTargetRaw(_curPhonBufIndex);
                if (rawTarg < kNoValue)
                {
                    // Diphthong envelope
                    GetDiphthongs(rawTarg & 0x7FFF);
                }
                else
                {
                    cb.curP_START_Targ = rawTarg;
                    cb.curTarget_STEP = 0;
                    cb.curTarget_TIME = _curPhonDur;

                    if (bt == kFreqType)
                    {
                        int artic = k1pct * 10;
                        if ((_curPhonCtrl & kStressField) != 0)
                            artic = (_curBlockIndex == kF2) ? k1pct * 25 : k1pct * 15;
                        cb.curP_START_Targ += (short)((((cb.prevP_END_Targ + cb.nextP_START_Targ) >> 1) - cb.curP_START_Targ) * artic >> 16);
                    }
                    cb.curP_END_Targ = cb.curP_START_Targ;
                }

                if (bt == kFreqType)
                    cb.nextP_START_Targ += (short)((cb.curP_END_Targ - cb.nextP_START_Targ) * (k1pct * 10) >> 16);

                // HEAD envelope
                _transLevel = (cb.prevP_END_Targ + cb.curP_START_Targ) >> 1;
                _transTime = 32 / kFrameTime;
                HeadRules(cb, bt);

                cb.HEAD_offs = 0; cb.HEAD_step = 0;
                if (_transTime > 0)
                {
                    cb.HEAD_offs = (_transLevel - cb.curP_START_Targ) << kStepSizeRes;
                    if (cb.HEAD_offs != 0)
                    {
                        int hs = (int)(((long)OvX(_transTime) * cb.HEAD_offs) >> 16);
                        cb.HEAD_step = hs;
                        cb.HEAD_offs = hs * _transTime;
                    }
                }

                // TAIL envelope
                _transLevel = (cb.curP_END_Targ + cb.nextP_START_Targ) >> 1;
                _transTime = 25 / kFrameTime;
                TailRules(cb, bt);

                cb.TAIL_offs = 0; cb.TAIL_step = 0;
                if (_transTime > 0)
                {
                    int ts = (_transLevel - cb.curP_END_Targ) << kStepSizeRes;
                    if (ts != 0)
                        cb.TAIL_step = (int)(((long)OvX(_transTime) * ts) >> 16);
                }
            }
            InsertBurst();
        }

        private void GetDiphthongs(int index)
        {
            var cb = _cb[_curBlockIndex];
            int bt = Tables.CtrlBlockTypeTbl[_curBlockIndex];

            short p1 = _envelopeListTbl[index];
            short t1 = _envelopeListTbl[index + 1];
            short p2 = _envelopeListTbl[index + 2];
            short t2 = _envelopeListTbl[index + 3];

            t1 = (short)ScalePrcnt(t1);
            t2 = (short)ScalePrcnt(t2);

            if (bt == kFreqType)
            {
                int artic = k1pct * 10;
                if (cb.prevP_END_Targ > 0) p1 += (short)(((cb.prevP_END_Targ - p1) * artic) >> 16);
                p1 += (short)AdjustColored(_curPhonBufIndex, 0);
                if (cb.nextP_START_Targ > 0) p2 += (short)(((cb.nextP_START_Targ - p2) * artic) >> 16);
                p2 += (short)AdjustColored(_curPhonBufIndex, 1);
            }

            int rampTime = t2 - t1;
            int diff = (p2 - p1) << kStepSizeRes;
            int step = rampTime > 0 ? (rampTime < kSizeOf1xTbl ? (int)(((long)OvX(rampTime) * diff) >> 16) : diff / rampTime) : 0;

            cb.curP_START_Targ = p1;
            cb.curTarget_TIME = t1;
            cb.curTarget_STEP = 0;
            cb.curP_END_Targ = p2;

            cb.ptrToTargetList = _nextDiphEntryIdx;
            _diphEntries[_nextDiphEntryIdx++] = (short)t2;
            _diphEntries[_nextDiphEntryIdx++] = (short)step;
            _diphEntries[_nextDiphEntryIdx++] = (short)_curPhonDur;
            _diphEntries[_nextDiphEntryIdx++] = 0;
        }

        private int ScalePrcnt(int pct)
        {
            long t = (pct * _curPhonPctOfMaxDur) >> 8;
            t = (_curPhonMaxDur * t / 100) >> 8;
            return t <= 0 ? 1 : (int)t;
        }

        private int AdjustColored(int index, int entry)
        {
            int cur = GP(index); int next = GP(index + 1); int prev = GP(index - 1);
            uint cf = PF(cur); uint nf = PF(next); uint pf = PF(prev);
            int ctrl = PC(index);
            int adj = 0;
            if (_curBlockIndex == kF3)
            {
                if ((cf & kVowel1F) != 0 && cur != _ER_ && ((pf & kLiqGlide2F) != 0 || (nf & kLiqGlide2F) != 0))
                    adj = -150;
            }
            else if (_curBlockIndex == kF2)
            {
                if (next == _LX_)
                {
                    if ((cf & kFrontF) != 0) adj = -150;
                    else if ((cur == _AY_ || cur == _OY_) && entry > 0) adj = -250;
                }
                if ((prev == _LX_ || prev == _l_ || prev == _w_) && (cf & kFrontF) != 0) adj = -150;
                if (cur == _UW_ && (pf & kAlveolarF) != 0) adj = 200;
                if (entry > 0 && (cur == _UW_ || cur == _YU_) && (nf & kAlveolarF) != 0) adj += 200;
                if ((ctrl & kStressField) != 0) adj >>= 1;
                else { adj += adj >> 1; if (entry > 0 && cur == _YU_) adj = 400; }
                if (adj > 400) adj = 400; else if (adj < -400) adj = -400;
            }
            return adj;
        }

        private void GetLocus(int iCons, int iVowel, int bType)
        {
            if (_curBlockIndex < kF1 || _curBlockIndex > kF3) return;
            int cons = GP(iCons); int vow = GP(iVowel);
            int vowRank, consRank;
            if (bType == C_V_type) { vowRank = Tables.Rank_FWD_Tbl[vow]; consRank = Tables.Rank_BKWD_Tbl[cons]; }
            else { vowRank = Tables.Rank_BKWD_Tbl[vow]; consRank = Tables.Rank_FWD_Tbl[cons]; }
            if (consRank != kConsonantR || vowRank == kConsonantR) return;

            uint vf = PF(vow); uint cf = PF(cons);
            bool f2y = (vf & kYGlideStartF) != 0;

            int v1Targ = (bType == C_V_type) ? GetFirstTarget(iVowel) : GetLastTarget(iVowel);

            int lociIdx = vowRank switch { kFrontR => Tables.Front_Loci_Tbl[cons], kMiddleR => Tables.Mid_Loci_Tbl[cons], _ => Tables.Back_Loci_Tbl[cons] };
            if (lociIdx == kNoValue) return;

            lociIdx = (lociIdx >> 1) + (_curBlockIndex - kF1) * 3;
            int lFreq = _lociTbl[lociIdx++] + _locusOffset;
            int lPcnt = _lociTbl[lociIdx++];
            _transTime = _lociTbl[lociIdx] / kFrameTime;

            if ((cf & kNasalF) == 0 && !f2y) _transTime -= _transTime >> 2;
            if (vowRank == kRoundR && _curBlockIndex != kF1 && (cf & (kDentalF | kPalatalF)) != 0)
                lPcnt = (lPcnt >> 1) + 50;
            if (f2y && _curBlockIndex == kF2) lPcnt = (25 - (lPcnt >> 2)) + lPcnt;

            _transLevel = lFreq + (lPcnt * (v1Targ - lFreq)) / 100;
        }

        private void HeadRules(ControlBlock cb, int bt)
        {
            if (bt == kFreqType)
            {
                if ((_curPhonFlags & kSonorant1F) != 0)
                {
                    if ((_curPhonFlags & kLiqGlideF) == 0)
                    {
                        _transTime = 45 / kFrameTime;
                        if ((_prevPhonFlags & kLiqGlideF) != 0)
                        {
                            _transLevel = (cb.prevP_END_Targ + _transLevel) >> 1;
                            if (_prevPhon == _l_ && _curBlockIndex == kF1) _transLevel += 80;
                            else if (_prevPhon == _r_ && _curBlockIndex != kF1) _transTime = 70 / kFrameTime;
                        }
                        else if (_curPhon == _h_) _transLevel = (cb.prevP_END_Targ + _transLevel) >> 1;
                    }
                    else
                    {
                        _transLevel = (cb.prevP_END_Targ + _transLevel) >> 1;
                        _transTime = 32 / kFrameTime;
                    }
                }
                if (_curPhon == _SIL_)
                {
                    _transLevel = cb.prevP_END_Targ; _transTime = _curPhonDur;
                }
                else
                {
                    GetLocus(_curPhonBufIndex - 1, _curPhonBufIndex, C_V_type);
                    GetLocus(_curPhonBufIndex, _curPhonBufIndex - 1, V_C_type);
                    if ((_prevPhonFlags & kStopF) != 0 && (_prevPhonFlags & kVoicedF) == 0 && _curBlockIndex == kF1)
                        _transLevel += 100;
                    if ((_curPhonFlags & kPlosFricF) != 0)
                    {
                        _transTime = (_curBlockIndex == kF1) ? 20 / kFrameTime : 30 / kFrameTime;
                        if ((_curPhonFlags & kStopF) != 0) _transTime = _curPhonDur;
                    }
                    if ((_curPhonFlags & kNasalF) != 0)
                    {
                        _transTime = (_curBlockIndex == kF1) ? 0 : _curPhonDur;
                        if ((_curPhon == _n_ || _curPhon == _EN_) && Tables.Rank_BKWD_Tbl[_prevPhon] == kFrontR)
                        {
                            if (_curBlockIndex == kF2) { _transLevel -= (_prevPhonFlags & kYGlideEndF) != 0 ? 200 : 100; }
                            else if (_curBlockIndex == kF3) _transLevel -= 100;
                        }
                        else if (_curPhon == _m_ && _curBlockIndex == kF2 && (_prevPhonFlags & kYGlideEndF) != 0)
                            _transLevel -= 150;
                    }
                }
                if ((_curPhonFlags & kPlosFricF) == 0 && Tables.Rank_BKWD_Tbl[_prevPhon] != kConsonantR && _transTime > 0)
                    _transTime = 1 + (int)((_curPhonPctOfMaxDur1 * _transTime) >> 16);
            }
            else if (bt == kFNZType)
            {
                if ((_prevPhonFlags & kNasalF) != 0 && (_curPhonFlags & kNasalF) == 0)
                { _transLevel = _nasalBaseFreq + ((_nasalTargFreq - _nasalBaseFreq) >> 1); _transTime = 80 / kFrameTime; }
                if ((_curPhonFlags & kNasalF) != 0) _transLevel = _nasalTargFreq;
            }
            else if (bt == kBWType)
            {
                if ((_curPhonFlags & kVoicedF) != 0)
                {
                    if ((_prevPhonFlags & kVoicedF) == 0 && _curBlockIndex == kBW1)
                    { _transTime = 50 / kFrameTime; _transLevel = (_cb[kF1].curP_START_Targ >> 3) + cb.curP_START_Targ; }
                    else _transTime = 40 / kFrameTime;
                }
                else _transTime = 20 / kFrameTime;

                if (_prevPhon == _SIL_)
                { _transLevel = (kBW3 - bt) * 50 + cb.curP_START_Targ; _transTime = 50 / kFrameTime; }
                else if (_curPhon == _SIL_)
                {
                    _transLevel = (kBW3 - bt) * 50 + cb.prevP_END_Targ;
                    if ((_prev2PhonFlags & kVoicedF) == 0 && (_prevPhonCtrl & kPlosive_Release) != 0 && _curBlockIndex == kBW1)
                        _transLevel = 250;
                    _transTime = 50 / kFrameTime;
                }
                if ((_prevPhonFlags & kNasalF) != 0)
                {
                    _transLevel = cb.curP_START_Targ;
                    if (_curBlockIndex == kBW2 && (_prevPhon == _n_ || _prevPhon == _EN_) && Tables.Rank_FWD_Tbl[_curPhon] != kFrontR)
                    { _transLevel += 60; _transTime = 60 / kFrameTime; }
                    else if (_curBlockIndex == kBW1) { _transLevel += 70; _transTime = 100 / kFrameTime; }
                }
                if ((_curPhonFlags & kNasalF) != 0) _transTime = 0;
            }
            else // kSourceAmpType / kResonAmpType
            {
                int ampT = cb.curP_START_Targ - 10;
                if (_transLevel < ampT || (_prevPhonFlags & kStopF) != 0 || _prevPhon == _JH_)
                {
                    _transLevel = ampT;
                    if ((_curPhonFlags & kPlosFricF) == 0) _transTime = 20 / kFrameTime;
                    if (_curBlockIndex == kAV)
                    {
                        if (_prevPhon == _SIL_ && (_curPhonFlags & kVoicedF) != 0)
                        { _transLevel -= 8; _transTime = 45 / kFrameTime; }
                        if ((_prevPhonFlags & kPlosFricF) != 0) _transLevel = ampT + 6;
                        if ((_prevPhonFlags & kStopF) != 0) _transLevel = cb.curP_START_Targ - 5;
                    }
                }
                if ((_curPhonFlags & kVoicedF) != 0 && (_prevPhonFlags & kNasalF) != 0) _transTime = 0;
                if ((_prevPhonFlags & kVoicedF) != 0 && (_curPhonFlags & kNasalF) != 0 && _curBlockIndex == kAV) _transTime = 0;
                ampT = cb.prevP_END_Targ - 10;
                if (_transLevel < ampT) { _transLevel = ampT - 3; if (_curPhon == _SIL_) _transTime = 70 / kFrameTime; }
                if (_curBlockIndex == kAp3 && (_curPhonFlags & kAffricateF) != 0)
                { _transTime = _curPhonDur - 2; _transLevel = cb.curP_START_Targ - 30; }
                if (_curBlockIndex == kAV && (_curPhonFlags & kPlosiveF) != 0) _transTime = 10 / kFrameTime;
                if (_curBlockIndex == kAF)
                {
                    if (_curPhon == _SIL_ || _curPhon == _f_ || _curPhon == _TH_ || _curPhon == _s_ || _curPhon == _SH_)
                    {
                        if ((_prevPhonFlags & kVoicedF) != 0 && (_prevPhonFlags & kPlosFricF) == 0)
                        {
                            if (_curPhon == _SIL_) { _transTime = 80 / kFrameTime; _transLevel = 52; }
                            else { _transTime = 45 / kFrameTime; _transLevel = 48; }
                        }
                    }
                }
            }
            if (_transTime > _curPhonDur) _transTime = _curPhonDur;
            if (_transTime > 130 / kFrameTime) _transTime = 130 / kFrameTime;
            if (_transTime < 0) _transTime = 0;
        }

        private void TailRules(ControlBlock cb, int bt)
        {
            if (bt == kFreqType)
            {
                if ((_curPhonFlags & kSonorant1F) != 0)
                {
                    _transTime = 45 / kFrameTime;
                    if ((_curPhonFlags & kLiqGlideF) == 0)
                    {
                        if ((_nextPhonFlags & kLiqGlideF) != 0)
                        {
                            if (_curBlockIndex == kF3) _transTime = 60 / kFrameTime;
                            if (_nextPhon == _l_ && _curBlockIndex == kF1) _transLevel += 80;
                        }
                        else if (_nextPhon == _h_) _transLevel = (cb.curP_END_Targ + _transLevel) >> 1;
                    }
                    else
                    {
                        if ((_nextPhonFlags & kLiqGlideF) == 0)
                        { _transLevel = (cb.curP_END_Targ + _transLevel) >> 1; _transTime = 20 / kFrameTime; }
                        else
                        { _transLevel = (cb.curP_END_Targ + _transLevel) >> 1; _transTime = 40 / kFrameTime; }
                    }
                }
                if (_nextPhon == _SIL_)
                {
                    _transTime = 0;
                }
                else
                {
                    GetLocus(_curPhonBufIndex + 1, _curPhonBufIndex, V_C_type);
                    GetLocus(_curPhonBufIndex, _curPhonBufIndex + 1, C_V_type);
                    if ((_curPhonFlags & kPlosFricF) != 0)
                    {
                        _transTime = (_curBlockIndex == kF1) ? 20 / kFrameTime : 30 / kFrameTime;
                        if ((_curPhonFlags & kStopF) != 0)
                        {
                            _transTime = _curPhonDur;
                            if ((_curPhonFlags & kVoicedF) == 0 && _curBlockIndex == kF1) _transLevel += 100;
                        }
                    }
                    if ((_curPhonFlags & kNasalF) != 0)
                    {
                        _transTime = (_curBlockIndex == kF1) ? 0 : _curPhonDur;
                        if ((_curPhon == _n_ || _curPhon == _EN_) && Tables.Rank_FWD_Tbl[_nextPhon] == kFrontR)
                        {
                            if (_curBlockIndex == kF2) { _transLevel -= 100; if ((_nextPhonFlags & kYGlideStartF) != 0) _transLevel -= 100; }
                            else if (_curBlockIndex == kF3) _transLevel -= 100;
                        }
                        else if (_curPhon == _m_ && _curBlockIndex == kF2 && (_nextPhonFlags & kYGlideStartF) != 0) _transLevel -= 150;
                    }
                }
                if ((_curPhonFlags & kPlosFricF) == 0 && Tables.Rank_FWD_Tbl[_nextPhon] != kConsonantR && _transTime > 0)
                    _transTime = 1 + (int)((_curPhonPctOfMaxDur2 * _transTime) >> 16);
            }
            else if (bt == kFNZType)
            {
                if ((_nextPhonFlags & kNasalF) != 0 && (_curPhonFlags & kNasalF) == 0)
                { _transLevel = _nasalTargFreq; _transTime = 80 / kFrameTime; }
            }
            else if (bt == kBWType)
            {
                if ((_curPhonFlags & kVoicedF) != 0)
                {
                    _transTime = 40 / kFrameTime;
                    if ((_nextPhonFlags & kVoicedF) == 0 && _curBlockIndex == kBW1)
                    { _transTime = 50 / kFrameTime; _transLevel = (_cb[kF1].curP_START_Targ >> 3) + cb.curP_END_Targ; }
                }
                else _transTime = 20 / kFrameTime;
                if (_nextPhon == _SIL_)
                { _transLevel = (kBW3 - bt) * 50 + cb.curP_END_Targ; _transTime = 50 / kFrameTime; }
                else if (_curPhon == _SIL_)
                { _transLevel = (kBW3 - bt) * 50 + cb.nextP_START_Targ; _transTime = 50 / kFrameTime; }
                if ((_nextPhonFlags & kNasalF) != 0)
                {
                    _transLevel = cb.curP_END_Targ;
                    if (_curBlockIndex == kBW2 && (_nextPhon == _n_ || _nextPhon == _EN_) && Tables.Rank_FWD_Tbl[_curPhon] != kFrontR)
                    { _transLevel += 60; _transTime = 60 / kFrameTime; }
                    else if (_curBlockIndex == kBW1) { _transLevel += 100; _transTime = 100 / kFrameTime; }
                }
                if ((_curPhonFlags & kNasalF) != 0) _transTime = 0;
            }
            else // kSourceAmpType / kResonAmpType
            {
                int ampT = cb.nextP_START_Targ - 10;
                if (_transLevel < ampT) { _transLevel = ampT; if (_curPhon == _SIL_) _transTime = 70 / kFrameTime; }

                bool gotoEnd = false;
                if (_curBlockIndex == kAV && _transLevel < cb.nextP_START_Targ)
                {
                    if (_curPhon != _v_ && _curPhon != _DH_ && _curPhon != _JH_ && _curPhon != _ZH_ && _curPhon != _z_)
                    {
                        _transTime = 0;
                        if ((_curPhonFlags & (kStopF | kAffricateF)) != 0)
                        {
                            if ((_curPhonFlags & kVoicedF) != 0)
                            { _transLevel = cb.curP_END_Targ - 3; _transTime = 45 / kFrameTime; }
                            else _transTime = 0;
                            gotoEnd = true;
                        }
                    }
                }
                if (!gotoEnd)
                {
                    if ((_curPhonFlags & kVoicedF) != 0 && (_nextPhonFlags & kNasalF) != 0) _transTime = 0;
                    if ((_curPhonFlags & kNasalF) != 0)
                    {
                        bool nextVoicedNonStop = (_nextPhonFlags & kVoicedF) != 0 && (_curPhonFlags & kPlosFricF) == 0 && (_nextPhonCtrl & kPlosive_Release) == 0;
                        _transTime = nextVoicedNonStop ? 0 : 40 / kFrameTime;
                    }
                    ampT = cb.curP_END_Targ - 10;
                    if ((_curPhonFlags & kPlosiveF) != 0)
                    {
                        _transTime = 15 / kFrameTime;
                        if ((_curPhonFlags & kStopF) != 0 || _curPhon == _DX_ || _curPhon == _QX_ || _curPhon == _DD_)
                            ampT = cb.curP_END_Targ;
                    }
                    if (_transLevel < ampT) { _transLevel = ampT - 3; _transTime = 20 / kFrameTime; }
                    if (_curBlockIndex == kAV)
                    {
                        if (_transLevel < ampT || (ampT > 0 && (_nextPhonCtrl & kPlosive_Release) != 0))
                        {
                            _transLevel = ampT + 3;
                            if (_nextPhon == _SIL_ || (_nextPhonCtrl & kPlosive_Release) != 0) _transTime = 75 / kFrameTime;
                        }
                    }
                    if (_nextPhon >= _p_)
                    {
                        if ((_curPhonFlags & kNasalF) == 0 || _curBlockIndex != kAV) _transTime = 0;
                    }
                    if (_curBlockIndex == kAF)
                    {
                        if (_curPhon == _f_ || _curPhon == _TH_ || _curPhon == _s_ || _curPhon == _SH_)
                        {
                            if ((_nextPhonFlags & kVoicedF) != 0 && (_nextPhonFlags & kPlosFricF) == 0)
                            { _transTime = 40 / kFrameTime; _transLevel = 52; }
                        }
                        if ((_curPhonFlags & kVowelF) != 0 && _nextPhon == _SIL_)
                        { _transTime = 130 / kFrameTime; _transLevel = 52; }
                    }
                }
            }
            if (_transTime > _curPhonDur) _transTime = _curPhonDur;
            if (_transTime > 130 / kFrameTime) _transTime = 130 / kFrameTime;
            _cb[_curBlockIndex].TAIL_START_time = _curPhonDur - _transTime;
            if (_transTime < 0) _transTime = 0;
        }

        private void InsertBurst()
        {
            if ((_curPhonFlags & kPlosiveF) != 0)
            {
                int burstDur = Tables.BurstDurTbl[_curPhon] / kFrameTime;
                if ((_curPhonFlags & kStopF) != 0 && (_curPhonFlags & kVoicedF) == 0)
                {
                    if ((_nextPhonFlags & (kStopF | kNasalF)) != 0)
                        burstDur = (_nextPhonCtrl & kPrimOrEmphStress) != 0 ? 0 : burstDur >> 1;
                }
                int closureDur = _curPhonDur - burstDur;
                if ((_curPhonFlags & kAffricateF) != 0 && closureDur > 80 / kFrameTime) closureDur = 80 / kFrameTime;
                for (int i = kAp2; i <= kAB; i++) { _cb[i].onset_END_TIME = closureDur; _cb[i].onset_VAL = 0; }
            }

            if ((_prevPhonFlags & kStopF) != 0 && (_prevPhonFlags & kVoicedF) == 0 && (_curPhonFlags & kSonorant1F) != 0)
            {
                int rel = 40 / kFrameTime;
                _cb[kAV].onset_VAL = 0;
                _cb[kAF].onset_VAL = (short)(Tables.Rank_FWD_Tbl[_nextPhon] == kFrontR ? 48 : 54);
                if ((_curPhonCtrl & kVowelF) == 0) { rel = 25 / kFrameTime; _cb[kAF].onset_VAL -= 3; }
                if ((_curPhonCtrl & kLiqGlideF) != 0 || _curPhon == _ER_) _cb[kAF].onset_VAL += 3;
                if (_prev2Phon == _s_)
                {
                    if ((_prev2PhonCtrl & kSyllableTypeField) == 0) rel = 10 / kFrameTime;
                }
                else if ((_curPhonCtrl & kVowelF) == 0) rel += 20 / kFrameTime;
                if (rel >= _curPhonDur) rel = _curPhonDur - 1;
                if (rel > (_curPhonDur >> 1) && (_curPhonFlags & kVowelF) != 0 && (_curPhonCtrl & kPrimOrEmphStress) != 0)
                    rel = _curPhonDur >> 1;
                if ((_curPhonCtrl & kPlosive_Release) != 0) { rel = _curPhonDur; _cb[kAF].onset_VAL = 0; }
                _cb[kAV].onset_END_TIME = _cb[kAF].onset_END_TIME = _cb[kBW1].onset_END_TIME = _cb[kBW2].onset_END_TIME = rel;
                _cb[kBW1].onset_VAL = (short)(_cb[kBW1].curP_START_Targ + 250);
                _cb[kBW2].onset_VAL = (short)(_cb[kBW2].curP_START_Targ + 70);
            }

            if ((_curPhonFlags & kStopF) != 0 && (_curPhonFlags & kVoicedF) != 0 &&
                (_prevPhonFlags & kVoicedF) != 0 && (_nextPhonFlags & kVoicedF) == 0 && _curPhon != _TX_)
            {
                _cb[kAV].onset_END_TIME = _curPhonDur - (10 / kFrameTime);
                _cb[kBW1].onset_END_TIME = _cb[kBW2].onset_END_TIME = _cb[kBW3].onset_END_TIME = _curPhonDur;
                _cb[kAV].onset_VAL = 53;
                _cb[kBW1].onset_VAL = 1000; _cb[kBW2].onset_VAL = 1000; _cb[kBW3].onset_VAL = 1200;
            }
        }

        private void InterpolateFormants()
        {
            // F1-FNZ: combined offset shifted at end
            for (int i = kF1; i <= kFNZ; i++)
            {
                var cb = _cb[i];
                if (cb.ptrToTargetList >= 0 && _durDoneInPhon > cb.curTarget_TIME)
                {
                    int p = cb.ptrToTargetList;
                    cb.curTarget_TIME = _diphEntries[p++];
                    cb.curTarget_STEP = _diphEntries[p++];
                    cb.ptrToTargetList = p;
                    cb.curP_START_Targ += (short)(cb.curTarget_OFFS >> kStepSizeRes);
                    cb.curTarget_OFFS = 0;
                }
                cb.curTarget_OFFS += cb.curTarget_STEP;

                int offset = cb.curTarget_OFFS + cb.HEAD_offs;
                if (cb.HEAD_offs != 0) cb.HEAD_offs -= cb.HEAD_step;
                if (_durDoneInPhon >= cb.TAIL_START_time) { offset += cb.TAIL_offs; cb.TAIL_offs += cb.TAIL_step; }

                _controlData[i] = (short)(cb.curP_START_Targ + (offset >> kStepSizeRes));
            }

            // AV-AB: HEAD and TAIL shifted separately (matches C's SaveFrame loop)
            for (int i = kAV; i <= kAB; i++)
            {
                var cb = _cb[i];
                int val = cb.curP_START_Targ + (cb.HEAD_offs >> kStepSizeRes);
                if (cb.HEAD_offs != 0) cb.HEAD_offs -= cb.HEAD_step;
                if (_durDoneInPhon >= cb.TAIL_START_time) { val += cb.TAIL_offs >> kStepSizeRes; cb.TAIL_offs += cb.TAIL_step; }
                _controlData[i] = (short)val;

                if (cb.onset_END_TIME > 0)
                {
                    if (_durDoneInPhon < cb.onset_END_TIME)
                        _controlData[i] = cb.onset_VAL;
                    else if (i >= kAp2 && _durDoneInPhon == cb.onset_END_TIME + 1 && _controlData[i] > 10)
                        _controlData[i] -= 10;
                }
            }
        }

        // Returns raw table value: >=0 direct Hz, -1 kNoValue, <-1 diphthong
        private short GetTargetRaw(int index)
        {
            int bt = Tables.CtrlBlockTypeTbl[_curBlockIndex];
            int cur = GP(index); uint cf = PF(cur); int ctrl = PC(index);
            int next = GP(index + 1); uint nf = PF(next);
            int prev = GP(index - 1); uint pf = PF(prev);
            short tv = -1;

            if (bt == kFreqType || bt == kBWType)
            {
                short[] tbl = GetVoiceFormantTable(_curBlockIndex);
                tv = tbl[cur];
                if (tv < kNoValue) return tv; // diphthong: return raw

                if (tv == kNoValue)
                {
                    tv = tbl[next];
                    if (tv == kNoValue)
                    {
                        tv = tbl[GP(index + 2)];
                        if (tv == kNoValue)
                        {
                            tv = tbl[prev];
                            if (tv < 0 && tv != kNoValue) tv = _envelopeListTbl[(tv & 0x7FFF) + 2];
                            if (tv == kNoValue) tv = Tables.DefaultTargTbl[_curBlockIndex];
                        }
                    }
                    if (tv < kNoValue) tv = _envelopeListTbl[tv & 0x7FFF];
                    if (_curBlockIndex == kF1 && (cf & kPlosFricF) != 0 && (cf & kObstF) == 0 && (pf & kVowelF) != 0) tv += 40;
                }
                if ((cur == _n_ || cur == _EN_) && _curBlockIndex == kBW2 && Tables.Rank_FWD_Tbl[next] != kFrontR) tv += 60;
                if ((cur == _n_ || cur == _EN_ || cur == _NG_) && _curBlockIndex == kBW3 &&
                    ((nf & kYGlideStartF) != 0 || (pf & kYGlideEndF) != 0)) tv = (short)kMaxBandWidth;
            }
            else if (bt == kFNZType)
                tv = (short)(((cf & kNasalF) != 0) ? _nasalTargFreq : _nasalBaseFreq);
            else if (bt == kSourceAmpType)
            {
                if (_curBlockIndex == kAV)
                {
                    tv = _voiceAvTbl[cur];
                    if ((ctrl & kPlosive_Release) != 0) tv -= (short)(((pf & kNasalF) != 0) ? 6 : 20);
                    if ((cf & kStopF) != 0 && (pf & kVoicedF) == 0) tv = 0;
                    if (cur == _h_ && (pf & kVoicedF) != 0 && (ctrl & kPrimOrEmphStress) == 0) tv = 54;
                }
                else if (cur == _h_)
                {
                    tv = (short)(Tables.Rank_FWD_Tbl[next] == kFrontR ? 58 : 62);
                    if ((ctrl & kStressField) == 0) tv -= 1;
                }
                else tv = 0;
            }
            else if (bt == kResonAmpType)
            {
                tv = Tables.NoiseIndexTbl[cur];
                if (tv == kNoValue) tv = 0;
                else
                {
                    int rank = (next == _SIL_) ? Tables.Rank_BKWD_Tbl[prev] : Tables.Rank_FWD_Tbl[next];
                    if (rank == kRoundR) rank = kBackR;
                    int idx2 = tv + (_curBlockIndex - kAp2) + rank * 6;
                    tv = _voiceNoiseAmpTbl[idx2];
                    if ((PC(index + 1) & kPlosive_Release) != 0 && tv >= 4) tv -= 4;
                }
            }
            return tv;
        }

        private int GetFirstTarget(int index)
        {
            short t = GetTargetRaw(index);
            if (t < kNoValue)
            {
                int i = t & 0x7FFF;
                t = _envelopeListTbl[i];
                if (Tables.CtrlBlockTypeTbl[_curBlockIndex] == kFreqType) t += (short)AdjustColored(index, 0);
            }
            return t;
        }

        private int GetLastTarget(int index)
        {
            short t = GetTargetRaw(index);
            if (t < kNoValue)
            {
                int i = (t & 0x7FFF) + 2;
                t = _envelopeListTbl[i];
                if (Tables.CtrlBlockTypeTbl[_curBlockIndex] == kFreqType) t += (short)AdjustColored(index, 1);
            }
            return t;
        }

        private short[] GetVoiceFormantTable(int bi)
        {
            bool m = _voice.VoiceType == 0;
            return bi switch
            {
                kF1 => m ? Tables.f1FreqTblM : Tables.f1FreqTblF,
                kF2 => m ? Tables.f2FreqTblM : Tables.f2FreqTblF,
                kF3 => m ? Tables.f3FreqTblM : Tables.f3FreqTblF,
                kBW1 => m ? Tables.b1FreqTblM : Tables.b1FreqTblF,
                kBW2 => m ? Tables.b2FreqTblM : Tables.b2FreqTblF,
                kBW3 => m ? Tables.b3FreqTblM : Tables.b3FreqTblF,
                _ => throw new ArgumentException()
            };
        }

        // Short helpers
        private int GP(int i) { if (i >= 0 && i < _dump.PhonBuf2.Length) return _dump.PhonBuf2[i]; return _SIL_; }
        private uint PF(int p) { if (p >= 0 && p < Tables.PhonFlags2.Length) return Tables.PhonFlags2[p]; return 0; }
        private int PC(int i) { if (i >= 0 && i < _dump.PhonCtrlBuf2.Length) return (int)_dump.PhonCtrlBuf2[i]; return 0; }
        private int OvX(int x) { if (x <= 0) return 0; if (x < kSizeOf1xTbl) return (int)Tables.One_Over_X_Tbl[x]; return (int)(65536L / x); }

        public static short LogToLin(short v)
        {
            if (v > 63) v = 63;
            if (v < 0) return 0;
            return Tables.LogToLin[v >> 1];
        }

        private Frame SaveFrame(short f0, byte phonCtrl)
        {
            var f = new Frame();
            f.F0 = f0;
            short curF1 = _controlData[kF1];
            short curF2 = _controlData[kF2];
            short curF3 = _controlData[kF3];
            while (curF2 - curF1 < 200) curF1 -= 10;
            while (curF3 - curF2 < 600) curF3 += 10;
            f.F1 = SynthesizerKlatt.HzToPitch(curF1);
            f.F2 = SynthesizerKlatt.HzToPitch(curF2);
            f.F3 = SynthesizerKlatt.HzToPitch(curF3);
            f.Bw1 = (short)((_controlData[kBW1] * _voiceBWgain1) >> 16);
            f.Bw2 = (short)((_controlData[kBW2] * _voiceBWgain2) >> 16);
            f.Bw3 = (short)((_controlData[kBW3] * _voiceBWgain3) >> 16);
            f.FNZ = SynthesizerKlatt.HzToPitch(_controlData[kFNZ]);
            f.Av = LogToLin(_controlData[kAV]);
            f.Af = LogToLin(_controlData[kAF]);
            f.A2 = LogToLin(_controlData[kAp2]);
            f.A3 = LogToLin(_controlData[kAp3]);
            f.A4 = LogToLin(_controlData[kAp4]);
            f.A5 = LogToLin(_controlData[kAp5]);
            f.A6 = LogToLin(_controlData[kAp6]);
            f.AB = LogToLin(_controlData[kAB]);
            f.PhonEdge = (short)(_durDoneInPhon == 0 ? 1 : 0);
            return f;
        }
    }
}  // namespace
