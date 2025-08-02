using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Xamarin.Essentials;
using Xamarin.Forms;
using Xerpi.Models.API;
using Xerpi.Services;

namespace Xerpi.ViewModels
{
    public class SettingsViewModel : BasePageViewModel
    {
        public override string Url => "settings";

        private readonly ISettingsService _settingsService;
        private readonly IDerpiNetworkService _networkService;
        private readonly IMessagingCenter _messagingService;

        // TODO: One day, this should include user-definable filters.
        private List<SettingsFilterViewModel> _filters = new List<SettingsFilterViewModel>();
        public List<SettingsFilterViewModel> Filters
        {
            get => _filters;
            set => Set(ref _filters, value);
        }

        private List<AppTheme> _themeChoices = new List<AppTheme> { AppTheme.Unspecified, AppTheme.Dark, AppTheme.Light };
        public List<AppTheme> ThemeChoices
        {
            get => _themeChoices;
            set => Set(ref _themeChoices, value);
        }

        private bool ignoreOnce = true;
        private AppTheme _selectedTheme;
        public AppTheme SelectedTheme
        {
            get => _selectedTheme;
            set
            {
                // Ignore the initial "Unspecified" from the UI control's two-way binding
                if (ignoreOnce)
                {
                    ignoreOnce = !ignoreOnce;
                    return;
                }
                Set(ref _selectedTheme, value);
                _settingsService.SelectedTheme = value;
            }
        }

        private bool _isLoading;
        public bool IsLoading
        {
            get => _isLoading;
            set => Set(ref _isLoading, value);
        }

        private bool _compactMode;
        public bool CompactMode
        {
            get => _compactMode;
            set
            {
                Set(ref _compactMode, value);
                _settingsService.CompactMode = value;
            }
        }

        private bool _showScoreIcons;
        public bool ShowScoreIcons
        {
            get => _showScoreIcons;
            set
            {
                Set(ref _showScoreIcons, value);
                _settingsService.ShowScoreIcons = value;
            }
        }

        private static SettingsViewModel _instance;
        public static SettingsViewModel Instance => _instance;

        public SettingsViewModel(ISettingsService settingsService,
            IDerpiNetworkService networkService,
            IMessagingCenter messagingService)
        {
            _instance = this;
            _settingsService = settingsService;
            _networkService = networkService;
            _messagingService = messagingService;

            Title = "Settings";

            CompactMode = _settingsService.CompactMode;
            ShowScoreIcons = _settingsService.ShowScoreIcons;

            IsLoading = true;
        }

        protected override async Task NavigatedToOverride()
        {
            SelectedTheme = _settingsService.SelectedTheme;
            var defaultFilters = await _networkService.GetDefaultFilters();
            if (defaultFilters != null)
            {
                Filters = defaultFilters
                    .Select(x => new SettingsFilterViewModel(x, _settingsService, _messagingService))
                    .ToList();
            }

            IsLoading = false;
        }
    }
}
