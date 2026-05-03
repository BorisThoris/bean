#nullable enable
namespace SharpTalk
{

    [System.Serializable]
    public sealed class VoiceData
    {
        public short PitchHz = 97;
        public short PitchRange = 100;
        public short StressGain = 60;
        public short Rate = 160;
        public short VoiceType = 0;
        public short VGain = 100;
        public short AGain = 0;
        public short ACycle = 192;

        public short F4Freq = 3000;
        public short F4BW = 200;
        public short F4pFreq = 3600;
        public short F4pBW = 150;
        public short F5pFreq = 3750;
        public short F5pBW = 100;
        public short F6pFreq = 4500;
        public short F6pBW = 150;

        public short NasalBase = 330;
        public short NasalTarg = 400;
        public short NasalBW = 60;

        public short Locus = 100;
        public short BwGain1 = 150;
        public short BwGain2 = 100;
        public short BwGain3 = 100;
        public short F1_Offset = 0;
        public short F2_Offset = 0;
        public short F3_Offset = 0;
        public short Chorus = 0;
        public short NGain = 100;

        public short SPitchMidi = 0;
        public short SGain = 0;
        public short AsperW = 2;
        public short VoiceVers = 3;

        public short NasalAmt = 0;
        public short EmphVoice = 1;
        public short RvbDelay = 35;
        public short RvbDepth = 0;

        public short WaveType = 0;
        public short[] VWave = new short[]
        {
        0, 14636, 6938, 3898, 1845, 1158, 694, 577, 434, 347, 309, 290, 274, 244, 217, 206,
        195, 195, 184, 173, 172, 164, 154, 154, 154, 154, 154, 154, 154, 154, 154, 154,
        154, 154, 145, 145, 102, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0,
        };
        public short[] VWave1 = new short[]
        {
        0, 15503, 7770, 3676, 2319, 1096, 823, 582, 411, 327, 309, 274, 244, 231, 218, 206,
        206, 194, 194, 194, 172, 164, 164, 154, 154, 154, 154, 154, 154, 154, 154, 138,
        123, 116, 108, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0,
        };

        public short PhonEdge = 1;
        public int LoopPoint = 0;

        public short RiseAmt = 29;
        public short FallAmt = -29;
        public short RiseAmt1 = 29;
        public short FallAmt1 = -29;
        public int Assertiveness = 0x10000;
        public short BaselineFall = 51;
        public int Quickness = 7200;
        public int DownRampStep = 15360;
        public short StressDurTime = 50;
        public short VibratoDepth1Raw = 31;
        public short VibratoDepth2Raw = 16;
        public short VibratoFreqRaw = 47;
        public short Intonation = 100;

        public static VoiceData BaselineVoice => new VoiceData();

        public static VoiceData WhisperVoice => new VoiceData
        {
            PitchHz = 110,
            StressGain = 70,
            Rate = 140,
            VGain = 0,
            AGain = 400,
            ACycle = 16,
            F4Freq = 3500,
            F4BW = 50,
            F4pFreq = 4500,
            BwGain1 = 100,
            BwGain3 = 50,
            NGain = 200,
            VWave = new short[]
            {
            0, 15476, 6866, 3395, 1831, 1167, 1000, 861, 747, 680, 600, 540, 496, 472, 430, 401,
            367, 354, 339, 309, 307, 290, 273, 262, 211, 189, 165, 156, 144, 137, 113, 107,
            113, 107, 94, 82, 89, 77, 77, 64, 56, 0, 0, 0, 0, 0, 0, 0,
            },
            VWave1 = new short[]
            {
            0, 15476, 6866, 3395, 1831, 1167, 1000, 861, 747, 680, 600, 540, 496, 472, 430, 401,
            367, 354, 339, 309, 307, 290, 273, 262, 211, 189, 165, 156, 144, 137, 113, 107,
            113, 107, 94, 82, 89, 77, 77, 64, 56, 0, 0, 0, 0, 0, 0, 0,
            },
        };
    }

}  // namespace
