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

namespace Xugl.ImmediatelyChat.MessageMainServer
{
    public partial class FrmMain : Form
    {
        SocketService socketService;
        private int logLength=0;

        public FrmMain()
        {
            InitializeComponent();

            new InitCommonVariables();

            socketService = new SocketService();

            timer1.Interval = 100;
            timer1.Enabled = true;
        }


        private void btn_StartServer_Click(object sender, EventArgs e)
        {
            socketService.StartMMSService();
        }

        private void btn_StartMessageServer_Click(object sender, EventArgs e)
        {
            socketService.StartService();
        }

        private void timer1_Tick(object sender, EventArgs e)
        {
            if(logLength!=CommonVariables.LogTool.GetLogMsg.Length)
            {
                txt_Log.Text=CommonVariables.LogTool.GetLogMsg;
                logLength=txt_Log.Text.Length;
            }
        }
    }
}
