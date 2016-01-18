﻿using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Data;
using System.Drawing;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows.Forms;
using Xugl.ImmediatelyChat.Core;

namespace Xugl.ImmediatelyChat.MessageChildServer
{
    public partial class FrmMain : Form
    {
        SocketService socketService;

        public FrmMain()
        {
            InitializeComponent();
            socketService = new SocketService();
            InitCommonVariables initCommonVariables = new InitCommonVariables();

            timer1.Interval = 100;
            timer1.Enabled = true;
        }

        private void btn_ConnectMainServer_Click(object sender, EventArgs e)
        {
            socketService.StartConnectMMS();
        }

        private void timer1_Tick(object sender, EventArgs e)
        {
            if (!CommonVariables.LogTool.IsRecord)
            {
                txt_Log.Text = Singleton<ICommonLog>.Instance.LogMsg;
                Singleton<ICommonLog>.Instance.IsRecord = true;
            }
        }
    }
}
