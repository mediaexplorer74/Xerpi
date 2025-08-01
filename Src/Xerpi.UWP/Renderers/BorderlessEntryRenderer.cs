using Xamarin.Forms;
using Xamarin.Forms.Platform.UWP;
using Xerpi.Controls;
using Windows.UI.Xaml.Controls;
using Windows.UI.Xaml;
using Windows.UI.Xaml.Media;

[assembly: ExportRenderer(typeof(BorderlessEntry), typeof(Xerpi.UWP.Renderers.BorderlessEntryRenderer))]
namespace Xerpi.UWP.Renderers
{
    public class BorderlessEntryRenderer : EntryRenderer
    {
        protected override void OnElementChanged(ElementChangedEventArgs<Entry> e)
        {
            base.OnElementChanged(e);

            if (Control != null)
            {
                // Remove border by setting a transparent background
                Control.BorderThickness = new Windows.UI.Xaml.Thickness(0);
                Control.Background = null;
                Control.Padding = new Windows.UI.Xaml.Thickness(0);
                Control.Margin = new Windows.UI.Xaml.Thickness(0);

                // Remove the border from the template if needed
                Control.Style = null;
            }
        }
    }
}
