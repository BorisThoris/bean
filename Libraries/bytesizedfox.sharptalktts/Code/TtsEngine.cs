#nullable enable
using System;
using System.Collections.Generic;

namespace SharpTalk
{

    public readonly struct PhonemeEvent
    {
        public readonly short Phoneme;
        public readonly float TimeSeconds;
        public PhonemeEvent(short phoneme, float timeSeconds) { Phoneme = phoneme; TimeSeconds = timeSeconds; }
    }

    public sealed class TtsEngine
    {
        public const int SampleRate = 22050;

        private readonly Phonemizer _fe;
        private VoiceData _voice;
        private AudioProcessor _be = null!;
        private SpeechRenderer _renderer = null!;
        private SynthesizerKlatt _synth = null!;

#if !SANDBOX
        public TtsEngine() : this(VoiceData.BaselineVoice) { }

        public TtsEngine(VoiceData voice)
        {
            _voice = voice;
            _fe = new Phonemizer(LibraryData.EnglishLex, LibraryData.Symbols);
            RebuildPipeline();
        }
#endif

        public TtsEngine(byte[] dictData, byte[] symbolsData)
            : this(VoiceData.BaselineVoice, dictData, symbolsData) { }

        public TtsEngine(VoiceData voice, byte[] dictData, byte[] symbolsData)
        {
            _voice = voice;
            _fe = new Phonemizer(dictData, symbolsData);
            RebuildPipeline();
        }

        public VoiceData Voice
        {
            get => _voice;
            set { _voice = value; RebuildPipeline(); }
        }

        public void ApplyVoice() => RebuildPipeline();

        public short[] Speak(string text)
        {
            var samples = new List<short>();
            Speak(text, buf => samples.AddRange(buf));
            return samples.ToArray();
        }

        public void Speak(string text, Action<short[]> onBuffer)
        {
            foreach (var seg in EmbeddedCmd.ParseSegments(text))
            {
                if (seg.IsCommand) { ApplyCommand(seg.Cmd!.Value); continue; }
                if (seg.IsSinging) { ProcessSentence(seg.Singing!.ToArray(), AudioProcessor._Period_, onBuffer, null, ref _dummy); continue; }
                foreach (var (tokens, endPunct) in _fe.TextToSentenceTokens(seg.PlainText!))
                    ProcessSentence(tokens, endPunct, onBuffer, null, ref _dummy);
            }
        }

        /// Like Speak, but also returns a timeline of phoneme events with start times
        /// in seconds relative to the start of the returned audio.
        public (short[] audio, PhonemeEvent[] events) SpeakWithEvents(string text)
        {
            var samples = new List<short>();
            var events = new List<PhonemeEvent>();
            int sampleOffset = 0;
            foreach (var seg in EmbeddedCmd.ParseSegments(text))
            {
                if (seg.IsCommand) { ApplyCommand(seg.Cmd!.Value); continue; }
                if (seg.IsSinging) { ProcessSentence(seg.Singing!.ToArray(), AudioProcessor._Period_, buf => samples.AddRange(buf), events, ref sampleOffset); continue; }
                foreach (var (tokens, endPunct) in _fe.TextToSentenceTokens(seg.PlainText!))
                    ProcessSentence(tokens, endPunct, buf => samples.AddRange(buf), events, ref sampleOffset);
            }
            return (samples.ToArray(), events.ToArray());
        }

        // Internal helpers

        static int _dummy;

        void ProcessSentence(PhonemeToken[] tokens, short endPunct, Action<short[]> onBuffer,
                             List<PhonemeEvent>? events, ref int sampleOffset)
        {
            var dump = _be.Process(tokens, endPunct);

            if (events != null)
            {
                int frameOffset = 0;
                for (int i = 0; i < dump.PhonBuf2InIndex; i++)
                {
                    float t = (float)(sampleOffset + frameOffset * SynthesizerKlatt.KSampFrameLen) / SampleRate;
                    events.Add(new PhonemeEvent(dump.PhonBuf2[i], t));
                    frameOffset += dump.DurBuf[i];
                }
            }

            var frames = _renderer.Render(dump);
            var audio = new short[frames.Length * SynthesizerKlatt.KSampFrameLen];
            int offset = 0;
            foreach (var frame in frames)
            {
                _synth.SynthesizeFrame(frame, audio, offset);
                offset += SynthesizerKlatt.KSampFrameLen;
            }
            onBuffer(audio);
            sampleOffset += audio.Length;
        }

        void ApplyCommand(EmbeddedCmd.VoiceCommand cmd)
        {
            switch (cmd.Type)
            {
                case EmbeddedCmd.VoiceCommand.Kind.Rate:
                    _voice.Rate = (short)Math.Clamp(cmd.Value, 40, 600);
                    _be = new AudioProcessor(_voice);
                    break;
                case EmbeddedCmd.VoiceCommand.Kind.Pitch:
                    _voice.PitchHz = (short)Math.Clamp(cmd.Value, 40, 500);
                    _be = new AudioProcessor(_voice);
                    break;
                case EmbeddedCmd.VoiceCommand.Kind.Volume:
                    _voice.VGain = (short)Math.Clamp(cmd.Value, 0, 100);
                    _synth.InvDFT(_voice.VWave, _voice.VWave1, (short)_voice.VGain);
                    break;
            }
        }

        void RebuildPipeline()
        {
            _be = new AudioProcessor(_voice);
            _renderer = new SpeechRenderer(_voice);
            _synth = new SynthesizerKlatt();
            _synth.SetVoice(_voice.NGain, true,
                _voice.F4Freq, _voice.F4BW,
                _voice.F4pFreq, _voice.F4pBW,
                _voice.F5pFreq, _voice.F5pBW,
                _voice.F6pFreq, _voice.F6pBW,
                _voice.NasalBase, _voice.NasalBW,
                _voice.AGain, _voice.ACycle);
            _synth.InvDFT(_voice.VWave, _voice.VWave1, (short)_voice.VGain);
        }
    }
}  // namespace
