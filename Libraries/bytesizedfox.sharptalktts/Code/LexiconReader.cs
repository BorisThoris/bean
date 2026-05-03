#nullable enable
using System;
using System.Buffers.Binary;

namespace SharpTalk
{

    public class DictReader
    {
        readonly byte[] _data;
        readonly int _wordsOff;
        readonly int[] _index;   // resolved per-entry byte offsets into _data
        readonly uint[] _hash;   // 27 entries: hash['A'-'A'] .. hash['Z'-'A'+1]
        readonly int _wordCount;

        // HASH_ENTRIES = 'Z'-'A'+2 = 27
        const int HASH_ENTRIES = 27;
        const byte kEndFlag = 0x80;
        const byte kAltFlag = 0xFF;

        public DictReader(byte[] data)
        {
            _data = data;

            // Header layout (all big-endian):
            //   0  uint32 nextDict_off
            //  4  uint32 version
            //   8  uint32 type
            //  12  uint32 wordCount
            //  16  uint32 hash[27]       (108 bytes)
            // 124  short  POScodes[128][4] (1024 bytes, ignored for now)
            // 1148 uint32 words_off
            // 1152 uint32 index_off
            // 1156 uint32 flags

            _wordCount = (int)BinaryPrimitives.ReadUInt32BigEndian(data.AsSpan(12));

            _hash = new uint[HASH_ENTRIES];
            for (int i = 0; i < HASH_ENTRIES; i++)
                _hash[i] = BinaryPrimitives.ReadUInt32BigEndian(data.AsSpan(16 + i * 4));

            _wordsOff = (int)BinaryPrimitives.ReadUInt32BigEndian(data.AsSpan(1148));
            int indexOff = (int)BinaryPrimitives.ReadUInt32BigEndian(data.AsSpan(1152));

            // Expand 32-bit big-endian index entries to int offsets
            _index = new int[_wordCount];
            for (int i = 0; i < _wordCount; i++)
                _index[i] = (int)BinaryPrimitives.ReadUInt32BigEndian(data.AsSpan(indexOff + i * 4));
        }

        public byte[]? Search(string word)
        {
            if (_wordCount == 0 || word.Length == 0) return null;

            int tLen = word.Length;
            char first = word[0];

            int lo, hi;
            if (first >= 'A' && first <= 'Z')
            {
                int letterIdx = first - 'A';
                lo = (int)_hash[letterIdx];
                hi = (int)_hash[letterIdx + 1] - 1;
            }
            else if (first < 'A')
            {
                lo = 0;
                hi = (int)_hash[0] - 1;
            }
            else // > 'Z'
            {
                lo = (int)_hash['Z' - 'A'];
                hi = _wordCount - 1;
            }

            while (lo <= hi)
            {
                int mid = (lo + hi) >> 1;
                int off = _index[mid];
                int dLen = _data[off];
                int diff = 0;
                int cmp = Math.Min(tLen, dLen);
                for (int i = 0; i < cmp; i++)
                {
                    diff = word[i] - _data[off + 1 + i];
                    if (diff != 0) break;
                }
                if (diff == 0) diff = tLen - dLen;

                if (diff > 0) lo = mid + 1;
                else if (diff < 0) hi = mid - 1;
                else
                {
                    // Found — copy phoneme bytes (up to kEndFlag)
                    int pStart = off + dLen + 1;
                    int pEnd = pStart;
                    while (pEnd < _data.Length && (_data[pEnd] & kEndFlag) == 0)
                        pEnd++;
                    byte[] phons = new byte[pEnd - pStart];
                    _data.AsSpan(pStart, phons.Length).CopyTo(phons);
                    return phons;
                }
            }
            return null;
        }
    }
}  // namespace
