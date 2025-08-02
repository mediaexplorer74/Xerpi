//using Rg.Plugins.Popup.Contracts;
//using Rg.Plugins.Popup.Pages;
using System;
using System.Collections.Generic;
using System.Reflection;
using System.Threading.Tasks;
using Xamarin.Forms;
using Xerpi.ViewModels;
using Xerpi.ViewModels.Popups;

namespace Xerpi.Services
{
    public class PopupService : IPopupService
    {
        //private readonly IPopupNavigation _popupNavigation;

        private Dictionary<Type, Type> _vmToPopupMapping = new Dictionary<Type, Type>();

        //public PopupService(IPopupNavigation popupNavigation)
        //{
        //    _popupNavigation = popupNavigation;
        //}

        public void RegisterViewModel<TViewModel, TPopup>()
        {
            RegisterViewModel(typeof(TViewModel), typeof(TPopup));
        }

        public void RegisterViewModel(Type vmType, Type popupType)
        {
            _vmToPopupMapping.Add(vmType, popupType);
        }

        /*public async Task<TResult> ShowPopup<TResult>(BasePopupViewModel<TResult> popupVieWModel)
        {
            // Create new popup associated with VM of given type
            if (!_vmToPopupMapping.TryGetValue(popupVieWModel.GetType(), out Type popupType))
            {
                throw new ArgumentOutOfRangeException($"Seems like {popupVieWModel} hasn't been registerd with the PopupService. Try again!");
            }

            ConstructorInfo popupPageConstructor = popupType.GetConstructor(Type.EmptyTypes);
            var page = (PopupPage)popupPageConstructor.Invoke(null);

            var tcs = new TaskCompletionSource<TResult>(TaskCreationOptions.RunContinuationsAsynchronously);
            page.Disappearing += (s, e) =>
            {
                var vm = (BasePopupViewModel<TResult>)page.BindingContext;
                tcs.SetResult(vm.Result);
            };

            // Navigate to it            
            await _popupNavigation.PushAsync(page);

            // Wait for it to close
            return await tcs.Task;
        }*/
        public async Task<TResult> ShowPopup<TResult>(BasePopupViewModel<TResult> popupViewModel)
        {
            // Create new popup associated with VM of given type
            if (!_vmToPopupMapping.TryGetValue(popupViewModel.GetType(), out Type popupType))
            {
                throw new ArgumentOutOfRangeException($"Seems like {popupViewModel} hasn't been registered with the PopupService. Try again!");
            }

            // Create a new instance of the popup page
            var popupPage = (Page)Activator.CreateInstance(popupType);

            // Set the BindingContext of the popup page to the view model
            popupPage.BindingContext = popupViewModel;

            /*
            // Create a new Popup instance
            var popup = new Popup();

            // Set the Child of the popup to the popup page
            popup.Child = popupPage;

            // Show the popup
            popup.IsOpen = true;

            // Wait for the popup to close
            await popupPage.Dispatcher.RunAsync(CoreDispatcherPriority.Normal, () =>
            {
                popupPage.Unloaded += (s, e) =>
                {
                    var vm = (BasePopupViewModel<TResult>)popupPage.BindingContext;
                    tcs.SetResult(vm.Result);
                };
            });

            // Hide the popup when it's closed
            popup.Closed += (s, e) =>
            {
                popup.IsOpen = false;
            };*/

            return default;
        }
    }
}
