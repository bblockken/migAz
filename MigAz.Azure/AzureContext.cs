﻿using Microsoft.IdentityModel.Clients.ActiveDirectory;
using MigAz.Azure.Interface;
using MigAz.Core;
using MigAz.Core.Interface;
using System;
using System.Threading.Tasks;

namespace MigAz.Azure
{
    public class AzureContext
    {
        private bool _includePreviewRegions = false;
        private AzureEnvironment _AzureEnvironment = AzureEnvironment.AzureCloud;
        private AzureServiceUrls _AzureServiceUrls;
        private ArmDiskType _DefaultTargetDiskType = ArmDiskType.ManagedDisk;

        private AzureTenant _AzureTenant;
        private AzureSubscription _AzureSubscription;
        private AzureRetriever _AzureRetriever;
        private ITokenProvider _TokenProvider;
        private ILogProvider _LogProvider;
        private IStatusProvider _StatusProvider;
        private TargetSettings _TargetSettings;

        public delegate Task BeforeAzureTenantChangedHandler(AzureContext sender);
        public event BeforeAzureTenantChangedHandler BeforeAzureTenantChange;

        public delegate Task AfterAzureTenantChangedHandler(AzureContext sender);
        public event AfterAzureTenantChangedHandler AfterAzureTenantChange;

        public delegate Task BeforeAzureSubscriptionChangedHandler(AzureContext sender);
        public event BeforeAzureSubscriptionChangedHandler BeforeAzureSubscriptionChange;

        public delegate Task AfterAzureSubscriptionChangedHandler(AzureContext sender);
        public event AfterAzureSubscriptionChangedHandler AfterAzureSubscriptionChange;

        public delegate Task AzureEnvironmentChangedHandler(AzureContext sender);
        public event AzureEnvironmentChangedHandler AzureEnvironmentChanged;

        public delegate Task UserAuthenticatedHandler(AzureContext sender);
        public event UserAuthenticatedHandler UserAuthenticated;

        public delegate Task BeforeUserSignOutHandler();
        public event BeforeUserSignOutHandler BeforeUserSignOut;

        public delegate Task AfterUserSignOutHandler();
        public event AfterUserSignOutHandler AfterUserSignOut;

        #region Constructors

        private AzureContext() { }

        public AzureContext(ILogProvider logProvider, IStatusProvider statusProvider, TargetSettings targetSetings)
        {
            _LogProvider = logProvider;
            _StatusProvider = statusProvider;
            _TargetSettings = targetSetings;
            _AzureServiceUrls = new AzureServiceUrls(this);
            _TokenProvider = new AzureTokenProvider(this);
            _AzureRetriever = new AzureRetriever(this);
        }

        #endregion

        #region Properties

        public AzureServiceUrls AzureServiceUrls
        {
            get { return _AzureServiceUrls; }
        }

        public AzureEnvironment AzureEnvironment
        {
            get { return _AzureEnvironment; }
            set
            {
                // Only allow value change when not authenticated
                if (_TokenProvider != null && _TokenProvider.AuthenticationResult != null && _AzureEnvironment != value)
                    throw new ArgumentException("Azure Environment cannot be changed while authenticated.  Sign out before chaning environments.");
                else
                    _AzureEnvironment = value;

                AzureEnvironmentChanged?.Invoke(this);
            }
        }

        public bool IncludePreviewRegions {
            get { return _includePreviewRegions; }
            set { _includePreviewRegions = value; }
        }

        public AzureTenant AzureTenant
        {
            get { return _AzureTenant; }
        }

        public AzureSubscription AzureSubscription
        {
            get { return _AzureSubscription; }
        }

        public AzureRetriever AzureRetriever
        {
            get { return _AzureRetriever; }
            set { _AzureRetriever = value; }
        }

        public ITokenProvider TokenProvider
        {
            get { return _TokenProvider; }
            set { _TokenProvider = value; }
        }

        public ILogProvider LogProvider
        {
            get { return _LogProvider; }
        }

        public IStatusProvider StatusProvider
        {
            get { return _StatusProvider; }
        }

        public TargetSettings TargetSettings
        {
            get { return _TargetSettings; }
        }

        public async Task CopyContext(AzureContext sourceContext)
        {
            this.IncludePreviewRegions = sourceContext.IncludePreviewRegions;
            this.AzureEnvironment = sourceContext.AzureEnvironment;
            await this.SetTenantContext(sourceContext.AzureTenant);
            await this.SetSubscriptionContext(sourceContext.AzureSubscription);
            this.TokenProvider.AuthenticationResult = sourceContext.TokenProvider.AuthenticationResult;
            await UserAuthenticated?.Invoke(this);
        }

        public ArmDiskType DefaultTargetDiskType
        {
            get { return _DefaultTargetDiskType; }
            set { _DefaultTargetDiskType = value; }
        }

        #endregion

        #region Methods

        public async Task Login()
        {
            await this.TokenProvider.LoginAzureProvider();
            UserAuthenticated?.Invoke(this);
        }

        public async Task SetTenantContext(AzureTenant azureTenant)
        {
            if (azureTenant == null && this._AzureSubscription != null)
                await this.SetSubscriptionContext(null);

            if (BeforeAzureTenantChange != null)
                await BeforeAzureTenantChange?.Invoke(this);

            _AzureTenant = azureTenant;

            if (AfterAzureTenantChange != null)
                await AfterAzureTenantChange?.Invoke(this);
        }

        public async Task SetSubscriptionContext(AzureSubscription azureSubscription)
        {
            if (BeforeAzureSubscriptionChange != null)
                await BeforeAzureSubscriptionChange?.Invoke(this);

            if (azureSubscription != null)
                if (azureSubscription.Parent != null)
                    if (azureSubscription.Parent != this._AzureTenant)
                        await SetTenantContext(azureSubscription.Parent);

            _AzureSubscription = azureSubscription;

            if (_AzureSubscription != null)
            {
                if (_TokenProvider != null)
                    await _TokenProvider.GetToken(_AzureSubscription);

                await _AzureRetriever.SetSubscriptionContext(_AzureSubscription);
            }

            if (AfterAzureSubscriptionChange != null)
                await AfterAzureSubscriptionChange?.Invoke(this);
        }

        public async Task Logout()
        {
            if (BeforeUserSignOut != null)
                await BeforeUserSignOut.Invoke();

            await this.SetTenantContext(null);

            _AzureRetriever.ClearCache();
            _TokenProvider.AuthenticationResult = null;

            if (AfterUserSignOut != null)
                await AfterUserSignOut?.Invoke();
        }

        #endregion
    }
}
