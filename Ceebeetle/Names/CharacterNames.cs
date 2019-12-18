using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace Ceebeetle.Names
{
    class CharacterNameGenerators
    {
        WesternFemaleNames m_westernFemaleNameGenerator;
        WesternMaleNames m_westernMaleNameGenerator;
        JapaneseFemaleNames m_japaneseFemaleNameGenerator;
        JapaneseMaleNames m_japaneseMaleNameGenerator;
        ElvenFemaleNames m_elvenFemaleNameGenerator;
        ElvenMaleNames m_elvenMaleNameGenerator;
        NordicDwarvenNames m_nordicDwarvenNameGenerator;
        TolkienDwarvenNames m_tolkienDwarvenNameGenerator;

        public CharacterNameGenerators()
        {
           m_westernFemaleNameGenerator = null;
           m_westernMaleNameGenerator = null;
           m_japaneseFemaleNameGenerator = null;
           m_japaneseMaleNameGenerator = null;
           m_elvenFemaleNameGenerator = null;
           m_elvenMaleNameGenerator = null;
           m_nordicDwarvenNameGenerator = null;
           m_tolkienDwarvenNameGenerator = null;
        }

        public CharacterNames GetWesternFemaleNameGenerator()
        {
            if (null == m_westernFemaleNameGenerator)
                m_westernFemaleNameGenerator = new WesternFemaleNames();
            return m_westernFemaleNameGenerator;
        }
        public CharacterNames GetWesternMaleNameGenerator()
        {
            if (null == m_westernMaleNameGenerator)
                m_westernMaleNameGenerator = new WesternMaleNames();
            return m_westernMaleNameGenerator;
        }
        public CharacterNames GetJapaneseFemaleNameGenerator()
        {
            if (null == m_japaneseFemaleNameGenerator)
                m_japaneseFemaleNameGenerator = new JapaneseFemaleNames();
            return m_japaneseFemaleNameGenerator;
        }
        public CharacterNames GetJapaneseMaleNameGenerator()
        {
            if (null == m_japaneseMaleNameGenerator)
                m_japaneseMaleNameGenerator = new JapaneseMaleNames();
            return m_japaneseMaleNameGenerator;
        }
        public CharacterNames GetElvenFemaleNameGenerator()
        {
            if (null == m_elvenFemaleNameGenerator)
                m_elvenFemaleNameGenerator = new ElvenFemaleNames();
            return m_elvenFemaleNameGenerator;
        }
        public CharacterNames GetElvenMaleNameGenerator()
        {
            if (null == m_elvenMaleNameGenerator)
                m_elvenMaleNameGenerator = new ElvenMaleNames();
            return m_elvenMaleNameGenerator;
        }
        public CharacterNames GetNordicDwarvenNameGenerator()
        {
            if (null == m_nordicDwarvenNameGenerator)
                m_nordicDwarvenNameGenerator = new NordicDwarvenNames();
            return m_nordicDwarvenNameGenerator;
        }
        public CharacterNames GetTolkienDwarvenNameGenerator()
        {
            if (null == m_tolkienDwarvenNameGenerator)
                m_tolkienDwarvenNameGenerator = new TolkienDwarvenNames();
            return m_tolkienDwarvenNameGenerator;
        }
    }

    abstract partial class CharacterNames
    {
        protected abstract string[] GetNames();

        public string GetRandomName(Random r)
        {
            string[] names = GetNames();
            int ixr = r.Next(names.Length);

            return names[ixr];
        }
        public string GenerateRandomName(Random rnd)
        {
            BuildBigramModel();
            return GenerateNameFromModel(rnd);
        }
    }

    class WesternFemaleNames : CharacterNames
    {
        protected override string[] GetNames()
        {
            return GetWesternFemaleNames();
        }
    }

    class WesternMaleNames : CharacterNames
    {
        protected override string[] GetNames()
        {
            return GetWesternMaleNames();
        }
    }

    class JapaneseFemaleNames : CharacterNames
    {
        protected override string[] GetNames()
        {
            return GetJapaneseFemaleNames();
        }
    }
    class JapaneseMaleNames : CharacterNames
    {
        protected override string[] GetNames()
        {
            return GetJapaneseMaleNames();
        }
    }
    class ElvenFemaleNames : CharacterNames
    {
        protected override string[] GetNames()
        {
            return GetElvenFemaleNames();
        }
    }
    class ElvenMaleNames : CharacterNames
    {
        protected override string[] GetNames()
        {
            return GetElvenMaleNames();
        }
    }
    class NordicDwarvenNames : CharacterNames
    {
        protected override string[] GetNames()
        {
            return GetNordicDwarvenNames();
        }
    }
    class TolkienDwarvenNames : CharacterNames
    {
        protected override string[] GetNames()
        {
            return GetTolkienDwarvenNames();
        }
    }
}
