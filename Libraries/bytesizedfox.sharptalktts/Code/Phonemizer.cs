#nullable enable
using System;
using System.Collections.Generic;
#if !SANDBOX
using System.Reflection;
#endif
using System.Text.RegularExpressions;
using static SharpTalk.AudioProcessor;
using static SharpTalk.Phonemizer.Normalizer;

namespace SharpTalk
{

    public class Phonemizer
    {
        readonly DictReader _dict;
        readonly DictReader _symbols;

        // Opcodes that are control codes, not actual phonemes (56-72)
        const byte OP_STRESS1 = 56;  // _Stress1_  → kPrimaryStress
        const byte OP_STRESS2 = 57;  // _Stress2_  → kSecondaryStress
        const byte OP_EMPHSTRESS = 58;  // _EmphStress_ → kEmphaticStress
        const byte OP_SYLL = 63;  // _Syll_     → kSyllable_Start
        const byte OP_WORD = 64;  // _Word_     → kWord_Start
        const byte OP_PREP = 65;  // _Prep_     → kPrep_Start
        const byte OP_VERB = 66;  // _Verb_     → kVerb_Start
        const byte OP_COMMA = 67;  // _Comma_
        const byte OP_PERIOD = 68;  // _Period_
        const byte OP_QUEST = 69;  // _Quest_
        const byte OP_EXCLAM = 70;  // _Exclam_

        // Function words — do NOT receive kContent_Word; primary dict stress is
        // suppressed so they don't drive pitch peaks in the BackEnd pitch algorithm.
        // Mirrors POS-based content/function distinction.
        static readonly HashSet<string> FuncWords = new(StringComparer.OrdinalIgnoreCase)
    {
        // articles / determiners
        "a", "an", "the",
        // prepositions
        "of", "in", "on", "at", "by", "for", "to", "up", "as", "into",
        "from", "with", "about", "over", "under", "out", "off", "than",
        // coordinating conjunctions
        "and", "or", "but", "nor", "yet", "so",
        // subordinating conjunctions
        "if", "that", "than", "when", "while", "because", "though",
        "although", "unless", "until", "since", "after", "before",
        // auxiliaries & copula
        "be", "am", "is", "are", "was", "were", "been", "being",
        "have", "has", "had", "do", "does", "did",
        "will", "would", "could", "should", "may", "might", "shall",
        "can", "must", "ought",
        // subject / object pronouns
        "i", "he", "she", "we", "they", "you", "it",
        "me", "him", "her", "us", "them",
        // possessive determiners
        "my", "your", "his", "its", "our", "their",
        // other function words
        "not", "no", "there", "here",
    };

        static readonly Regex TokenRe = new(
            @"(\d+)|([a-zA-Z]+(?:'[a-zA-Z]+)*)|([,;:])|([.!?])|(\s+)",
            RegexOptions.Compiled);

        static readonly Regex CamelSplit = new(
            @"(?<=[a-z])(?=[A-Z])|(?<=[A-Z])(?=[A-Z][a-z])",
            RegexOptions.Compiled);

#if !SANDBOX
        public Phonemizer()
        {
            var asm = Assembly.GetExecutingAssembly();
            _dict = LoadEmbedded(asm, "SharpTalk.english_lex.bin");
            _symbols = LoadEmbedded(asm, "SharpTalk.symbols.bin");
        }
#endif

        public Phonemizer(byte[] dictData, byte[] symbolsData)
        {
            _dict = new DictReader(dictData);
            _symbols = new DictReader(symbolsData);
        }

#if !SANDBOX
        static DictReader LoadEmbedded(Assembly asm, string name)
        {
            using var s = asm.GetManifestResourceStream(name)
                ?? throw new Exception($"Missing embedded resource: {name}");
            var buf = new byte[(int)s.Length];
            int read = 0;
            while (read < buf.Length)
            {
                int n = s.Read(buf, read, buf.Length - read);
                if (n == 0) throw new System.IO.EndOfStreamException();
                read += n;
            }
            return new DictReader(buf);
        }
#endif

        public short LastEndPunct { get; private set; } = _Period_;

        public (PhonemeToken[] Tokens, short EndPunct)[] TextToSentenceTokens(string text)
        {
            var result = new List<(PhonemeToken[], short)>();
            var segments = EmbeddedCmd.ParseSegments(text);

            foreach (var seg in segments)
            {
                if (seg.IsCommand) continue; // handled by TtsEngine, not FrontEnd

                if (seg.IsSinging)
                {
                    // Each singing block is its own clause — never mix with speech
                    if (seg.Singing!.Count > 0)
                        result.Add((seg.Singing.ToArray(), _Period_));
                    continue;
                }

                // Split at sentence boundaries (.!?) and clause boundaries (,;:).
                // Each clause gets its own BackEnd.Process call so pitch resets cleanly.
                string plain = Normalize(seg.PlainText!);
                int start = 0;
                foreach (Match m in TokenRe.Matches(plain))
                {
                    if (!m.Groups[4].Success && !m.Groups[3].Success) continue;
                    string sentence = plain[start..(m.Index + m.Length)];
                    var tokens = TextSegmentToPhonemes(sentence);
                    result.Add((tokens, LastEndPunct));
                    start = m.Index + m.Length;
                }
                if (start < plain.Length)
                {
                    string remaining = plain[start..];
                    if (remaining.Trim().Length > 0)
                    {
                        var tokens = TextSegmentToPhonemes(remaining);
                        result.Add((tokens, LastEndPunct));
                    }
                }
            }

            if (result.Count == 0)
            {
                var tokens = TextToPhonemes(text);
                result.Add((tokens, LastEndPunct));
            }

            return result.ToArray();
        }

        // Process a pure-text span (no embedded commands) into phoneme tokens.
        private PhonemeToken[] TextSegmentToPhonemes(string text)
        {
            text = Normalize(text);
            var tokens = new List<PhonemeToken>();
            LastEndPunct = _Period_;

            foreach (Match m in TokenRe.Matches(text))
            {
                if (m.Groups[1].Success)
                {
                    if (long.TryParse(m.Groups[1].Value, out long n))
                        AppendWordTokens(tokens, NumberToPhonStream(n), isContent: true);
                }
                else if (m.Groups[2].Success)
                {
                    string word = m.Groups[2].Value;
                    AppendWordTokens(tokens, WordToPhonStream(word.ToUpperInvariant()), !FuncWords.Contains(word));
                }
                else if (m.Groups[3].Success)
                {
                    tokens.Add(new PhonemeToken
                    {
                        Phon = _SIL_,
                        Ctrl = kTerm_Bound | ((long)kBND_Pause << kSilenceTypeShift),
                    });
                    LastEndPunct = _Comma_;
                }
                else if (m.Groups[4].Success)
                {
                    char p = m.Groups[4].Value[0];
                    LastEndPunct = p == '?' ? _Quest_ : p == '!' ? _Exclam_ : _Period_;
                }
            }

            return tokens.ToArray();
        }

        public PhonemeToken[] TextToPhonemes(string text)
        {
            var tokens = new List<PhonemeToken>();
            LastEndPunct = _Period_;

            // Split into ordered segments (plain text spans interleaved with singing blocks)
            var segments = EmbeddedCmd.ParseSegments(text);

            foreach (var seg in segments)
            {
                if (seg.IsCommand) continue; // handled by TtsEngine, not FrontEnd

                if (seg.IsSinging)
                {
                    tokens.AddRange(seg.Singing!);
                    continue;
                }

                foreach (Match m in TokenRe.Matches(Normalize(seg.PlainText!)))
                {
                    if (m.Groups[1].Success)           // number
                    {
                        if (long.TryParse(m.Groups[1].Value, out long n))
                            AppendWordTokens(tokens, NumberToPhonStream(n), isContent: true);
                    }
                    else if (m.Groups[2].Success)      // word
                    {
                        string word = m.Groups[2].Value;
                        bool isContent = !FuncWords.Contains(word);
                        AppendWordTokens(tokens, WordToPhonStream(word.ToUpperInvariant()), isContent);
                    }
                    else if (m.Groups[3].Success)      // , ;
                    {
                        tokens.Add(new PhonemeToken
                        {
                            Phon = _SIL_,
                            Ctrl = kTerm_Bound | ((long)kBND_Pause << kSilenceTypeShift),
                        });
                        LastEndPunct = _Comma_;
                    }
                    else if (m.Groups[4].Success)      // . ! ?
                    {
                        char p = m.Groups[4].Value[0];
                        LastEndPunct = p == '?' ? _Quest_ : p == '!' ? _Exclam_ : _Period_;
                    }
                    // whitespace: skip
                }
            }

            return tokens.ToArray();
        }

        // Text normalization
        // Nested static class keeps normalizer state (regexes, tables) out of the
        // FrontEnd field list without a separate file.
        internal static class Normalizer
        {
            static readonly Regex ReCurrency = new(
                @"\$\s*(\d+)(?:\.(\d{1,2}))?", RegexOptions.Compiled);
            static readonly Regex RePercent = new(
                @"(\d+)\s*%", RegexOptions.Compiled);
            static readonly Regex ReOrdinal = new(
                @"\b(\d+)\s*(?:st|nd|rd|th)\b", RegexOptions.Compiled | RegexOptions.IgnoreCase);
            static readonly Regex ReDecimal = new(
                @"\b(\d+)\.(\d+)\b", RegexOptions.Compiled);
            static readonly Regex ReAcronym = new(
                @"\b[A-Z]{2,}\b", RegexOptions.Compiled);
            static readonly string[] LetterNames =
            {
                "ay","bee","see","dee","ee","ef","gee","aitch","eye","jay",
                "kay","el","em","en","oh","pee","cue","ar","ess","tee",
                "you","vee","double you","ex","why","zee"
            };
            static readonly Regex ReAbbrev = new(
                @"\b(Dr|Mr|Mrs|Ms|Prof|Jr|Sr|Vs|Etc|St|Ave|Blvd|Rd|Ln"
              + @"|Jan|Feb|Mar|Apr|Jun|Jul|Aug|Sep|Sept|Oct|Nov|Dec"
              + @"|Lt|Cpt|Capt|Gen|Sgt|Pvt|Col|Maj|Rev|Dept|Inc|Corp|Approx)\.",
                RegexOptions.Compiled | RegexOptions.IgnoreCase);

            static readonly Dictionary<string, string> AbbrevMap =
                new(StringComparer.OrdinalIgnoreCase)
                {
                    ["Dr"] = "Doctor",
                    ["Mr"] = "Mister",
                    ["Mrs"] = "Missus",
                    ["Ms"] = "Miss",
                    ["Prof"] = "Professor",
                    ["Jr"] = "Junior",
                    ["Sr"] = "Senior",
                    ["Vs"] = "versus",
                    ["Etc"] = "etcetera",
                    ["St"] = "Saint",
                    ["Ave"] = "Avenue",
                    ["Blvd"] = "Boulevard",
                    ["Rd"] = "Road",
                    ["Ln"] = "Lane",
                    ["Lt"] = "Lieutenant",
                    ["Cpt"] = "Captain",
                    ["Capt"] = "Captain",
                    ["Gen"] = "General",
                    ["Sgt"] = "Sergeant",
                    ["Pvt"] = "Private",
                    ["Col"] = "Colonel",
                    ["Maj"] = "Major",
                    ["Rev"] = "Reverend",
                    ["Dept"] = "Department",
                    ["Inc"] = "Incorporated",
                    ["Corp"] = "Corporation",
                    ["Approx"] = "approximately",
                    ["Jan"] = "January",
                    ["Feb"] = "February",
                    ["Mar"] = "March",
                    ["Apr"] = "April",
                    ["Jun"] = "June",
                    ["Jul"] = "July",
                    ["Aug"] = "August",
                    ["Sep"] = "September",
                    ["Sept"] = "September",
                    ["Oct"] = "October",
                    ["Nov"] = "November",
                    ["Dec"] = "December",
                };

            static readonly string[] DigitWords =
                new string[] { "zero", "one", "two", "three", "four", "five", "six", "seven", "eight", "nine" };

            static readonly string[] OnesOrd = new string[]
            {
            "zeroth","first","second","third","fourth","fifth","sixth","seventh",
            "eighth","ninth","tenth","eleventh","twelfth","thirteenth","fourteenth",
            "fifteenth","sixteenth","seventeenth","eighteenth","nineteenth",
            };
            static readonly string[] TensOrd = new string[]
                {"","","twentieth","thirtieth","fortieth","fiftieth",
             "sixtieth","seventieth","eightieth","ninetieth"};
            static readonly string[] TensWords = new string[]
                {"","","twenty","thirty","forty","fifty","sixty","seventy","eighty","ninety"};

            static string OrdinalToWord(long n)
            {
                if (n < 0) return n.ToString();
                if (n < 20) return OnesOrd[n];
                if (n < 100)
                {
                    int t = (int)(n / 10), o = (int)(n % 10);
                    return o == 0 ? TensOrd[t] : TensWords[t] + " " + OnesOrd[o];
                }
                return n.ToString(); // cardinal fallback for 100+ (rare as ordinal)
            }

            public static string Normalize(string text)
            {
                // 0. Split CamelCase/PascalCase so "SharpTalk" → "Sharp Talk"
                text = CamelSplit.Replace(text, " ");

                // 0.5 Spell out consonant-only acronyms letter by letter ("TTS" → "tee tee ess").
                // Vowel-containing all-caps are left as pronounceable words (NASA, RADAR, etc.).
                text = ReAcronym.Replace(text, m =>
                {
                    foreach (char c in m.Value)
                        if (c is 'A' or 'E' or 'I' or 'O' or 'U') return m.Value;
                    var names = new string[m.Value.Length];
                    for (int i = 0; i < m.Value.Length; i++)
                        names[i] = LetterNames[m.Value[i] - 'A'];
                    return string.Join(" ", names);
                });

                // 1. Currency — before decimal so $3.99 isn't split at the dot
                text = ReCurrency.Replace(text, m =>
                {
                    long dollars = long.Parse(m.Groups[1].Value);
                    string r = dollars + " dollar" + (dollars == 1 ? "" : "s");
                    if (m.Groups[2].Success)
                    {
                        string cs = m.Groups[2].Value.PadRight(2, '0')[..2];
                        long cents = long.Parse(cs);
                        if (cents > 0)
                            r += " and " + cents + " cent" + (cents == 1 ? "" : "s");
                    }
                    return r;
                });

                // 2. Percentages
                text = RePercent.Replace(text, m => m.Groups[1].Value + " percent");

                // 3. Ordinals — before decimals to avoid "1.5th" oddities
                text = ReOrdinal.Replace(text, m => OrdinalToWord(long.Parse(m.Groups[1].Value)));

                // 4. Decimal numbers — spell each digit after the point individually
                text = ReDecimal.Replace(text, m =>
                {
                    string r = m.Groups[1].Value + " point";
                    foreach (char c in m.Groups[2].Value)
                        r += " " + DigitWords[c - '0'];
                    return r;
                });

                // 5. Abbreviations — expand so their period doesn't trigger sentence split
                text = ReAbbrev.Replace(text, m => AbbrevMap[m.Groups[1].Value]);

                // 6. Hyphens → space (compound words read naturally)
                text = text.Replace('-', ' ');

                return text;
            }
        }

        // Word → raw phoneme stream

        byte[] WordToPhonStream(string upperWord)
        {
            // 1. Try dictionary directly
            byte[]? phons = _dict.Search(upperWord);

            // 2. Try morphological decomposition (suffix stripping + root lookup)
            phons ??= Morph.TryDecompose(upperWord, _dict);

            // 3. Fall back to letter-to-sound rules
            phons ??= PhonemizerNRL.Convert(upperWord);

            // Prepend OP_WORD marker
            var buf = new byte[phons.Length + 1];
            buf[0] = OP_WORD;
            phons.CopyTo(buf, 1);
            return buf;
        }

        // Number → raw phoneme stream

        byte[] NumberToPhonStream(long n)
        {
            var buf = new List<byte>();
            BuildNumberPhons(buf, n);
            return buf.ToArray();
        }

        void BuildNumberPhons(List<byte> buf, long n)
        {
            if (n < 0) { AppendSymbol(buf, "MINUS"); BuildNumberPhons(buf, -n); return; }
            if (n == 0) { AppendSymbol(buf, "0"); return; }

            if (n >= 1_000_000)
            {
                BuildNumberPhons(buf, n / 1_000_000);
                AppendSymbol(buf, "MILLION");
                n %= 1_000_000;
            }
            if (n >= 1_000)
            {
                BuildNumberPhons(buf, n / 1_000);
                AppendSymbol(buf, "THOUSAND");
                n %= 1_000;
            }
            if (n >= 100)
            {
                AppendDigit(buf, (int)(n / 100));
                AppendSymbol(buf, "HUNDRED");
                n %= 100;
            }
            if (n >= 20)
            {
                AppendTens(buf, (int)(n / 10));
                n %= 10;
                if (n > 0) AppendDigit(buf, (int)n);
            }
            else if (n >= 10)
            {
                AppendTeen(buf, (int)n);
            }
            else if (n > 0)
            {
                AppendDigit(buf, (int)n);
            }
        }

        static readonly string[] DigitNames = new string[] { "0", "1", "2", "3", "4", "5", "6", "7", "8", "9" };
        static readonly string[] TeenNames = new string[] { "10", "11", "12", "13", "14", "15", "16", "17", "18", "19" };
        static readonly string[] TensNames = new string[] { "", "", "20", "30", "40", "50", "60", "70", "80", "90" };

        void AppendDigit(List<byte> buf, int d) => AppendSymbol(buf, DigitNames[d]);
        void AppendTeen(List<byte> buf, int n) => AppendSymbol(buf, TeenNames[n - 10]);
        void AppendTens(List<byte> buf, int t) => AppendSymbol(buf, TensNames[t]);

        void AppendSymbol(List<byte> buf, string sym)
        {
            if (buf.Count == 0) buf.Add(OP_WORD);
            byte[]? phons = _symbols.Search(sym);
            if (phons == null) return;
            buf.AddRange(phons);
        }

        // Stream → PhonemeToken list

        void AppendWordTokens(List<PhonemeToken> tokens, byte[] stream, bool isContent)
        {
            long pending = 0;
            int startIdx = tokens.Count;
            bool hadPrimary = false;

            foreach (byte b in stream)
            {
                switch (b)
                {
                    case OP_WORD:
                        pending |= kWord_Start;
                        if (isContent) pending |= kContent_Word;
                        break;
                    case OP_STRESS1:
                        // Function words: demote dict primary stress to secondary so they
                        // don't trigger pitch peaks in the BackEnd pitch algorithm.
                        if (isContent) { pending |= kPrimaryStress; hadPrimary = true; }
                        else pending |= kSecondaryStress;
                        break;
                    case OP_STRESS2: pending |= kSecondaryStress; break;
                    case OP_EMPHSTRESS: pending |= kEmphaticStress; break;
                    case OP_SYLL: pending |= kSyllable_Start; break;
                    case OP_PREP: pending |= kPrep_Start; break;
                    case OP_VERB: pending |= kVerb_Start; break;
                    case OP_COMMA:
                    case OP_PERIOD:
                    case OP_QUEST:
                    case OP_EXCLAM:
                        tokens.Add(new PhonemeToken { Phon = (short)b, Ctrl = kTerm_Bound });
                        pending = 0;
                        break;
                    default:
                        if (b <= 55)
                        {
                            tokens.Add(new PhonemeToken { Phon = (short)b, Ctrl = pending });
                            pending = 0;
                        }
                        break;
                }
            }

            // Content word with only secondary stress: promote to primary so the pitch
            // algorithm has a peak to work with on words like "how".
            if (isContent && !hadPrimary)
            {
                for (int i = startIdx; i < tokens.Count; i++)
                {
                    if ((tokens[i].Ctrl & kSecondaryStress) != 0)
                    {
                        tokens[i] = new PhonemeToken
                        {
                            Phon = tokens[i].Phon,
                            Ctrl = (tokens[i].Ctrl & ~kSecondaryStress) | kPrimaryStress,
                            UserPitch = tokens[i].UserPitch,
                            UserDur = tokens[i].UserDur,
                            UserNote = tokens[i].UserNote,
                            UserRate = tokens[i].UserRate,
                        };
                        break;
                    }
                }
            }
        }

    }
}  // namespace
