#nullable enable
using System;

namespace SharpTalk
{

    public class SynthesizerKlatt
    {
        public const int KMaxBandWidth = 1225;
        public const int KPrecision = 13;
        public const int KNoiseLen = 2048;
        public const int KOnePtOh = 0x2000;
        public const int KNoiseGain = 3200;
        public const int KSampFrameLen = 112;

        // Filter coefficients
        private short Acoeff1, Bcoeff1, Ccoeff1;
        private short Acoeff2, Bcoeff2, Ccoeff2;
        private short Acoeff3, Bcoeff3, Ccoeff3;
        private short Acoeff4, Bcoeff4, Ccoeff4;
        private short Acoeff4p, Bcoeff4p, Ccoeff4p;
        private short Acoeff5, Bcoeff5, Ccoeff5;
        private short Acoeff6, Bcoeff6, Ccoeff6;
        private short AcoeffNZ, BcoeffNZ, CcoeffNZ;
        private short AcoeffNP, BcoeffNP, CcoeffNP;

        // IIR delay taps
        private short Na1, Nb1;
        private short Na2, Nb2;
        private short Na3, Nb3;
        private short Na4, Nb4;
        private short Na5, Nb5;
        private short Na6, Nb6;
        private short Na2a, Nb2a;
        private short Na3a, Nb3a;
        private short Na4a, Nb4a;
        private short NaNZ, NbNZ;
        private short NaNP, NbNP;

        // Parallel bank input gains
        private short amp2, amp3, amp4, amp5, amp6, ab;

        // State
        private int glotIndex;
        private int noiseIndex;
        private short lastnSamp;
        private long curAmp_Full;
        private short curAmp;
        private short lastSample;
        private long ampStep;
        private long lastAmp;

        // Glottal excitation
        private int glotInc;
        private int glotInc1;
        private int glotIndex1;
        private short[] voiceWaveform = new short[256];
        private short[] voiceWaveform1 = new short[256];
        public short VoiceChorus { get; set; }
        public int GlotType { get; set; } = KUseHarm;
        public byte[]? SampleWave { get; set; }
        public int SampleInc { get; set; }
        public int SampleIndex { get; set; }

        public const int KUseHarm = 0;
        public const int KUseSnd = 1;
        public const int KUseSyncSnd = 2;

        // Noise excitation
        private byte[] noiseWave = Tables.NoiseWave;
        private byte[] bandNoise = Tables.BandNoise;
        private byte[] hpNoise = Tables.HPNoise;

        // Gain
        private short Av, Af;
        private short wavesampleGain;
        private short voiceNoiseGain;
        private short reverbDepth;
        private int reverbDelay;
        private bool addReverb;
        private bool hfEmph = true;

        private short speechVolume = 256;
        private short setNoiseGain = 3200;
        private short voiceF1Gain, voiceF2Gain, voiceF3Gain;
        private short nasalAmt;
        private short fNP;
        private short bNP;
        private short breathGain;
        private short breathCycle;
        private byte[] breathWave;
        private short voiceMinBW = 50;

        // Parallel F-bank params (from voiceData)
        private short f4_Par;
        private short bw4_Par;
        private short f5_Par;
        private short bw5_Par;
        private short f6_Par;
        private short bw6_Par;

        private short voice_F4_Freq;
        private short voice_F4_BW;

        public SynthesizerKlatt()
        {
            maxRvbDelay = 4096;
            delayBuffer = new short[maxRvbDelay];
            breathWave = Tables.BandNoise;
            tapBuffer[0] = 404;
            tapBuffer[1] = 1058;
            tapBuffer[2] = 1362;
            tapBuffer[3] = 2318;
            tapBuffer[4] = 2909;
            tapBuffer[5] = 3723;
            tapBuffer[6] = 4030;
            tapBuffer[7] = 4096;
        }

        public void SetVoice(short nGain, bool bit16, short f4_Freq, short f4_BW, short f4p_Freq, short bw4p_BW, short f5p_Freq, short bw5p_BW, short f6p_Freq, short bw6p_BW, short nasal_Base, short nasal_BW, short aGain = 0, short aCycle = 192)
        {
            breathGain = (short)((aGain * KNoiseGain) / 100);
            breathCycle = aCycle;

            long tempLong = nGain;
            voiceNoiseGain = (short)MRatio(tempLong, 100, KPrecision);
            if (bit16)
            {
                voiceNoiseGain = (short)MMul2(voiceNoiseGain, 0xCCCC, 16);
            }
            setNoiseGain = voiceNoiseGain;

            voice_F4_Freq = HzToPitch(f4_Freq);
            voice_F4_BW = f4_BW;
            f4_Par = HzToPitch(f4p_Freq);
            bw4_Par = bw4p_BW;
            f5_Par = HzToPitch(f5p_Freq);
            bw5_Par = bw5p_BW;
            f6_Par = HzToPitch(f6p_Freq);
            bw6_Par = bw6p_BW;
            fNP = HzToPitch(nasal_Base);
            bNP = nasal_BW;

            InitFixedFormants();
        }

        private void InitFixedFormants()
        {
            Calc_Pole_Coefficients(out Acoeff4, out Bcoeff4, out Ccoeff4, voice_F4_Freq, voice_F4_BW);

            Calc_Pole_Coefficients(out Acoeff4p, out Bcoeff4p, out Ccoeff4p, f4_Par, bw4_Par);
            Acoeff4p = (short)MMul2(Acoeff4p, KNoiseGain, KPrecision);

            Calc_Pole_Coefficients(out Acoeff5, out Bcoeff5, out Ccoeff5, f5_Par, bw5_Par);
            Acoeff5 = (short)MMul2(Acoeff5, KNoiseGain, KPrecision);

            Calc_Pole_Coefficients(out Acoeff6, out Bcoeff6, out Ccoeff6, f6_Par, bw6_Par);
            Acoeff6 = (short)MMul2(Acoeff6, KNoiseGain, KPrecision);

            Calc_Pole_Coefficients(out AcoeffNP, out BcoeffNP, out CcoeffNP, fNP, bNP);
        }

        // Reverb state
        private const int KNumOfTaps = 8;
        private short[] tapBuffer = new short[KNumOfTaps];
        private short[] delayBuffer;
        private int maxRvbDelay;
        private int delay_Index;
        private long lastRevbSample;

        private static int MMul2(long x, long y, int s)
        {
            return (int)((x * y) >> s);
        }

        private static int MRatio(long x, long y, int s)
        {
            return (int)((x << s) / y);
        }

        private static int MUnScale(long x, int s)
        {
            return (int)(x >> s);
        }

        private static short MDiv(int x, int y, int s)
        {
            return (short)(x >> s);
        }

        public void Calc_Pole_Coefficients(out short Acoeff, out short Bcoeff, out short Ccoeff, short pitch, short bandWidth, int voiceMinBW = 50)
        {
            if (bandWidth > KMaxBandWidth) bandWidth = (short)KMaxBandWidth;
            if (bandWidth < voiceMinBW) bandWidth = (short)voiceMinBW;
            if (pitch < 256) pitch = 256;
            if (pitch >= 256 + Tables.CosTbl.Length) pitch = (short)(256 + Tables.CosTbl.Length - 1);

            int bwIndex = (bandWidth - 50) / 5;
            Ccoeff = Tables.CcoeffTbl[bwIndex];
            short cosVal = Tables.CosTbl[pitch - 256];
            Bcoeff = (short)MMul2(Tables.BcoeffTbl[bwIndex], cosVal, KPrecision - 1);
            Acoeff = (short)(KOnePtOh - Bcoeff - Ccoeff);
        }
        public void Calc_Zero_Coefficients(out short Acoeff, out short Bcoeff, out short Ccoeff, short pitch, short bandWidth)
        {
            if (bandWidth > KMaxBandWidth) bandWidth = (short)KMaxBandWidth;
            if (pitch < 256) pitch = 256;
            if (pitch >= 256 + Tables.CosTbl.Length) pitch = (short)(256 + Tables.CosTbl.Length - 1);

            int bwIndex = (bandWidth - 50) / 5;
            Ccoeff = Tables.CcoeffTbl[bwIndex];
            short cosVal = Tables.CosTbl[pitch - 256];
            Bcoeff = (short)MMul2(Tables.BcoeffTbl[bwIndex], cosVal, KPrecision - 1);
            Bcoeff = (short)(-Bcoeff);
            Ccoeff = (short)(-Ccoeff);
            Acoeff = (short)(KOnePtOh + Bcoeff + Ccoeff);
        }

        public void InvDFT(short[] vWave, short[] vWave1, short vGain)
        {
            if (vWave == null || vWave1 == null)
            {
                for (int j = 0; j < 256; j++)
                {
                    voiceWaveform[j] = 0;
                    voiceWaveform1[j] = 0;
                }
                return;
            }

            int voiceWaveGain = MRatio(vGain, 200, 16);

            for (int j = 0; j < 256; j++)
            {
                voiceWaveform[j] = 0;
                voiceWaveform1[j] = 0;
            }

            for (int i = 0; i < 48; i++)
            {
                short amp = (short)MMul2(vWave[i], voiceWaveGain, 16);
                short amp1 = (short)MMul2(vWave1[i], voiceWaveGain, 16);

                int sIndex = 0;
                for (int j = 0; j < 256; j++)
                {
                    short sine = Tables.SineWave15[sIndex];
                    voiceWaveform[j] = (short)(voiceWaveform[j] + (short)MMul2(amp, sine, 16));
                    voiceWaveform1[j] = (short)(voiceWaveform1[j] + (short)MMul2(amp1, sine, 16));
                    sIndex = (sIndex + i) & 0xFF;
                }
            }

            int max = 0;
            int max1 = 0;
            for (int j = 0; j < 256; j++)
            {
                if (Math.Abs(voiceWaveform[j]) > max) max = Math.Abs(voiceWaveform[j]);
                if (Math.Abs(voiceWaveform1[j]) > max1) max1 = Math.Abs(voiceWaveform1[j]);
            }

            if (max1 > 0)
            {
                int max2 = MRatio(max, max1, 16);
                for (int j = 0; j < 256; j++)
                {
                    voiceWaveform1[j] = (short)MMul2(voiceWaveform1[j], max2, 16);
                }
            }
        }

        public void SynthesizeFrame(Frame frame, short[] outputBuffer, int offset)
        {
            if ((curAmp == 0) && (Af == 0))
            {
                glotIndex = 0;
                glotIndex1 = 0;
                Na1 = Nb1 = Na2 = Nb2 = Na3 = Nb3 = Na4 = Nb4 = 0;
                NaNP = NbNP = NaNZ = NbNZ = 0;
                lastAmp = 0;
            }

            Calc_Pole_Coefficients(out Acoeff1, out Bcoeff1, out Ccoeff1, (short)(frame.F1 + voiceF1Gain), frame.Bw1);
            Calc_Pole_Coefficients(out Acoeff2, out Bcoeff2, out Ccoeff2, (short)(frame.F2 + voiceF2Gain), frame.Bw2);
            Calc_Pole_Coefficients(out Acoeff3, out Bcoeff3, out Ccoeff3, (short)(frame.F3 + voiceF3Gain), frame.Bw3);

            bool noNasal;
            int nGain = 0;
            if (frame.FNZ != fNP)
            {
                noNasal = false;
                Calc_Zero_Coefficients(out AcoeffNZ, out BcoeffNZ, out CcoeffNZ, (short)(frame.FNZ + nasalAmt), bNP);
                nGain = MRatio(AcoeffNP, AcoeffNZ, 16);
            }
            else
            {
                noNasal = true;
            }

            bool ampBank = false;
            short rawAv = frame.Av;
            Av = (short)(rawAv * speechVolume);
            Af = (short)((frame.Af * speechVolume) << 2);
            ab = (short)(frame.AB * speechVolume);
            if (Af > 0 || ab > 0) ampBank = true;
            short totalBreathGain = (short)MMul2(breathGain, Av, KPrecision);

            short Acoeff2q = 0, Acoeff3q = 0, Acoeff4q = 0, Acoeff5q = 0, Acoeff6q = 0;

            if (frame.A2 > 0) { amp2 = (short)(frame.A2 << (KPrecision - 5)); Acoeff2q = (short)MMul2(Acoeff2, amp2, KPrecision); ampBank = true; }
            else { amp2 = 0; Nb2a = 0; Na2a = 0; }
            if (frame.A3 > 0) { amp3 = (short)(frame.A3 << (KPrecision - 5)); Acoeff3q = (short)MMul2(Acoeff3, amp3, KPrecision); ampBank = true; }
            else { amp3 = 0; Nb3a = 0; Na3a = 0; }
            if (frame.A4 > 0) { amp4 = (short)(frame.A4 << (KPrecision - 5)); Acoeff4q = (short)MMul2(Acoeff4p, amp4, KPrecision); ampBank = true; }
            else { amp4 = 0; Nb4a = 0; Na4a = 0; }
            if (frame.A5 > 0) { amp5 = (short)(frame.A5 << (KPrecision - 5)); Acoeff5q = (short)MMul2(Acoeff5, amp5, KPrecision); ampBank = true; }
            else { amp5 = 0; Nb5 = 0; Na5 = 0; }
            if (frame.A6 > 0) { amp6 = (short)(frame.A6 << (KPrecision - 5)); Acoeff6q = (short)MMul2(Acoeff6, amp6, KPrecision); ampBank = true; }
            else { amp6 = 0; Nb6 = 0; Na6 = 0; }

            glotInc = Tables.TopOctave[frame.F0 & 0xFF] >> (3 - (frame.F0 >> 8));
            if (VoiceChorus != 0)
            {
                int curF0Pitch = frame.F0 + VoiceChorus;
                if (curF0Pitch < 0) curF0Pitch = 0;
                glotInc1 = Tables.TopOctave[curF0Pitch & 0xFF] >> (3 - (curF0Pitch >> 8));
            }

            const int kAmpStepRes = 16;
            ampStep = (((long)Av << kAmpStepRes) - lastAmp) >> 3;
            curAmp_Full = lastAmp;
            lastAmp = ((long)Av << kAmpStepRes);
            int local_ampCtr = 0;

            for (int sampCtr = (KSampFrameLen / 2) - 1; sampCtr >= 0; --sampCtr)
            {
                if (local_ampCtr < 8) { curAmp_Full += ampStep; curAmp = (short)(curAmp_Full >> kAmpStepRes); local_ampCtr++; }
                else { curAmp = Av; }

                int sourceC = 0, SampV = 0, sourceP = 0, SampAB = 0, Samp2 = 0, Samp3 = 0, Samp4 = 0, Samp5 = 0, Samp6 = 0;

                if (curAmp > 0 || ampBank || totalBreathGain > 0)
                {
                    noiseIndex = (noiseIndex + 1) & (KNoiseLen - 1);
                    if (curAmp > 0)
                    {
                        short vPulse;
                        if (GlotType == KUseHarm)
                        {
                            glotIndex = (glotInc + glotIndex) & 0xFFFFFF;
                            vPulse = voiceWaveform[glotIndex >> 16];
                            if (VoiceChorus != 0)
                            {
                                glotIndex1 = (glotInc1 + glotIndex1) & 0xFFFFFF;
                                vPulse = MDiv(vPulse + voiceWaveform1[glotIndex1 >> 16], 2, 1);
                            }
                        }
                        else
                        {
                            glotIndex = (glotInc + glotIndex) & 0xFFFFFF;
                            if (SampleWave != null)
                            {
                                SampleIndex = (SampleInc + SampleIndex) & 0xFFFFFF;
                                vPulse = (short)(SampleWave[SampleIndex >> 16] - 128);
                                vPulse = (short)MMul2(vPulse, wavesampleGain, KPrecision);
                            }
                            else vPulse = 0;
                        }
                        sourceC = MMul2(vPulse, curAmp, KPrecision);
                    }
                    else
                    {
                        // No voicing, but still advance glotIndex for breathCycle gating
                        if (totalBreathGain > 0) glotIndex = (glotInc + glotIndex) & 0xFFFFFF;
                        else { lastnSamp = 0; glotIndex = 0; glotIndex1 = 0; }
                        sourceC = 0;
                    }

                    // Breath (aspiration) source — injected when cycle position exceeds breathCycle
                    if (totalBreathGain > 0 && (glotIndex >> 16) > breathCycle)
                        sourceC += MMul2((short)(breathWave[noiseIndex] - 128), totalBreathGain, KPrecision - 2);

                    if (curAmp > 0 || Af > 0 || totalBreathGain > 0)
                    {
                        sourceC += MMul2((short)(bandNoise[noiseIndex] - 128), Af, KPrecision);
                        if (noNasal) SampV = sourceC;
                        else
                        {
                            SampV = sourceC + MUnScale(((long)BcoeffNZ * NaNZ) + ((long)CcoeffNZ * NbNZ), KPrecision);
                            NbNZ = NaNZ; NaNZ = (short)sourceC;
                            SampV = MMul2(SampV, nGain, 16);
                            SampV = SampV + MUnScale(((long)BcoeffNP * NaNP) + ((long)CcoeffNP * NbNP), KPrecision);
                            NbNP = NaNP; NaNP = (short)SampV;
                        }

                        SampV = MUnScale(((long)Acoeff1 * SampV) + ((long)Bcoeff1 * Na1) + ((long)Ccoeff1 * Nb1), KPrecision);
                        Nb1 = Na1; Na1 = (short)SampV;
                        SampV = MUnScale(((long)Acoeff2 * SampV) + ((long)Bcoeff2 * Na2) + ((long)Ccoeff2 * Nb2), KPrecision);
                        Nb2 = Na2; Na2 = (short)SampV;
                        SampV = MUnScale(((long)Acoeff3 * SampV) + ((long)Bcoeff3 * Na3) + ((long)Ccoeff3 * Nb3), KPrecision);
                        Nb3 = Na3; Na3 = (short)SampV;
                        SampV = MUnScale(((long)Acoeff4 * SampV) + ((long)Bcoeff4 * Na4) + ((long)Ccoeff4 * Nb4), KPrecision);
                        Nb4 = Na4; Na4 = (short)SampV;
                    }

                    sourceP = MMul2((short)(noiseWave[noiseIndex] - 128), voiceNoiseGain, KPrecision);

                    if (ab > 0) SampAB = MMul2(sourceP, ab, KPrecision - 1);
                    if (amp2 > 0) { Samp2 = MUnScale(((long)Acoeff2q * sourceP) + ((long)Bcoeff2 * Na2a) + ((long)Ccoeff2 * Nb2a), KPrecision); Nb2a = Na2a; Na2a = (short)Samp2; }
                    if (amp3 > 0) { Samp3 = MUnScale(((long)Acoeff3q * sourceP) + ((long)Bcoeff3 * Na3a) + ((long)Ccoeff3 * Nb3a), KPrecision); Nb3a = Na3a; Na3a = (short)Samp3; }
                    if (amp4 > 0) { Samp4 = MUnScale(((long)Acoeff4q * sourceP) + ((long)Bcoeff4p * Na4a) + ((long)Ccoeff4p * Nb4a), KPrecision); Nb4a = Na4a; Na4a = (short)Samp4; }
                    if (amp5 > 0) { Samp5 = MUnScale(((long)Acoeff5q * sourceP) + ((long)Bcoeff5 * Na5) + ((long)Ccoeff5 * Nb5), KPrecision); Nb5 = Na5; Na5 = (short)Samp5; }
                    if (amp6 > 0) { Samp6 = MUnScale(((long)Acoeff6q * sourceP) + ((long)Bcoeff6 * Na6) + ((long)Ccoeff6 * Nb6), KPrecision); Nb6 = Na6; Na6 = (short)Samp6; }

                    int nSamp = SampV + (SampAB - Samp3 + Samp4 - Samp5 + Samp6 - Samp2);
                    if (hfEmph)
                    {
                        nSamp += (nSamp >> 2);
                        int tSamp = nSamp - (lastSample - (lastSample >> 2));
                        lastSample = (short)nSamp;
                        nSamp = tSamp + (nSamp >> 1);
                    }

                    if (nSamp > 8191) nSamp = 8191; else if (nSamp < -8191) nSamp = -8191;
                    outputBuffer[offset++] = (short)((((nSamp - lastnSamp) >> 1) + lastnSamp) << 2);
                    outputBuffer[offset++] = (short)(nSamp << 2);
                    lastnSamp = (short)nSamp;
                }
                else
                {
                    lastnSamp = 0; glotIndex = 0; glotIndex1 = 0;
                    outputBuffer[offset++] = 0; outputBuffer[offset++] = 0;
                }
            }
        }

        public static short HzToPitch(short hz)
        {
            const int ratioK = 2621;
            int fk, freq;
            if (hz <= 0) return 0;
            if (hz < 100) { freq = hz << 3; fk = 0x0; }
            else if (hz < 200) { freq = hz << 2; fk = 0x100; }
            else if (hz < 400) { freq = hz << 1; fk = 0x200; }
            else if (hz < 800) { freq = hz; fk = 0x300; }
            else if (hz < 1600) { freq = hz >> 1; fk = 0x400; }
            else if (hz < 3200) { freq = hz >> 2; fk = 0x500; }
            else { freq = hz >> 3; fk = 0x600; }

            int ratio = ((freq - 400) * ratioK) >> 11;
            if (ratio < 0) ratio = 0;
            if (ratio >= Tables.logOf2Tbl.Length) ratio = Tables.logOf2Tbl.Length - 1;
            return (short)(Tables.logOf2Tbl[ratio] + fk);
        }

        public static short PitchToHz(short pitch)
        {
            int freq = (Tables.OctFreqTbl[(pitch & 0xF00) >> 8] * Tables.ExpOf2Tbl[pitch & 0xFF]) >> 15;
            return (short)freq;
        }
    }

    public struct Frame
    {
        public short Av;
        public short Af;
        public short F0;
        public short F1;
        public short F2;
        public short F3;
        public short A2;
        public short A3;
        public short A4;
        public short A5;
        public short A6;
        public short FNZ;
        public short AB;
        public short Bw1;
        public short Bw2;
        public short Bw3;
        public short PhonEdge;
        public long Marker;
    }
}  // namespace
