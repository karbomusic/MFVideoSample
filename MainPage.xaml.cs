using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Runtime.InteropServices.WindowsRuntime;
using System.Threading.Tasks;
using Windows.Foundation;
using Windows.Foundation.Collections;
using Windows.Media.Capture;
using Windows.UI.Xaml;
using Windows.UI.Xaml.Controls;
using Windows.UI.Xaml.Controls.Primitives;
using Windows.UI.Xaml.Data;
using Windows.UI.Xaml.Input;
using Windows.UI.Xaml.Media;
using Windows.UI.Xaml.Navigation;
using System.Diagnostics;
using Windows.Media.MediaProperties;
using Windows.Storage.Streams;
using static System.Net.Mime.MediaTypeNames;
using System.Text.RegularExpressions;

// The Blank Page item template is documented at https://go.microsoft.com/fwlink/?LinkId=402352&clcid=0x409

/*

   UWP App capabilites/permissions must be enabled for Videos Library, Photos Library, Webcam and Microphone or this code will silently fail.

*/ 
namespace VidCapCSharp
{
    /// <summary>
    /// An empty page that can be used on its own or navigated to within a Frame.
    /// </summary>
    public sealed partial class MainPage : Page
    {
        private MediaCapture mediaCapture;
        private bool isInitialized = false;

        public MainPage()
        {
            this.InitializeComponent();
            Loaded += async (sender, args) =>
            {
                await PrintMessage("Initializing camera...");
                await InitializeCameraAsync();

                // Grab a photo and save to Pictures library
                await PrintMessage("Capturing and saving still image...");
                await CapturePhotoAsync();

                // Begin capturing a video stream, let it run for 10 seconds, then stop and save the stream
                await PrintMessage("Capturing video for 10 seconds...");

                // Start the video capture
                await StartVideoCaptureAsync();

                //---------------------------------------------------------------------------
                // OK, video is recording, let's try capturing a photo while
                // the video stream is open that is not a video frame but a still image.
                // The device must support this so we check first.

                if (mediaCapture.MediaCaptureSettings.ConcurrentRecordAndPhotoSupported)
                {
                    // Device supports capturing photos while recording video
                    await PrintMessage("Device supports concurrent video/photo capture. Capturing still...");
                    await CapturePhotoWhileRecordingAsync();
                    await PrintMessage("Concurrent still image captured successfuly.");
                }
                else
                {
                    // Device does not support capturing photos while recording video
                    await PrintMessage("Device doesn't appear to support concurrent video/photo capture. No concurrent still captured.");
                }
                //---------------------------------------------------------------------------

                // Wait for the desired duration (10 seconds in this case)
                await Task.Delay(TimeSpan.FromSeconds(10));

                // Stop the video capture
                await PrintMessage("10-second duration complete, saving video...");
                await StopVideoCaptureAsync();
                await PrintMessage("Video save complete...");

            };

        }

        private async Task PrintMessage(string message)
        {
            Debug.WriteLine(message);
            StatusTextBlock.Text += message + "\r\n";
        }
        private async Task InitializeCameraAsync()
        {
            if (mediaCapture == null)
            {
                mediaCapture = new MediaCapture();
                try
                {
                    var settings = new MediaCaptureInitializationSettings
                    {
                        StreamingCaptureMode = StreamingCaptureMode.Video,
                        PhotoCaptureSource = PhotoCaptureSource.VideoPreview
                    };
                    await mediaCapture.InitializeAsync();
                    isInitialized = true;
                }
                catch (UnauthorizedAccessException uex)
                {
                    // User has denied access to the camera.
                    await PrintMessage("Init failed: Access denied to camera! " + uex.Message);
                }
                catch (Exception ex)
                {
                    // Other initialization error has occurred.
                    await PrintMessage("Init failed: Access denied to camera! " + ex.Message);
                }
            }
        }

        private async Task StartVideoCaptureAsync()
        {
            if (isInitialized)
            {
                var storageFile = await Windows.Storage.KnownFolders.VideosLibrary.CreateFileAsync("test-video.mp4", Windows.Storage.CreationCollisionOption.GenerateUniqueName);
                var recordProfile = Windows.Media.MediaProperties.MediaEncodingProfile.CreateMp4(Windows.Media.MediaProperties.VideoEncodingQuality.Auto);
                await mediaCapture.StartRecordToStorageFileAsync(recordProfile, storageFile);
                // Recording has started.
            }
        }

        private async Task StopVideoCaptureAsync()
        {
            if (isInitialized)
            {
                await mediaCapture.StopRecordAsync();
                // Recording has stopped and the file is saved.
            }
        }

        private async Task CapturePhotoAsync()
        {
            if (isInitialized)
            {
                var storageFile = await Windows.Storage.KnownFolders.PicturesLibrary.CreateFileAsync("test-photo.jpg", Windows.Storage.CreationCollisionOption.GenerateUniqueName);
                await mediaCapture.CapturePhotoToStorageFileAsync(Windows.Media.MediaProperties.ImageEncodingProperties.CreateJpeg(), storageFile);
                // Photo has been captured and saved.
            }
        }

        private async Task CapturePhotoWhileRecordingAsync()
        {
            if (mediaCapture != null && isInitialized)
            {
                using (var photoStream = new InMemoryRandomAccessStream())
                {
                    // Use JPEG format for the photo
                    var properties = ImageEncodingProperties.CreateJpeg();

                    // Capture the photo to the stream
                    await mediaCapture.CapturePhotoToStreamAsync(properties, photoStream);

                    // Reset stream position to the beginning since the capture operation will have moved the current position to the end
                    photoStream.Seek(0);

                    // Create a file to save the photo
                    var file = await Windows.Storage.KnownFolders.PicturesLibrary.CreateFileAsync("concurrent-test-photo.jpg", Windows.Storage.CreationCollisionOption.GenerateUniqueName);

                    // Write the stream to the file
                    using (var fileStream = await file.OpenAsync(Windows.Storage.FileAccessMode.ReadWrite))
                    {
                        await RandomAccessStream.CopyAndCloseAsync(photoStream.GetInputStreamAt(0), fileStream.GetOutputStreamAt(0));
                    }
                }
            }
        }
    }
}
