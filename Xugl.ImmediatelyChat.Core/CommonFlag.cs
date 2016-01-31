using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Xugl.ImmediatelyChat.Core
{
    public class CommonFlag
    {
        private static string f_ArrangeChars = "ArrangeChars";
        public static string F_ArrangeChars { get { return f_ArrangeChars; } }

        private static string f_MMSVerifyMCS = "MCS";
        private static string f_MMSVerifyMDS = "MDS";
        private static string f_MMSCallMCSStart = "MCS start";
        private static string f_MMSCallMDSStart = "MDS start";
        private static string f_MMSReciveStopMCS = "stopMCS";
        private static string f_MMSReciveStopMDS = "stopMDS";
        private static string f_MMSVerifyUA = "Verify";
        public static string F_MMSVerifyMCS { get { return f_MMSVerifyMCS; } }
        public static string F_MMSVerifyMDS { get { return f_MMSVerifyMDS; } }
        public static string F_MMSCallMCSStart { get { return f_MMSCallMCSStart; } }
        public static string F_MMSCallMDSStart { get { return f_MMSCallMDSStart; } }
        public static string F_MMSReciveStopMCS { get { return f_MMSReciveStopMCS; } }
        public static string F_MMSReciveStopMDS { get { return f_MMSReciveStopMDS; } }
        public static string F_MMSVerifyUA { get { return f_MMSVerifyUA; } }

        private static string f_MCSVerifyUA = "VerifyAccount";
        private static string f_MCSVerifyUAMSG = "VerifyMSG";
        private static string f_MCSVerifyUAGetMSG = "VerifyGetMSG";
        private static string f_MCSVerfiyMDSMSG = "VerifyMDSMSG";
        private static string f_MCSReciveUAMSGFB = "VerifyMSGFB";
        public static string F_MCSVerifyUA { get { return f_MCSVerifyUA; } }
        public static string F_MCSVerifyUAMSG { get { return f_MCSVerifyUAMSG; } }
        public static string F_MCSVerifyUAGetMSG { get { return f_MCSVerifyUAGetMSG; } }
        public static string F_MCSVerfiyMDSMSG { get { return f_MCSVerfiyMDSMSG; } }
        public static string F_MCSReciveUAMSGFB { get { return f_MCSReciveUAMSGFB; } }

        private static string f_MDSVerifyMCSMSG = "VerifyMCSMSG";
        private static string f_MDSVerifyMCSGetMSG = "VerifyMCSGetMSG";
        private static string f_MDSReciveMCSMSGFB = "VerifyMSGFB";

        public static string F_MDSVerifyMCSMSG { get { return f_MDSVerifyMCSMSG; } }
        public static string F_MDSVerifyMCSGetMSG { get { return f_MDSVerifyMCSGetMSG; } }
        public static string F_MDSReciveMCSMSGFB { get { return f_MDSReciveMCSMSGFB; } }

    }
}
