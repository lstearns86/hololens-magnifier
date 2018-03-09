namespace MagnificationServer
{
    partial class Form1
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
            this.Display = new System.Windows.Forms.PictureBox();
            this.label1 = new System.Windows.Forms.Label();
            this.InfoLabel = new System.Windows.Forms.Label();
            this.PrescaleSlider = new System.Windows.Forms.TrackBar();
            this.label2 = new System.Windows.Forms.Label();
            this.label3 = new System.Windows.Forms.Label();
            this.RateSlider = new System.Windows.Forms.TrackBar();
            this.label4 = new System.Windows.Forms.Label();
            this.SizeSlider = new System.Windows.Forms.TrackBar();
            this.label5 = new System.Windows.Forms.Label();
            this.ZoomSlider = new System.Windows.Forms.TrackBar();
            this.ServerDisplay = new System.Windows.Forms.PictureBox();
            this.ResetPositionButton = new System.Windows.Forms.Button();
            this.label6 = new System.Windows.Forms.Label();
            this.EnableThresholdCheckbox = new System.Windows.Forms.CheckBox();
            this.InvertCheckbox = new System.Windows.Forms.CheckBox();
            this.groupBox3 = new System.Windows.Forms.GroupBox();
            this.OtsuThresholdButton = new System.Windows.Forms.RadioButton();
            this.AdaptiveThresholdButton = new System.Windows.Forms.RadioButton();
            this.label7 = new System.Windows.Forms.Label();
            this.ExposureSlider = new System.Windows.Forms.TrackBar();
            this.EnableMenuCheckbox = new System.Windows.Forms.CheckBox();
            this.ShowMenuButton = new System.Windows.Forms.Button();
            this.HideMenuButton = new System.Windows.Forms.Button();
            this.DesignChooser = new System.Windows.Forms.ComboBox();
            this.DragModeChooser = new System.Windows.Forms.ComboBox();
            this.EnableCameraCheckbox = new System.Windows.Forms.CheckBox();
            this.FixLagButton = new System.Windows.Forms.Button();
            this.VerboseLoggingCheckbox = new System.Windows.Forms.CheckBox();
            this.ResetCameraButton = new System.Windows.Forms.Button();
            this.label8 = new System.Windows.Forms.Label();
            this.ClientStreamingRateSlider = new System.Windows.Forms.TrackBar();
            this.label9 = new System.Windows.Forms.Label();
            this.DistanceSlider = new System.Windows.Forms.TrackBar();
            this.ShowHololensPlaceholderTextCheckbox = new System.Windows.Forms.CheckBox();
            this.ValuesTooltip = new System.Windows.Forms.ToolTip(this.components);
            ((System.ComponentModel.ISupportInitialize)(this.Display)).BeginInit();
            ((System.ComponentModel.ISupportInitialize)(this.PrescaleSlider)).BeginInit();
            ((System.ComponentModel.ISupportInitialize)(this.RateSlider)).BeginInit();
            ((System.ComponentModel.ISupportInitialize)(this.SizeSlider)).BeginInit();
            ((System.ComponentModel.ISupportInitialize)(this.ZoomSlider)).BeginInit();
            ((System.ComponentModel.ISupportInitialize)(this.ServerDisplay)).BeginInit();
            this.groupBox3.SuspendLayout();
            ((System.ComponentModel.ISupportInitialize)(this.ExposureSlider)).BeginInit();
            ((System.ComponentModel.ISupportInitialize)(this.ClientStreamingRateSlider)).BeginInit();
            ((System.ComponentModel.ISupportInitialize)(this.DistanceSlider)).BeginInit();
            this.SuspendLayout();
            // 
            // Display
            // 
            this.Display.Location = new System.Drawing.Point(6, 6);
            this.Display.Margin = new System.Windows.Forms.Padding(2);
            this.Display.Name = "Display";
            this.Display.Size = new System.Drawing.Size(482, 482);
            this.Display.SizeMode = System.Windows.Forms.PictureBoxSizeMode.Zoom;
            this.Display.TabIndex = 0;
            this.Display.TabStop = false;
            // 
            // label1
            // 
            this.label1.AutoSize = true;
            this.label1.Location = new System.Drawing.Point(790, 498);
            this.label1.Name = "label1";
            this.label1.Size = new System.Drawing.Size(88, 13);
            this.label1.TabIndex = 2;
            this.label1.Text = "Interface Design:";
            // 
            // InfoLabel
            // 
            this.InfoLabel.Location = new System.Drawing.Point(227, 556);
            this.InfoLabel.Name = "InfoLabel";
            this.InfoLabel.Size = new System.Drawing.Size(261, 25);
            this.InfoLabel.TabIndex = 6;
            this.InfoLabel.Text = "0 fps";
            this.InfoLabel.TextAlign = System.Drawing.ContentAlignment.BottomRight;
            // 
            // PrescaleSlider
            // 
            this.PrescaleSlider.Location = new System.Drawing.Point(59, 536);
            this.PrescaleSlider.Maximum = 100;
            this.PrescaleSlider.Minimum = 1;
            this.PrescaleSlider.Name = "PrescaleSlider";
            this.PrescaleSlider.Size = new System.Drawing.Size(162, 45);
            this.PrescaleSlider.TabIndex = 7;
            this.PrescaleSlider.TickFrequency = 10;
            this.PrescaleSlider.Value = 1;
            this.PrescaleSlider.Scroll += new System.EventHandler(this.PrescaleSlider_Scroll);
            // 
            // label2
            // 
            this.label2.AutoSize = true;
            this.label2.Location = new System.Drawing.Point(2, 539);
            this.label2.Name = "label2";
            this.label2.Size = new System.Drawing.Size(51, 13);
            this.label2.TabIndex = 8;
            this.label2.Text = "Prescale:";
            // 
            // label3
            // 
            this.label3.AutoSize = true;
            this.label3.Location = new System.Drawing.Point(2, 583);
            this.label3.Name = "label3";
            this.label3.Size = new System.Drawing.Size(33, 13);
            this.label3.TabIndex = 10;
            this.label3.Text = "Rate:";
            // 
            // RateSlider
            // 
            this.RateSlider.Location = new System.Drawing.Point(59, 580);
            this.RateSlider.Maximum = 100;
            this.RateSlider.Minimum = 1;
            this.RateSlider.Name = "RateSlider";
            this.RateSlider.Size = new System.Drawing.Size(162, 45);
            this.RateSlider.TabIndex = 9;
            this.RateSlider.TickFrequency = 10;
            this.RateSlider.Value = 1;
            this.RateSlider.Scroll += new System.EventHandler(this.RateSlider_Scroll);
            // 
            // label4
            // 
            this.label4.AutoSize = true;
            this.label4.Location = new System.Drawing.Point(565, 498);
            this.label4.Name = "label4";
            this.label4.Size = new System.Drawing.Size(30, 13);
            this.label4.TabIndex = 12;
            this.label4.Text = "Size:";
            // 
            // SizeSlider
            // 
            this.SizeSlider.Location = new System.Drawing.Point(622, 495);
            this.SizeSlider.Maximum = 90;
            this.SizeSlider.Name = "SizeSlider";
            this.SizeSlider.Size = new System.Drawing.Size(162, 45);
            this.SizeSlider.TabIndex = 11;
            this.SizeSlider.TickFrequency = 10;
            this.SizeSlider.Scroll += new System.EventHandler(this.SizeSlider_Scroll);
            // 
            // label5
            // 
            this.label5.AutoSize = true;
            this.label5.Location = new System.Drawing.Point(565, 539);
            this.label5.Name = "label5";
            this.label5.Size = new System.Drawing.Size(37, 13);
            this.label5.TabIndex = 14;
            this.label5.Text = "Zoom:";
            // 
            // ZoomSlider
            // 
            this.ZoomSlider.Location = new System.Drawing.Point(622, 536);
            this.ZoomSlider.Maximum = 100;
            this.ZoomSlider.Minimum = 25;
            this.ZoomSlider.Name = "ZoomSlider";
            this.ZoomSlider.Size = new System.Drawing.Size(162, 45);
            this.ZoomSlider.TabIndex = 13;
            this.ZoomSlider.TickFrequency = 5;
            this.ZoomSlider.Value = 25;
            this.ZoomSlider.Scroll += new System.EventHandler(this.ZoomSlider_Scroll);
            // 
            // ServerDisplay
            // 
            this.ServerDisplay.Location = new System.Drawing.Point(492, 6);
            this.ServerDisplay.Margin = new System.Windows.Forms.Padding(2);
            this.ServerDisplay.Name = "ServerDisplay";
            this.ServerDisplay.Size = new System.Drawing.Size(663, 482);
            this.ServerDisplay.SizeMode = System.Windows.Forms.PictureBoxSizeMode.Zoom;
            this.ServerDisplay.TabIndex = 15;
            this.ServerDisplay.TabStop = false;
            this.ServerDisplay.Paint += new System.Windows.Forms.PaintEventHandler(this.ServerDisplay_Paint);
            this.ServerDisplay.MouseDown += new System.Windows.Forms.MouseEventHandler(this.ServerDisplay_MouseDown);
            this.ServerDisplay.MouseEnter += new System.EventHandler(this.ServerDisplay_MouseEnter);
            this.ServerDisplay.MouseLeave += new System.EventHandler(this.ServerDisplay_MouseLeave);
            this.ServerDisplay.MouseMove += new System.Windows.Forms.MouseEventHandler(this.ServerDisplay_MouseMove);
            this.ServerDisplay.MouseUp += new System.Windows.Forms.MouseEventHandler(this.ServerDisplay_MouseUp);
            // 
            // ResetPositionButton
            // 
            this.ResetPositionButton.Location = new System.Drawing.Point(794, 566);
            this.ResetPositionButton.Name = "ResetPositionButton";
            this.ResetPositionButton.Size = new System.Drawing.Size(151, 36);
            this.ResetPositionButton.TabIndex = 17;
            this.ResetPositionButton.Text = "Reset Position";
            this.ResetPositionButton.UseVisualStyleBackColor = true;
            this.ResetPositionButton.Click += new System.EventHandler(this.ResetPositionButton_Click);
            // 
            // label6
            // 
            this.label6.AutoSize = true;
            this.label6.Location = new System.Drawing.Point(791, 530);
            this.label6.Name = "label6";
            this.label6.Size = new System.Drawing.Size(83, 13);
            this.label6.TabIndex = 19;
            this.label6.Text = "Dragging Mode:";
            // 
            // EnableThresholdCheckbox
            // 
            this.EnableThresholdCheckbox.AutoSize = true;
            this.EnableThresholdCheckbox.Location = new System.Drawing.Point(274, 502);
            this.EnableThresholdCheckbox.Name = "EnableThresholdCheckbox";
            this.EnableThresholdCheckbox.Size = new System.Drawing.Size(73, 17);
            this.EnableThresholdCheckbox.TabIndex = 21;
            this.EnableThresholdCheckbox.Text = "Threshold";
            this.EnableThresholdCheckbox.UseVisualStyleBackColor = true;
            this.EnableThresholdCheckbox.CheckedChanged += new System.EventHandler(this.EnableThresholdCheckbox_CheckedChanged);
            // 
            // InvertCheckbox
            // 
            this.InvertCheckbox.AutoSize = true;
            this.InvertCheckbox.Location = new System.Drawing.Point(274, 525);
            this.InvertCheckbox.Name = "InvertCheckbox";
            this.InvertCheckbox.Size = new System.Drawing.Size(53, 17);
            this.InvertCheckbox.TabIndex = 23;
            this.InvertCheckbox.Text = "Invert";
            this.InvertCheckbox.UseVisualStyleBackColor = true;
            this.InvertCheckbox.CheckedChanged += new System.EventHandler(this.InvertCheckbox_CheckedChanged);
            // 
            // groupBox3
            // 
            this.groupBox3.Controls.Add(this.OtsuThresholdButton);
            this.groupBox3.Controls.Add(this.AdaptiveThresholdButton);
            this.groupBox3.Location = new System.Drawing.Point(353, 493);
            this.groupBox3.Name = "groupBox3";
            this.groupBox3.Size = new System.Drawing.Size(135, 29);
            this.groupBox3.TabIndex = 21;
            this.groupBox3.TabStop = false;
            // 
            // OtsuThresholdButton
            // 
            this.OtsuThresholdButton.AutoSize = true;
            this.OtsuThresholdButton.Checked = true;
            this.OtsuThresholdButton.Location = new System.Drawing.Point(6, 8);
            this.OtsuThresholdButton.Name = "OtsuThresholdButton";
            this.OtsuThresholdButton.Size = new System.Drawing.Size(47, 17);
            this.OtsuThresholdButton.TabIndex = 1;
            this.OtsuThresholdButton.TabStop = true;
            this.OtsuThresholdButton.Text = "Otsu";
            this.OtsuThresholdButton.UseVisualStyleBackColor = true;
            this.OtsuThresholdButton.CheckedChanged += new System.EventHandler(this.OtsuThresholdButton_CheckedChanged);
            // 
            // AdaptiveThresholdButton
            // 
            this.AdaptiveThresholdButton.AutoSize = true;
            this.AdaptiveThresholdButton.Location = new System.Drawing.Point(61, 8);
            this.AdaptiveThresholdButton.Name = "AdaptiveThresholdButton";
            this.AdaptiveThresholdButton.Size = new System.Drawing.Size(67, 17);
            this.AdaptiveThresholdButton.TabIndex = 3;
            this.AdaptiveThresholdButton.TabStop = true;
            this.AdaptiveThresholdButton.Text = "Adaptive";
            this.AdaptiveThresholdButton.UseVisualStyleBackColor = true;
            this.AdaptiveThresholdButton.CheckedChanged += new System.EventHandler(this.AdaptiveThresholdButton_CheckedChanged);
            // 
            // label7
            // 
            this.label7.AutoSize = true;
            this.label7.Location = new System.Drawing.Point(3, 499);
            this.label7.Name = "label7";
            this.label7.Size = new System.Drawing.Size(51, 13);
            this.label7.TabIndex = 25;
            this.label7.Text = "Exposure";
            // 
            // ExposureSlider
            // 
            this.ExposureSlider.Location = new System.Drawing.Point(59, 496);
            this.ExposureSlider.Maximum = 255;
            this.ExposureSlider.Minimum = 1;
            this.ExposureSlider.Name = "ExposureSlider";
            this.ExposureSlider.Size = new System.Drawing.Size(162, 45);
            this.ExposureSlider.TabIndex = 24;
            this.ExposureSlider.TickFrequency = 32;
            this.ExposureSlider.Value = 1;
            this.ExposureSlider.Scroll += new System.EventHandler(this.ExposureSlider_Scroll);
            // 
            // EnableMenuCheckbox
            // 
            this.EnableMenuCheckbox.AutoSize = true;
            this.EnableMenuCheckbox.Location = new System.Drawing.Point(1036, 519);
            this.EnableMenuCheckbox.Name = "EnableMenuCheckbox";
            this.EnableMenuCheckbox.Size = new System.Drawing.Size(89, 17);
            this.EnableMenuCheckbox.TabIndex = 26;
            this.EnableMenuCheckbox.Text = "Enable Menu";
            this.EnableMenuCheckbox.UseVisualStyleBackColor = true;
            this.EnableMenuCheckbox.CheckedChanged += new System.EventHandler(this.EnableMenuCheckbox_CheckedChanged);
            // 
            // ShowMenuButton
            // 
            this.ShowMenuButton.Location = new System.Drawing.Point(1036, 542);
            this.ShowMenuButton.Name = "ShowMenuButton";
            this.ShowMenuButton.Size = new System.Drawing.Size(43, 23);
            this.ShowMenuButton.TabIndex = 27;
            this.ShowMenuButton.Text = "Show";
            this.ShowMenuButton.UseVisualStyleBackColor = true;
            this.ShowMenuButton.Click += new System.EventHandler(this.ShowMenuButton_Click);
            // 
            // HideMenuButton
            // 
            this.HideMenuButton.Location = new System.Drawing.Point(1090, 542);
            this.HideMenuButton.Name = "HideMenuButton";
            this.HideMenuButton.Size = new System.Drawing.Size(43, 23);
            this.HideMenuButton.TabIndex = 28;
            this.HideMenuButton.Text = "Hide";
            this.HideMenuButton.UseVisualStyleBackColor = true;
            this.HideMenuButton.Click += new System.EventHandler(this.HideMenuButton_Click);
            // 
            // DesignChooser
            // 
            this.DesignChooser.DropDownStyle = System.Windows.Forms.ComboBoxStyle.DropDownList;
            this.DesignChooser.FormattingEnabled = true;
            this.DesignChooser.Items.AddRange(new object[] {
            "Disabled",
            "1: Heads-up Display",
            "2: Virtual Monitor",
            "3: Tabletop Display",
            "4: Magnifying Glass"});
            this.DesignChooser.Location = new System.Drawing.Point(884, 495);
            this.DesignChooser.Name = "DesignChooser";
            this.DesignChooser.Size = new System.Drawing.Size(126, 21);
            this.DesignChooser.TabIndex = 29;
            this.DesignChooser.SelectedIndexChanged += new System.EventHandler(this.DesignChooser_SelectedIndexChanged);
            // 
            // DragModeChooser
            // 
            this.DragModeChooser.DropDownStyle = System.Windows.Forms.ComboBoxStyle.DropDownList;
            this.DragModeChooser.FormattingEnabled = true;
            this.DragModeChooser.Items.AddRange(new object[] {
            "Disable",
            "Move",
            "Size",
            "Zoom"});
            this.DragModeChooser.Location = new System.Drawing.Point(884, 527);
            this.DragModeChooser.Name = "DragModeChooser";
            this.DragModeChooser.Size = new System.Drawing.Size(126, 21);
            this.DragModeChooser.TabIndex = 30;
            this.DragModeChooser.SelectedIndexChanged += new System.EventHandler(this.DragModeChooser_SelectedIndexChanged);
            // 
            // EnableCameraCheckbox
            // 
            this.EnableCameraCheckbox.AutoSize = true;
            this.EnableCameraCheckbox.Location = new System.Drawing.Point(1036, 496);
            this.EnableCameraCheckbox.Name = "EnableCameraCheckbox";
            this.EnableCameraCheckbox.Size = new System.Drawing.Size(98, 17);
            this.EnableCameraCheckbox.TabIndex = 31;
            this.EnableCameraCheckbox.Text = "Enable Camera";
            this.EnableCameraCheckbox.UseVisualStyleBackColor = true;
            this.EnableCameraCheckbox.CheckedChanged += new System.EventHandler(this.EnableCameraCheckbox_CheckedChanged);
            // 
            // FixLagButton
            // 
            this.FixLagButton.Location = new System.Drawing.Point(413, 529);
            this.FixLagButton.Name = "FixLagButton";
            this.FixLagButton.Size = new System.Drawing.Size(75, 23);
            this.FixLagButton.TabIndex = 32;
            this.FixLagButton.Text = "Fix Lag";
            this.FixLagButton.UseVisualStyleBackColor = true;
            this.FixLagButton.Click += new System.EventHandler(this.FixLagButton_Click);
            // 
            // VerboseLoggingCheckbox
            // 
            this.VerboseLoggingCheckbox.AutoSize = true;
            this.VerboseLoggingCheckbox.Location = new System.Drawing.Point(1036, 597);
            this.VerboseLoggingCheckbox.Name = "VerboseLoggingCheckbox";
            this.VerboseLoggingCheckbox.Size = new System.Drawing.Size(106, 17);
            this.VerboseLoggingCheckbox.TabIndex = 33;
            this.VerboseLoggingCheckbox.Text = "Verbose Logging";
            this.VerboseLoggingCheckbox.UseVisualStyleBackColor = true;
            this.VerboseLoggingCheckbox.CheckedChanged += new System.EventHandler(this.VerboseLoggingCheckbox_CheckedChanged);
            // 
            // ResetCameraButton
            // 
            this.ResetCameraButton.Location = new System.Drawing.Point(951, 566);
            this.ResetCameraButton.Name = "ResetCameraButton";
            this.ResetCameraButton.Size = new System.Drawing.Size(59, 36);
            this.ResetCameraButton.TabIndex = 34;
            this.ResetCameraButton.Text = "Reset Camera";
            this.ResetCameraButton.UseVisualStyleBackColor = true;
            this.ResetCameraButton.Click += new System.EventHandler(this.ResetCameraButton_Click);
            // 
            // label8
            // 
            this.label8.AutoSize = true;
            this.label8.Location = new System.Drawing.Point(269, 589);
            this.label8.Name = "label8";
            this.label8.Size = new System.Drawing.Size(33, 13);
            this.label8.TabIndex = 36;
            this.label8.Text = "Rate:";
            // 
            // ClientStreamingRateSlider
            // 
            this.ClientStreamingRateSlider.Location = new System.Drawing.Point(326, 586);
            this.ClientStreamingRateSlider.Maximum = 20;
            this.ClientStreamingRateSlider.Minimum = 1;
            this.ClientStreamingRateSlider.Name = "ClientStreamingRateSlider";
            this.ClientStreamingRateSlider.Size = new System.Drawing.Size(162, 45);
            this.ClientStreamingRateSlider.TabIndex = 35;
            this.ClientStreamingRateSlider.Value = 5;
            this.ClientStreamingRateSlider.Scroll += new System.EventHandler(this.ClientStreamingRateSlider_Scroll);
            // 
            // label9
            // 
            this.label9.AutoSize = true;
            this.label9.Location = new System.Drawing.Point(537, 586);
            this.label9.Name = "label9";
            this.label9.Size = new System.Drawing.Size(79, 13);
            this.label9.TabIndex = 38;
            this.label9.Text = "HUD Distance:";
            // 
            // DistanceSlider
            // 
            this.DistanceSlider.Location = new System.Drawing.Point(622, 583);
            this.DistanceSlider.Maximum = 100;
            this.DistanceSlider.Name = "DistanceSlider";
            this.DistanceSlider.Size = new System.Drawing.Size(162, 45);
            this.DistanceSlider.TabIndex = 37;
            this.DistanceSlider.TickFrequency = 10;
            this.DistanceSlider.Value = 50;
            this.DistanceSlider.Scroll += new System.EventHandler(this.DistanceSlider_Scroll);
            // 
            // ShowHololensPlaceholderTextCheckbox
            // 
            this.ShowHololensPlaceholderTextCheckbox.AutoSize = true;
            this.ShowHololensPlaceholderTextCheckbox.Location = new System.Drawing.Point(1036, 574);
            this.ShowHololensPlaceholderTextCheckbox.Name = "ShowHololensPlaceholderTextCheckbox";
            this.ShowHololensPlaceholderTextCheckbox.Size = new System.Drawing.Size(126, 17);
            this.ShowHololensPlaceholderTextCheckbox.TabIndex = 39;
            this.ShowHololensPlaceholderTextCheckbox.Text = "Hololens Placeholder";
            this.ShowHololensPlaceholderTextCheckbox.UseVisualStyleBackColor = true;
            this.ShowHololensPlaceholderTextCheckbox.CheckedChanged += new System.EventHandler(this.ShowHololensPlaceholderTextCheckbox_CheckedChanged);
            // 
            // Form1
            // 
            this.AutoScaleDimensions = new System.Drawing.SizeF(6F, 13F);
            this.AutoScaleMode = System.Windows.Forms.AutoScaleMode.Font;
            this.ClientSize = new System.Drawing.Size(1162, 626);
            this.Controls.Add(this.ShowHololensPlaceholderTextCheckbox);
            this.Controls.Add(this.label9);
            this.Controls.Add(this.DistanceSlider);
            this.Controls.Add(this.label8);
            this.Controls.Add(this.ClientStreamingRateSlider);
            this.Controls.Add(this.ResetCameraButton);
            this.Controls.Add(this.VerboseLoggingCheckbox);
            this.Controls.Add(this.FixLagButton);
            this.Controls.Add(this.EnableCameraCheckbox);
            this.Controls.Add(this.DragModeChooser);
            this.Controls.Add(this.DesignChooser);
            this.Controls.Add(this.HideMenuButton);
            this.Controls.Add(this.ShowMenuButton);
            this.Controls.Add(this.EnableMenuCheckbox);
            this.Controls.Add(this.label7);
            this.Controls.Add(this.groupBox3);
            this.Controls.Add(this.InvertCheckbox);
            this.Controls.Add(this.EnableThresholdCheckbox);
            this.Controls.Add(this.label6);
            this.Controls.Add(this.ResetPositionButton);
            this.Controls.Add(this.ServerDisplay);
            this.Controls.Add(this.label5);
            this.Controls.Add(this.ZoomSlider);
            this.Controls.Add(this.label4);
            this.Controls.Add(this.SizeSlider);
            this.Controls.Add(this.label3);
            this.Controls.Add(this.RateSlider);
            this.Controls.Add(this.label2);
            this.Controls.Add(this.PrescaleSlider);
            this.Controls.Add(this.InfoLabel);
            this.Controls.Add(this.label1);
            this.Controls.Add(this.Display);
            this.Controls.Add(this.ExposureSlider);
            this.Margin = new System.Windows.Forms.Padding(2);
            this.Name = "Form1";
            this.Text = "Magnification Server";
            this.FormClosing += new System.Windows.Forms.FormClosingEventHandler(this.Form1_FormClosing);
            ((System.ComponentModel.ISupportInitialize)(this.Display)).EndInit();
            ((System.ComponentModel.ISupportInitialize)(this.PrescaleSlider)).EndInit();
            ((System.ComponentModel.ISupportInitialize)(this.RateSlider)).EndInit();
            ((System.ComponentModel.ISupportInitialize)(this.SizeSlider)).EndInit();
            ((System.ComponentModel.ISupportInitialize)(this.ZoomSlider)).EndInit();
            ((System.ComponentModel.ISupportInitialize)(this.ServerDisplay)).EndInit();
            this.groupBox3.ResumeLayout(false);
            this.groupBox3.PerformLayout();
            ((System.ComponentModel.ISupportInitialize)(this.ExposureSlider)).EndInit();
            ((System.ComponentModel.ISupportInitialize)(this.ClientStreamingRateSlider)).EndInit();
            ((System.ComponentModel.ISupportInitialize)(this.DistanceSlider)).EndInit();
            this.ResumeLayout(false);
            this.PerformLayout();

        }

        #endregion

        private System.Windows.Forms.PictureBox Display;
        private System.Windows.Forms.Label label1;
        private System.Windows.Forms.Label InfoLabel;
        private System.Windows.Forms.TrackBar PrescaleSlider;
        private System.Windows.Forms.Label label2;
        private System.Windows.Forms.Label label3;
        private System.Windows.Forms.TrackBar RateSlider;
        private System.Windows.Forms.Label label4;
        private System.Windows.Forms.TrackBar SizeSlider;
        private System.Windows.Forms.Label label5;
        private System.Windows.Forms.TrackBar ZoomSlider;
        private System.Windows.Forms.PictureBox ServerDisplay;
        private System.Windows.Forms.Button ResetPositionButton;
        private System.Windows.Forms.Label label6;
        private System.Windows.Forms.CheckBox EnableThresholdCheckbox;
        private System.Windows.Forms.CheckBox InvertCheckbox;
        private System.Windows.Forms.GroupBox groupBox3;
        private System.Windows.Forms.RadioButton OtsuThresholdButton;
        private System.Windows.Forms.RadioButton AdaptiveThresholdButton;
        private System.Windows.Forms.Label label7;
        private System.Windows.Forms.TrackBar ExposureSlider;
        private System.Windows.Forms.CheckBox EnableMenuCheckbox;
        private System.Windows.Forms.Button ShowMenuButton;
        private System.Windows.Forms.Button HideMenuButton;
        private System.Windows.Forms.ComboBox DesignChooser;
        private System.Windows.Forms.ComboBox DragModeChooser;
        private System.Windows.Forms.CheckBox EnableCameraCheckbox;
        private System.Windows.Forms.Button FixLagButton;
        private System.Windows.Forms.CheckBox VerboseLoggingCheckbox;
        private System.Windows.Forms.Button ResetCameraButton;
        private System.Windows.Forms.Label label8;
        private System.Windows.Forms.TrackBar ClientStreamingRateSlider;
        private System.Windows.Forms.Label label9;
        private System.Windows.Forms.TrackBar DistanceSlider;
        private System.Windows.Forms.CheckBox ShowHololensPlaceholderTextCheckbox;
        private System.Windows.Forms.ToolTip ValuesTooltip;
    }
}

