using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace BrokenRailServer
{
    public enum CommandType
    {
        RequestConfig = 0xA0,
        AssignClientID = 0xA1,
        UploadConfig = 0xA2,
        SubscribeAllRailInfo = 0xA3,
        BroadcastConfigFileSize = 0xA4,
        ConfigInitialInfo = 0xF0,
        ReadPointInfo = 0xF1,
        ThresholdSetting = 0xF2,
        GetHistory = 0xF4,
        GetPointRailInfo = 0xF5,
        ImmediatelyRespond = 0xFE,
        RealTimeConfig = 0x52,
        GetOneSectionInfo = 0x55,
        EraseFlash = 0x56,
        ErrorReport = 0x88
    }

    public enum FileRequestMode
    {
        Upload,
        Download
    }
}
