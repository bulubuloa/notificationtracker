using System;
using System.Net;
using System.Net.Http;
using System.Threading;
using System.Threading.Tasks;
using Xamarin.Android.Net;

namespace notificationtracker
{
    public class WorkaroundAndroidClientHandler : AndroidClientHandler
    {
        public WorkaroundAndroidClientHandler()
        {
            this.AutomaticDecompression = System.Net.DecompressionMethods.GZip | System.Net.DecompressionMethods.Deflate;
        }

        protected override async Task<HttpResponseMessage> SendAsync(HttpRequestMessage request, CancellationToken cancellationToken)
        {
            // The native Android HTTP client handler bubbles up Java exceptions in contrast to the iOS native handler.
            // See for instance https://github.com/xamarin/xamarin-android/issues/3216.
            // We see this behavior on a Samsung 9 with Android 9.
            // Remedy is to explicitly catch expected native network exceptions.
            try
            {
                return await base.SendAsync(request, cancellationToken).ConfigureAwait(false);
            }
            catch (Java.Net.SocketException ex)
            {
                throw new WebException($"Native SocketException on HTTP request: {ex.Message}", ex);
            }
            catch (Java.IO.IOException ex)
            {
                throw new WebException($"Native IOException on HTTP request: {ex.Message}", ex);
            }
            catch (Java.Lang.Exception ex)
            {
                throw new WebException($"Native Exception on HTTP request: {ex.Message}", ex);
            }
        }
    }
}