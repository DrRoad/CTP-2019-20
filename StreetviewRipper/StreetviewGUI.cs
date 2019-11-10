﻿using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Drawing;
using System.IO;
using System.Net;
using System.Text;
using System.Windows.Forms;

namespace StreetviewRipper
{
    public partial class StreetviewGUI : Form
    {
        StreetviewImageProcessor processor = new StreetviewImageProcessor();
        StreetviewQualityDef thisQuality;
        int downloadCount = 0;
        List<string> downloadedIDs = new List<string>();

        public StreetviewGUI()
        {
            InitializeComponent();
            streetviewZoom.SelectedIndex = 0;
            straightBias.SelectedIndex = 1;
        }

        private void downloadStreetview_Click(object sender, EventArgs e)
        {
            //Setup UI
            downloadStreetview.Enabled = false;
            Cursor.Current = Cursors.WaitCursor;
            downloadProgress.Value = 0;
            downloadProgress.Maximum = streetviewURL.Lines.Length;

            //Work out what zoom to use
            thisQuality = new StreetviewQualityDef();
            switch (streetviewZoom.Text)
            {
                case "Ultra":
                    thisQuality.Set(5, 26, 13, 512, 45);
                    break;
                case "High":
                    thisQuality.Set(4, 13, 7, 512, 35);
                    break;
                case "Medium":
                    thisQuality.Set(3, 7, 4, 512, 25);
                    break;
                case "Low":
                    thisQuality.Set(2, 4, 2, 512, 20);
                    break;
                case "Lower":
                    thisQuality.Set(1, 2, 1, 512, 15);
                    break;
                case "Lowest":
                    thisQuality.Set(0, 1, 1, 512, 10);
                    break;
            }

            //Check the user means to recurse!
            if (recurseNeighbours.Checked)
                if (MessageBox.Show("You have selected recursion - this will download neighbouring spheres from the provided URLs until no unique neighbours can be found (unlikely).\nFor that reason, manual termination of the program is required to stop download.\nAre you sure you wish to proceed?", "Are you sure?", MessageBoxButtons.YesNo, MessageBoxIcon.Warning) != DialogResult.Yes) return;

            //Download all in list
            downloadCount = 0;
            downloadedIDs.Clear();
            string streetviewID = "";
            foreach (string thisURL in streetviewURL.Lines)
            {
                try
                {
                    //Get the streetview ID from string and download sphere if one is found
                    streetviewID = (thisURL.Split(new string[] { "!1s" }, StringSplitOptions.None)[1].Split(new string[] { "!2e" }, StringSplitOptions.None)[0]).Replace("%2F", "/");
                    if (streetviewID != "")
                    {
                        DownloadStreetview(streetviewID);
                        if (followNeighbours.Checked) DownloadNeighbours(streetviewID);
                    }
                }
                catch { }
                downloadProgress.PerformStep();
            }
            
            //Finished
            streetviewURL.Text = "";
            downloadStreetview.Enabled = true;
            Cursor.Current = Cursors.Default;
            downloadProgress.Value = downloadProgress.Maximum;
            MessageBox.Show("Downloaded " + downloadCount + " Streetview sphere(s) from " + downloadProgress.Maximum + " URL(s)!", "Complete!", MessageBoxButtons.OK, MessageBoxIcon.Information);
        }

        /* Recurse into neighbours and download them, for a specified count */
        private void DownloadNeighbours(string id)
        {
            var request = WebRequest.Create("http://streetview.mattfiler.co.uk?panoid=" + id);
            using (var response = request.GetResponse())
            using (var reader = new System.IO.StreamReader(response.GetResponseStream(), ASCIIEncoding.ASCII))
            {
                foreach (string newStreetviewID in reader.ReadToEnd().Split('\n'))
                {
                    if (!downloadedIDs.Contains(newStreetviewID))
                    {
                        DownloadStreetview(newStreetviewID);
                        if (recurseNeighbours.Checked) DownloadNeighbours(newStreetviewID);
                    }
                }
            }
        }

        /* Download a complete sphere from Streetview at a globally defined quality */
        private void DownloadStreetview(string id)
        {
            //Load every tile
            int xOffset = 0;
            int yOffset = 0;
            List<StreetviewTile> streetviewTiles = new List<StreetviewTile>();
            for (int y = 0; y < thisQuality.y; y++)
            {
                for (int x = 0; x < thisQuality.x; x++)
                {
                    StreetviewTile newTile = new StreetviewTile();

                    var request = WebRequest.Create("https://geo1.ggpht.com/cbk?cb_client=maps_sv.tactile&authuser=0&hl=en&gl=uk&panoid=" + id + "&output=tile&x=" + x + "&y=" + y + "&zoom=" + thisQuality.zoom);
                    using (var response = request.GetResponse())
                    using (var stream = response.GetResponseStream())
                    {
                        newTile.image = Bitmap.FromStream(stream);
                    }
                    newTile.x = xOffset;
                    newTile.y = yOffset;

                    streetviewTiles.Add(newTile);
                    xOffset += thisQuality.size;
                }
                yOffset += thisQuality.size;
                xOffset = 0;
            }

            //Compile all image tiles to one whole image
            Bitmap streetviewImage = new Bitmap(thisQuality.size * thisQuality.x, thisQuality.size * thisQuality.y);
            Graphics streetviewRenderer = Graphics.FromImage(streetviewImage);
            foreach (StreetviewTile thisTile in streetviewTiles)
            {
                streetviewRenderer.DrawImage(thisTile.image, thisTile.x, thisTile.y, thisQuality.size, thisQuality.size);
            }
            streetviewRenderer.Dispose();
            if (!trimGround.Checked) streetviewImage.Save(id + "_" + streetviewZoom.Text.ToLower() + ".png");
            if (trimGround.Checked) processor.CutOutSky(streetviewImage, thisQuality.acc, straightTrim.Checked, (StraightLineBias)straightBias.SelectedIndex).Save(id + "_" + streetviewZoom.Text.ToLower() + ".png");
            downloadCount++;
            downloadedIDs.Add(id);

            //If requested to guess sun pos, do that and output it
            if (!guessSun.Checked) return;
            File.WriteAllText(id + "_" + streetviewZoom.Text.ToLower() + ".json", "{\n\"sun_pos\": " + processor.GetSunXPos(streetviewImage) + "\n}");
        }

        /* Update UI when options are enabled/disabled */
        private void followNeighbours_CheckedChanged(object sender, EventArgs e)
        {
            recurseNeighbours.Enabled = followNeighbours.Checked;
            recurseNeighbours.Checked = false;
        }
        private void trimGround_CheckedChanged(object sender, EventArgs e)
        {
            straightTrim.Enabled = trimGround.Checked;
            straightTrim.Checked = false;
            if (!trimGround.Checked)
            {
                straightBias.Enabled = false;
                straightBias.SelectedIndex = 1;
            }
        }
        private void straightTrim_CheckedChanged(object sender, EventArgs e)
        {
            straightBias.Enabled = straightTrim.Checked;
            straightBias.SelectedIndex = 1;
        }
    }
}
