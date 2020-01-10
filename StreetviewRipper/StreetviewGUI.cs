﻿using Newtonsoft.Json;
using Newtonsoft.Json.Linq;
using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Drawing;
using System.IO;
using System.Net;
using System.Text;
using System.Threading;
using System.Windows.Forms;

namespace StreetviewRipper
{
    public partial class StreetviewGUI : Form
    {
        int downloadCount = 0;
        int selectedQuality = 4;
        bool shouldStop = false;
        bool canEnableProcessing = true;
        StraightLineBias selectedBias = StraightLineBias.MIDDLE;
        List<string> downloadedIDs = new List<string>();

        public StreetviewGUI()
        {
            InitializeComponent();
            imageQuality.SelectedIndex = selectedQuality;
            straightBias.SelectedIndex = (int)selectedBias;
        }

        /* Start downloading */
        private void downloadStreetview_Click(object sender, EventArgs e)
        {
            //Setup UI
            stopThreadedDownload.Enabled = true;
            downloadStreetview.Enabled = false;
            processImages.Enabled = false;
            straightBias.Enabled = false;
            imageQuality.Enabled = false;
            streetviewURL.Enabled = false;

            //Download all in list
            downloadCount = 0;
            UpdateDownloadCountText(downloadCount);
            selectedQuality = imageQuality.SelectedIndex;
            shouldStop = false;
            selectedBias = (StraightLineBias)straightBias.SelectedIndex;
            downloadedIDs.Clear();
            List<string> streetviewIDs = new List<string>();
            foreach (string thisURL in streetviewURL.Lines)
            {
                try
                {
                    //Get the streetview ID from string and download sphere if one is found
                    streetviewIDs.Add((thisURL.Split(new string[] { "!1s" }, StringSplitOptions.None)[1].Split(new string[] { "!2e" }, StringSplitOptions.None)[0]).Replace("%2F", "/"));
                }
                catch { }
            }
            Thread t = new Thread(() => StartDownloading(streetviewIDs));
            t.Start();
        }
        private void StartDownloading(List<string> ids)
        {
            try
            {
                foreach (string id in ids)
                {
                    if (id != "")
                    {
                        JArray neighbours = DownloadStreetview(id);
                        DownloadNeighbours(neighbours);
                    }
                }
            }
            catch { }

            //Downloads are done, re-enable UI
            stoppingText.Visible = false;
            downloadStreetview.Enabled = true;
            straightBias.Enabled = true;
            if (canEnableProcessing) processImages.Enabled = true;
            imageQuality.Enabled = true;
            streetviewURL.Enabled = true;
            streetviewURL.Text = "";
        }

        /* Stop downloading */
        private void stopThreadedDownload_Click(object sender, EventArgs e)
        {
            shouldStop = true;
            stopThreadedDownload.Enabled = false;
            stoppingText.Visible = true;
        }

        /* Update download text (multithread support) */
        public void UpdateDownloadCountText(int value)
        {
            if (InvokeRequired)
            {
                this.Invoke(new Action<int>(UpdateDownloadCountText), new object[] { value });
                return;
            }
            downloadTracker.Text = "Total: " + value;
        }
        public void UpdateDownloadStatusText(string value)
        {
            if (InvokeRequired)
            {
                this.Invoke(new Action<string>(UpdateDownloadStatusText), new object[] { value });
                return;
            }
            statusText.Text = "Currently: " + value;
        }

        /* Get metadata for a Streetview ID */
        private JToken GetMetadata(string id)
        {
            var request = WebRequest.Create("http://streetview.mattfiler.co.uk?panoid=" + id);
            using (var response = request.GetResponse())
            using (var reader = new System.IO.StreamReader(response.GetResponseStream(), ASCIIEncoding.ASCII))
            {
                return JToken.Parse(reader.ReadToEnd());
            }
        }

        /* Recurse into neighbours and download them, for a specified count */
        private void DownloadNeighbours(JArray neighbourInfos)
        {
            if (shouldStop) return;
            foreach (JObject thisNeighbour in neighbourInfos)
            {
                string thisID = thisNeighbour["id"].Value<string>();
                if (!downloadedIDs.Contains(thisID))
                {
                    JArray neighbours = DownloadStreetview(thisID);
                    if (neighbours == null) continue;
                    DownloadNeighbours(neighbours);
                }
            }
        }

        /* Download a complete sphere from Streetview at a globally defined quality */
        private JArray DownloadStreetview(string id)
        {
            UpdateDownloadStatusText("finished!");
            if (shouldStop) return null;
            UpdateDownloadStatusText("downloading LDR image...");

            //Get metadata
            downloadedIDs.Add(id);
            JToken thisMeta = GetMetadata(id);
            if (thisMeta["error"].Value<string>() != "") return null;
            int tileWidth = thisMeta["tile_size"][0].Value<int>();
            int tileHeight = thisMeta["tile_size"][1].Value<int>();

            //Load every tile
            int xOffset = 0;
            int yOffset = 0;
            bool stop = false;
            List<StreetviewTile> streetviewTiles = new List<StreetviewTile>();
            for (int y = 0; y < int.MaxValue; y++)
            {
                for (int x = 0; x < int.MaxValue; x++)
                {
                    StreetviewTile newTile = new StreetviewTile();

                    WebRequest request = WebRequest.Create(thisMeta["tile_url"].Value<string>().Replace("*X*", x.ToString()).Replace("*Y*", y.ToString()).Replace("*Z*", selectedQuality.ToString()));
                    try
                    {
                        using (WebResponse response = request.GetResponse())
                        using (Stream stream = response.GetResponseStream())
                        {
                            newTile.image = Bitmap.FromStream(stream);
                        }
                    }
                    catch
                    {
                        if (x == 0) stop = true;
                        break;
                    }
                    newTile.x = xOffset;
                    newTile.y = yOffset;

                    streetviewTiles.Add(newTile);
                    xOffset += tileWidth;
                }
                if (stop) break;
                yOffset += tileWidth;
                xOffset = 0;
            }

            //Cap zoom to the max available for this sphere (UGC varies)
            if (selectedQuality >= thisMeta["compiled_sizes"].Value<JArray>().Count) selectedQuality = thisMeta["compiled_sizes"].Value<JArray>().Count - 1;

            //Compile all image tiles to one whole image
            Bitmap streetviewImage = new Bitmap(thisMeta["compiled_sizes"][selectedQuality][0].Value<int>(), thisMeta["compiled_sizes"][selectedQuality][1].Value<int>());
            Graphics streetviewRenderer = Graphics.FromImage(streetviewImage);
            for (int i = 0; i < streetviewTiles.Count; i++)
            {
                streetviewRenderer.DrawImage(streetviewTiles[i].image, streetviewTiles[i].x, streetviewTiles[i].y, tileWidth, tileHeight);
            }
            streetviewRenderer.Dispose();
            if (!Directory.Exists("OutputImages")) Directory.CreateDirectory("OutputImages");
            streetviewImage.Save("OutputImages/" + id + ".jpg", System.Drawing.Imaging.ImageFormat.Jpeg);

            //Only continue if the user chose to process images
            if (!processImages.Checked)
            {
                UpdateDownloadStatusText("finished!");
                downloadCount++;
                UpdateDownloadCountText(downloadCount);
                return thisMeta["neighbours"].Value<JArray>();
            }

            //Calculate metadata
            UpdateDownloadStatusText("calculating metadata...");
            StreetviewImageProcessor processor = new StreetviewImageProcessor();
            List<GroundInfo> groundPositions = processor.GuessGroundPositions(streetviewImage, (selectedQuality * 5) + 15, true, (StraightLineBias)selectedBias);
            int groundY = (int)groundPositions[0].position.y; //We only have [0] and [1] when using straight line cutting, both have the same Y
            Vector2 sunPos = processor.GuessSunPosition(streetviewImage, groundY);

            //Write some of the metadata locally
            JToken localMeta = JToken.Parse("{}");
            if (thisMeta["road"].Value<string>() == null) localMeta["location"] = new JArray { "Unknown", "Unknown" };
            else localMeta["location"] = new JArray { thisMeta["road"].Value<string>(), thisMeta["region"].Value<string>() };
            localMeta["coordinates"] = new JArray { thisMeta["coordinates"][0].Value<double>(), thisMeta["coordinates"][1].Value<double>() };
            localMeta["date"] = new JArray { thisMeta["date"][1].Value<int>(), thisMeta["date"][0].Value<int>() };
            localMeta["resolution"] = new JArray { thisMeta["compiled_sizes"][selectedQuality][0].Value<int>(), thisMeta["compiled_sizes"][selectedQuality][1].Value<int>() };
            localMeta["history"] = thisMeta["history"];
            if (thisMeta["is_ugc"].Value<bool>()) localMeta["creator"] = thisMeta["creator"];
            else localMeta["creator"] = "Google";
            localMeta["ground_y"] = groundY;
            localMeta["sun"] = new JArray { (int)sunPos.x, (int)sunPos.y };
            File.WriteAllText("OutputImages/" + id + ".json", localMeta.ToString(Formatting.Indented));

            //Shift the image to match Hosek-Wilkie sun position
            UpdateDownloadStatusText("adjusting LDR image...");
            int shiftDist = (int)sunPos.x - (streetviewImage.Width / 4);
            streetviewImage = processor.ShiftImageLeft(streetviewImage, shiftDist);
            streetviewImage.Save("OutputImages/" + id + "_shifted.jpg", System.Drawing.Imaging.ImageFormat.Jpeg);

            //Trim the ground from the adjusted image
            UpdateDownloadStatusText("cropping LDR image...");
            Bitmap streetviewImageTrim = new Bitmap(thisMeta["compiled_sizes"][selectedQuality][0].Value<int>(), groundY);
            for (int x = 0; x < streetviewImageTrim.Width; x++)
            {
                for (int y = 0; y < streetviewImageTrim.Height; y++)
                {
                    streetviewImageTrim.SetPixel(x, y, streetviewImage.GetPixel(x, y));
                }
            }

            //Create Hosek-Wilkie sky model for background
            UpdateDownloadStatusText("calculating sky model...");
            if (File.Exists("PBRT/" + id + ".exr")) File.Delete("PBRT/" + id + ".exr");

            float groundAlbedo = 0.5f; //TODO: set this between 0-1
            float sunElevation = (sunPos.y / groundY) * 90;
            float skyTurbidity = 3.0f; //TODO: set this between 1.7-10

            ProcessStartInfo processInfo = new ProcessStartInfo(AppDomain.CurrentDomain.BaseDirectory + "/PBRT/imgtool.exe", "makesky --albedo " + groundAlbedo + " --elevation " + sunElevation + " --outfile " + id + ".exr --turbidity " + skyTurbidity + " --resolution " + (int)(thisMeta["compiled_sizes"][selectedQuality][0].Value<int>() / 2));
            processInfo.WorkingDirectory = AppDomain.CurrentDomain.BaseDirectory + "/PBRT/";
            processInfo.CreateNoWindow = true;
            processInfo.UseShellExecute = false;
            Process process = Process.Start(processInfo);
            process.WaitForExit();
            process.Close();

            if (File.Exists("PBRT/" + id + ".exr")) File.Copy("PBRT/" + id + ".exr", "OutputImages/" + id + "_sky.exr", true);
            if (File.Exists("PBRT/" + id + ".exr")) File.Delete("PBRT/" + id + ".exr");

            //Convert sky model to HDR from LDR
            UpdateDownloadStatusText("converting sky model...");
            processInfo = new ProcessStartInfo(AppDomain.CurrentDomain.BaseDirectory + "/EXR2LDR/exr2ldr.exe", id + "_sky.exr " + id + "_sky.png");
            processInfo.WorkingDirectory = AppDomain.CurrentDomain.BaseDirectory + "/OutputImages/";
            processInfo.CreateNoWindow = true;
            processInfo.UseShellExecute = false;
            process = Process.Start(processInfo);
            process.WaitForExit();
            process.Close();

            //Cut the sky model out of the original LDR image
            UpdateDownloadStatusText("removing sky model from LDR...");
            Bitmap skyModel = (Bitmap)Image.FromFile("OutputImages/" + id + "_sky.png");
            Bitmap streetviewNoSky = new Bitmap(streetviewImageTrim.Width, streetviewImageTrim.Height);
            for (int x = 0; x < streetviewImageTrim.Width; x++)
            {
                for (int y = 0; y < streetviewImageTrim.Height; y++)
                {
                    Color skyPixel = skyModel.GetPixel(x, y);
                    Color originalPixel = streetviewImageTrim.GetPixel(x, y);

                    float newR = (float)(originalPixel.R - skyPixel.R) / 255.0f;
                    if (newR < 0) newR = 0;
                    float newG = (float)(originalPixel.G - skyPixel.G) / 255.0f;
                    if (newG < 0) newG = 0;
                    float newB = (float)(originalPixel.B - skyPixel.B) / 255.0f;
                    if (newB < 0) newB = 0;

                    float newA = (newR * 0.2126f) + (newG * 0.7152f) + (newB * 0.0722f);
                    int newAFinal = ((int)(newA * 255) - 255) * -1;
                    if (newAFinal < 0) newAFinal = 0;
                    if (newAFinal > 255) newAFinal = 255;
                    //int grayScale = (int)((originalPixel.R * .3) + (originalPixel.G * .59) + (originalPixel.B * .11));
                    Color newColor = Color.FromArgb(newAFinal, originalPixel.R, originalPixel.G, originalPixel.B);

                    streetviewNoSky.SetPixel(x, y, newColor);
                }
            }
            streetviewNoSky.Save("OutputImages/" + id + "_removedsky.png", System.Drawing.Imaging.ImageFormat.Png);

            //Write LDR histograms
            HistogramTools histogramUtils = new HistogramTools();
            UpdateDownloadStatusText("calculating LDR histograms...");
            histogramUtils.CreateLDRHistogram(streetviewImage, id + "_ldr");
            Image downsampledStreetview = new Bitmap(128, 64);
            using (Graphics g = Graphics.FromImage(downsampledStreetview))
            {
                g.InterpolationMode = System.Drawing.Drawing2D.InterpolationMode.High;
                g.DrawImage(streetviewImage, new Rectangle(Point.Empty, downsampledStreetview.Size));
            }
            histogramUtils.CreateLDRHistogram((Bitmap)downsampledStreetview, id + "_ldrdownscaled");

            //Convert to HDR image
            UpdateDownloadStatusText("converting to HDR...");
            streetviewImage.Save("LDR2HDR/streetview.jpg", System.Drawing.Imaging.ImageFormat.Jpeg);
            if (File.Exists("LDR2HDR/streetview.hdr")) File.Delete("LDR2HDR/streetview.hdr");

            processInfo = new ProcessStartInfo("cmd.exe", "/c \"" + AppDomain.CurrentDomain.BaseDirectory + "/LDR2HDR/run.bat\"");
            processInfo.WorkingDirectory = AppDomain.CurrentDomain.BaseDirectory + "/LDR2HDR/";
            processInfo.CreateNoWindow = true;
            processInfo.UseShellExecute = false;
            process = Process.Start(processInfo);
            process.WaitForExit();
            process.Close();

            if (File.Exists("OutputImages/" + id + ".hdr")) File.Delete("OutputImages/" + id + ".hdr");
            if (File.Exists("LDR2HDR/streetview.hdr")) File.Copy("LDR2HDR/streetview.hdr", "OutputImages/" + id + ".hdr");
            if (File.Exists("LDR2HDR/streetview.jpg")) File.Delete("LDR2HDR/streetview.jpg");
            if (File.Exists("LDR2HDR/streetview.hdr")) File.Delete("LDR2HDR/streetview.hdr");

            //If we didn't get a HDR image back, the Python environment probably isn't installed properly
            if (!File.Exists("OutputImages/" + id + ".hdr"))
            {
                MessageBox.Show("Could not convert to HDR.\nCheck Conda environment!", "Conversion error.", MessageBoxButtons.OK, MessageBoxIcon.Error);
                shouldStop = true;
                downloadCount++;
                UpdateDownloadCountText(downloadCount);
                UpdateDownloadStatusText("failed on HDR stage!");
                return null;
            }

            //Read in HDR values
            UpdateDownloadStatusText("reading HDR output...");
            HDRImage hdrImage = new HDRImage();
            hdrImage.Open("OutputImages/" + id + ".hdr");

            //Upscale the HDR image
            UpdateDownloadStatusText("upscaling HDR...");
            HDRUtilities hdrUtils = new HDRUtilities();
            HDRImage hdrUpscaled = hdrUtils.Upscale(hdrImage, thisMeta["compiled_sizes"][selectedQuality][0].Value<int>() / hdrImage.Width);
            hdrUpscaled.Save("OutputImages/" + id + "_upscaled.hdr");

            //Calculate histograms for HDR
            UpdateDownloadStatusText("calculating HDR histograms...");
            histogramUtils.CreateHDRHistogram(hdrImage, id + "_hdr");
            histogramUtils.CreateHDRHistogram(hdrUpscaled, id + "_hdrupscaled");

            //Re-write the upscaled HDR image without the ground
            UpdateDownloadStatusText("cropping upscaled HDR...");
            HDRImage hdrCropped = new HDRImage();
            hdrCropped.SetResolution(thisMeta["compiled_sizes"][selectedQuality][0].Value<int>(), groundY);
            for (int x = 0; x < hdrCropped.Width; x++)
            {
                for (int y = 0; y < hdrCropped.Height; y++)
                {
                    hdrCropped.SetPixel(x, y, hdrUpscaled.GetPixel(x, y));
                }
            }
            hdrCropped.Save("OutputImages/" + id + "_upscaled_trim.hdr");

            //Re-write the upscaled & cropped HDR image as a fisheye ready for classifying
            /*
            UpdateDownloadStatusText("converting to fisheye...");
            HDRImage hdrFisheye = hdrUtils.ToFisheye(hdrCropped, hdrCropped.Width / 10);
            hdrFisheye.Save("OutputImages/" + id + "_upscaled_trim_fisheye.hdr");
            */

            //Classify the upscaled image
            UpdateDownloadStatusText("classifying cloud formations...");
            if (File.Exists("Classify/Input_Output_Files/" + id + ".hdr")) File.Delete("Classify/Input_Output_Files/" + id + ".hdr");
            if (File.Exists("Classify/Input_Output_Files/" + id + "_classified.hdr")) File.Delete("Classify/Input_Output_Files/" + id + "_classified.hdr");
            //File.Copy("OutputImages/" + id + "_upscaled_trim_fisheye.hdr", "Classify/Input_Output_Files/" + id + ".hdr");
            File.Copy("OutputImages/" + id + "_upscaled_trim.hdr", "Classify/Input_Output_Files/" + id + ".hdr"); //Fisheye is a bit janky, so for now, use the trim

            processInfo = new ProcessStartInfo(AppDomain.CurrentDomain.BaseDirectory + "/Classify/Classify.exe", "5 400 100 0 " + id + " " + id + "_classified");
            processInfo.WorkingDirectory = AppDomain.CurrentDomain.BaseDirectory + "/Classify/";
            processInfo.CreateNoWindow = true;
            processInfo.UseShellExecute = false;
            process = Process.Start(processInfo);
            process.WaitForExit();
            process.Close();

            if (File.Exists("OutputImages/" + id + "_classified.hdr")) File.Delete("OutputImages/" + id + "_classified.hdr");
            if (File.Exists("Classify/Input_Output_Files/" + id + "_classified.hdr")) File.Copy("Classify/Input_Output_Files/" + id + "_classified.hdr", "OutputImages/" + id + "_classified.hdr");
            if (File.Exists("Classify/Input_Output_Files/" + id + ".hdr")) File.Delete("Classify/Input_Output_Files/" + id + ".hdr");
            if (File.Exists("Classify/Input_Output_Files/" + id + "_classified.hdr")) File.Delete("Classify/Input_Output_Files/" + id + "_classified.hdr");
            HDRImage hdrClassified = new HDRImage();
            hdrClassified.Open("OutputImages/" + id + "_classified.hdr");

            //Pull classified clouds from the image & save them
            UpdateDownloadStatusText("extracting classified clouds...");
            Bitmap cutoutStratocumulus = hdrUtils.PullCloudType(hdrClassified, streetviewImageTrim, HDRUtilities.CloudTypes.STRATOCUMULUS);
            Bitmap cutoutCumulus = hdrUtils.PullCloudType(hdrClassified, streetviewImageTrim, HDRUtilities.CloudTypes.CUMULUS);
            Bitmap cutoutCirrus = hdrUtils.PullCloudType(hdrClassified, streetviewImageTrim, HDRUtilities.CloudTypes.CIRRUS);
            Bitmap cutoutClearSky = hdrUtils.PullCloudType(hdrClassified, streetviewImageTrim, HDRUtilities.CloudTypes.CLEAR_SKY);
            cutoutStratocumulus.Save("OutputImages/" + id + "_classified_stratocumulus.png", System.Drawing.Imaging.ImageFormat.Png);
            cutoutCumulus.Save("OutputImages/" + id + "_classified_cumulus.png", System.Drawing.Imaging.ImageFormat.Png);
            cutoutCirrus.Save("OutputImages/" + id + "_classified_cirrus.png", System.Drawing.Imaging.ImageFormat.Png);
            cutoutClearSky.Save("OutputImages/" + id + "_classified_clearsky.png", System.Drawing.Imaging.ImageFormat.Png);

            /*
            //Convert HDR values to regular float values
            UpdateDownloadStatusText("converting HDR output...");
            if (File.Exists("HDR2Float/streetview.hdr")) File.Delete("HDR2Float/streetview.hdr");
            File.Copy("OutputImages/" + id + ".hdr", "HDR2Float/streetview.hdr");

            processInfo = new ProcessStartInfo(AppDomain.CurrentDomain.BaseDirectory + "/HDR2Float/HDR2Float.exe", "");
            processInfo.WorkingDirectory = AppDomain.CurrentDomain.BaseDirectory + "/HDR2Float/";
            processInfo.CreateNoWindow = true;
            processInfo.UseShellExecute = false;
            process = Process.Start(processInfo);
            process.WaitForExit();
            process.Close();

            if (File.Exists("OutputImages/" + id + ".bin")) File.Delete("OutputImages/" + id + ".bin");
            if (File.Exists("HDR2Float/streetview.bin")) File.Copy("HDR2Float/streetview.bin", "OutputImages/" + id + ".bin");
            if (File.Exists("HDR2Float/streetview.bin")) File.Delete("HDR2Float/streetview.bin");
            if (File.Exists("HDR2Float/streetview.hdr")) File.Delete("HDR2Float/streetview.hdr");

            //Read in the converted float values from the HDR
            BinaryReader binReader = new BinaryReader(File.OpenRead("OutputImages/" + id + ".bin"));
            List<HDRPixelAsFloat> parsedPixels = new List<HDRPixelAsFloat>();
            for (int i = 0; i < binReader.BaseStream.Length / sizeof(float) / 3; i++)
            {
                HDRPixelAsFloat newPixel = new HDRPixelAsFloat();
                newPixel.R = binReader.ReadSingle();
                newPixel.G = binReader.ReadSingle();
                newPixel.B = binReader.ReadSingle();
                parsedPixels.Add(newPixel);
            }
            binReader.Close();
            */

            //Done!
            downloadCount++;
            UpdateDownloadCountText(downloadCount);
            UpdateDownloadStatusText("finished!");
            return thisMeta["neighbours"].Value<JArray>();
        }

        /* On load, validate our setup, and disable image processing options if external tools are unavailable. */
        private void StreetviewGUI_Load(object sender, EventArgs e)
        {
            Control.CheckForIllegalCrossThreadCalls = false; //We always do this safely.

            if (!File.Exists(AppDomain.CurrentDomain.BaseDirectory + "/PBRT/imgtool.exe") ||
                !File.Exists(AppDomain.CurrentDomain.BaseDirectory + "/LDR2HDR/run.bat") ||
                !File.Exists(AppDomain.CurrentDomain.BaseDirectory + "/EXR2LDR/exr2ldr.exe") ||
                !File.Exists(AppDomain.CurrentDomain.BaseDirectory + "/HDR2Float/HDR2Float.exe"))
            {
                processImages.Checked = false;
                processImages.Enabled = false;
                straightBias.Enabled = false;
                canEnableProcessing = false;
            }
        }

        /* Change available options on check changed */
        private void processImages_CheckedChanged(object sender, EventArgs e)
        {
            straightBias.Enabled = processImages.Checked;
        }
    }
}
