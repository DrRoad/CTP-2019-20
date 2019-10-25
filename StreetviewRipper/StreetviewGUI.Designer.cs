﻿namespace StreetviewRipper
{
    partial class StreetviewGUI
    {
        /// <summary>
        /// Required designer variable.
        /// </summary>
        private System.ComponentModel.IContainer components = null;

        /// <summary>
        /// Clean up any resources being used.
        /// </summary>
        /// <param name="disposing">true if managed resources should be disposed; otherwise, false.</param>
        protected override void Dispose(bool disposing)
        {
            if (disposing && (components != null))
            {
                components.Dispose();
            }
            base.Dispose(disposing);
        }

        #region Windows Form Designer generated code

        /// <summary>
        /// Required method for Designer support - do not modify
        /// the contents of this method with the code editor.
        /// </summary>
        private void InitializeComponent()
        {
            this.components = new System.ComponentModel.Container();
            this.downloadStreetview = new System.Windows.Forms.Button();
            this.label1 = new System.Windows.Forms.Label();
            this.streetviewURL = new System.Windows.Forms.TextBox();
            this.streetviewZoom = new System.Windows.Forms.ComboBox();
            this.label2 = new System.Windows.Forms.Label();
            this.downloadProgress = new System.Windows.Forms.ProgressBar();
            this.followNeighbours = new System.Windows.Forms.CheckBox();
            this.recurseNeighbours = new System.Windows.Forms.CheckBox();
            this.toolTip1 = new System.Windows.Forms.ToolTip(this.components);
            this.SuspendLayout();
            // 
            // downloadStreetview
            // 
            this.downloadStreetview.Location = new System.Drawing.Point(348, 300);
            this.downloadStreetview.Name = "downloadStreetview";
            this.downloadStreetview.Size = new System.Drawing.Size(151, 67);
            this.downloadStreetview.TabIndex = 3;
            this.downloadStreetview.Text = "Download";
            this.toolTip1.SetToolTip(this.downloadStreetview, "Download the provided URLs with given settings.");
            this.downloadStreetview.UseVisualStyleBackColor = true;
            this.downloadStreetview.Click += new System.EventHandler(this.downloadStreetview_Click);
            // 
            // label1
            // 
            this.label1.AutoSize = true;
            this.label1.Location = new System.Drawing.Point(12, 9);
            this.label1.Name = "label1";
            this.label1.Size = new System.Drawing.Size(151, 13);
            this.label1.TabIndex = 1;
            this.label1.Text = "Streetview URLs (one per line)";
            // 
            // streetviewURL
            // 
            this.streetviewURL.Location = new System.Drawing.Point(15, 25);
            this.streetviewURL.Multiline = true;
            this.streetviewURL.Name = "streetviewURL";
            this.streetviewURL.Size = new System.Drawing.Size(484, 267);
            this.streetviewURL.TabIndex = 1;
            this.toolTip1.SetToolTip(this.streetviewURL, "URLs to download - copy this from Streetview on Google Maps.");
            // 
            // streetviewZoom
            // 
            this.streetviewZoom.DropDownStyle = System.Windows.Forms.ComboBoxStyle.DropDownList;
            this.streetviewZoom.FormattingEnabled = true;
            this.streetviewZoom.Items.AddRange(new object[] {
            "Ultra",
            "High",
            "Medium",
            "Low",
            "Lower",
            "Lowest"});
            this.streetviewZoom.Location = new System.Drawing.Point(91, 300);
            this.streetviewZoom.Name = "streetviewZoom";
            this.streetviewZoom.Size = new System.Drawing.Size(153, 21);
            this.streetviewZoom.TabIndex = 2;
            this.toolTip1.SetToolTip(this.streetviewZoom, "The quality of the spheres to download.");
            // 
            // label2
            // 
            this.label2.AutoSize = true;
            this.label2.Location = new System.Drawing.Point(14, 304);
            this.label2.Name = "label2";
            this.label2.Size = new System.Drawing.Size(72, 13);
            this.label2.TabIndex = 4;
            this.label2.Text = "Image quality:";
            // 
            // downloadProgress
            // 
            this.downloadProgress.Location = new System.Drawing.Point(15, 373);
            this.downloadProgress.Name = "downloadProgress";
            this.downloadProgress.Size = new System.Drawing.Size(484, 23);
            this.downloadProgress.Step = 1;
            this.downloadProgress.TabIndex = 5;
            // 
            // followNeighbours
            // 
            this.followNeighbours.AutoSize = true;
            this.followNeighbours.CheckAlign = System.Drawing.ContentAlignment.MiddleRight;
            this.followNeighbours.Location = new System.Drawing.Point(22, 327);
            this.followNeighbours.Name = "followNeighbours";
            this.followNeighbours.Size = new System.Drawing.Size(83, 17);
            this.followNeighbours.TabIndex = 6;
            this.followNeighbours.Text = "Neighbours:";
            this.toolTip1.SetToolTip(this.followNeighbours, "If checked, neighbouring spheres will be downloaded for all provided URLs.");
            this.followNeighbours.UseVisualStyleBackColor = true;
            this.followNeighbours.CheckedChanged += new System.EventHandler(this.followNeighbours_CheckedChanged);
            // 
            // recurseNeighbours
            // 
            this.recurseNeighbours.AutoSize = true;
            this.recurseNeighbours.CheckAlign = System.Drawing.ContentAlignment.MiddleRight;
            this.recurseNeighbours.Enabled = false;
            this.recurseNeighbours.Location = new System.Drawing.Point(36, 350);
            this.recurseNeighbours.Name = "recurseNeighbours";
            this.recurseNeighbours.Size = new System.Drawing.Size(69, 17);
            this.recurseNeighbours.TabIndex = 7;
            this.recurseNeighbours.Text = "Recurse:";
            this.toolTip1.SetToolTip(this.recurseNeighbours, "If checked neighbours will recurse infinitely, for bulk image gathering!");
            this.recurseNeighbours.UseVisualStyleBackColor = true;
            // 
            // StreetviewGUI
            // 
            this.AutoScaleDimensions = new System.Drawing.SizeF(6F, 13F);
            this.AutoScaleMode = System.Windows.Forms.AutoScaleMode.Font;
            this.ClientSize = new System.Drawing.Size(515, 407);
            this.Controls.Add(this.recurseNeighbours);
            this.Controls.Add(this.followNeighbours);
            this.Controls.Add(this.downloadProgress);
            this.Controls.Add(this.label2);
            this.Controls.Add(this.streetviewZoom);
            this.Controls.Add(this.streetviewURL);
            this.Controls.Add(this.label1);
            this.Controls.Add(this.downloadStreetview);
            this.FormBorderStyle = System.Windows.Forms.FormBorderStyle.FixedToolWindow;
            this.MaximizeBox = false;
            this.MinimizeBox = false;
            this.Name = "StreetviewGUI";
            this.StartPosition = System.Windows.Forms.FormStartPosition.CenterScreen;
            this.Text = "Streetview Ripper";
            this.ResumeLayout(false);
            this.PerformLayout();

        }

        #endregion

        private System.Windows.Forms.Button downloadStreetview;
        private System.Windows.Forms.Label label1;
        private System.Windows.Forms.TextBox streetviewURL;
        private System.Windows.Forms.ComboBox streetviewZoom;
        private System.Windows.Forms.Label label2;
        private System.Windows.Forms.ProgressBar downloadProgress;
        private System.Windows.Forms.CheckBox followNeighbours;
        private System.Windows.Forms.ToolTip toolTip1;
        private System.Windows.Forms.CheckBox recurseNeighbours;
    }
}

