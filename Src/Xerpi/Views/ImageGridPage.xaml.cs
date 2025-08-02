using System;
using System.Linq;
using System.Threading.Tasks;
using System.Diagnostics;
using Xamarin.Forms;
using Xerpi.ViewModels;
using Xerpi.Models.API;
using Microsoft.Extensions.DependencyInjection;
using Xerpi.Messages;
using Xerpi.Models;

namespace Xerpi.Views
{
    public partial class ImageGridPage : NavigablePage
    {
        private bool _isDisposed;
        private bool _isProcessingSelection;
        private readonly IMessagingCenter _messagingService;
        private ImageGridViewModel ViewModel => (ImageGridViewModel)_viewModel;

        public ImageGridPage() : base(typeof(ImageGridViewModel))
        {
            InitializeComponent();
            BindingContext = ViewModel;
            _messagingService = Xamarin.Forms.MessagingCenter.Instance;
            _messagingService.Subscribe<ImageGridViewModel, NavigatedBackToImageGridMessage>(
                this, 
                string.Empty, 
                OnNavigatedFromGallery);
        }

        private async void OnNavigatedFromGallery(ImageGridViewModel _, NavigatedBackToImageGridMessage args)
        {
            try
            {
                if (args?.Image == null)
                {
                    Debug.WriteLine("[ImageGridPage] No image provided for navigation");
                    return;
                }

                Debug.WriteLine($"[ImageGridPage] Starting navigation back to grid for image ID: {args.Image.Id}");
                
                // Small delay to ensure UI is ready
                await Task.Delay(200);

                if (_isDisposed || ImageListCollectionView == null)
                {
                    Debug.WriteLine($"[ImageGridPage] Cannot scroll - Page is {(ImageListCollectionView == null ? "missing CollectionView" : "disposed")}");
                    return;
                }

                // Store references to avoid potential null refs
                var collectionView = ImageListCollectionView;
                var imageToScroll = args.Image;

                if (collectionView == null || imageToScroll == null || _isDisposed)
                {
                    Debug.WriteLine("[ImageGridPage] Critical objects not available for scrolling");
                    return;
                }

                Debug.WriteLine($"[ImageGridPage] Attempting to scroll to image ID: {imageToScroll.Id}");
                
                // Use a local function to avoid capturing 'this'
                static async Task SafeScrollToAsync(CollectionView collectionView, object item)
                {
                    try
                    {
                        await Task.Delay(100); // Additional delay for UI to stabilize
                        if (collectionView != null)
                        {
                            /*await*/ collectionView.ScrollTo(item, position: ScrollToPosition.Start, animate: false);
                            Debug.WriteLine("[ImageGridPage] Scroll completed successfully");
                        }
                    }
                    catch (Exception ex)
                    {
                        Debug.WriteLine($"[ImageGridPage] Error in SafeScrollToAsync: {ex.GetType().Name} - {ex.Message}");
                    }
                }

                // Use the dispatcher to ensure we're on the UI thread
                await Device.InvokeOnMainThreadAsync(async () =>
                {
                    try
                    {
                        if (!_isDisposed && collectionView != null)
                        {
                            await SafeScrollToAsync(collectionView, imageToScroll);
                        }
                    }
                    catch (Exception ex)
                    {
                        Debug.WriteLine($"[ImageGridPage] Error in UI thread dispatch: {ex.GetType().Name} - {ex.Message}");
                    }
                });
            }
            catch (Exception ex)
            {
                Debug.WriteLine($"[ImageGridPage] Critical error in OnNavigatedFromGallery: {ex.GetType().Name} - {ex.Message}");
                Debug.WriteLine($"Stack trace: {ex.StackTrace}");
            }
        }

        private async void CollectionView_SelectionChanged(object sender, SelectionChangedEventArgs e)
        {
            // Skip if we're already processing a selection or the page is disposed
            if (_isProcessingSelection || _isDisposed)
            {
                Debug.WriteLine("[ImageGridPage] Selection already in progress or page disposed");
                if (sender is CollectionView collectionView) 
                {
                    collectionView.SelectedItem = null;
                }
                return;
            }

            // Skip if no selection
            if (e.CurrentSelection == null || e.CurrentSelection.Count == 0)
            {
                Debug.WriteLine("[ImageGridPage] No selection");
                return;
            }

            // Validate sender
            if (!(sender is CollectionView cv))
            {
                Debug.WriteLine("[ImageGridPage] Sender is not a CollectionView");
                return;
            }

            _isProcessingSelection = true;
            Debug.WriteLine("[ImageGridPage] Selection processing started");
            
            try
            {
                // Get the selected item
                var selectedItem = e.CurrentSelection.FirstOrDefault();
                if (selectedItem == null)
                {
                    Debug.WriteLine("[ImageGridPage] No item selected");
                    return;
                }

                // Try to cast to ApiImage
                if (!(selectedItem is ApiImage selectedImage))
                {
                    Debug.WriteLine($"[ImageGridPage] Selected item is of type {selectedItem.GetType().Name}, expected ApiImage");
                    return;
                }

                Debug.WriteLine($"[ImageGridPage] Selected image ID: {selectedImage.Id}");
                
                // Clear selection immediately to allow re-selection of the same item
                Device.BeginInvokeOnMainThread(() =>
                {
                    try
                    {
                        cv.SelectedItem = null;
                    }
                    catch (Exception ex)
                    {
                        Debug.WriteLine($"[ImageGridPage] Error clearing selection: {ex.Message}");
                    }
                });
                
                // Small delay to ensure UI is responsive
                await Task.Delay(50);
                
                if (_isDisposed)
                {
                    Debug.WriteLine("[ImageGridPage] Page was disposed during selection processing");
                    return;
                }

                // Unsubscribe from any existing messages to prevent duplicates
                try
                {
                    _messagingService?.Unsubscribe<ImageGridViewModel, NavigatedBackToImageGridMessage>(this, string.Empty);
                }
                catch (Exception ex)
                {
                    Debug.WriteLine($"[ImageGridPage] Error unsubscribing from messages: {ex.Message}");
                }
                
                // Trigger navigation
                Debug.WriteLine($"[ImageGridPage] Navigating to image {selectedImage.Id}");
                Device.BeginInvokeOnMainThread(() =>
                {
                    try
                    {
                        ViewModel.ImageSelected(selectedImage);
                    }
                    catch (Exception ex)
                    {
                        Debug.WriteLine($"[ImageGridPage] Error in ImageSelected: {ex.Message}");
                    }
                });
            }
            catch (Exception ex)
            {
                Debug.WriteLine($"[ImageGridPage] Error in CollectionView_SelectionChanged: {ex.GetType().Name} - {ex.Message}");
                Debug.WriteLine($"Stack trace: {ex.StackTrace}");
            }
            finally
            {
                _isProcessingSelection = false;
                Debug.WriteLine("[ImageGridPage] Selection processing completed");
            }
        }

        protected override async Task OnAppearingOverride()
        {
            await base.OnAppearingOverride();
            // Any additional appearance logic can go here
        }

        protected override async Task OnDisappearingOverride()
        {
            try
            {
                Debug.WriteLine("[ImageGridPage] OnDisappearing - Cleaning up");
                _isDisposed = true;
                _messagingService.Unsubscribe<ImageGridViewModel, NavigatedBackToImageGridMessage>(this, "");
                await base.OnDisappearingOverride();
            }
            catch (Exception ex)
            {
                Debug.WriteLine($"[ImageGridPage] Error during cleanup: {ex.Message}");
                await base.OnDisappearingOverride();
                throw;
            }
        }

        private void TitleSearch_SearchSortOptionsChanged(object sender, SearchSortOptions newOptions)
        {
            try
            {
                if (ViewModel?.SortOptionsChangedCommand?.CanExecute(newOptions) == true)
                {
                    ViewModel.SortOptionsChangedCommand.Execute(newOptions);
                }
            }
            catch (Exception ex)
            {
                Debug.WriteLine($"[ImageGridPage] Error in TitleSearch_SearchSortOptionsChanged: {ex.GetType().Name} - {ex.Message}");
                Debug.WriteLine($"Stack trace: {ex.StackTrace}");
            }
        }

        private void ImageListCollectionView_Scrolled(object sender, ItemsViewScrolledEventArgs e)
        {
            if (e.FirstVisibleItemIndex <= ImageListCollectionView.RemainingItemsThreshold)
            {
                // TODO: Manually scroll the list once it updates? hmmmm
                ViewModel.GetPreviousPageCommand.Execute(null);
            }
        }
    }
}