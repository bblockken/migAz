﻿using MigAz.Core.Interface;
using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Data;
using System.Drawing;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows.Forms;

namespace MigAz.Azure.Forms
{
    public partial class UnhandledExceptionDialog : Form
    {
        Exception _UnhandledException;
        ILogProvider _LogProvider;

        private UnhandledExceptionDialog() { }

        public UnhandledExceptionDialog(ILogProvider fileLogProvider, Exception e)
        {
            InitializeComponent();
            
            if (e != null)
            {
                _UnhandledException = e;

                if (e.GetType() == typeof(System.Net.WebException))
                {
                    System.Net.WebException webException = (System.Net.WebException)e;
                    Stream responseStream = webException.Response.GetResponseStream();
                    responseStream.Position = 0;
                    StreamReader sr = new StreamReader(responseStream);
                    string responseBody = sr.ReadToEnd();
                    textBox1.Text = responseBody + Environment.NewLine + Environment.NewLine + webException.Message + Environment.NewLine + Environment.NewLine + webException.StackTrace;
                }
                else
                    textBox1.Text = e.Message + Environment.NewLine + e.StackTrace;
            }

            if (fileLogProvider != null)
            {
                _LogProvider = fileLogProvider;
                _LogProvider.WriteLog("UnhandledExceptionDialog", textBox1.Text);
            }
        }

        private void button1_Click(object sender, EventArgs e)
        {
            this.Close();
        }

        private void button2_Click(object sender, EventArgs e)
        {
            Clipboard.SetText(textBox1.Text);
        }

        private void linkLabel1_LinkClicked(object sender, LinkLabelLinkClickedEventArgs e)
        {
            System.Diagnostics.Process.Start("https://github.com/Azure/migAz/issues/new");
        }
    }
}
