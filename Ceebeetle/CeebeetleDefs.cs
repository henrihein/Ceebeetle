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

    public delegate void DOnNewCharacter(CCBCharacter newCharacter);
    public delegate void DOnCreateNewGame(CCBGameTemplate template, string name);
    public delegate CCBGameTemplate DOnCreateNewTemplate(CCBGame game, string name);
    public delegate void DOnCharacterListUpdate();
    public delegate void DOnGameUpdate(CCBGame game);
    public delegate void DOnCopyBagItems(CCBBag targetBag, string[] bagItems);
    public delegate void DOnDeleteBagItems(CCBBag targetBag, string[] bagItems);
}
