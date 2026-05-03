#nullable enable

namespace SharpTalk
{

    public sealed partial class AudioProcessor
    {
        // Pitch_RaiseAndFall

        private void PitchRaiseAndFall()
        {
            const int kFallen = 0, kRaised = 1, kStart = 2, kFinished = 3;

            int pState = kStart, lastState = kStart;
            int wdIndex = 0, firstWord = 0, lastWord = 0;
            long[] wdType = new long[64];
            int stressCount = 1;

            for (int index = 0; index < _phonBuf2InIndex; index++)
            {
                short curPhon = _phonBuf2[index];
                long curCtrl = _phonCtrlBuf2[index];
                uint curFlags = Tables.PhonFlags2[curPhon];

                // Clause boundary (comma, semicolon, colon): restart pitch contour for next clause.
                if ((curCtrl & kSilenceTypeField) != 0)
                {
                    pState = kStart;
                    wdIndex = 0; firstWord = 0; lastWord = 0; stressCount = 1;
                    continue;
                }

                if (pState == kRaised && (curCtrl & kBoundryTypeField) == kWord_Start)
                {
                    wdType[wdIndex] = (curCtrl & kContent_Word) != 0 ? kPitchRise1 : kPitchFall1;
                    if (wdIndex < 63) wdIndex++;
                    stressCount = 0;
                    lastWord = index;
                    if (lastState == kStart && pState == kRaised)
                    { lastState = kRaised; firstWord = index; }
                }

                if ((curFlags & kVowelF) != 0)
                {
                    if (pState == kStart)
                    {
                        if (CountVowelsTillBoundry(kTerm_End, index) == 0)
                        {
                            _phonCtrlBuf2[index] |= kPitchFall;
                            pState = kFinished;
                            break;
                        }
                        else if (CountStressVowelsTillBoundry(kTerm_End, index) == 0)
                        {
                            _phonCtrlBuf2[index] |= kPitchFall;
                            pState = kFinished;
                        }
                        else if ((curCtrl & kIsStressed) != 0)
                        {
                            _phonCtrlBuf2[index] |= kPitchRise;
                            pState = kRaised;
                        }
                    }
                    else if (pState == kRaised)
                    {
                        if ((curCtrl & kPrimOrEmphStress) != 0) stressCount++;

                        if (CountVowelsTillBoundry(kTerm_End, index) == 0)
                        {
                            _phonCtrlBuf2[index] |= kPitchFall;
                            pState = kFallen;
                        }
                        else if ((curCtrl & kPrimOrEmphStress) != 0 &&
                                 CountStressVowelsTillBoundry(kTerm_End, index) == 0)
                        {
                            _phonCtrlBuf2[index] |= kPitchFall;
                            pState = kFallen;
                        }
                    }
                }
            }

            wdIndex -= 2;
            if (wdIndex > 1 && pState != kFinished)
            {
                pState = kFallen;
                for (int i = 0; i < wdIndex; i++)
                {
                    if (pState == kFallen) { wdType[i] = kPitchRise1; pState = kRaised; }
                    else { wdType[i] = kPitchFall1; pState = kFallen; }
                }
                if (pState == kRaised)
                { wdType[wdIndex] = kPitchFall1; wdIndex++; }

                bool action = false;
                int wi = 0;
                for (int index = firstWord; index < lastWord; index++)
                {
                    short curPhon = _phonBuf2[index];
                    long curCtrl = _phonCtrlBuf2[index];
                    uint curFlags = Tables.PhonFlags2[curPhon];

                    if ((curCtrl & kBoundryTypeField) == kWord_Start) action = true;

                    if ((curFlags & kVowelF) != 0 && action)
                    {
                        if (!AnyStressVowelsRemain(index))
                        {
                            action = false;
                            if (wi < wdIndex) _phonCtrlBuf2[index] |= wdType[wi];
                            wi++;
                        }
                    }
                }
            }
        }

        private int CountVowelsTillBoundry(long boundary, int curIndex)
        {
            int count = 0;
            for (int i = curIndex; i < _phonBuf2InIndex; i++)
            {
                if (i != curIndex && (PhonFlags2Safe(_phonBuf2[i]) & kVowelF) != 0) count++;
                if ((_phonCtrlBuf2[i] & kSyllableTypeField) >= boundary) break;
            }
            return count;
        }

        private int CountStressVowelsTillBoundry(long boundary, int curIndex)
        {
            int count = 0;
            for (int i = curIndex; i < _phonBuf2InIndex; i++)
            {
                if (i != curIndex &&
                    (_phonCtrlBuf2[i] & kPrimOrEmphStress) != 0 &&
                    (PhonFlags2Safe(_phonBuf2[i]) & kVowelF) != 0)
                    count++;
                if ((_phonCtrlBuf2[i] & kSyllableTypeField) >= boundary) break;
            }
            return count;
        }

        private bool AnyStressVowelsRemain(int curIndex)
        {
            for (int i = curIndex + 1; i < _phonBuf2InIndex; i++)
            {
                if ((_phonCtrlBuf2[i] & kBoundryTypeField) == kWord_Start) break;
                if ((_phonCtrlBuf2[i] & kPrimOrEmphStress) != 0 &&
                    (PhonFlags2Safe(_phonBuf2[i]) & kVowelF) != 0)
                    return true;
            }
            return false;
        }

        static uint PhonFlags2Safe(short p) =>
            (p >= 0 && p < Tables.PhonFlags2.Length) ? Tables.PhonFlags2[p] : 0u;

        // Calc_Ramp_Steps

        private void CalcRampSteps()
        {
            const int kRampMode = 0; // const int kSusMode = 1;
            int rampIndex = 0, mode = kRampMode, accum = 1;

            for (int i = 0; i < _phonBuf2InIndex; i++)
            {
                long curCtrl = GetCtrl2(i);
                long curSylType = curCtrl & kSyllableTypeField;
                short curDur = _durBuf[i];

                if (mode == kRampMode)
                {
                    if ((curCtrl & kSilenceTypeField) != 0 || (curSylType & kTerm_End) != 0)
                    {
                        long step = ((long)(_baselineFallStart - _baselineFallEnd) << 16) / accum;
                        if ((curSylType & kTerm_End) != 0)
                        {
                            if (_endPunctuation == _Comma_ || _endPunctuation == _Quest_)
                                step >>= 1;
                        }
                        if (rampIndex < kMaxRamps)
                            _rampSteps[rampIndex++] = step;
                        accum = 1;
                    }
                    else
                    {
                        accum += curDur;
                    }
                }
            }

            _curRamp = 0;
        }

        // Fill_Pitch_Buf

        private void FillPitchBuf()
        {
            bool pitchIsFallen = true;
            _pitchBufInIndex = 0;
            int stressCounter = 0;
            int curBaseline = 0;
            _pitchTimeOffset = 0;
            short raiseAmt = 0, fallAmt = 0, raiseAmt1 = 0, fallAmt1 = 0;

            for (int i = 0; i < _phonBuf2InIndex; i++)
            {
                short curPhon = GetPhon2(i);
                long curCtrl = GetCtrl2(i);
                uint curFlags = Tables.PhonFlags2[curPhon];
                long curStress = curCtrl & kStressField;
                long curSylType = curCtrl & kSyllableTypeField;
                short curDur = _durBuf[i];

                long prevCtrl = GetCtrl2(i - 1);

                // Phrase reset after silence boundary — must happen before pitch processing
                // so the subsequent pitch rise isn't cancelled by the baseline wipe.
                if (((prevCtrl & kSilenceTypeField) >> kSilenceTypeShift) != 0)
                {
                    StoreF0AndTime((short)(0 - curBaseline), 0, kPhraseReset);
                    curBaseline = 0;
                    pitchIsFallen = true;
                }

                if ((curFlags & kVowelF) != 0)
                {
                    // PITCH RISE
                    if ((curCtrl & kPitchRise) != 0 && pitchIsFallen)
                    {
                        raiseAmt = _vpRiseAmt;
                        if (_endPunctuation == _Quest_) raiseAmt >>= 1;
                        short timeT = (curCtrl & kPitchFall) != 0
                            ? (short)((-80) / kFrameTime)
                            : (short)0;
                        StoreF0AndTime(raiseAmt, timeT, kPitchRiseFall_Flg);
                        curBaseline += raiseAmt;
                        pitchIsFallen = false;
                    }

                    // PITCH RISE1 / FALL1
                    if ((curCtrl & kPitchRise1) != 0)
                    {
                        raiseAmt1 = _vpRiseAmt1;
                        if (_endPunctuation == _Quest_) raiseAmt1 >>= 1;
                        StoreF0AndTime(raiseAmt1, 0, kPitchRiseFall1_Flg);
                    }
                    else if ((curCtrl & kPitchFall1) != 0)
                    {
                        fallAmt1 = _vpFallAmt1;
                        StoreF0AndTime(fallAmt1, 0, kPitchRiseFall1_Flg);
                    }

                    // PRIMARY / EMPHATIC STRESS
                    if ((curStress & kPrimOrEmphStress) != 0)
                    {
                        short pitchT;
                        if (curStress == kEmphaticStress)
                            pitchT = kHZ_28;
                        else
                            pitchT = kHZ_14;

                        pitchT += stressCounter switch
                        {
                            0 => kHZ_10,
                            1 => kHZ_9,
                            2 => kHZ_6,
                            3 => kHZ_4,
                            _ => 0,
                        };

                        if (_endPunctuation == _Quest_) pitchT >>= 1;

                        short timeT;
                        if ((curCtrl & kPitchFall) != 0 || (curSylType & kTerm_End) != 0)
                            timeT = (short)((-60) / kFrameTime);
                        else if (curStress == kEmphaticStress)
                            timeT = 0;
                        else
                            timeT = (short)(curDur >> 2);

                        pitchT = (short)((_vpStressGain * pitchT) >> 16);

                        if ((curSylType & kTerm_End) != 0 && curStress != kEmphaticStress)
                            pitchT = (short)(0 - kHZ_4);

                        StoreF0AndTime(pitchT, timeT, kPitchStress_Flg);
                        stressCounter++;
                    }

                    // PITCH FALL
                    if ((curCtrl & kPitchFall) != 0)
                    {
                        short timeT = (short)(curDur - (160 / kFrameTime));
                        if (timeT < 25 / kFrameTime) timeT = (short)(25 / kFrameTime);

                        if ((curSylType & kTerm_End) != 0)
                        {
                            fallAmt = _endPunctuation switch
                            {
                                _Comma_ => (short)(0 - kHZ_12),
                                _Period_ => (short)(0 - kHZ_20),
                                _Quest_ => (short)(0 - kHZ_7),
                                _Exclam_ => (short)(0 - kHZ_20),
                                _ => (short)(0 - kHZ_12),
                            };
                        }
                        else if ((curSylType & kVerb_End) != 0)
                        {
                            fallAmt = 0;
                        }
                        else
                        {
                            fallAmt = _vpFallAmt;
                        }

                        fallAmt = (short)(((long)_vpAssertiveness * fallAmt >> 16) - raiseAmt);
                        StoreF0AndTime(fallAmt, timeT, kPitchRiseFall_Flg);
                        curBaseline += fallAmt;
                        pitchIsFallen = true;
                    }

                    // Raise-type boundary (comma or question)
                    if ((curSylType & kTerm_End) != 0 &&
                        (_endPunctuation == _Comma_ || _endPunctuation == _Quest_))
                    {
                        if (_endPunctuation == _Quest_)
                        {
                            StoreF0AndTime(kHZ_18, 0, kPitchBoundry_Flg);
                            StoreF0AndTime(kHZ_25, curDur, kPitchBoundry_Flg);
                        }
                        else
                        {
                            StoreF0AndTime(kHZ_7, 0, kPitchBoundry_Flg);
                            StoreF0AndTime(kHZ_10, curDur, kPitchBoundry_Flg);
                        }
                    }
                }

                _pitchTimeOffset += curDur;
            }
        }

        private void StoreF0AndTime(short pitch, short time, short flags)
        {
            if (_pitchTimeOffset + time >= 0)
            {
                _pitchBufTime[_pitchBufInIndex] = (short)(_pitchTimeOffset + time);
                _pitchTimeOffset = 0 - time;
            }
            else
            {
                _pitchBufTime[_pitchBufInIndex] = 0;
            }
            _pitchBufFreq[_pitchBufInIndex] = pitch;
            _pitchBufFlags[_pitchBufInIndex] = flags;
            if (_pitchBufInIndex < kPitchBufSize - 1)
                _pitchBufInIndex++;
        }

        // StartNew_PitchClause

        private void StartNewPitchClause()
        {
            _baselineStartOffset = _baselineFallStart;
            _baselineEndOffset = _baselineFallEnd;
            // (start_of_Paragraph_Flag adjustments omitted – not paragraph start)
        }

        private const uint kHasReleaseF = 1u << 23;
        private const uint kFrontF_BE = 1u << 21;
        private const long kPlosive_Release = 0x4000;

        private void InsertPlosiveRelease()
        {
            if (_singing) return;
            for (int i = 0; i < _phonBuf2InIndex; i++)
            {
                short cur = _phonBuf2[i];
                short next = i + 1 < _phonBuf2InIndex ? _phonBuf2[i + 1] : _SIL_;
                if (next != _SIL_) continue;

                uint curFlags = Tables.PhonFlags2[cur >= 0 && cur < Tables.PhonFlags2.Length ? cur : 0];
                if ((curFlags & kHasReleaseF) == 0) continue;
                if (_phonBuf2InIndex >= kPhonBuf_Red_Zone) break;

                // Shift everything after i+1 up by one slot
                for (int k = _phonBuf2InIndex; k > i + 1; k--)
                {
                    _phonBuf2[k] = _phonBuf2[k - 1];
                    _phonCtrlBuf2[k] = _phonCtrlBuf2[k - 1];
                    _durBuf[k] = _durBuf[k - 1];
                    _userPitchBuf2[k] = _userPitchBuf2[k - 1];
                    _userDurBuf2[k] = _userDurBuf2[k - 1];
                    _userNoteBuf2[k] = _userNoteBuf2[k - 1];
                    _userRateBuf2[k] = _userRateBuf2[k - 1];
                }
                _phonBuf2InIndex++;

                // Decide IX vs AX: IX for /t/ or /d/, or if prev phoneme is front
                short prevPhon = i > 0 ? _phonBuf2[i - 1] : _SIL_;
                uint prevFlags = Tables.PhonFlags2[prevPhon >= 0 && prevPhon < Tables.PhonFlags2.Length ? prevPhon : 0];
                bool useIX = (cur == _t_ || cur == _d_) || ((prevFlags & kFrontF_BE) != 0);
                _phonBuf2[i + 1] = useIX ? _IX_ : _AX_;
                _phonCtrlBuf2[i + 1] = _phonCtrlBuf2[i] | kPlosive_Release;
                _durBuf[i + 1] = 25 / kFrameTime;
                _userPitchBuf2[i + 1] = _userPitchBuf2[i];
                _userDurBuf2[i + 1] = kDur_One;
                _userNoteBuf2[i + 1] = 0;
                _userRateBuf2[i + 1] = 0;

                i++; // skip over the inserted release slot
            }
        }
    }
}  // namespace
