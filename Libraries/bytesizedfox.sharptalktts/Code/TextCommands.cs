#nullable enable
using System;
using System.Collections.Generic;
using static SharpTalk.AudioProcessor;

namespace SharpTalk
{

    public static class EmbeddedCmd
    {
        // note N → internal pitch value
        static short NoteToInternalPitch(int dtNote)
        {
            if (dtNote <= 0) return 0;
            short midiQ8 = (short)((dtNote + 42) << 8);
            if (midiQ8 < 0x1F59) return 0;
            midiQ8 -= 0x1F59;
            return (short)((((long)midiQ8 * 0x1555L) + 0x8000L) >> 16);
        }

        static short MapPhoneme(string p) => p switch
        {
            "iy" => _IY_,
            "ih" => _IH_,
            "eh" => _EH_,
            "ae" => _AE_,
            "aa" => _AA_,
            "ah" => _AH_,
            "ao" => _AO_,
            "uh" => _UH_,
            "ax" => _AX_,
            "er" => _ER_,
            "ey" => _EY_,
            "ay" => _AY_,
            "oy" => _OY_,
            "aw" => _AW_,
            "ow" => _OW_,
            "uw" => _UW_,
            "yu" => _YU_,
            "ix" => _IX_,
            "ir" => _IR_,
            "xr" => _XR_,
            "ar" => _AR_,
            "or" => _OR_,
            "ur" => _UR_,
            "el" => _EL_,
            "en" => _EN_,
            "rr" => _RX_,
            "hx" => _h_,
            "nx" => _n_,
            "dx" => _DX_,
            "zh" => _ZH_,
            "sh" => _SH_,
            "th" => _TH_,
            "dh" => _DH_,
            "ch" => _CH_,
            "jh" => _JH_,
            "ng" => _NG_,
            "wh" => _w_,
            "b" => _b_,
            "d" => _d_,
            "f" => _f_,
            "g" => _g_,
            "h" => _h_,
            "k" => _k_,
            "l" => _l_,
            "m" => _m_,
            "n" => _n_,
            "p" => _p_,
            "r" => _r_,
            "s" => _s_,
            "t" => _t_,
            "v" => _v_,
            "w" => _w_,
            "y" => _y_,
            "z" => _z_,
            "q" => _QX_,
            "_" => _SIL_,
            _ => -1,
        };

        public readonly struct VoiceCommand
        {
            public enum Kind { Rate, Pitch, Volume }
            public readonly Kind Type;
            public readonly int Value;
            public VoiceCommand(Kind type, int value) { Type = type; Value = value; }
        }

        public readonly struct Segment
        {
            public readonly string? PlainText;
            public readonly List<PhonemeToken>? Singing;
            public readonly VoiceCommand? Cmd;
            public bool IsSinging => Singing != null;
            public bool IsCommand => Cmd != null;
            public Segment(string text) { PlainText = text; Singing = null; Cmd = null; }
            public Segment(List<PhonemeToken> s) { PlainText = null; Singing = s; Cmd = null; }
            public Segment(VoiceCommand cmd) { PlainText = null; Singing = null; Cmd = cmd; }
        }

        public static List<Segment> ParseSegments(string text)
        {
            var segments = new List<Segment>();
            if (!text.Contains('['))
            {
                if (text.Length > 0) segments.Add(new Segment(text));
                return segments;
            }

            var plain = new System.Text.StringBuilder();
            bool inSingMode = false;
            int i = 0;

            void FlushPlain()
            {
                if (plain.Length > 0) { segments.Add(new Segment(plain.ToString())); plain.Clear(); }
            }

            while (i < text.Length)
            {
                if (text[i] != '[') { plain.Append(text[i++]); continue; }

                i++; // consume '['
                if (i >= text.Length) break;

                if (text[i] == ':')
                {
                    // [:command arg] — parse mode-switch and voice-param commands
                    i++;
                    int cmdStart = i;
                    while (i < text.Length && text[i] != ' ' && text[i] != ']') i++;
                    string cmd = text[cmdStart..i].ToLowerInvariant();

                    // capture optional integer argument
                    while (i < text.Length && text[i] == ' ') i++;
                    int argStart = i;
                    while (i < text.Length && text[i] != ']') i++;
                    string argStr = text[argStart..i].Trim();
                    if (i < text.Length) i++; // consume ']'

                    if (cmd == "sing") inSingMode = true;
                    else if (cmd == "talk" || cmd == "stop") inSingMode = false;
                    else if (int.TryParse(argStr, out int argVal))
                    {
                        VoiceCommand.Kind? kind = cmd switch
                        {
                            "rate" => VoiceCommand.Kind.Rate,
                            "pitch" => VoiceCommand.Kind.Pitch,
                            "volume" => VoiceCommand.Kind.Volume,
                            _ => null,
                        };
                        if (kind is { } k)
                        {
                            FlushPlain();
                            segments.Add(new Segment(new VoiceCommand(k, argVal)));
                        }
                    }
                    continue;
                }

                // Phoneme block [phoneme<dur,note> ...]
                var blockSing = new List<PhonemeToken>();
                bool firstPhon = true;
                short lastPitch = 0; // inherited by trailing consonants with no <note>

                while (i < text.Length && text[i] != ']')
                {
                    while (i < text.Length && text[i] == ' ') i++;
                    if (i >= text.Length || text[i] == ']') break;

                    if (text[i] == '_' || char.IsLetter(text[i]))
                    {
                        // Collect all phonemes up to '<', ']', or ' '
                        // e.g. "dey<600,24>" → [d, ey] with dur=600 note=24
                        var group = new List<short>();
                        while (i < text.Length && text[i] != '<' && text[i] != ']' && text[i] != ' ')
                        {
                            if (text[i] == '_') { group.Add(_SIL_); i++; continue; }
                            bool matched2 = false;
                            if (i + 1 < text.Length && char.IsLetter(text[i + 1]))
                            {
                                string two = string.Concat(text[i], text[i + 1]).ToLowerInvariant();
                                short op2 = MapPhoneme(two);
                                if (op2 >= 0) { group.Add(op2); i += 2; matched2 = true; }
                            }
                            if (!matched2)
                            {
                                string one = text[i].ToString().ToLowerInvariant();
                                short op1 = MapPhoneme(one);
                                group.Add(op1 >= 0 ? op1 : _SIL_);
                                i++;
                            }
                        }

                        int dur = 0, note = 0;
                        bool hasNote = false;
                        if (i < text.Length && text[i] == '<')
                        {
                            hasNote = true;
                            i++;
                            while (i < text.Length && char.IsDigit(text[i]))
                                dur = dur * 10 + (text[i++] - '0');
                            if (i < text.Length && text[i] == ',')
                            {
                                i++;
                                while (i < text.Length && char.IsDigit(text[i]))
                                    note = note * 10 + (text[i++] - '0');
                            }
                            while (i < text.Length && text[i] != '>' && text[i] != ']') i++;
                            if (i < text.Length && text[i] == '>') i++;
                        }

                        // In [:sing] text mode, skip bare phonemes with no note/dur.
                        // In an explicit block, always include them (trailing consonants, etc.)
                        if (!hasNote && !inSingMode && blockSing.Count == 0) continue;

                        short pitch = hasNote
                            ? (note > 37 ? (short)-note : NoteToInternalPitch(note))
                            : lastPitch;
                        if (hasNote) lastPitch = pitch;

                        for (int gi = 0; gi < group.Count; gi++)
                        {
                            bool isLast = gi == group.Count - 1;
                            long ctrl = kWord_Start | kContent_Word;
                            if (hasNote) ctrl |= kSingingPhon;
                            if (hasNote && isLast) ctrl |= kSingingDuration;
                            if (firstPhon) { firstPhon = false; }
                            else ctrl &= ~(kWord_Start | kContent_Word);
                            blockSing.Add(new PhonemeToken
                            {
                                Phon = group[gi],
                                Ctrl = ctrl,
                                UserDur = hasNote && isLast ? (short)dur : (short)0,
                                UserNote = pitch,
                            });
                        }
                    }
                    else { i++; }
                }
                if (i < text.Length && text[i] == ']') i++;

                if (blockSing.Count > 0)
                {
                    FlushPlain();
                    segments.Add(new Segment(blockSing));
                }
            }

            FlushPlain();
            return segments;
        }

        public static string Parse(string text, out List<PhonemeToken>? singingTokens)
        {
            singingTokens = null;
            var segments = ParseSegments(text);

            var plain = new System.Text.StringBuilder();
            List<PhonemeToken>? sing = null;

            foreach (var seg in segments)
            {
                if (seg.IsSinging)
                {
                    sing ??= new List<PhonemeToken>();
                    sing.AddRange(seg.Singing!);
                }
                else if (!seg.IsCommand)
                {
                    plain.Append(seg.PlainText);
                }
            }

            singingTokens = sing;
            return plain.ToString();
        }

        public static string StripCommands(string text)
        {
            var result = Parse(text, out _);
            return result;
        }
    }
}  // namespace
