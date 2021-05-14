using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Windows;

namespace Lge.Tools.Download.Models
{
    public interface ITabModel
    {
        TabType TabId { get; }
        void    TabActiveChanged(bool aActived);
        string  ErrorMessages { get; set; }

        #region options
        bool    AllErase { get; set; }
        int     LogLevel { get; set; }
        bool    SkipEraseEfs { get; set; }
        bool    UseMicomUpdate { get; set; }
        bool    UseEfsBackup { get; set; }
        bool    UseUpdateOnly { get; set; }
        bool    DiagRequestEnable { get; set; }
        bool    CanEfsBackup { get; }
        bool    IsIdle { get; }
        bool    FotaErase { get; set; }
        bool CCMOnly { get; set; } // jwoh CCM Only function
        int SelBoard { get; set; } // jwoh add User/Factory mode
        int SelFirehose { get; set; } // jwoh add GM Signing
        int SelBaudrate { get; set; } // jwoh add Baudrate
        int SelModel { get; set; } // jwoh add Model
        bool ECCCheck { get; set; } // jwoh add ECCCheck

        Visibility DonlyVisible { get; }
        Visibility NonlyCollapsed { get; } // jwoh add Micom Download only
        Visibility MonlyVisible { get; }
        #endregion options
    }

    public enum TabType
    {
        Multi = 0,
        Emergency = 1,
        Dump = 2,
        Normal = 3, // jwoh add Micom Download only
    }
}
