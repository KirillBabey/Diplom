using System;
using Android.App;
using Android.OS;
using Android.Runtime;
using Android.Views;
using AndroidX.AppCompat.Widget;
using AndroidX.AppCompat.App;
using Google.Android.Material.FloatingActionButton;
using Google.Android.Material.Snackbar;
using System.ComponentModel;
using System.IO;
//using Microsoft.Azure.CognitiveServices.Vision.Face;
using System.Threading.Tasks;
using System.Collections.Generic;
//using Microsoft.Azure.CognitiveServices.Vision.Face.Models;
using System.Linq;
using System.Net;
using System.Net.Http;
using System.Text;
using Newtonsoft.Json;
using System.Net.Http.Headers;
using System.Threading;
using Microsoft.Azure.CognitiveServices.Vision.Face;
using Microsoft.Azure.CognitiveServices.Vision.Face.Models;

namespace XamarinAndroidAzureFace
{
    [DesignTimeVisible(true)]
    [Activity(Label = "@string/app_name", Theme = "@style/AppTheme.NoActionBar", MainLauncher = true)]
    public class MainActivity : AppCompatActivity
    {
        private const string SUBSCRIPTION_KEY = "bb4e459531fb4a23ac4dbd31f2fda69f";
        private const string ENDPOINT = "https://freefaceres.cognitiveservices.azure.com/";
        private static IFaceClient client = Authenticate(ENDPOINT, SUBSCRIPTION_KEY);

        protected override void OnCreate(Bundle savedInstanceState)
        {
            base.OnCreate(savedInstanceState);
            Xamarin.Essentials.Platform.Init(this, savedInstanceState);
            SetContentView(Resource.Layout.activity_main);

            Toolbar toolbar = FindViewById<Toolbar>(Resource.Id.toolbar);
            SetSupportActionBar(toolbar);

            FloatingActionButton fab = FindViewById<FloatingActionButton>(Resource.Id.fab);
            fab.Click += FabOnClick;
        }

        public override bool OnCreateOptionsMenu(IMenu menu)
        {
            MenuInflater.Inflate(Resource.Menu.menu_main, menu);
            return true;
        }

        public override bool OnOptionsItemSelected(IMenuItem item)
        {
            int id = item.ItemId;
            if (id == Resource.Id.action_settings)
            {
                return true;
            }

            return base.OnOptionsItemSelected(item);
        }

        [Obsolete]
        private void FabOnClick(object sender, EventArgs eventArgs)
        {
            string RECOGNITION_MODEL3 = RecognitionModel.Recognition03;
            string folderPath = (string)Android.OS.Environment.GetExternalStoragePublicDirectory("Faces2");
            List<string> answer = IdentifyPersonInGroup("testgroup1", $"{folderPath}/3-Jason.png", RECOGNITION_MODEL3).Result;
            View view = (View)sender;
            Snackbar.Make(view, answer[0], Snackbar.LengthLong)
                .SetAction("Action", (View.IOnClickListener)null).Show();
        }

        public override void OnRequestPermissionsResult(int requestCode, string[] permissions, [GeneratedEnum] Android.Content.PM.Permission[] grantResults)
        {
            Xamarin.Essentials.Platform.OnRequestPermissionsResult(requestCode, permissions, grantResults);

            base.OnRequestPermissionsResult(requestCode, permissions, grantResults);
        }
        
        public static IFaceClient Authenticate(string endpoint, string key)
        {
            return new FaceClient(new ApiKeyServiceClientCredentials(key)) { Endpoint = endpoint };
        }
        
        public static async Task<List<DetectedFace>> DetectFaces(string identifiableFacialImagePath, string recognitionModel)
        {
            IList<DetectedFace> detectedFaces;
            using (Stream stream = File.OpenRead(identifiableFacialImagePath))
            {
                long i = stream.Length;
                detectedFaces = client.Face.DetectWithStreamAsync(stream, recognitionModel: recognitionModel, detectionModel: DetectionModel.Detection02).Result;
            }
            return detectedFaces.ToList();
        }
        
        public static async Task<List<string>> IdentifyPersonInGroup(string personGroupId, string identifiableFacialImagePath, string recognitionModel)
        {           
            List<Guid> faceIds = new List<Guid>();
            // Detect faces from source image url.
            List<DetectedFace> detectedFaces = DetectFaces(identifiableFacialImagePath, recognitionModel).Result;
            // Add detected faceId to sourceFaceIds.
            foreach (var detectedFace in detectedFaces)
                faceIds.Add(detectedFace.FaceId.Value);

            // Identify the faces in a person group. 
            var identifyResults = client.Face.IdentifyAsync(faceIds, personGroupId).Result;
            List<string> answer = new List<string>();

            foreach (var identifyResult in identifyResults)
            {
                if (identifyResult.Candidates.Count > 0)
                {
                    Microsoft.Azure.CognitiveServices.Vision.Face.Models.Person person = 
                        client.PersonGroupPerson.GetAsync(personGroupId, identifyResult.Candidates[0].PersonId).Result;
                    answer.Add(person.Name);
                }
                else
                {
                    answer.Add("Unknown");
                }
            }
            return answer;
        }
        
        /*
        public static async Task DetectFaces2(string identifiableFacialImagePath, string recognitionModel)
        {
            using (Stream stream = File.OpenRead(identifiableFacialImagePath))
            {
                await DetectAsync(stream, "detection_02", recognitionModel);
            }
        }

        public static async Task DetectAsync(Stream imageStream, string detectionModel, string recognitionModel)
        {
            string Endpoint = "https://freefaceres.cognitiveservices.azure.com/face/v1.0";
            var requestUrl =
              $"{Endpoint}/detect?overload=stream&detectionModel={detectionModel}" +
              $"&recognitionModel={recognitionModel}";
            await SendRequestAsync<Stream>(HttpMethod.Post, requestUrl, imageStream);
        }

        public static async Task SendRequestAsync<TRequest>(HttpMethod httpMethod, string requestUrl, TRequest requestBody)
        {
            string Endpoint = "https://southeastasia.api.cognitive.microsoft.com/face/v1.0";
            HttpClient client = new HttpClient();
            client.DefaultRequestHeaders.Add("Ocp-Apim-Subscription-Key", SUBSCRIPTION_KEY);
            var request = new HttpRequestMessage(httpMethod, Endpoint);
            request.RequestUri = new Uri(requestUrl);
            if (requestBody != null)
            {
                if (requestBody is Stream)
                {
                    request.Content = new StreamContent(requestBody as Stream);
                    request.Content.Headers.ContentType = new MediaTypeHeaderValue("application/octet-stream");
                }
                else
                {
                    // If the image is supplied via a URL
                    request.Content = new StringContent(JsonConvert.SerializeObject(requestBody), Encoding.UTF8, "application/json");
                }
            }

            client.Timeout = TimeSpan.FromSeconds(15);
            HttpResponseMessage responseMessage = await client.SendAsync(request).ConfigureAwait(false);

            HttpContent c = responseMessage.Content;
            string s = await c.ReadAsStringAsync();
            string st;
            if (responseMessage.IsSuccessStatusCode)
            {
                string responseContent = null;
                if (responseMessage.Content != null)
                {
                    responseContent = await responseMessage.Content.ReadAsStringAsync();
                }
                if (!string.IsNullOrWhiteSpace(responseContent))
                {
                    st = JsonConvert.DeserializeObject<string>(responseContent);
                }
            }
            else
            {
                throw new HttpRequestException();
            }
        }
        */
    }
}
