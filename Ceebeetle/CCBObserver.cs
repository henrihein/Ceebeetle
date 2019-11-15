using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace Ceebeetle
{
    enum TStatusUpdate
    {
        tsuNone = 0,
        tsuFileLoaded,
        tsuFileSaved,
        tsuCancelled,
        tsuError
    }

    public delegate void OnNewCharacter(CCBCharacter newCharacter);
    public delegate void OnCharacterListUpdate();

    class CCBObserver
    {
    }
}
