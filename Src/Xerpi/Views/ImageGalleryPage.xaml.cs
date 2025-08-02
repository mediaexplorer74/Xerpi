using Microsoft.Extensions.DependencyInjection;
using System;
using System.ComponentModel;
using System.Threading;
using System.Diagnostics;
using Xamarin.Forms;
using Xamarin.Forms.Xaml;
using Xerpi.Messages;
using Xerpi.ViewModels;

namespace Xerpi.Views
{
    [XamlCompilation(XamlCompilationOptions.Compile)]
    public partial class ImageGalleryPage : NavigablePage
    {
        private const int SwipeThreshold = 100;
        private double _startX;
        private bool _isSwiping;
        private readonly IMessagingCenter _messagingService;
        private ImageGalleryViewModel ViewModel => (ImageGalleryViewModel)_viewModel;

        public ImageGalleryPage() : base(typeof(ImageGalleryViewModel))
        {
            InitializeComponent();
            BindingContext = ViewModel;
            _messagingService = Startup.ServiceProvider.GetRequiredService<IMessagingCenter>();

            // Subscribe to property changes
            ViewModel.PropertyChanged += ViewModel_PropertyChanged;

            Disappearing += ImageGalleryPage_Disappearing;
        }

        private void ImageGalleryPage_Disappearing(object sender, EventArgs e)
        {
            base.OnDisappearing();
            ViewModel.PropertyChanged -= ViewModel_PropertyChanged;
        }


        private void ViewModel_PropertyChanged(object sender, PropertyChangedEventArgs e)
        {
            if (e.PropertyName == nameof(ViewModel.CurrentImage))
            {
                // Update the UI when the current image changes
                UpdateImageDisplay();
            }
        }

        private void UpdateImageDisplay()
        {
            if (ViewModel?.CurrentImage == null)
            {
                Debug.WriteLine("[ImageGallery] Cannot update display - CurrentImage is null");
                return;
            }

            var currentImageId = ViewModel.CurrentImage.BackingImage.Id;
            Debug.WriteLine($"[ImageGallery] Updating display for image ID: {currentImageId}");
            
            // Ensure we're on the UI thread
            Device.BeginInvokeOnMainThread(async () =>
            {
                try
                {
                    // Force update all bindings
                    OnPropertyChanged(string.Empty);
                    
                    // Force update the image source
                    if (ImageControl != null)
                    {
                        // Clear the image source first to force a reload
                        var temp = ImageControl.Source;
                        ImageControl.Source = null;
                        ImageControl.Source = temp;
                        
                        Debug.WriteLine($"[ImageGallery] Image source updated for ID: {currentImageId}");
                    }
                    
                    // Ensure tags are loaded for the current image
                    if (!ViewModel.CurrentImage.IsInitialized)
                    {
                        Debug.WriteLine($"[ImageGallery] Initializing data for image ID: {currentImageId}");
                        await ViewModel.CurrentImage.InitExternalData(CancellationToken.None);
                        
                        // Force update the UI after tags are loaded
                        OnPropertyChanged(nameof(ViewModel.CurrentImage));
                        OnPropertyChanged(nameof(ViewModel.CurrentImage.Tags));
                        
                        // Update the bottom panel to show the loaded tags
                        if (BottomPanel != null)
                        {
                            BottomPanel.ForceUpdate();
                        }
                    }
                }
                catch (Exception ex)
                {
                    Debug.WriteLine($"[ImageGallery] Error in UpdateImageDisplay: {ex.Message}");
                    Debug.WriteLine($"Stack trace: {ex.StackTrace}");
                }
            });
        }

      

        private void OnPanUpdated(object sender, PanUpdatedEventArgs e)
        {
            switch (e.StatusType)
            {
                case GestureStatus.Started:
                    _startX = e.TotalX;
                    _isSwiping = true;
                    break;

                case GestureStatus.Running:
                    if (_isSwiping)
                    {
                        var diff = _startX - e.TotalX;
                        // Only process horizontal swipes
                        if (Math.Abs(diff) > SwipeThreshold)
                        {
                            _isSwiping = false;
                            if (diff > 0 && ViewModel.CanGoForward)
                            {
                                ViewModel.NextImageCommand.Execute(null);
                            }
                            else if (diff < 0 && ViewModel.CanGoBack)
                            {
                                ViewModel.PreviousImageCommand.Execute(null);
                            }
                        }
                    }
                    break;

                case GestureStatus.Completed:
                case GestureStatus.Canceled:
                    _isSwiping = false;
                    break;
            }
        }

        private void TapGestureRecognizer_Tapped(object sender, EventArgs e)
        {
            if (BottomPanel.IsOpen)
            {
                BottomPanel.IsOpen = false;
            }
            else if (ViewModel?.FullSizeButtonCommand?.CanExecute(null) == true)
            {
                ViewModel.FullSizeButtonCommand.Execute(null);
            }
        }
    }
}