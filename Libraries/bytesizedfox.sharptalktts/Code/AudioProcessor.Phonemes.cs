#nullable enable
using System;

namespace SharpTalk
{

    public sealed partial class AudioProcessor
    {
        // LoadPhonemes

        private void LoadPhonemes(PhonemeToken[] tokens)
        {
            // Slot 0 is the initial SIL (already filled by ClearBuffers)
            _phonBuf1InIndex = 1;

            foreach (var tok in tokens)
            {
                if (_phonBuf1InIndex >= kPhonBuf_Red_Zone) break;
                _phonBuf1[_phonBuf1InIndex] = tok.Phon;
                _phonCtrlBuf1[_phonBuf1InIndex] = tok.Ctrl;
                _userPitchBuf1[_phonBuf1InIndex] = tok.UserPitch;
                _userDurBuf1[_phonBuf1InIndex] = tok.UserDur == 0 ? (short)kDur_One : tok.UserDur;
                _userNoteBuf1[_phonBuf1InIndex] = tok.UserNote;
                _userRateBuf1[_phonBuf1InIndex] = tok.UserRate;
                if ((tok.Ctrl & kSingingDuration) != 0) _singing = true;
                _phonBuf1InIndex++;
            }

            // Add trailing boundary SIL, but only if the last phoneme isn't already a
            // terminal SIL (e.g. sentence-final comma from FrontEnd). If one is already
            // there, upgrade its boundary type to match the sentence-ending punctuation.
            if (_phonBuf1InIndex < kPhonBuf_Red_Zone)
            {
                int bndType = _endPunctuation switch
                {
                    _Period_ => kBND_Decl,
                    _Quest_ => kBND_Quest,
                    _Exclam_ => kBND_Emph,
                    _ => kBND_Pause,
                };
                int lastIdx = _phonBuf1InIndex - 1;
                bool lastIsSilBoundary = lastIdx >= 1 &&
                    _phonBuf1[lastIdx] == _SIL_ &&
                    (_phonCtrlBuf1[lastIdx] & kTerm_Bound) != 0;

                if (lastIsSilBoundary)
                {
                    // Replace the existing boundary type with the sentence-final type
                    _phonCtrlBuf1[lastIdx] = (_phonCtrlBuf1[lastIdx] & ~kSilenceTypeField)
                        | ((long)bndType << kSilenceTypeShift);
                }
                else
                {
                    _phonCtrlBuf1[_phonBuf1InIndex] |= kTerm_Bound;
                    _phonCtrlBuf1[_phonBuf1InIndex] |= ((long)bndType << kSilenceTypeShift);
                    // _phonBuf1[index] is already _SIL_ from ClearBuffers
                    _phonBuf1InIndex++;
                }
            }

            // Auto-compute kWord_Initial_Consonant for each word.
            // Any boundary bit (kWord_Start, kTerm_Bound, kVerb_Start, kPrep_Start) resets
            // wordInitial=true. kWord_Start is on the first real phoneme of a word, so we
            // must NOT skip it — it may itself be a word-initial consonant. SIL phonemes
            // (sentence boundaries with kTerm_Bound) are skipped since they're not consonants.
            bool wordInitial = true;
            for (int i = 1; i < _phonBuf1InIndex; i++)
            {
                long ctrl = _phonCtrlBuf1[i];
                if ((ctrl & kBoundryTypeField) != 0) wordInitial = true;
                if (_phonBuf1[i] == _SIL_) continue;
                uint flags = GetPhonFlags1(i);
                if (wordInitial)
                {
                    if ((flags & kVowelF) != 0)
                        wordInitial = false;
                    else
                        _phonCtrlBuf1[i] |= kWord_Initial_Consonant;
                }
            }
        }

        // Flag_PhonBuf_1

        private void FlagPhonBuf1()
        {
            _isCompoundNoun = false;
            for (_scanIndex = 0; _scanIndex < _phonBuf1InIndex; _scanIndex++)
            {
                long ctrl = _phonCtrlBuf1[_scanIndex];
                if ((ctrl & kCompoundNoun) != 0) _isCompoundNoun = true;
                else if ((ctrl & kBoundryTypeField) != 0) _isCompoundNoun = false;

                uint phonFlags = GetPhonFlags1(_scanIndex);
                if ((phonFlags & kVowelF) != 0)
                    MarkSyllable();

                MarkBoundry();
            }
            MarkSyllableStart();
        }

        private void MarkSyllable()
        {
            long order = 0;

            // scan backward for another vowel in same word
            for (int idx = _scanIndex - 1; idx > 0; idx--)
            {
                long syl = _phonCtrlBuf1[idx] & kSyllableTypeField;
                if (syl >= kWord_End) break;
                uint flags = GetPhonFlags1(idx);
                if ((flags & kVowelF) != 0) { order = kLast_Syllable_In_Word; break; }
            }

            // scan forward for another vowel in same word
            for (int idx = _scanIndex + 1; idx < _phonBuf1InIndex; idx++)
            {
                long bnd = _phonCtrlBuf1[idx] & kBoundryTypeField;
                uint flags = GetPhonFlags1(idx);
                if (bnd != 0)
                {
                    _phonCtrlBuf1[_scanIndex] |= order;
                    break;
                }
                if ((flags & kVowelF) != 0)
                {
                    if (order == kLast_Syllable_In_Word)
                        order = kMid_Syllable_In_Word;
                    else if (order == 0)
                        order = kFirst_Syllable_In_Word;
                }
            }
        }

        private void MarkBoundry()
        {
            for (int idx = _scanIndex + 1; idx < _phonBuf1InIndex; idx++)
            {
                long bnd = _phonCtrlBuf1[idx] & kBoundryTypeField;
                uint flags = GetPhonFlags1(idx);
                if (bnd != 0)
                {
                    long boundType = 0;
                    if ((bnd & kTerm_Bound) != 0) boundType |= kTerm_End | kWord_End;
                    if ((bnd & kPrep_Start) != 0) boundType |= kPrep_End | kWord_End;
                    if ((bnd & kVerb_Start) != 0) boundType |= kVerb_End | kWord_End;
                    if ((bnd & kWord_Start) != 0) boundType |= kWord_End;
                    _phonCtrlBuf1[_scanIndex] |= boundType;
                    break;
                }
                if ((flags & kVowelF) != 0) break;
            }
        }

        private void MarkSyllableStart()
        {
            int syllIdx = 0;
            int idx = 0;
            while (idx < _phonBuf1InIndex)
            {
                while (idx < _phonBuf1InIndex && _phonBuf1[idx] == _SIL_)
                { syllIdx++; idx++; }
                if (idx >= _phonBuf1InIndex) break;

                uint flags = GetPhonFlags1(idx);
                if ((flags & kVowelF) != 0)
                {
                    _phonCtrlBuf1[syllIdx] |= kSyllable_Start;
                    long syllOrder = _phonCtrlBuf1[idx] & kSyllableOrderField;
                    if (syllOrder == 0 || syllOrder == kLast_Syllable_In_Word)
                    {
                        idx = FindNextWordBound(idx);
                        syllIdx = idx;
                    }
                    else
                    {
                        // scan forward to next vowel counting consonants
                        int dist = -1;
                        int startIdx = idx;
                        do
                        {
                            idx++;
                            dist++;
                            if (idx >= _phonBuf1InIndex) goto SYLL_DONE;
                        } while ((GetPhonFlags1(idx) & kVowelF) == 0);

                        if (dist == 0)
                        {
                            syllIdx = idx;
                        }
                        else if (dist == 1)
                        {
                            idx--; syllIdx = idx;
                        }
                        else if (dist == 2)
                        {
                            short p2 = _phonBuf1[idx - 1];
                            short p1 = _phonBuf1[idx - 2];
                            if (IfConsonantCluster(p1, p2)) idx -= 2;
                            else idx--;
                            syllIdx = idx;
                        }
                        else if (dist == 3)
                        {
                            short p2 = _phonBuf1[idx - 1];
                            short p1 = _phonBuf1[idx - 2];
                            if (IfConsonantCluster(p1, p2))
                            {
                                if (_phonBuf1[idx - 3] == _s_) idx -= 3;
                                else idx -= 2;
                            }
                            else idx--;
                            syllIdx = idx;
                        }
                        else
                        {
                            short p1 = _phonBuf1[idx - dist];
                            short p2 = _phonBuf1[idx - dist + 1];
                            if (IfConsonantCluster(p1, p2)) idx -= dist - 2;
                            else idx -= dist >> 1;
                            syllIdx = idx;
                        }
                    }
                }
                else
                    idx++;
            }
        SYLL_DONE:;
        }

        private int FindNextWordBound(int index)
        {
            for (int i = index + 1; i < _phonBuf1InIndex; i++)
            {
                if ((_phonCtrlBuf1[i] & (kBoundryTypeField | kWord_Start)) != 0)
                    return i;
            }
            return _phonBuf1InIndex;
        }

        private static bool IfConsonantCluster(short c1, short c2) => (c1, c2) switch
        {
            (_f_, _r_) or (_f_, _l_) => true,
            (_v_, _r_) or (_v_, _l_) => true,
            (_TH_, _r_) or (_TH_, _w_) => true,
            (_s_, _w_) or (_s_, _l_) or (_s_, _p_) or (_s_, _t_) or (_s_, _k_)
                or (_s_, _m_) or (_s_, _n_) or (_s_, _f_) => true,
            (_SH_, _w_) or (_SH_, _l_) or (_SH_, _p_) or (_SH_, _t_)
                or (_SH_, _r_) or (_SH_, _m_) or (_SH_, _n_) => true,
            (_p_, _r_) or (_p_, _l_) => true,
            (_b_, _r_) or (_b_, _l_) => true,
            (_t_, _r_) or (_t_, _w_) => true,
            (_d_, _r_) or (_d_, _w_) => true,
            (_k_, _r_) or (_k_, _l_) or (_k_, _w_) => true,
            (_g_, _r_) or (_g_, _l_) or (_g_, _w_) => true,
            _ => false,
        };

        // Fill_Phon_Buf_2

        private void FillPhonBuf2()
        {
            _phonBuf2InIndex = 0;
            short lastStoredPhon = _SIL_;
            short lastUserPitch = 0;

            for (int outIdx = 0; outIdx < _phonBuf1InIndex; outIdx++)
            {
                short curPhon = _phonBuf1[outIdx];
                long curCtrl = _phonCtrlBuf1[outIdx];
                uint curFlags = Tables.PhonFlags2[curPhon];

                // next
                short nextPhon, next2Phon, next3Phon;
                long nextCtrl, next2Ctrl;
                if (outIdx < _phonBuf1InIndex - 1)
                { nextPhon = _phonBuf1[outIdx + 1]; nextCtrl = _phonCtrlBuf1[outIdx + 1]; }
                else { nextPhon = _SIL_; nextCtrl = 0; }
                if (outIdx < _phonBuf1InIndex - 2)
                { next2Phon = _phonBuf1[outIdx + 2]; next2Ctrl = _phonCtrlBuf1[outIdx + 2]; }
                else { next2Phon = _SIL_; next2Ctrl = 0; }
                next3Phon = outIdx < _phonBuf1InIndex - 3 ? _phonBuf1[outIdx + 3] : _SIL_;

                uint nextFlags = Tables.PhonFlags2[nextPhon];
                uint next2Flags = Tables.PhonFlags2[next2Phon];

                // prev
                short prevPhon, prev2Phon, prev3Phon;
                long prevCtrl;
                if (outIdx > 0) { prevPhon = _phonBuf1[outIdx - 1]; prevCtrl = _phonCtrlBuf1[outIdx - 1]; }
                else { prevPhon = _SIL_; prevCtrl = 0; }
                prev2Phon = outIdx > 1 ? _phonBuf1[outIdx - 2] : _SIL_;
                prev3Phon = outIdx > 2 ? _phonBuf1[outIdx - 3] : _SIL_;

                uint prevFlags = Tables.PhonFlags2[prevPhon];
                uint prev2Flags = Tables.PhonFlags2[prev2Phon];
                uint prev3Flags = Tables.PhonFlags2[prev3Phon];

                if (_phonBuf2InIndex == 0) lastStoredPhon = _SIL_;
                else lastStoredPhon = _phonBuf2[_phonBuf2InIndex - 1];
                uint lastPhonFlags = Tables.PhonFlags2[lastStoredPhon];

                short userPitch = _userPitchBuf1[outIdx];
                short userDur = _userDurBuf1[outIdx];
                short userNote = _userNoteBuf1[outIdx];
                short userRate = _userRateBuf1[outIdx];

                short targetPhon = curPhon;
                bool delFwd = false;
                bool insertGlot = false;

                // EN rule
                if (curPhon == _n_ && prevPhon == _IX_)
                {
                    if ((prev2Flags & kPlosFricF) != 0 && prev2Phon != _b_ && prev2Phon != _g_)
                    {
                        if (!(prev2Phon == _d_ && (prev3Flags & kVowelF) != 0))
                        {
                            _phonBuf2[_phonBuf2InIndex - 1] = _EN_;
                            delFwd = true;
                        }
                    }
                }

                // EL rule
                if (curPhon == _l_ && (curCtrl & (kPrimOrEmphStress | kWord_Initial_Consonant)) == 0)
                {
                    if (prevPhon == _AX_ || prevPhon == _UH_)
                    {
                        _phonBuf2[_phonBuf2InIndex - 1] = _EL_;
                        delFwd = true;
                        goto STUFF_BUFF;
                    }
                }

                // LX / RX rules
                if ((curCtrl & (kPrimOrEmphStress | kWord_Initial_Consonant)) == 0 &&
                    (prevFlags & kVowel1F) != 0)
                {
                    if (curPhon == _l_)
                    {
                        targetPhon = _LX_;
                    }
                    else if (curPhon == _r_)
                    {
                        targetPhon = _RX_;
                        switch (prevPhon)
                        {
                            case _UW_:
                            case _UH_:
                                _phonBuf2[_phonBuf2InIndex - 1] = _UR_; delFwd = true; break;
                            case _AO_:
                            case _OW_:
                                _phonBuf2[_phonBuf2InIndex - 1] = _OR_; delFwd = true; break;
                            case _AA_:
                                _phonBuf2[_phonBuf2InIndex - 1] = _AR_; delFwd = true; break;
                            case _AH_:
                            case _AX_:
                                _phonBuf2[_phonBuf2InIndex - 1] = _ER_; delFwd = true; break;
                            case _IH_:
                            case _IY_:
                                _phonBuf2[_phonBuf2InIndex - 1] = _IR_; delFwd = true; break;
                            case _AE_:
                            case _EH_:
                            case _EY_:
                                _phonBuf2[_phonBuf2InIndex - 1] = _XR_; delFwd = true; break;
                        }
                    }
                }

                // yUW -> YU rule
                if ((prevCtrl & kWord_Initial_Consonant) != 0 && prevPhon == _y_ &&
                    curPhon == _UW_ && nextPhon != _r_ &&
                    (curCtrl & kSyllableTypeField) >= kWord_End)
                {
                    _phonBuf2[_phonBuf2InIndex - 1] = _YU_;
                    _phonCtrlBuf2[_phonBuf2InIndex - 1] = curCtrl;
                    delFwd = true;
                }

                // DHAH -> DHIY rule
                if ((nextFlags & kVowelF) != 0 && curPhon == _AH_ &&
                    (curCtrl & kSyllableTypeField) != 0 && prevPhon == _DH_ &&
                    (prevCtrl & kWord_Initial_Consonant) != 0 &&
                    (nextCtrl & kPrimOrEmphStress) != 0)
                {
                    targetPhon = _IY_;
                }

                // EHnd -> AEnd rule
                if (curPhon == _SIL_ && nextPhon == _EH_ && next2Phon == _n_ &&
                    next3Phon == _d_ && (nextCtrl & kPrimOrEmphStress) != 0)
                {
                    _phonBuf1[outIdx + 1] = _AE_;
                    nextPhon = _AE_;
                    nextFlags = Tables.PhonFlags2[_AE_];
                }

                // Glottal insertion
                if ((curFlags & kVowelF) != 0 && (nextFlags & kVowelF) != 0 &&
                    (nextCtrl & kPrimOrEmphStress) != 0 && (curCtrl & kWord_End) != 0)
                {
                    insertGlot = true;
                }

                // Dental/affricate y-slur: d -> JH before YU/y (unstressed)
                if ((nextPhon == _YU_ || nextPhon == _y_) && (nextCtrl & kPrimOrEmphStress) == 0)
                {
                    if (curPhon == _d_) { targetPhon = _JH_; goto STUFF_BUFF; }
                }

                // t rules
                if (curPhon == _t_)
                {
                    // tUH -> tUW
                    if (nextPhon == _UW_ && (nextCtrl & kSyllableTypeField) >= kWord_End &&
                        (curCtrl & kPrimOrEmphStress) == 0 &&
                        (next2Phon == _SIL_ || (Tables.PhonFlags2[next2Phon] & kVowelF) != 0))
                    {
                        _phonBuf1[outIdx + 1] = _UW_;
                    }
                    else
                    {
                        // Glottalize t before l or DH
                        if (nextPhon == _l_ || nextPhon == _DH_)
                        {
                            goto SUB_T_GLOT;
                        }
                        // At word end before sonorant/h
                        else if ((curCtrl & kSyllableTypeField) >= kWord_End)
                        {
                            if (((nextFlags & kSonorConsonF) != 0 && nextPhon != _EN_) || nextPhon == _h_)
                                goto SUB_T_GLOT;
                        }
                        else if (nextPhon == _EN_ || (nextPhon == _IX_ && next2Phon == _n_))
                        {
                            goto SUB_T_GLOT;
                        }
                        goto SKIP_T_GLOT;
                    SUB_T_GLOT:
                        targetPhon = (lastPhonFlags & kSonorantF) != 0 ? _TX_ : _d_;
                        goto STUFF_BUFF;
                    SKIP_T_GLOT:;
                    }
                }

                // Dental flap DX rules (d or t before vowel, after sonorant non-nasal)
                if (curPhon == _d_ || curPhon == _t_)
                {
                    // Don't flap before syllabic n
                    if (nextPhon == _IX_ && next2Phon == _n_)
                    {
                        if (curPhon == _t_) goto SKIP_FLAP;
                        if ((prevFlags & kVowelF) == 0) goto SKIP_FLAP;
                    }

                    if ((nextFlags & kVowelF) != 0 &&
                        (lastPhonFlags & kSonorantF) != 0 && (lastPhonFlags & kNasalF) == 0)
                    {
                        if ((nextCtrl & kWord_Start) != 0)
                        {
                            targetPhon = _DX_;
                        }
                        else if ((curCtrl & kPrimOrEmphStress) == 0)
                        {
                            if ((curCtrl & kWord_Initial_Consonant) != 0)
                            {
                                if (nextPhon == _AX_ || nextPhon == _IX_ || nextPhon == _UH_)
                                    targetPhon = _DX_;
                            }
                            else if (curPhon == _t_)
                            {
                                // T flap rules
                                if (nextPhon == _OW_)
                                {
                                    if ((_phonCtrlBuf2[_phonBuf2InIndex - 1] & kStressField) != 0 &&
                                        (next2Phon != _r_ || (next2Ctrl & kWord_Initial_Consonant) != 0))
                                        targetPhon = _DX_;
                                }
                                else if ((nextPhon == _AH_ || nextPhon == _AX_) &&
                                         next2Phon == _r_ && (nextCtrl & kPrimaryStress) == 0)
                                {
                                    if ((curCtrl & kWord_Initial_Consonant) == 0 && (nextCtrl & kPrimaryStress) == 0)
                                        targetPhon = _DX_;
                                }
                                else if (nextPhon == _ER_)
                                {
                                    if ((curCtrl & kWord_Initial_Consonant) == 0) targetPhon = _DX_;
                                }
                                else if ((nextPhon == _AX_ || nextPhon == _IY_ || nextPhon == _IX_ || nextPhon == _EL_) &&
                                         (next2Phon != _r_ || (next2Ctrl & kWord_Initial_Consonant) != 0) &&
                                         (nextCtrl & kPrimaryStress) == 0)
                                {
                                    targetPhon = _DX_;
                                }
                            }
                            else // curPhon == _d_
                            {
                                if (nextPhon == _OW_)
                                {
                                    if ((_phonCtrlBuf2[_phonBuf2InIndex - 1] & kStressField) != 0)
                                        targetPhon = _DX_;
                                }
                                else if (nextPhon == _AX_ || nextPhon == _IY_ || nextPhon == _IX_ ||
                                         nextPhon == _EL_ || nextPhon == _ER_ || nextPhon == _IH_ ||
                                         nextPhon == _AH_ || nextPhon == _AA_)
                                {
                                    targetPhon = _DX_;
                                }
                            }
                        }
                    }
                }
            SKIP_FLAP:

                // DH rules
                if (curPhon == _DH_ && (curCtrl & kPrimaryStress) == 0)
                {
                    switch (lastStoredPhon)
                    {
                        case _t_:
                        case _TX_:
                        case _d_:
                            targetPhon = _DD_; break;
                        case _n_:
                            targetPhon = _n_; break;
                    }
                }

            STUFF_BUFF:
                if (!delFwd)
                {
                    _phonBuf2[_phonBuf2InIndex] = targetPhon;
                    _phonCtrlBuf2[_phonBuf2InIndex] = curCtrl;
                    _userPitchBuf2[_phonBuf2InIndex] = (short)(userPitch + lastUserPitch);
                    _userDurBuf2[_phonBuf2InIndex] = userDur;
                    _userNoteBuf2[_phonBuf2InIndex] = userNote;
                    _userRateBuf2[_phonBuf2InIndex] = userRate;

                    if (_phonBuf2InIndex < kPhonBuf_Red_Zone)
                        _phonBuf2InIndex++;

                    if (insertGlot)
                    {
                        _phonBuf2[_phonBuf2InIndex] = _QX_;
                        _phonCtrlBuf2[_phonBuf2InIndex] = 0;
                        _userPitchBuf2[_phonBuf2InIndex] = _userPitchBuf2[_phonBuf2InIndex - 1];
                        _userDurBuf2[_phonBuf2InIndex] = kDur_One;
                        _userNoteBuf2[_phonBuf2InIndex] = 0;
                        _userRateBuf2[_phonBuf2InIndex] = 0;
                        if (_phonBuf2InIndex < kPhonBuf_Red_Zone)
                            _phonBuf2InIndex++;
                    }
                }
                else
                {
                    _userPitchBuf2[_phonBuf2InIndex - 1] += userPitch;
                    if (userDur != kDur_One)
                        _userDurBuf2[_phonBuf2InIndex - 1] = userDur;
                    if (userRate != 0)
                        _userRateBuf2[_phonBuf2InIndex - 1] = userRate;
                    if ((curCtrl & kSyllable_Start) != 0)
                        _phonCtrlBuf1[outIdx + 1] |= kSyllable_Start;
                }

                lastUserPitch += userPitch;
            }
        }
    }
}  // namespace
