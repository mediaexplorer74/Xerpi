using DynamicData;
using DynamicData.Binding;
using System;
using System.Collections.ObjectModel;
using System.Diagnostics;
using System.Linq;
using System.Reactive.Linq;
using System.Threading;
using System.Threading.Tasks;
using Xamarin.Essentials;
using Xamarin.Forms;
using Xerpi.Models;
using Xerpi.Models.API;
using Xerpi.Services;

namespace Xerpi.ViewModels
{
    public class ImageGalleryViewModel : BasePageViewModel
    {
        public override string Url => "imagegallery";
        private readonly IImageService _imageService;
        private readonly INavigationService _navigationService;
        private readonly IDerpiNetworkService _networkService;
        private readonly ISynchronizationContextService _syncContextService;
        private readonly IMessagingCenter _messagingService;
        private readonly SearchPageComparer _pageComparer = new SearchPageComparer();

        private ApiImage? _navParameterImage;
        private CancellationTokenSource _cts = new CancellationTokenSource();

        private DetailedImageViewModel? _currentImage;
        public DetailedImageViewModel CurrentImage
        {
            get => _currentImage;
            set => Set(ref _currentImage, value);
        }

        private ReadOnlyObservableCollection<DetailedImageViewModel> _images;
        public ReadOnlyObservableCollection<DetailedImageViewModel> Images
        {
            get => _images;
            set => Set(ref _images, value);
        }

        private bool _isImageViewerOpen = false;
        public bool IsImageViewerOpen
        {
            get => _isImageViewerOpen;
            set => Set(ref _isImageViewerOpen, value);
        }

        private int _currentImageNumber = 0;
        public int CurrentImageNumber
        {
            get => _currentImageNumber;
            set => Set(ref _currentImageNumber, value);
        }

        public uint CurrentTotalImages => _imageService.CurrentTotalImages;

        public Command<DetailedImageViewModel> CurrentImageChangedCommand { get; private set; }
        public Command SoftBackPressedCommand { get; private set; }
        public Command FullSizeButtonCommand { get; private set; }
        public Command ThresholdReachedCommand { get; private set; }
        public Command OpenInBrowserCommand { get; private set; }
        public Command<ApiTag> TagTappedCommand { get; private set; }

        public ImageGalleryViewModel(IImageService imageService,
            INavigationService navigationService,
            IDerpiNetworkService networkSerivce,
            ISynchronizationContextService syncContextService,
            IMessagingCenter messagingService)
        {
            _imageService = imageService;
            _navigationService = navigationService;
            _networkService = networkSerivce;
            _syncContextService = syncContextService;
            _messagingService = messagingService;
            _images = new ReadOnlyObservableCollection<DetailedImageViewModel>(new ObservableCollectionExtended<DetailedImageViewModel>());

            CurrentImageChangedCommand = new Command<DetailedImageViewModel>(CurrentImageChanged);
            SoftBackPressedCommand = new Command(SoftBackPressed);
            FullSizeButtonCommand = new Command(FullSizeButtonPressed);
            ThresholdReachedCommand = new Command(ThresholdReached);
            OpenInBrowserCommand = new Command(OpenInBrowserPressed);
            TagTappedCommand = new Command<ApiTag>(TagTapped);

            _imageService.CurrentImages.Connect()
                 .Filter(x => !x.MimeType.Contains("video")) // TODO: Make sure this only covers webm, and not other things we can actually handle
                 .Sort(_pageComparer, SortOptimisations.ComparesImmutableValuesOnly)
                 .Transform(x => new DetailedImageViewModel(x, _imageService, _networkService))
                 .ObserveOn(_syncContextService.UIThread)
                 .Bind(out _images, resetThreshold: 75)
                 .DisposeMany()
                 .Subscribe(x =>
                 {
                     if (_navParameterImage != null)
                     {
                         NavigateToSelectedImage();
                     }
                 });
        }

        private async void OpenInBrowserPressed()
        {
            await Browser.OpenAsync($"{_networkService.BaseUri}images/{CurrentImage.BackingImage.Id}", BrowserLaunchMode.External);
        }

        private bool _gettingPage = false;
        private async void ThresholdReached()
        {
            if (_gettingPage)
            {
                return;
            }

            _gettingPage = true;
            await _imageService.AddPageToSearch(_imageService.PagesVisible.Max() + 1, 50);
            _gettingPage = false;
        }

        private void TagTapped(ApiTag tag)
        {
            _backPayloadPrepared = true;
            _navigationService.Back(tag);
            _backPayloadPrepared = false;
        }

        private async void NavigateToSelectedImage()
        {
            try
            {
                if (_navParameterImage == null)
                {
                    Debug.WriteLine("[ImageGallery] No navigation parameter image set");
                    return;
                }

                Debug.WriteLine($"[ImageGallery] Looking for image with ID: {_navParameterImage.Id}");
                
                // First, check if we already have the image in our collection
                var foundImage = Images.FirstOrDefault(x => x.BackingImage.Id == _navParameterImage.Id);
                if (foundImage != null)
                {
                    Debug.WriteLine($"[ImageGallery] Found existing image with ID: {_navParameterImage.Id}");
                    CurrentImage = foundImage;
                    _navParameterImage = null;
                    return;
                }

                // If not found, we need to load it
                Debug.WriteLine($"[ImageGallery] Image not found in current collection, creating new view model");
                var newImageVm = new DetailedImageViewModel(_navParameterImage, _imageService, _networkService);
                await newImageVm.InitExternalData(CancellationToken.None);

                // Add to our collection and set as current
                //(Images as ObservableCollectionExtended<DetailedImageViewModel>)?.Add(newImageVm);
                var images = new ObservableCollectionExtended<DetailedImageViewModel>(Images);
                images.Add(newImageVm);

                CurrentImage = newImageVm;
                _navParameterImage = null;
                
                Debug.WriteLine($"[ImageGallery] Successfully loaded and set image {newImageVm.BackingImage.Id}");
            }
            catch (Exception ex)
            {
                Debug.WriteLine($"[ImageGallery] Error navigating to selected image: {ex}");
            }
        }

        private async void CurrentImageChanged(DetailedImageViewModel newImage)
        {
            _cts.Cancel();
            _cts = new CancellationTokenSource();
            Title = $"{newImage.BackingImage.Id}";
            CurrentImageNumber = CurrentImageNumber = Images.IndexOf(CurrentImage) + 1;
            await newImage.InitExternalData(_cts.Token);
        }

        private bool _backPayloadPrepared = false;
        public override bool OnBack()
        {
            if (IsImageViewerOpen)
            {
                IsImageViewerOpen = false;
                Title = $"{CurrentImage.BackingImage.Id}";
                return false;
            }

            if (!_backPayloadPrepared)
            {
                _backPayloadPrepared = true;
                _navigationService.Back(CurrentImage.BackingImage);
                return false;
            }

            return true;
        }

        private void FullSizeButtonPressed()
        {
            IsImageViewerOpen = true;
            Title = $"{CurrentImage.BackingImage.Id}: Large";
        }

        private void SoftBackPressed()
        {
            _navigationService.Back();
        }

        protected override async Task NavigatedToOverride()
        {
            try
            {
                _backPayloadPrepared = false;
                _navParameterImage = NavigationParameter as ApiImage;
                
                if (_navParameterImage == null)
                {
                    Debug.WriteLine("[ImageGallery] No image provided in navigation parameters");
                    return;
                }

                Debug.WriteLine($"[ImageGallery] Navigated to with image ID: {_navParameterImage.Id}");
                
                // Initialize the view with the first image if we don't have any yet
                if (Images.Count == 0 && _imageService.CurrentImages.Count > 0)
                {
                    var firstImage = _imageService.CurrentImages.Items.FirstOrDefault();
                    if (firstImage != null)
                    {
                        var firstVm = new DetailedImageViewModel(firstImage, _imageService, _networkService);
                        await firstVm.InitExternalData(CancellationToken.None);
                        //(Images as ObservableCollectionExtended<DetailedImageViewModel>)?.Add(firstVm);
                        var images = new ObservableCollectionExtended<DetailedImageViewModel>(Images);
                        images.Add(firstVm);
                        CurrentImage = firstVm;
                    }
                }

                // Navigate to the selected image
                NavigateToSelectedImage();
                OnPropertyChanged(nameof(CurrentTotalImages));
            }
            catch (Exception ex)
            {
                Debug.WriteLine($"[ImageGallery] Error in NavigatedToOverride: {ex}");
                throw;
            }
        }

        public override Task NavigatedFromOverride()
        {
            return base.NavigatedFromOverride();
        }
    }
}
