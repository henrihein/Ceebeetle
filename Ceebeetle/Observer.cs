using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace Ceebeetle
{
    enum TStatusUpdate
    {
        tsuNone = 0,
        tsuFileWork = 0x1000,
        tsuFileLoaded = 0x10 | tsuFileWork,
        tsuFileNothingLoaded = 0x11,
        tsuFileSaved = 0x20 | tsuFileWork,
        tsuCancelled = 0x40,
        tsuError = 0x800,
        tsuParseError = 0x801
    }

    public delegate void OnNewCharacter(CCBCharacter newCharacter);
    public delegate void OnCharacterListUpdate();

    class CCBObserver
    {
    }
}
