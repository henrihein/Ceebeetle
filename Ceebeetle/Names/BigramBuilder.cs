using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Globalization;

namespace Ceebeetle.Names
{
    class CharacterCount : Dictionary<char, int>
    {
        private int m_freq;

        public int Freq
        {
            get { return m_freq; }
        }

        public static CharacterCount operator ++(CharacterCount c)
        {
            c.m_freq++;
            return c;
        }
        public static CharacterCount operator +(CharacterCount c, char ch)
        {
            if (c.ContainsKey(ch))
                c[ch]++;
            else
                c.Add(ch, 1);
            return c;
        }
        public int TotalFreq
        {
            get
            {
                int totalFreq = 0;
                foreach (int freq in this.Values)
                    totalFreq += freq;
                return totalFreq;
            }
        }

        public CharacterCount()
            : base()
        {
            m_freq = 1;
        }
    }

    class CharacterFreq
    {
        Dictionary<char, CharacterCount> m_knownCharacters;
        int m_totalFreq;

        public int TotalFreq
        {
            get { return m_totalFreq; }
        }
        public Dictionary<char, CharacterCount>.KeyCollection Keys
        {
            get { return m_knownCharacters.Keys; }
        }
        public CharacterCount this[char ch]
        {
            get { return m_knownCharacters[ch]; }
        }

        public CharacterFreq()
        { 
            m_knownCharacters = new Dictionary<char, CharacterCount>();
        }
        public void AddCharacterPair(char ch1, char ch2)
        {
            m_totalFreq++;
            if (m_knownCharacters.ContainsKey(ch1))
                m_knownCharacters[ch1]++;
            else
                m_knownCharacters.Add(ch1, new CharacterCount());
            m_knownCharacters[ch1] += ch2;
        }
        public int GetCharacterFreq(char ch)
        {
            System.Diagnostics.Debug.Assert(m_knownCharacters.ContainsKey(ch));
            if (m_knownCharacters.ContainsKey(ch))
                return m_knownCharacters[ch].Freq;
            return 0;
        }
    }

    class NameBigramData
    {
        private int m_maxWordLength;
        private int m_minWordLength;
        private CharacterFreq m_root;

        public int FirstLetterTotalFreq
        {
            get { return m_root.TotalFreq; }
        }

        public NameBigramData()
        {
            m_maxWordLength = 0;
            m_minWordLength = 0;
            m_root = new CharacterFreq();
        }
        public void AddWord(string word)
        {
            int ixch = 0;

            if ((0 == m_minWordLength) || (word.Length < m_minWordLength))
                m_minWordLength = word.Length;
            if (word.Length > m_maxWordLength)
                m_maxWordLength = word.Length;
            for (ixch = 0; ixch < word.Length - 1; ixch++)
                m_root.AddCharacterPair(word[ixch], word[ixch + 1]);
        }

        public int GetAWordLength(int rnd)
        {
            //Favor lengths below the median length
            int maxLen = m_maxWordLength - m_minWordLength;
            int minLen = 4;
            int favorLen = m_minWordLength + (maxLen - 1) / 2;
            int candidateLen = rnd % (8 + maxLen);

            if (candidateLen < m_minWordLength)
            {
                candidateLen = favorLen - candidateLen;
                if (candidateLen < m_minWordLength) 
                    return favorLen;
            }
            if (candidateLen > m_maxWordLength)
            {
                candidateLen = m_minWordLength + (candidateLen % maxLen);
                if (candidateLen > m_maxWordLength)
                {
                    //Should not be possible
                    System.Diagnostics.Debug.Assert(false);
                    return favorLen;
                }
            }
            //Really short generated names don't work well.
            if (candidateLen < minLen)
                return minLen;
            return candidateLen;
        }
        public char GetFirstLetter(int rnd)
        {
            int ix = 0;
            char chDef = Char.MinValue;

            foreach (char ch in m_root.Keys)
            {
                if ((m_root.GetCharacterFreq(ch) + ix) > rnd)
                    return ch;
                ix += m_root.GetCharacterFreq(ch);
                chDef = ch;
            }
            System.Diagnostics.Debug.Assert(false);
            //Last known letter is still pseudo-random.
            return chDef;
        }
        public char GetSecondLetter(char chFrom, int rnd)
        {
            CharacterCount cc = m_root[chFrom];
            int ix = 0;
            char chDef = Char.MinValue;

            //Caller doesn't know total frequency, so adjust randomizer.
            rnd = rnd % cc.TotalFreq;
            foreach (char ch in cc.Keys)
            {
                if ((cc[ch] + ix) >= rnd)
                    return ch;
                ix += cc[ch];
                chDef = ch;
            }
            System.Diagnostics.Debug.Assert(false);
            return chDef;
        }
    }

    abstract partial class CharacterNames
    {
        private NameBigramData m_bigramData;

        protected void BuildBigramModel()
        {
            if (null == m_bigramData)
            {
                string[] knownNames = GetNames();

                m_bigramData = new NameBigramData();

                foreach (string strName in knownNames)
                    m_bigramData.AddWord(strName);
            }
        }

        protected string GenerateNameFromModel(Random rnd)
        {
            int wordLength = m_bigramData.GetAWordLength(rnd.Next());
            StringBuilder newWord = new StringBuilder();
            char chFirst = m_bigramData.GetFirstLetter(rnd.Next(m_bigramData.FirstLetterTotalFreq));
            char chNext = chFirst;

            while (newWord.Length < wordLength)
            {
                newWord.Append(chNext);
                chNext = m_bigramData.GetSecondLetter(chNext, rnd.Next());
            }
            return CultureInfo.CurrentCulture.TextInfo.ToTitleCase(newWord.ToString());
        }
    }
}

