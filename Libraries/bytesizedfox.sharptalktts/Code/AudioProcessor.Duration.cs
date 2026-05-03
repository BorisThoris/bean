#nullable enable
using System;

namespace SharpTalk
{

    public sealed partial class AudioProcessor
    {
        // Mod_Duration

        private void ModDuration()
        {
            _durBuf[0] = 1; // initial SIL = 5ms (1 frame)

            short prevPhon = _SIL_;
            long prevCtrl = 0;
            bool eFlag = false;

            for (int i = 1; i < _phonBuf2InIndex; i++)
            {
                short curPhon = GetPhon2(i);
                long curCtrl = GetCtrl2(i);
                long curSylType = curCtrl & kSyllableTypeField;
                long curStress = curCtrl & kStressField;
                uint curFlags = Tables.PhonFlags2[curPhon];
                bool curIsVowel = (curFlags & kVowelF) != 0;

                prevPhon = GetPhon2(i - 1);
                prevCtrl = GetCtrl2(i - 1);
                uint prevFlags = Tables.PhonFlags2[prevPhon];

                short nextPhon = GetPhon2(i + 1);
                long nextCtrl = GetCtrl2(i + 1);
                uint nextFlags = Tables.PhonFlags2[nextPhon];

                short next2Phon = GetPhon2(i + 2);
                long next2Ctrl = GetCtrl2(i + 2);
                uint next2Flags = Tables.PhonFlags2[next2Phon];

                int percent = k100pct_Dur;
                int fixedDur = 0;
                int maxDur = Tables.MaxDurTbl[curPhon];
                int minDur = Tables.MinDurTbl[curPhon];

                // Pause insertion
                if (curPhon == _SIL_)
                {
                    int tempS = (int)((curCtrl & kSilenceTypeField) >> kSilenceTypeShift);
                    int durHold = tempS != 0
                        ? Tables.BoundryDurTbl[Math.Min(tempS, Tables.BoundryDurTbl.Length - 1)]
                        : 200;

                    durHold = (int)((durHold * _rateRatio) >> 16);

                    if (!_singing && (curCtrl & kSilenceDuration) != 0)
                        durHold = _userNoteBuf2[i];

                    if (durHold < 10) durHold = 10;
                    {
                        int d = (durHold * _userDurBuf2[i]) >> kDurStepRes;
                        d /= kFrameTime;
                        if (curPhon != _SIL_ && d < 8 / kFrameTime) d = 8 / kFrameTime;
                        _durBuf[i] = (short)Math.Max(d, 1);
                        goto DURATION_DONE_END;
                    }
                }

                // Clause-final lengthening
                if ((curSylType & kTerm_End) != 0)
                {
                    if ((curFlags & kStopF) != 0) fixedDur = 0;
                    else if ((curFlags & kVoicedF) != 0 && (curFlags & kFric) != 0) fixedDur = 20;
                    else if ((curFlags & kVocLiq) != 0 &&
                             (nextFlags & kPlosFricF) != 0 && (nextFlags & kVoicedF) == 0)
                        fixedDur = 15;
                    else fixedDur = 40;

                    if ((nextFlags & kSonorantF) != 0) fixedDur -= 20;

                    if (_phonBuf2InIndex < 10 && curStress != 0 && curIsVowel)
                        fixedDur += (10 - _phonBuf2InIndex) * 5;
                }

                if (curIsVowel)
                {
                    // Non-phrase-final shortening
                    if (curSylType < kVerb_End)
                        percent = (int)((long)percent * 60 * pct >> 16);

                    // Non-word-final shortening
                    if ((curStress & kPrimOrEmphStress) == 0 && (curCtrl & kMore_Than_One_Syllable_In_Word) == 0)
                    {
                        if ((curStress & kSecondaryStress) != 0)
                            percent = (int)((long)percent * 85 * pct >> 16);
                        else
                            percent = (int)((long)percent * 55 * pct >> 16);
                    }
                    else if ((curCtrl & kMore_Than_One_Syllable_In_Word) != 0 &&
                             (curSylType & kSyllableTypeField) < kWord_End &&
                             (curStress & kPrimOrEmphStress) == 0)
                    {
                        if ((curCtrl & kSyllableOrderField) <= kFirst_Syllable_In_Word)
                            percent = (int)((long)percent * 85 * pct >> 16);
                        else
                            percent = (int)((long)percent * 80 * pct >> 16);
                    }

                    // Polysyllabic shortening
                    if ((curCtrl & kMore_Than_One_Syllable_In_Word) != 0)
                        percent = (int)((long)percent * 80 * pct >> 16);
                }

                // Non-word-initial consonant shortening
                if (!curIsVowel && (curCtrl & kWord_Initial_Consonant) == 0)
                {
                    if ((curFlags & kFric) != 0 && (curSylType & kWord_End) != 0)
                        fixedDur += 20;
                    else
                        percent = (int)((long)percent * 85 * pct >> 16);
                }

                // Unstressed shortening
                if ((curStress & kPrimOrEmphStress) == 0)
                {
                    if ((curFlags & kPlosFricF) == 0 && (curFlags & kGStopF) == 0)
                        minDur -= minDur >> 2;

                    if (curIsVowel)
                    {
                        if ((curCtrl & kSyllableOrderField) == kMid_Syllable_In_Word)
                            percent = (int)((long)percent * 55 * pct >> 16);
                        else
                            percent = (int)((long)percent * 70 * pct >> 16);
                    }
                    else
                    {
                        if (curPhon >= _w_ && curPhon <= _l_)
                            percent = (int)((long)percent * 60 * pct >> 16);
                        else
                            percent = (int)((long)percent * 70 * pct >> 16);
                    }
                }

                // Emphatic lengthening
                if ((curCtrl & kWord_Initial_Consonant) != 0 ||
                    (curIsVowel && curStress != kEmphaticStress))
                    eFlag = false;
                if (curStress == kEmphaticStress) eFlag = true;
                if (eFlag) fixedDur += curIsVowel ? 60 : 20;

                // Postvocalic context
                {
                    bool vocFlag = false;
                    short theObstr = _SIL_;
                    long num1 = k100percent;

                    if (curIsVowel ||
                        (((curFlags & kVocLiq) != 0 || (curFlags & kNasalF) != 0) &&
                         (curCtrl & kStressedWInitial) == 0 && (nextFlags & kPlosFricF) != 0))
                    {
                        if ((nextFlags & kVowelF) == 0 && (nextCtrl & kStressedWInitial) == 0)
                        {
                            theObstr = nextPhon;
                            if (((nextFlags & kVocLiq) != 0 || (nextFlags & kNasalF) != 0) &&
                                (next2Ctrl & kStressedWInitial) == 0 && (next2Flags & kPlosFricF) != 0)
                            {
                                vocFlag = true;
                                theObstr = next2Phon;
                            }

                            if (theObstr != _SIL_)
                            {
                                uint obFlags = Tables.PhonFlags2[theObstr];
                                if ((obFlags & kVoicedF) == 0)
                                {
                                    fixedDur -= fixedDur >> 1;
                                    num1 = k1pct * 80;
                                    if ((obFlags & (kStopF | kAffricateF)) != 0)
                                        num1 = k1pct * 55;
                                }
                                else if ((obFlags & kPlosFricF) != 0)
                                {
                                    num1 = k1pct * 120;
                                    if ((obFlags & kStopF) == 0 && theObstr != _DX_ &&
                                        (curFlags & kPrimOrEmphStress) != 0)
                                        fixedDur += 25;
                                }
                                else if ((obFlags & kNasalF) != 0)
                                {
                                    num1 = k1pct * 85;
                                }
                            }
                        }

                        if (curSylType < kTerm_End || vocFlag)
                            num1 = (num1 >> 1) + kOneHalf;

                        percent = (int)((long)percent * num1 >> 16);
                    }
                }

                // Cluster shortening
                if (curIsVowel)
                {
                    if ((nextFlags & kVowelF) != 0) fixedDur += 30;
                    if ((curCtrl & kSyllableOrderField) == kFirst_Syllable_In_Word &&
                        (curCtrl & kPrimOrEmphStress) != 0 &&
                        (prevCtrl & kWord_Initial_Consonant) == 0)
                        fixedDur += 25;
                    if (nextPhon == _LX_) fixedDur -= 20;
                }
                else if ((curFlags & kConsonantF) != 0)
                {
                    if ((nextFlags & kConsonantF) != 0 && curSylType < kTerm_End)
                    {
                        long num1 = k1pct * 55;
                        if ((curFlags & kNasalF) != 0 && (nextCtrl & kWord_Initial_Consonant) != 0)
                            num1 = k1pct * 150;
                        minDur -= minDur >> 2;
                        if (curPhon == _s_ || curPhon == _TH_)
                        {
                            if ((nextFlags & kStopF) != 0) num1 = k1pct * 50;
                            if (nextPhon == _SH_)
                            {
                                int dh = 12;
                                int dd = (dh * _userDurBuf2[i]) >> kDurStepRes;
                                dd /= kFrameTime;
                                if (dd < 1) dd = 1;
                                _durBuf[i] = (short)dd;
                                goto DURATION_DONE_END;
                            }
                        }
                        percent = (int)((long)percent * num1 >> 16);
                    }

                    if ((prevFlags & kConsonantF) != 0)
                    {
                        long num1 = k1pct * 55;
                        minDur -= minDur >> 2;
                        if ((curFlags & kStopF) != 0)
                        {
                            if (prevPhon == _s_) num1 = k1pct * 60;
                            else if ((prevFlags & kNasalF) != 0 && curStress == 0) num1 = k1pct * 10;
                        }
                        percent = (int)((long)percent * num1 >> 16);
                    }
                }

                // Plosive aspiration lengthening
                if ((curFlags & kSonorantF) != 0 && (prevFlags & kVoicedF) == 0 && (prevFlags & kStopF) != 0)
                    fixedDur += 20;

                // Glide lengthening
                if ((curFlags & kVowel1F) != 0 && (prevFlags & kSonorConsonF) != 0 && (prevFlags & kNasalF) == 0)
                {
                    if (fixedDur == 0) fixedDur = 20;
                }

                // Short phrase lengthening
                if (_phonBuf2InIndex < 10 && minDur != maxDur)
                    fixedDur += (5 - (_phonBuf2InIndex >> 1)) * kFrameTime;

                // Rate change
                {
                    short rateVal = _userRateBuf2[i];
                    if (rateVal != 0)
                    {
                        _speechRate = rateVal;
                        InitRateParams();
                    }
                }

                // Singing duration override
                if ((curCtrl & kSingingDuration) != 0)
                {
                    int dd = _userDurBuf2[i] / kFrameTime;
                    if (dd < 1) dd = 1;
                    _durBuf[i] = (short)dd;
                    goto DURATION_DONE_END;
                }

                {
                    int durHold = (percent * (maxDur - minDur) >> 7) + minDur;
                    if (_speechRate != kNormal_Speech_Rate && durHold != 0)
                    {
                        durHold = (int)((durHold * _rateRatioLowGain) >> 16);
                        fixedDur = (int)((fixedDur * _rateRatio) >> 16);
                    }
                    durHold += fixedDur;

                    int d = (durHold * _userDurBuf2[i]) >> kDurStepRes;
                    d /= kFrameTime;
                    if (curPhon != _SIL_ && d < 8 / kFrameTime) d = 1;
                    _durBuf[i] = (short)Math.Max(d, 1);
                }

            DURATION_DONE_END:;
            }
        }
    }
}  // namespace
