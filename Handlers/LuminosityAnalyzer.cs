using Android.App;
using Android.Gms.Tasks;
using Android.Media;
using Android.Runtime;
using Android.Widget;
using AndroidX.Camera.Core;
using Java.Nio;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Xamarin.Google.MLKit.Vision.BarCode;
using Xamarin.Google.MLKit.Vision.Common;

namespace CameraX
{
    //https://codelabs.developers.google.com/codelabs/camerax-getting-started#5

    public class LuminosityAnalyzer : Java.Lang.Object, ImageAnalysis.IAnalyzer
    {
        private const string TAG = "CameraXBasic";
        private readonly Action<List<string>> lumaListerner;

        public LuminosityAnalyzer(Action<List<string>> callback) //LumaListener listener)
        {
            this.lumaListerner = callback;
        }

        public async void Analyze(IImageProxy image)
        {
            //var buffer = image.GetPlanes()[0].Buffer;

            //await ExtractBarcode(buffer, image.Width, image.Height, image.ImageInfo.RotationDegrees);
            var result = await ExtractBarcode(image.Image, image.ImageInfo.RotationDegrees);

            //var data = ToByteArray(buffer);

            //var pixels = data.ToList();
            //pixels.ForEach(x => x = (byte)((int)x & 0xFF));
            //var luma = pixels.Average(x => x);
            ////Log.Debug(TAG, $"Average luminosity: {luma}");

            image.Close();

            lumaListerner.Invoke(result);
        }

        private byte[] ToByteArray(ByteBuffer buff)
        {
            buff.Rewind();    // Rewind the buffer to zero
            var data = new byte[buff.Remaining()];
            buff.Get(data);   // Copy the buffer into a byte array
            return data;      // Return the byte array
        }

        public async Task<List<string>> ExtractBarcode(Image image, int rotationDegrees)
        {
            //var bitmap = await BitmapFactory.DecodeStreamAsync(image);
            //var inputImage = InputImage.FromBitmap(bitmap, 0);
            //Only 0, 90, 180, 270 are supported.
            //Camera2 API를 사용할 경우 ImageFormat.YUV_420_888 : 35
            var inputImage = InputImage.FromMediaImage(image, rotationDegrees);

            var b = new BarcodeScannerOptions.Builder();
            b.SetBarcodeFormats(Barcode.FormatAllFormats);
            var opts = b.Build();

            var recognizer = BarcodeScanning.GetClient(opts);
            var result = await ToAwaitableTask(recognizer.Process(inputImage));

            var barcodes = result.JavaCast<Android.Runtime.JavaList<Barcode>>();

            List<string> barcodeList = new List<string>();
            foreach (Barcode item in barcodes)
            {
                barcodeList.Add(item.RawValue);
                Console.WriteLine("Barcode = " + item.RawValue);
            }

            return barcodeList;
        }

        public async Task<List<string>> ExtractBarcode(ByteBuffer buffer, int width, int height, int rotationDegrees)
        {
            //var bitmap = await BitmapFactory.DecodeStreamAsync(image);
            //var inputImage = InputImage.FromBitmap(bitmap, 0);
            //Only 0, 90, 180, 270 are supported.
            //Camera2 API를 사용할 경우
            //ImageFormat.YUV_420_888 : 35
            //ImageFormat.NV21 : 17
            //
            var inputImage = InputImage.FromByteBuffer(buffer, width, height, rotationDegrees, 17);

            var b = new BarcodeScannerOptions.Builder();
            b.SetBarcodeFormats(Barcode.FormatAllFormats);
            var opts = b.Build();

            var recognizer = BarcodeScanning.GetClient(opts);
            var result = await ToAwaitableTask(recognizer.Process(inputImage));

            var barcodes = result.JavaCast<Android.Runtime.JavaList<Barcode>>();

            List<string> barcodeList = new List<string>();
            foreach (Barcode item in barcodes)
            {
                barcodeList.Add(item.RawValue);
                Console.WriteLine("Barcode = " + item.RawValue);
            }

            return barcodeList;
        }

        public static Task<Java.Lang.Object> ToAwaitableTask(Android.Gms.Tasks.Task task)
        {
            var taskCompletionSource = new TaskCompletionSource<Java.Lang.Object>();
            var taskCompleteListener = new TaskCompleteListener(taskCompletionSource);
            task.AddOnCompleteListener(taskCompleteListener);

            return taskCompletionSource.Task;
        }


        class TaskCompleteListener : Java.Lang.Object, IOnCompleteListener
        {
            private readonly TaskCompletionSource<Java.Lang.Object> taskCompletionSource;

            public TaskCompleteListener(TaskCompletionSource<Java.Lang.Object> tcs)
            {
                this.taskCompletionSource = tcs;
            }

            public void OnComplete(Android.Gms.Tasks.Task task)
            {
                if (task.IsCanceled)
                {
                    this.taskCompletionSource.SetCanceled();
                }
                else if (task.IsSuccessful)
                {
                    this.taskCompletionSource.SetResult(task.Result);
                }
                else
                {
                    this.taskCompletionSource.SetException(task.Exception);
                }
            }
        }
    }
}
