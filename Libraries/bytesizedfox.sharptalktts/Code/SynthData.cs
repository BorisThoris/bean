#nullable enable
namespace SharpTalk
{

    public sealed class SynthInputDump
    {
        public int PhonBuf2InIndex { get; }
        public short[] PhonBuf2 { get; }
        public long[] PhonCtrlBuf2 { get; }
        public short[] DurBuf { get; }
        public short[] UserPitchBuf2 { get; }
        public short[] UserNoteBuf2 { get; }

        public uint PitchBufInIndex { get; }
        public short[] PitchBufFreq { get; }
        public short[] PitchBufTime { get; }
        public short[] PitchBufFlags { get; }

        public PitchState Pitch { get; }

        private SynthInputDump(
            int phonBuf2InIndex,
            short[] phonBuf2,
            long[] controls,
            short[] durBuf,
            short[] userPitchBuf2,
            short[] userNoteBuf2,
            uint pitchBufInIndex,
            short[] pitchBufFreq,
            short[] pitchBufTime,
            short[] pitchBufFlags,
            PitchState pitch)
        {
            PhonBuf2InIndex = phonBuf2InIndex;
            PhonBuf2 = phonBuf2;
            PhonCtrlBuf2 = controls;
            DurBuf = durBuf;
            UserPitchBuf2 = userPitchBuf2;
            UserNoteBuf2 = userNoteBuf2;
            PitchBufInIndex = pitchBufInIndex;
            PitchBufFreq = pitchBufFreq;
            PitchBufTime = pitchBufTime;
            PitchBufFlags = pitchBufFlags;
            Pitch = pitch;
        }

        internal static SynthInputDump Create(
            int phonBuf2InIndex,
            short[] phonBuf2,
            long[] controls,
            short[] durBuf,
            short[] userPitchBuf2,
            short[] userNoteBuf2,
            uint pitchBufInIndex,
            short[] pitchBufFreq,
            short[] pitchBufTime,
            short[] pitchBufFlags,
            PitchState pitch)
        => new SynthInputDump(
            phonBuf2InIndex, phonBuf2, controls, durBuf,
            userPitchBuf2, userNoteBuf2,
            pitchBufInIndex, pitchBufFreq, pitchBufTime, pitchBufFlags,
            pitch);
    }

    public sealed class PitchState
    {
        public short NextPitchBufTime { get; set; }
        public short PitchBufOutIndex { get; set; }
        public short CurPitchBufTime { get; set; }
        public short CurPitchBufPitch { get; set; }
        public short CurPitchBufFlags { get; set; }

        public short PhonIndexTarg { get; set; }
        public short PhonIndexCp { get; set; }
        public short TimeIntoPhonTarg { get; set; }
        public short TimeIntoPhonCp { get; set; }
        public short CurPhonDurCc { get; set; }
        public short CurPhonDurCp { get; set; }
        public short PhonDurDelay { get; set; }

        public short UvPhonPitchTarg { get; set; }
        public short PhonPitchOffset { get; set; }
        public short PhonPitchOffset1 { get; set; }

        public short FallRiseOffset { get; set; }
        public short FallRise1Offset { get; set; }
        public short StressTarget { get; set; }
        public short PunctOffset { get; set; }
        public short StressActiveTime { get; set; }
        public short StressDuration { get; set; }

        public short BaseLineOffset { get; set; }
        public short BasePitchOffset { get; set; }
        public short PitchBoundry { get; set; }
        public short LowGainCp { get; set; }

        public short BaselineFallStart { get; set; }
        public short BaselineFallEnd { get; set; }
        public short BaselineStartOffset { get; set; }
        public short BaselineEndOffset { get; set; }

        public long DownRampOffset { get; set; }
        public long DownRampStep { get; set; }
        public long[] RampSteps { get; set; } = new long[16];
        public short CurRamp { get; set; }

        public long PFilterOut1 { get; set; }
        public long PFilterOut2 { get; set; }
        public long PFilterInGain { get; set; }
        public long PFilterFbGain { get; set; }

        public long VpIntonation { get; set; }
        public long VpPitchRange { get; set; }
        public short VpBaselinePitch { get; set; }

        public long VibratoDepth1 { get; set; }
        public long VibratoDepth2 { get; set; }
        public long VibratoFreq { get; set; }
        public int VibratoPhase1 { get; set; }

        public short Singing { get; set; }
        public short HzGlide { get; set; }
        public short MusicalNoteActive { get; set; }
        public long PortamentoAccum { get; set; }
        public long PortamentoStep { get; set; }
        public short NewPortaTarget { get; set; }
        public short NewSentence { get; set; }
        public short SpeechRate { get; set; }
    }
}  // namespace
