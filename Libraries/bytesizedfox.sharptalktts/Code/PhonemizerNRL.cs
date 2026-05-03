#nullable enable
using System;
using System.Collections.Generic;
using System.Text;

namespace SharpTalk
{
    // rule-based phonemizer based on NRL Report 7948 (1976).
    public static class PhonemizerNRL
    {
        private struct Rule
        {
            public string Left;
            public string Mid;
            public string Right;
            public byte[] Phonemes;

            public Rule(string left, string mid, string right, params byte[] phonemes)
            {
                Left = left;
                Mid = mid;
                Right = right;
                Phonemes = phonemes;
            }
        }

        private static readonly Dictionary<char, List<Rule>> _rules = new();

        // Phoneme IDs from AudioProcessor
        private const byte _IY_ = 0; private const byte _IH_ = 1;
        private const byte _EH_ = 2; private const byte _AE_ = 3;
        private const byte _AA_ = 4; private const byte _AH_ = 5;
        private const byte _AO_ = 6; private const byte _UH_ = 7;
        private const byte _AX_ = 8; private const byte _ER_ = 9;
        private const byte _EY_ = 10; private const byte _AY_ = 11;
        private const byte _OY_ = 12; private const byte _AW_ = 13;
        private const byte _OW_ = 14; private const byte _UW_ = 15;
        private const byte _YU_ = 16; private const byte _IR_ = 17;
        private const byte _XR_ = 18; private const byte _AR_ = 19;
        private const byte _OR_ = 20; private const byte _UR_ = 21;
        private const byte _IX_ = 22; private const byte _SIL_ = 23;
        private const byte _RX_ = 24; private const byte _LX_ = 25;
        private const byte _EL_ = 26; private const byte _EN_ = 27;
        private const byte _w_ = 28; private const byte _y_ = 29;
        private const byte _r_ = 30; private const byte _l_ = 31;
        private const byte _h_ = 32; private const byte _m_ = 33;
        private const byte _n_ = 34; private const byte _NG_ = 35;
        private const byte _f_ = 36; private const byte _v_ = 37;
        private const byte _TH_ = 38; private const byte _DH_ = 39;
        private const byte _s_ = 40; private const byte _z_ = 41;
        private const byte _SH_ = 42; private const byte _ZH_ = 43;
        private const byte _p_ = 44; private const byte _b_ = 45;
        private const byte _t_ = 46; private const byte _d_ = 47;
        private const byte _k_ = 48; private const byte _g_ = 49;
        private const byte _CH_ = 50; private const byte _JH_ = 51;

        static PhonemizerNRL()
        {
            // Punctuation Rules
            AddRule(' ', "", " ", " ", _SIL_);
            AddRule('.', "", ".", "", _SIL_);
            AddRule(',', "", ",", "", _SIL_);
            AddRule('?', "", "?", "", _SIL_);
            AddRule('!', "", "!", "", _SIL_);

            // A Rules
            AddRule('A', "", "A", " ", _AX_);
            AddRule('A', " ", "ARE", " ", _AA_, _r_);
            AddRule('A', " ", "AR", "O", _AX_, _r_);
            AddRule('A', "", "AR", "#", _EH_, _r_);
            AddRule('A', "^", "AS", ".", _EY_, _s_);
            AddRule('A', "", "A", "WA", _AX_);
            AddRule('A', "", "ANY", "", _EH_, _n_, _IY_);
            AddRule('A', "", "ALLY", "", _AX_, _l_, _IY_);
            AddRule('A', "", "AL", "#", _AX_, _l_);
            AddRule('A', "", "AGAIN", "", _AX_, _g_, _EH_, _n_);
            AddRule('A', "#", "AG", "E", _EY_, _JH_);
            AddRule('A', "", "A", "+", _EY_);
            AddRule('A', "", "A", ":^E", _EY_);
            AddRule('A', ":", "AR", "", _AA_, _r_);
            AddRule('A', "", "AR", "", _ER_);
            AddRule('A', "", "AIR", "", _EH_, _r_);
            AddRule('A', "", "AI", "", _EY_);
            AddRule('A', "", "AY", "", _EY_);
            AddRule('A', "", "AU", "", _AO_);
            AddRule('A', "#", "AL", " ", _AX_, _l_);
            AddRule('A', "", "AL", "S", _AX_, _l_, _z_);
            AddRule('A', "", "AL", "", _AO_, _l_);
            AddRule('A', "", "A", "^%", _EY_);
            AddRule('A', "", "A", "", _AE_);

            // B Rules
            AddRule('B', "", "BE", "#", _b_, _IH_);
            AddRule('B', "", "BEING", "", _b_, _IY_, _IH_, _NG_);
            AddRule('B', "", "BOTH", "", _b_, _OW_, _TH_);
            AddRule('B', "", "BUS", "#", _b_, _IH_, _z_);
            AddRule('B', "", "BUILD", "", _b_, _IH_, _l_, _d_);
            AddRule('B', "", "B", "", _b_);

            // C Rules
            AddRule('C', "", "CH", "^", _k_);
            AddRule('C', "^", "CH", "", _CH_);
            AddRule('C', "", "CH", "", _CH_);
            AddRule('C', " ", "CI", "A", _SH_);
            AddRule('C', "", "CI", "O", _SH_);
            AddRule('C', "", "CI", "EN", _SH_);
            AddRule('C', "", "C", "+", _s_);
            AddRule('C', "", "CK", "", _k_);
            AddRule('C', "", "COM", "%", _k_, _AH_, _m_);
            AddRule('C', "", "C", "", _k_);

            // D Rules
            AddRule('D', "#", "D", " ", _d_);
            AddRule('D', "", "DE", "#", _d_, _IH_);
            AddRule('D', "", "DO", " ", _d_, _UW_);
            AddRule('D', "", "DOING", "", _d_, _UW_, _IH_, _NG_);
            AddRule('D', "", "DOW", "", _d_, _AW_);
            AddRule('D', "", "DU", "A", _JH_, _UW_);
            AddRule('D', "", "D", "", _d_);

            // E Rules
            AddRule('E', "#:", "E", " ", (byte)0); // Silent E
            AddRule('E', "'", "E", " ", (byte)0);
            AddRule('E', " :", "ED", " ", _d_);
            AddRule('E', "#:", "ED", " ", (byte)0);
            AddRule('E', "", "EV", "ER", _EH_, _v_);
            AddRule('E', "", "E", "R#", _IY_, _r_);
            AddRule('E', "", "ERI", "#", _IY_, _r_, _IY_);
            AddRule('E', "", "ERI", "", _EH_, _r_, _IH_);
            AddRule('E', "#", "EER", "", _IY_, _r_);
            AddRule('E', "", "EER", "", _IH_, _r_);
            AddRule('E', "", "ER", "", _ER_);
            AddRule('E', "", "EW", "", _y_, _UW_);
            AddRule('E', "@", "EW", "", _UW_);
            AddRule('E', "", "E", "O", _IY_);
            AddRule('E', "#:&", "ES", " ", _IH_, _z_);
            AddRule('E', "#:", "ES", " ", _z_);
            AddRule('E', "#:", "ELY", " ", _l_, _IY_);
            AddRule('E', "#:", "EMENT", "", _m_, _EH_, _n_, _t_);
            AddRule('E', "", "EFUL", "", _f_, _UH_, _l_);
            AddRule('E', "", "EE", "", _IY_);
            AddRule('E', "", "EARN", "", _ER_, _n_);
            AddRule('E', "", "EAR", "#", _IY_, _r_);
            AddRule('E', "", "EAD", "", _EH_, _d_);
            AddRule('E', "#", "EA", "", _IY_, _AX_);
            AddRule('E', "", "EA", "SU", _EH_);
            AddRule('E', "", "EA", "", _IY_);
            AddRule('E', "", "EIGH", "", _EY_);
            AddRule('E', "", "EI", "", _IY_);
            AddRule('E', "", "EYE", "", _AY_);
            AddRule('E', "", "EY", "", _IY_);
            AddRule('E', "", "EU", "", _y_, _UW_);
            AddRule('E', "", "E", "", _EH_);

            // F Rules
            AddRule('F', "", "FUL", "", _f_, _UH_, _l_);
            AddRule('F', "", "F", "", _f_);

            // G Rules
            AddRule('G', "", "GIV", "", _g_, _IH_, _v_);
            AddRule('G', "", "G", "E", _JH_);
            AddRule('G', "", "G", "I", _JH_);
            AddRule('G', "", "G", "Y", _JH_);
            AddRule('G', " ", "GET", "", _g_, _EH_, _t_);
            AddRule('G', "#", "G", "%", _g_);
            AddRule('G', "", "G", "+", _JH_);
            AddRule('G', "", "GG", "", _g_);
            AddRule('G', "", "G", "", _g_);

            // H Rules
            AddRule('H', " ", "HAV", " ", _HH_(), _AE_, _v_);
            AddRule('H', " ", "HERE", " ", _HH_(), _IY_, _r_);
            AddRule('H', " ", "HOUR", " ", _AW_, _ER_);
            AddRule('H', "", "HOW", "", _HH_(), _AW_);
            AddRule('H', "", "H", "#", _HH_());
            AddRule('H', "", "H", "", (byte)0); // Silent H

            // I Rules
            AddRule('I', " ", "IN", " ", _IH_, _n_);
            AddRule('I', " ", "I", " ", _AY_);
            AddRule('I', "", "IR", "#", _AY_, _r_);
            AddRule('I', "", "IER", "", _IY_, _ER_);
            AddRule('I', "", "IED", " ", _AY_, _d_);
            AddRule('I', "", "IED", "", _IY_, _d_);
            AddRule('I', "", "IEN", "", _IY_, _EH_, _n_);
            AddRule('I', "", "IE", "T", _AY_, _EH_);
            AddRule('I', " ", "IE", "", _AY_);
            AddRule('I', "", "I", "%", _AY_);
            AddRule('I', "", "I", "LD", _AY_);
            AddRule('I', "", "I", "GN", _AY_);
            AddRule('I', "", "I", "GHT", _AY_);
            AddRule('I', "", "IR", "", _ER_);
            AddRule('I', "", "I", "", _IH_);

            // J Rules
            AddRule('J', "", "J", "", _JH_);

            // K Rules
            AddRule('K', " ", "KIN", "G", _k_, _IH_, _n_);
            AddRule('K', "", "K", "N", (byte)0);
            AddRule('K', "", "K", "", _k_);

            // L Rules
            AddRule('L', "", "LO", "C#", _l_, _OW_);
            AddRule('L', "L", "L", "", (byte)0);
            AddRule('L', "#", "L", "%", _AX_, _l_);
            AddRule('L', "", "LEAD", "", _l_, _IY_, _d_);
            AddRule('L', "", "L", "", _l_);

            // M Rules
            AddRule('M', "", "M", "", _m_);

            // N Rules
            AddRule('N', "E", "NG", "+", _n_, _JH_);
            AddRule('N', "", "NG", "R", _NG_, _g_);
            AddRule('N', "", "NG", "#", _NG_, _g_);
            AddRule('N', "", "NG", "L", _NG_, _g_);
            AddRule('N', "", "NG", "", _NG_);
            AddRule('N', "", "NK", "", _NG_, _k_);
            AddRule('N', "", "N", "", _n_);

            // O Rules
            AddRule('O', "", "OF", " ", _AH_, _v_);
            AddRule('O', "", "OROUGH", "", _ER_, _OW_);
            AddRule('O', "#", "OR", "", _ER_);
            AddRule('O', "#", "ORS", " ", _ER_, _z_);
            AddRule('O', "", "OR", "", _AO_, _r_);
            AddRule('O', "", "ONE", "", _w_, _AH_, _n_);
            AddRule('O', "", "OW", "", _OW_);
            AddRule('O', "", "OVER", "", _OW_, _v_, _ER_);
            AddRule('O', "", "OV", "", _AH_, _v_);
            AddRule('O', "", "O", "%", _OW_);
            AddRule('O', "", "O", "^%", _OW_);
            AddRule('O', "", "O", "^+ ", _OW_);
            AddRule('O', "", "O", "^EH ", _OW_);
            AddRule('O', "", "O", "^ER ", _OW_);
            AddRule('O', "", "O", "^EY ", _OW_);
            AddRule('O', "", "O", " =", _OW_);
            AddRule('O', "#", "O", " ", _OW_);
            AddRule('O', "", "OA", "", _OW_);
            AddRule('O', " ", "ONLY", "", _OW_, _n_, _l_, _IY_);
            AddRule('O', " ", "ONCE", "", _w_, _AH_, _n_, _s_);
            AddRule('O', "", "O'C", "", _OW_);
            AddRule('O', "", "O", "", _AA_);

            // P Rules
            AddRule('P', "", "PH", "", _f_);
            AddRule('P', "", "PEOP", "", _p_, _IY_, _p_);
            AddRule('P', "", "POW", "", _p_, _AW_);
            AddRule('P', "", "PUT", " ", _p_, _UH_, _t_);
            AddRule('P', "", "P", "", _p_);

            // Q Rules
            AddRule('Q', "", "QUAR", "", _k_, _w_, _AO_, _r_);
            AddRule('Q', "", "QU", "", _k_, _w_);
            AddRule('Q', "", "Q", "", _k_);

            // R Rules
            AddRule('R', "", "RE", "#", _r_, _IY_);
            AddRule('R', "", "R", "", _r_);

            // S Rules
            AddRule('S', "", "SH", "", _SH_);
            AddRule('S', "#", "SION", "", _ZH_, _AX_, _n_);
            AddRule('S', "", "SOME", "", _s_, _AH_, _m_);
            AddRule('S', "#", "SUR", "#", _ZH_, _ER_);
            AddRule('S', "", "SUR", "#", _SH_, _ER_);
            AddRule('S', "#", "SU", "#", _ZH_, _UW_);
            AddRule('S', "", "SU", "#", _SH_, _UW_);
            AddRule('S', "#", "SED", " ", _z_, _d_);
            AddRule('S', "#", "S", "#", _z_);
            AddRule('S', " ", "SAID", "", _s_, _EH_, _d_);
            AddRule('S', "", "SION", "", _SH_, _AX_, _n_);
            AddRule('S', "", "S", "s", (byte)0);
            AddRule('S', ".", "S", " ", _z_);
            AddRule('S', "#", "S", " ", _z_);
            AddRule('S', "", "S", "", _s_);

            // T Rules
            AddRule('T', " ", "THE", " ", _DH_, _AX_);
            AddRule('T', " ", "TO", " ", _t_, _UW_);
            AddRule('T', " ", "THAT", " ", _DH_, _AE_, _t_);
            AddRule('T', " ", "THIS", " ", _DH_, _IH_, _s_);
            AddRule('T', " ", "THEY", " ", _DH_, _EY_);
            AddRule('T', " ", "THERE", " ", _DH_, _EH_, _r_);
            AddRule('T', "", "THER", "", _DH_, _ER_);
            AddRule('T', "", "THEIR", "", _DH_, _EH_, _r_);
            AddRule('T', " ", "THAN", " ", _DH_, _AE_, _n_);
            AddRule('T', " ", "THEM", " ", _DH_, _EH_, _m_);
            AddRule('T', "", "THESE", " ", _DH_, _IY_, _z_);
            AddRule('T', " ", "THEN", " ", _DH_, _EH_, _n_);
            AddRule('T', " ", "THROUGH", "", _TH_, _r_, _UW_);
            AddRule('T', " ", "THOSE", " ", _DH_, _OW_, _z_);
            AddRule('T', " ", "THOUGH", " ", _DH_, _OW_);
            AddRule('T', " ", "THUS", " ", _DH_, _AH_, _s_);
            AddRule('T', "#", "TED", " ", _t_, _IH_, _d_);
            AddRule('T', "S", "TI", "#", _CH_);
            AddRule('T', "", "TI", "O", _SH_);
            AddRule('T', "", "TI", "A", _SH_);
            AddRule('T', "", "TI", "EN", _SH_);
            AddRule('T', "", "TUR", "#", _CH_, _ER_);
            AddRule('T', "", "TU", "A", _CH_, _UW_);
            AddRule('T', " ", "TWO", " ", _t_, _UW_);
            AddRule('T', "", "TH", "", _TH_);
            AddRule('T', "", "T", "", _t_);

            // U Rules
            AddRule('U', " ", "UN", "I", _y_, _UW_, _n_);
            AddRule('U', " ", "UN", "", _AH_, _n_);
            AddRule('U', "", "UPON", "", _AX_, _p_, _AO_, _n_);
            AddRule('U', "@", "UR", "#", _UH_, _r_);
            AddRule('U', "", "UR", "#", _y_, _UH_, _r_);
            AddRule('U', "", "U", " ", _AH_);
            AddRule('U', "", "UY", "", _AY_);
            AddRule('U', " G", "U", "#", (byte)0);
            AddRule('U', "G", "U", "%", (byte)0);
            AddRule('U', "G", "U", "+", _w_);
            AddRule('U', "#N", "U", "", _y_, _UW_);
            AddRule('U', "@", "U", "", _UW_);
            AddRule('U', "", "U", "", _y_, _UW_);

            // V Rules
            AddRule('V', "", "V", "", _v_);

            // W Rules
            AddRule('W', " ", "WERE", " ", _w_, _ER_);
            AddRule('W', "", "WA", "S", _w_, _AA_);
            AddRule('W', "", "WA", "T", _w_, _AA_);
            AddRule('W', "", "WHERE", "", _w_, _EH_, _r_);
            AddRule('W', "", "WA", "SH", _w_, _AA_);
            AddRule('W', "", "WHOL", "", _HH_(), _OW_, _l_);
            AddRule('W', "", "WHO", "", _HH_(), _UW_);
            AddRule('W', "", "WH", "", _w_); // Simplified
            AddRule('W', "", "WAR", "", _w_, _AO_, _r_);
            AddRule('W', "", "W", "", _w_);

            // X Rules
            AddRule('X', "", "X", "", _k_, _s_);

            // Y Rules
            AddRule('Y', "", "YOUNG", "", _y_, _AH_, _NG_);
            AddRule('Y', "", "YOU", "", _y_, _UW_);
            AddRule('Y', "", "YES", "", _y_, _EH_, _s_);
            AddRule('Y', "#", "Y", " ", _IY_);
            AddRule('Y', "#", "Y", "#", _IY_);
            AddRule('Y', "#", "Y", "", _IH_);
            AddRule('Y', "", "Y", "", _y_);

            // Z Rules
            AddRule('Z', "", "Z", "", _z_);
        }

        private static byte _HH_() => _h_;

        private static void AddRule(char firstLetter, string left, string mid, string right, params byte[] phons)
        {
            if (!_rules.ContainsKey(firstLetter)) _rules[firstLetter] = new List<Rule>();
            _rules[firstLetter].Add(new Rule(left, mid, right, phons));
        }

        public static byte[] Convert(string input)
        {
            if (string.IsNullOrEmpty(input)) return Array.Empty<byte>();

            // Normalize input: uppercase and pad with spaces for context
            input = " " + input.ToUpperInvariant() + " ";
            var result = new List<byte>();

            int pos = 1;
            while (pos < input.Length - 1)
            {
                char c = input[pos];
                bool matched = false;

                if (_rules.TryGetValue(c, out var rules))
                {
                    foreach (var rule in rules)
                    {
                        if (MatchRule(input, pos, rule))
                        {
                            foreach (var p in rule.Phonemes)
                            {
                                if (p != 0) result.Add(p);
                            }
                            pos += rule.Mid.Length;
                            matched = true;
                            break;
                        }
                    }
                }

                if (!matched)
                {
                    // Fallback for unknown characters
                    pos++;
                }
            }

            return result.ToArray();
        }

        private static bool MatchRule(string input, int pos, Rule rule)
        {
            // Match Mid (the bracketed part)
            if (pos + rule.Mid.Length >= input.Length) return false;
            for (int i = 0; i < rule.Mid.Length; i++)
            {
                if (input[pos + i] != rule.Mid[i]) return false;
            }

            // Match Left
            if (!MatchContext(input, pos - 1, rule.Left, -1)) return false;

            // Match Right
            if (!MatchContext(input, pos + rule.Mid.Length, rule.Right, 1)) return false;

            return true;
        }

        private static bool MatchContext(string input, int pos, string pattern, int direction)
        {
            if (string.IsNullOrEmpty(pattern)) return true;

            int patternIdx = (direction > 0) ? 0 : pattern.Length - 1;
            int inputIdx = pos;

            while (patternIdx >= 0 && patternIdx < pattern.Length)
            {
                char p = pattern[patternIdx];

                if (direction > 0) // Right context
                {
                    if (inputIdx >= input.Length) return false;
                    if (!MatchChar(input, ref inputIdx, p, direction)) return false;
                    patternIdx++;
                }
                else // Left context
                {
                    if (inputIdx < 0) return false;
                    if (!MatchChar(input, ref inputIdx, p, direction)) return false;
                    patternIdx--;
                }
            }

            return true;
        }

        private static bool MatchChar(string input, ref int inputIdx, char p, int direction)
        {
            switch (p)
            {
                case '#': // 1 or more vowels
                    if (!IsVowel(input[inputIdx])) return false;
                    while (inputIdx >= 0 && inputIdx < input.Length && IsVowel(input[inputIdx])) inputIdx += direction;
                    return true;
                case '*': // 1 or more consonants
                    if (!IsConsonant(input[inputIdx])) return false;
                    while (inputIdx >= 0 && inputIdx < input.Length && IsConsonant(input[inputIdx])) inputIdx += direction;
                    return true;
                case '.': // Voiced consonant
                    if (!IsVoicedConsonant(input[inputIdx])) return false;
                    inputIdx += direction;
                    return true;
                case '$': // Consonant followed by E or I (simplified as consonant)
                    if (!IsConsonant(input[inputIdx])) return false;
                    inputIdx += direction;
                    return true;
                case '%': // Suffix (simplified)
                    // In a real implementation, we'd check for ER, E, ES, etc.
                    // For now, let's just match a vowel or end of word
                    if (input[inputIdx] != ' ' && !IsVowel(input[inputIdx])) return false;
                    inputIdx += direction;
                    return true;
                case '&': // Sibilant
                    if (!IsSibilant(input[inputIdx])) return false;
                    inputIdx += direction;
                    return true;
                case '@': // Long-u influencing consonant
                    if (!IsLongUInfluence(input[inputIdx])) return false;
                    inputIdx += direction;
                    return true;
                case '^': // Single consonant
                    if (!IsConsonant(input[inputIdx])) return false;
                    inputIdx += direction;
                    return true;
                case '+': // Front vowel
                    if (!IsFrontVowel(input[inputIdx])) return false;
                    inputIdx += direction;
                    return true;
                case ':': // Zero or more consonants
                    while (inputIdx >= 0 && inputIdx < input.Length && IsConsonant(input[inputIdx])) inputIdx += direction;
                    return true;
                default:
                    if (input[inputIdx] != p) return false;
                    inputIdx += direction;
                    return true;
            }
        }

        private static bool IsVowel(char c) => "AEIOUY".Contains(c);
        private static bool IsConsonant(char c) => char.IsLetter(c) && !IsVowel(c);
        private static bool IsVoicedConsonant(char c) => "BDVGJLMNRWZ".Contains(c);
        private static bool IsSibilant(char c) => "SCGZXJ".Contains(c);
        private static bool IsLongUInfluence(char c) => "TSRDLZN J".Contains(c);
        private static bool IsFrontVowel(char c) => "EIY".Contains(c);
    }
}
