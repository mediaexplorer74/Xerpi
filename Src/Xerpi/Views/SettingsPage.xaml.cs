using Xamarin.Forms;
using Xerpi.ViewModels;

namespace Xerpi.Views
{
    public partial class SettingsPage : NavigablePage
    {
        private SettingsViewModel ViewModel
        {
            get
            {
                return (SettingsViewModel)_viewModel;
            }
        }

        public SettingsPage() : base(typeof(SettingsViewModel))
        {
            InitializeComponent();
            
            BindingContext = ViewModel;
            //Experimental
            //BindingContext = new SettingsViewModel(
            //   DependencyService.Get<Xerpi.Services.ISettingsService>(),
            //   DependencyService.Get<Xerpi.Services.IDerpiNetworkService>(),
            //   DependencyService.Get<IMessagingCenter>()   );

        }
    }
}