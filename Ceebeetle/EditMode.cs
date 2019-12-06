using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Windows;
using System.Windows.Controls;

namespace Ceebeetle
{
    public enum EEditMode
    {
        em_None = 0,
        em_Frozen,
        em_AddGame,
        em_ModifyGame,
        em_AddCharacter,
        em_ModifyCharacter,
        em_AddProperty,
        em_ModifyProperty,
        em_AddBag,
        em_ModifyBag,
        em_AddBagItem,
        em_ModifyBagItem
    }

    public class CEditModeProperty : DependencyObject
    {
        public static readonly DependencyProperty
                                EditMode =
                                    DependencyProperty.RegisterAttached("EditMode", typeof(object), typeof(CEditModeProperty), new PropertyMetadata(""));
        public static CEditMode GetEditNode(DependencyObject ctl)
        {
            object propValue = ctl.GetValue(EditMode);

            if (null != propValue)
                return (CEditMode)propValue;
            return null;
        }
        public static void SetEditNode(DependencyObject ctl, CEditMode value)
        {
            ctl.SetValue(EditMode, value);
        }
        public static void ClearEditNode(DependencyObject ctl)
        {
            ctl.SetValue(EditMode, null);
        }
    }

    public class CEditMode
    {
        public EEditMode EditMode
        {
            get;
            set;
        }
        public CCBTreeViewItem Node
        {
            get;
            set;
        }
        public CEditMode()
        {
            EditMode = EEditMode.em_None;
            Node = null;
        }
        public CEditMode(CCBTreeViewItem node)
        {
            EditMode = EEditMode.em_None;
            Node = node;
        }
        public CEditMode(EEditMode mode, CCBTreeViewItem node)
        {
            EditMode = mode;
            Node = node;
        }
    }
}
