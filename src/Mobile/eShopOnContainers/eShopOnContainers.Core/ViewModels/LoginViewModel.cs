﻿using eShopOnContainers.Core.Helpers;
using eShopOnContainers.Core.Models.User;
using eShopOnContainers.Core.Services.Dependency;
using eShopOnContainers.Core.Services.Identity;
using eShopOnContainers.Core.Validations;
using eShopOnContainers.Core.ViewModels.Base;
using eShopOnContainers.Core.Extensions;
using IdentityModel.Client;
using System;
using System.Diagnostics;
using System.Threading.Tasks;
using System.Windows.Input;
using Xamarin.Forms;

namespace eShopOnContainers.Core.ViewModels
{
    public class LoginViewModel : ViewModelBase
    {
        private ValidatableObject<string> _userName;
        private ValidatableObject<string> _password;
        private bool _isMock;
        private bool _isValid;
        //private bool _isLogin;
        //private string _authUrl;

        private IIdentityService _identityService;
        private IDependencyService _dependencyService;

        public LoginViewModel(
            IIdentityService identityService,
            IDependencyService dependencyService)
        {
            _identityService = identityService;
            _dependencyService = dependencyService;

            _userName = new ValidatableObject<string>();
            _password = new ValidatableObject<string>();

            InvalidateMock();
            AddValidations();
        }

        public ValidatableObject<string> UserName
        {
            get
            {
                return _userName;
            }
            set
            {
                _userName = value;
                RaisePropertyChanged(() => UserName);
            }
        }

        public ValidatableObject<string> Password
        {
            get
            {
                return _password;
            }
            set
            {
                _password = value;
                RaisePropertyChanged(() => Password);
            }
        }

        public bool IsMock
        {
            get
            {
                return _isMock;
            }
            set
            {
                _isMock = value;
                RaisePropertyChanged(() => IsMock);
            }
        }

        public bool IsValid
        {
            get
            {
                return _isValid;
            }
            set
            {
                _isValid = value;
                RaisePropertyChanged(() => IsValid);
            }
        }

        //public bool IsLogin
        //{
        //    get
        //    {
        //        return _isLogin;
        //    }
        //    set
        //    {
        //        _isLogin = value;
        //        RaisePropertyChanged(() => IsLogin);
        //    }
        //}

        //public string LoginUrl
        //{
        //    get
        //    {
        //        return _authUrl;
        //    }
        //    set
        //    {
        //        _authUrl = value;
        //        RaisePropertyChanged(() => LoginUrl);
        //    }
        //}

        public ICommand MockSignInCommand => new Command(async () => await MockSignInAsync());

        public ICommand SignInCommand => new Command(async () => await SignInAsync());

        public ICommand RegisterCommand => new Command(async () => await RegisterAsync());

        //public ICommand NavigateCommand => new Command<string>(async (url) => await NavigateAsync(url));

        public ICommand SettingsCommand => new Command(async () => await SettingsAsync());

        public ICommand ValidateUserNameCommand => new Command(() => ValidateUserName());

        public ICommand ValidatePasswordCommand => new Command(() => ValidatePassword());

        public override async Task<object> InitializeAsync(object navigationData)
        {
            if (navigationData is LogoutParameter)
            {
                var logoutParameter = (LogoutParameter)navigationData;
                if (logoutParameter.Logout)
                {
                    await LogoutAsync();
                }
            }

            return base.InitializeAsync(navigationData);
        }

        private async Task MockSignInAsync()
        {
            IsBusy = true;
            IsValid = true;
            bool isValid = Validate();
            bool isAuthenticated = false;

            if (isValid)
            {
                try
                {
                    await Task.Delay(1000);

                    isAuthenticated = true;
                }
                catch (Exception ex)
                {
                    Debug.WriteLine($"[SignIn] Error signing in: {ex}");
                }
            }
            else
            {
                IsValid = false;
            }

            if (isAuthenticated)
            {
                Settings.AuthAccessToken = GlobalSetting.Instance.AuthToken;

                await NavigationService.NavigateToAsync<MainViewModel>();
                await NavigationService.RemoveLastFromBackStackAsync();
            }

            IsBusy = false;
        }

        private async Task SignInAsync()
        {
            //IsBusy = true;

            //await Task.Delay(500);

            var result = await _dependencyService.Get<INativeBrowserService>().LaunchBrowserAsync(_identityService.CreateAuthorizationRequest());
            if (!result.IsError)
            {
                var unescapedUrl = System.Net.WebUtility.UrlDecode(result.Response);
                if (unescapedUrl.Contains(GlobalSetting.Instance.IdentityCallback))
                {
                    var authResponse = new AuthorizeResponse(result.Response);
                    if (!authResponse.IsError && authResponse.Code.IsPresent())
                    {
                        var userToken = await _identityService.GetTokenAsync(authResponse.Code);
                        if (userToken.AccessToken.IsPresent())
                        {
                            Settings.AuthAccessToken = userToken.AccessToken;
                            Settings.AuthIdToken = authResponse.IdentityToken;
                            await NavigationService.NavigateToAsync<MainViewModel>();
                            await NavigationService.RemoveLastFromBackStackAsync();
                        }
                    }
                }
            }

            //IsValid = true;
            //IsLogin = true;
            //IsBusy = false;
        }

        private async Task RegisterAsync()
        {
            await _dependencyService.Get<INativeBrowserService>().LaunchBrowserAsync(GlobalSetting.Instance.RegisterWebsite);
        }

        private async Task LogoutAsync()
        {
            var authIdToken = Settings.AuthIdToken;
            var logoutRequest = _identityService.CreateLogoutRequest(authIdToken);

            if (!string.IsNullOrWhiteSpace(logoutRequest))
            {
                var result = await _dependencyService.Get<INativeBrowserService>().LaunchBrowserAsync(logoutRequest);
                if (!result.IsError)
                {
                    var unescapedUrl = System.Net.WebUtility.UrlDecode(result.Response);
                    if (unescapedUrl.Equals(GlobalSetting.Instance.LogoutCallback))
                    {
                        Settings.AuthAccessToken = string.Empty;
                        Settings.AuthIdToken = string.Empty;
                    }
                }
            }

            if (Settings.UseMocks)
            {
                Settings.AuthAccessToken = string.Empty;
                Settings.AuthIdToken = string.Empty;
            }
            Settings.UseFakeLocation = false;
        }

        //private async Task NavigateAsync(string url)
        //{
        //    var unescapedUrl = System.Net.WebUtility.UrlDecode(url);

        //    if (unescapedUrl.Equals(GlobalSetting.Instance.LogoutCallback))
        //    {
        //        Settings.AuthAccessToken = string.Empty;
        //        Settings.AuthIdToken = string.Empty;
        //        IsLogin = false;
        //        LoginUrl = _identityService.CreateAuthorizationRequest();
        //    }
        //    else if (unescapedUrl.Contains(GlobalSetting.Instance.IdentityCallback))
        //    {
        //        var authResponse = new AuthorizeResponse(url);
        //        if (!string.IsNullOrWhiteSpace(authResponse.Code))
        //        {
        //            var userToken = await _identityService.GetTokenAsync(authResponse.Code);
        //            string accessToken = userToken.AccessToken;

        //            if (!string.IsNullOrWhiteSpace(accessToken))
        //            {
        //                Settings.AuthAccessToken = accessToken;
        //                Settings.AuthIdToken = authResponse.IdentityToken;
        //                await NavigationService.NavigateToAsync<MainViewModel>();
        //                await NavigationService.RemoveLastFromBackStackAsync();
        //            }
        //        }
        //    }
        //}

        private async Task SettingsAsync()
        {
            await NavigationService.NavigateToAsync<SettingsViewModel>();
        }

        private bool Validate()
        {
            bool isValidUser = ValidateUserName();
            bool isValidPassword = ValidatePassword();

            return isValidUser && isValidPassword;
        }

        private bool ValidateUserName()
        {
            return _userName.Validate();
        }

        private bool ValidatePassword()
        {
            return _password.Validate();
        }

        private void AddValidations()
        {
            _userName.Validations.Add(new IsNotNullOrEmptyRule<string> { ValidationMessage = "A username is required." });
            _password.Validations.Add(new IsNotNullOrEmptyRule<string> { ValidationMessage = "A password is required." });
        }

        public void InvalidateMock()
        {
            IsMock = Settings.UseMocks;
        }
    }
}