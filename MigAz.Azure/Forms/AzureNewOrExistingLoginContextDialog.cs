﻿using Microsoft.IdentityModel.Clients.ActiveDirectory;
using MigAz.Azure.UserControls;
using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Data;
using System.Drawing;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows.Forms;

namespace MigAz.Azure.Forms
{
    public partial class AzureNewOrExistingLoginContextDialog : Form
    {
        private AzureLoginContextViewer _AzureLoginContextViewer;

        public AzureNewOrExistingLoginContextDialog()
        {
            InitializeComponent();
        }

        public async Task InitializeDialog(AzureLoginContextViewer azureLoginContextViewer)
        {
            _AzureLoginContextViewer = azureLoginContextViewer;
            await azureArmLoginControl1.BindContext(azureLoginContextViewer.AzureContext);

            azureLoginContextViewer.ExistingContext.LogProvider.WriteLog("InitializeDialog", "Start AzureSubscriptionContextDialog InitializeDialog");

            lblSameEnviroronment.Text = azureLoginContextViewer.ExistingContext.AzureEnvironment.ToString();
            lblSameUsername.Text = azureLoginContextViewer.ExistingContext.TokenProvider.AuthenticationResult.UserInfo.DisplayableId;
            lblSameTenant.Text = azureLoginContextViewer.ExistingContext.AzureTenant.ToString();
            lblSameSubscription.Text = azureLoginContextViewer.ExistingContext.AzureSubscription.ToString();

            lblSameEnvironment2.Text = azureLoginContextViewer.ExistingContext.AzureEnvironment.ToString();
            lblSameUsername2.Text = azureLoginContextViewer.ExistingContext.TokenProvider.AuthenticationResult.UserInfo.DisplayableId;

            int subscriptionCount = 0;
            cboTenant.Items.Clear();
            if (azureLoginContextViewer.ExistingContext.AzureRetriever != null && azureLoginContextViewer.ExistingContext.TokenProvider != null && azureLoginContextViewer.ExistingContext.TokenProvider.AuthenticationResult != null)
            {
                azureLoginContextViewer.ExistingContext.LogProvider.WriteLog("InitializeDialog", "Loading Azure Tenants");
                foreach (AzureTenant azureTenant in await azureLoginContextViewer.ExistingContext.AzureRetriever.GetAzureARMTenants())
                {
                    subscriptionCount += azureTenant.Subscriptions.Count;

                    if (azureTenant.Subscriptions.Count > 0) // Only add Tenants that have one or more Subscriptions
                    {
                        cboTenant.Items.Add(azureTenant);
                        azureLoginContextViewer.ExistingContext.LogProvider.WriteLog("InitializeDialog", "Added Azure Tenant '" + azureTenant.ToString() + "'");
                    }
                    else
                    {
                        azureLoginContextViewer.ExistingContext.LogProvider.WriteLog("InitializeDialog", "Not adding Azure Tenant '" + azureTenant.ToString() + "'.  Contains no subscriptions.");
                    }
                }
                cboTenant.Enabled = true;

                if (azureLoginContextViewer.ExistingContext.AzureTenant != null)
                {
                    foreach (AzureTenant azureTenant in cboTenant.Items)
                    {
                        if (azureLoginContextViewer.ExistingContext.AzureTenant == azureTenant)
                            cboTenant.SelectedItem = azureTenant;
                    }
                }
            }

            rbSameUserDifferentSubscription.Enabled = subscriptionCount > 1;

            switch (azureLoginContextViewer.AzureContextSelectedType)
            {
                case AzureContextSelectedType.ExistingContext:
                    rbExistingContext.Checked = true;
                    break;
                case AzureContextSelectedType.SameUserDifferentSubscription:
                    if (rbSameUserDifferentSubscription.Enabled)
                        rbSameUserDifferentSubscription.Checked = true;
                    else
                        rbExistingContext.Checked = true;
                    break;
                case AzureContextSelectedType.NewContext:
                    rbNewContext.Checked = true;
                    break;
            }

            azureLoginContextViewer.ExistingContext.LogProvider.WriteLog("InitializeDialog", "End AzureSubscriptionContextDialog InitializeDialog");
        }



        private void btnClose_Click(object sender, EventArgs e)
        {
            this.Close();
        }

        private void rbExistingContext_CheckedChanged(object sender, EventArgs e)
        {
            if (rbExistingContext.Checked)
            {
                cboTenant.Enabled = false;
                cboSubscription.Enabled = false;
                _AzureLoginContextViewer.AzureContextSelectedType = AzureContextSelectedType.ExistingContext;
                _AzureLoginContextViewer.AzureContext.AzureRetriever.PromptBehavior = PromptBehavior.Auto;
            }

            _AzureLoginContextViewer.UpdateLabels();
        }

        private void rbSameUserDifferentSubscription_CheckedChanged(object sender, EventArgs e)
        {
            if (rbSameUserDifferentSubscription.Checked)
            {
                _AzureLoginContextViewer.AzureContextSelectedType = AzureContextSelectedType.SameUserDifferentSubscription;
                _AzureLoginContextViewer.AzureContext.AzureRetriever.PromptBehavior = PromptBehavior.Auto;

                _AzureLoginContextViewer.AzureContext.CopyContext(_AzureLoginContextViewer.ExistingContext);
            }

            azureArmLoginControl1.BindContext(_AzureLoginContextViewer.AzureContext);
            cboTenant.Enabled = rbSameUserDifferentSubscription.Checked;
            cboSubscription.Enabled = rbSameUserDifferentSubscription.Checked;
            _AzureLoginContextViewer.UpdateLabels();
        }

        private void rbNewContext_CheckedChanged(object sender, EventArgs e)
        {
            if (rbNewContext.Checked)
            {
                cboTenant.Enabled = false;
                cboSubscription.Enabled = false;
                _AzureLoginContextViewer.AzureContextSelectedType = AzureContextSelectedType.NewContext;
                _AzureLoginContextViewer.AzureContext.AzureRetriever.PromptBehavior = PromptBehavior.SelectAccount;
            }

            azureArmLoginControl1.BindContext(_AzureLoginContextViewer.AzureContext);

            azureArmLoginControl1.Enabled = rbNewContext.Checked;
            _AzureLoginContextViewer.UpdateLabels();
        }

        private void AzureNewOrExistingLoginContextDialog_FormClosing(object sender, FormClosingEventArgs e)
        {
            bool isValidTargetContext = false;

            if (rbExistingContext.Checked)
            {
                isValidTargetContext = true;
            }
            else if (rbSameUserDifferentSubscription.Checked)
            {
                isValidTargetContext = cboSubscription.SelectedIndex >= 0;
            }
            else if (rbNewContext.Checked)
            {
                isValidTargetContext = _AzureLoginContextViewer.AzureContext.AzureSubscription != null;
            }

            if (!isValidTargetContext)
            {
                e.Cancel = true;
                MessageBox.Show("You must have a valid target Azure Subscription selected.");
            }
        }

        private void cboTenant_SelectedIndexChanged(object sender, EventArgs e)
        {
            cboSubscription.Items.Clear();
            if (cboTenant.SelectedItem != null)
            {
                AzureTenant azureTenant = (AzureTenant)cboTenant.SelectedItem;
                foreach (AzureSubscription azureSubscription in azureTenant.Subscriptions)
                {
                    cboSubscription.Items.Add(azureSubscription);
                }

                if (cboSubscription.Items.Count > 0)
                    cboSubscription.SelectedIndex = 0;
            }
        }
    }
}
