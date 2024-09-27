using Newtonsoft.Json;
using Stylet;
using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using XboxCsMgr.Client.ViewModels;
using XboxCsMgr.Helpers.Win32;
using XboxCsMgr.XboxLive;
using XboxCsMgr.XboxLive.Model.Authentication;
using XboxCsMgr.XboxLive.Services;

namespace XboxCsMgr.Client
{
    public class AppBootstrapper : Bootstrapper<ShellViewModel>
    {
        public static XboxLiveConfig? XblConfig { get; internal set; }
        private AuthenticateService authenticateService;
        private string DeviceToken { get; set; }
        private string UserToken { get; set; }

        protected override void ConfigureIoC(StyletIoC.IStyletIoCBuilder builder)
        {
            base.ConfigureIoC(builder);

            builder.Bind<IDialogFactory>().ToAbstractFactory();
        }

        protected override async void OnStart()
        {
            authenticateService = new AuthenticateService(XblConfig);

            LoadXblTokenCredentials();

            var result = await authenticateService.AuthorizeXsts(UserToken, DeviceToken);
            if (result != null)
            {
                Debug.WriteLine("Authorized! Token: " + result.Token);
                XblConfig = new XboxLiveConfig(result.Token, result.DisplayClaims.XboxUserIdentity[0]);
                this.RootViewModel.OnAuthComplete();
            }

            base.OnStart();
        }

        private void LoadXblTokenCredentials()
        {
            // Lookup current Xbox Live authentication data stored via wincred
            Dictionary<string, string> currentCredentials = CredentialUtil.EnumerateCredentials();

            var xblCredentials = currentCredentials.Where(k => k.Key.Contains("Xbl|") || k.Key.Contains("XblGrts|")
                    && k.Key.Contains("Dtoken")
                    || k.Key.Contains("Utoken"))
                    .ToDictionary(p => p.Key, p => p.Value);

            string PartialCredential = null;
            // turns out my whole issue was that a token was split between two credentials. i had to do this to get both Dtoken and Utoken

            foreach (var credential in xblCredentials)
            {
                // there could be a X at the end of a token. so we wont remove it like the old code
                //var fixedJson = credential.Value.TrimEnd('X').ToString();

                var json = credential.Value;
                XboxLiveToken? token = null;
                try
                {
                    token = JsonConvert.DeserializeObject<XboxLiveToken>(json);
                }
                catch (JsonReaderException ex1)
                {
                    //partial token merge here

                    if (PartialCredential == null)
                    {
                        PartialCredential = json;
                    }
                    else
                    {
                        try
                        {
                            token = JsonConvert.DeserializeObject<XboxLiveToken>(json + PartialCredential);
                        }
                        catch (JsonReaderException ex2)
                        {
                            try
                            {
                                token = JsonConvert.DeserializeObject<XboxLiveToken>(PartialCredential + json);
                            }
                            catch (JsonReaderException ex3)
                            {
                                PartialCredential = json;
                                break;
                            }
                        }
                    }
                }
                if (token != null)
                {
                    if (token.TokenData.NotAfter > DateTime.UtcNow)
                    {
                        if (credential.Key.Contains("Dtoken"))
                        {
                            DeviceToken = token.TokenData.Token;
                        }
                        else if (credential.Key.Contains("Utoken"))
                        {
                            if (token.TokenData.Token != "AAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAA") UserToken = token.TokenData.Token;
                        }
                    }
                }
            }
        }
    }
}
