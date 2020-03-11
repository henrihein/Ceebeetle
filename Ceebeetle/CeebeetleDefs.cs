using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace Ceebeetle
{
    public class CCBSettings
    {
        public static bool m_simSlowIO = true;
    }

    public enum TStatusUpdate
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

    //Sundry UI events and callbacks
    public delegate void DOnKnownUIUpdate();
    public delegate void DOnNewCharacter(CCBCharacter newCharacter);
    public delegate void DOnCreateNewGame(CCBGameTemplate template, string name);
    public delegate CCBGameTemplate DOnCreateNewTemplate(CCBGame game, string name);
    public delegate void DOnCharacterListUpdate();
    public delegate void DOnGameUpdate(CCBGame game);
    public delegate void DOnCopyBagItems(CCBBag targetBag, string[] bagItems);
    public delegate bool DOnDeleteBagItems(CCBBag targetBag, string[] bagItems);
    public delegate void DOnCopyName(string name);
    public delegate void DStorePicked(CCBStore store);
    //Merge actions
    public delegate TStatusUpdate DMergeGame(CCBGame game);
    public delegate TStatusUpdate DMergeTemplate(CCBGameTemplate template);
    //P2P events and callbacks
    public delegate void DOnFileTransferDone(string sender, string filename, bool success);
    public delegate CCBStore DSelectStoreToPublish();
    public delegate TStatusUpdate DGetFileStatus(string filename, out long cbXfer, out long cbMax);
}
