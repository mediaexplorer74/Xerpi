using DynamicData;
using DynamicData.Binding;
using System;
using System.Collections.Generic;
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

        private int _currentImageIndex;
        public int CurrentImageIndex
        {
            get => _currentImageIndex;
            set
            {
                if (Set(ref _currentImageIndex, value) && value >= 0 && value < (Images?.Count ?? 0))
                {
                    CurrentImage = Images[value];
                    OnPropertyChanged(nameof(CanGoBack));
                    OnPropertyChanged(nameof(CanGoForward));
                    OnPropertyChanged(nameof(CurrentImageNumber));
                }
            }
        }

        private DetailedImageViewModel? _currentImage;
        public DetailedImageViewModel? CurrentImage
        {
            get => _currentImage;
            private set
            {
                if (_currentImage != value)
                {
                    _currentImage = value;
                    OnPropertyChanged();
                    if (value != null)
                    {
                        Title = $"{value.BackingImage.Id}";
                    }
                }
            }
        }

        public bool CanGoBack => CurrentImageIndex > 0;
        public bool CanGoForward => CurrentImageIndex < (Images?.Count - 1 ?? 0);
        public int CurrentImageNumber => CurrentImageIndex + 1;

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

        public uint CurrentTotalImages => _imageService.CurrentTotalImages;

        public Command<DetailedImageViewModel> CurrentImageChangedCommand { get; private set; }
        public Command SoftBackPressedCommand { get; private set; }
        public Command FullSizeButtonCommand { get; private set; }
        public Command ThresholdReachedCommand { get; private set; }
        public Command OpenInBrowserCommand { get; private set; }
        public Command<ApiTag> TagTappedCommand { get; private set; }
        public Command NextImageCommand { get; private set; }
        public Command PreviousImageCommand { get; private set; }

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
            NextImageCommand = new Command(() => MoveToImage(CurrentImageIndex + 1));
            PreviousImageCommand = new Command(() => MoveToImage(CurrentImageIndex - 1));

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
            if (_navParameterImage == null)
            {
                Debug.WriteLine("[ImageGallery] No navigation parameter image set");
                return;
            }

            Debug.WriteLine($"[ImageGallery] Looking for image with ID: {_navParameterImage.Id}");

            try
            {
                var foundIndex = Images?.ToList().FindIndex(x => x?.BackingImage?.Id == _navParameterImage.Id) ?? -1;
                if (foundIndex >= 0)
                {
                    // Image found in collection
                    Debug.WriteLine($"[ImageGallery] Found existing image at index {foundIndex}");
                    CurrentImageIndex = foundIndex;
                }
                else
                {
                    // Create and add new image to collection
                    Debug.WriteLine("[ImageGallery] Image not found, adding to collection");
                    var newImageVm = new DetailedImageViewModel(_navParameterImage, _imageService, _networkService);

                    try
                    {
                        if (!newImageVm.IsInitialized)
                        {
                            await newImageVm.InitExternalData(CancellationToken.None);
                        }
                    }
                    catch (Exception ex)
                    {
                        Debug.WriteLine($"[ImageGallery] Error initializing image data: {ex.Message}");
                    }

                    // Add to collection and set as current
                    var newList = Images?.ToList() ?? new List<DetailedImageViewModel>();
                    newList.Insert(0, newImageVm);

                    var newCollection = new ObservableCollectionExtended<DetailedImageViewModel>(newList);
                    Images = new ReadOnlyObservableCollection<DetailedImageViewModel>(newCollection);

                    // Set as current after collection is updated
                    CurrentImageIndex = 0;
                }
            }
            catch (Exception ex)
            {
                Debug.WriteLine($"[ImageGallery] Error in NavigateToSelectedImage: {ex.Message}");
                Debug.WriteLine($"Stack trace: {ex.StackTrace}");
            }
            finally
            {
                _navParameterImage = null;
            }
        }

        private void MoveToImage(int index)
        {
            if (index >= 0 && index < (Images?.Count ?? 0))
            {
                CurrentImageIndex = index;
            }
        }

        private async void CurrentImageChanged(DetailedImageViewModel newImage)
        {
            try
            {
                if (newImage?.BackingImage == null)
                {
                    Debug.WriteLine("[ImageGallery] Cannot change to null image");
                    return;
                }

                _cts.Cancel();
                _cts = new CancellationTokenSource();
                
                // Update the title with the image ID
                Title = $"{newImage.BackingImage.Id}";
                
                // Don't update CurrentImageNumber here to prevent scrolling issues
                // The CarouselView will handle the scrolling automatically
                
                // Initialize the new image's data if not already loaded
                if (!newImage.IsInitialized)
                {
                    try
                    {
                        await newImage.InitExternalData(_cts.Token);
                    }
                    catch (Exception ex)
                    {
                        Debug.WriteLine($"[ImageGallery] Error initializing image data: {ex.Message}");
                    }
                }
            }
            catch (Exception ex)
            {
                Debug.WriteLine($"[ImageGallery] Error in CurrentImageChanged: {ex.Message}");
                Debug.WriteLine($"Stack trace: {ex.StackTrace}");
            }
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
