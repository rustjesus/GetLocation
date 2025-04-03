using System;
using System.Net.Http;
using System.Threading.Tasks;
using System.Windows.Forms;
using Newtonsoft.Json;
using Newtonsoft.Json.Linq;
using GeoJSON.Net.Geometry;
using GeoJSON.Net.Feature;
using System.IO;
using System.Threading;

namespace GetLocation
{
    public partial class Form1 : Form
    {
        private bool onLoop = true;
        private int secondsToWait = 100;
        private CancellationTokenSource cts;

        public Form1()
        {
            InitializeComponent();
            cts = new CancellationTokenSource();
        }

        private void Form1_Load(object sender, EventArgs e)
        {
            Task.Run(async () => await LoopGeoJSON(cts.Token)); // Run in background
        }

        private async Task LoopGeoJSON(CancellationToken token)
        {
            while (onLoop && !token.IsCancellationRequested)
            {
                await GetAndSaveGeoJSON();
                await Task.Delay(secondsToWait * 1000, token); // Wait before fetching again
            }
        }

        private async Task GetAndSaveGeoJSON()
        {
            try
            {
                using (HttpClient client = new HttpClient())
                {
                    string response = await client.GetStringAsync("http://ip-api.com/json/");
                    JObject json = JObject.Parse(response);

                    string status = json["status"].ToString();
                    string country = json["country"].ToString();
                    string countryCode = json["countryCode"].ToString();
                    string region = json["region"].ToString();
                    string regionName = json["regionName"].ToString();
                    string city = json["city"].ToString();
                    string zip = json["zip"].ToString();
                    double latitude = json["lat"].ToObject<double>();
                    double longitude = json["lon"].ToObject<double>();
                    string timezone = json["timezone"].ToString();
                    string isp = json["isp"].ToString();
                    string org = json["org"].ToString();
                    string asInfo = json["as"].ToString();
                    string queryIP = json["query"].ToString();

                    var point = new Point(new Position(latitude, longitude));
                    var properties = new
                    {
                        status,
                        country,
                        countryCode,
                        region,
                        regionName,
                        city,
                        zip,
                        timezone,
                        isp,
                        org,
                        asInfo,
                        queryIP
                    };
                    var feature = new Feature(point, properties);

                    string geoJson = JsonConvert.SerializeObject(feature, Formatting.Indented);

                    string folderPath = Path.Combine(AppDomain.CurrentDomain.BaseDirectory, "LocationData");
                    Directory.CreateDirectory(folderPath);

                    string timestamp = DateTime.Now.ToString("yyyy-MM-dd_HH-mm-ss");
                    string fileName = $"location_{timestamp}.json";
                    string filePath = Path.Combine(folderPath, fileName);

                    File.WriteAllText(filePath, geoJson);

                    Console.WriteLine($"GeoJSON saved to: {filePath}");
                }
            }
            catch (Exception ex)
            {
                Console.WriteLine($"Error retrieving location: {ex.Message}");
            }
        }

        protected override void OnFormClosing(FormClosingEventArgs e)
        {
            onLoop = false;
            cts.Cancel(); // Stop the loop gracefully
            base.OnFormClosing(e);
        }
    }
}
