namespace hyoteiten
{
    partial class Form1
    {
        /// <summary>
        /// 必要なデザイナー変数です。
        /// </summary>
        private System.ComponentModel.IContainer components = null;

        /// <summary>
        /// 使用中のリソースをすべてクリーンアップします。
        /// </summary>
        /// <param name="disposing">マネージド リソースを破棄する場合は true を指定し、その他の場合は false を指定します。</param>
        protected override void Dispose(bool disposing)
        {
            if (disposing && (components != null))
            {
                components.Dispose();
            }
            base.Dispose(disposing);
        }

        #region Windows フォーム デザイナーで生成されたコード

        /// <summary>
        /// デザイナー サポートに必要なメソッドです。このメソッドの内容を
        /// コード エディターで変更しないでください。
        /// </summary>
        private void InitializeComponent()
        {
            this.btnSelectFile = new System.Windows.Forms.Button();
            this.lblFilePath = new System.Windows.Forms.Label();
            this.comboZone = new System.Windows.Forms.ComboBox();
            this.numOuterPitch = new System.Windows.Forms.NumericUpDown();
            this.numInnerCoverage = new System.Windows.Forms.NumericUpDown();
            this.numToleranceM = new System.Windows.Forms.NumericUpDown();
            this.btnRun = new System.Windows.Forms.Button();
            this.label1 = new System.Windows.Forms.Label();
            this.label2 = new System.Windows.Forms.Label();
            this.label3 = new System.Windows.Forms.Label();
            this.label4 = new System.Windows.Forms.Label();
            ((System.ComponentModel.ISupportInitialize)(this.numOuterPitch)).BeginInit();
            ((System.ComponentModel.ISupportInitialize)(this.numInnerCoverage)).BeginInit();
            ((System.ComponentModel.ISupportInitialize)(this.numToleranceM)).BeginInit();
            this.SuspendLayout();
            // 
            // btnSelectFile
            // 
            this.btnSelectFile.Font = new System.Drawing.Font("MS UI Gothic", 12F, System.Drawing.FontStyle.Regular, System.Drawing.GraphicsUnit.Point, ((byte)(128)));
            this.btnSelectFile.Location = new System.Drawing.Point(12, 12);
            this.btnSelectFile.Name = "btnSelectFile";
            this.btnSelectFile.Size = new System.Drawing.Size(257, 44);
            this.btnSelectFile.TabIndex = 0;
            this.btnSelectFile.Text = "KML / KMZ ファイル選択";
            this.btnSelectFile.UseVisualStyleBackColor = true;
            this.btnSelectFile.Click += new System.EventHandler(this.btnSelectFile_Click);
            // 
            // lblFilePath
            // 
            this.lblFilePath.AutoSize = true;
            this.lblFilePath.Font = new System.Drawing.Font("MS UI Gothic", 12F, System.Drawing.FontStyle.Regular, System.Drawing.GraphicsUnit.Point, ((byte)(128)));
            this.lblFilePath.Location = new System.Drawing.Point(296, 24);
            this.lblFilePath.Name = "lblFilePath";
            this.lblFilePath.Size = new System.Drawing.Size(234, 20);
            this.lblFilePath.TabIndex = 1;
            this.lblFilePath.Text = "ファイルが選択されていません";
            this.lblFilePath.Click += new System.EventHandler(this.lblFilePath_Click);
            // 
            // comboZone
            // 
            this.comboZone.DropDownStyle = System.Windows.Forms.ComboBoxStyle.DropDownList;
            this.comboZone.Font = new System.Drawing.Font("MS UI Gothic", 12F, System.Drawing.FontStyle.Regular, System.Drawing.GraphicsUnit.Point, ((byte)(128)));
            this.comboZone.FormattingEnabled = true;
            this.comboZone.Items.AddRange(new object[] {
            "第 1 系",
            "第 2 系",
            "第 3 系",
            "第 4 系",
            "第 5 系",
            "第 6 系",
            "第 7 系",
            "第 8 系",
            "第 9 系",
            "第 10 系",
            "第 11 系",
            "第 12 系",
            "第 13 系",
            "第 14 系",
            "第 15 系",
            "第 16 系",
            "第 17 系",
            "第 18 系",
            "第 19 系"});
            this.comboZone.Location = new System.Drawing.Point(300, 76);
            this.comboZone.Name = "comboZone";
            this.comboZone.Size = new System.Drawing.Size(109, 28);
            this.comboZone.TabIndex = 2;
            this.comboZone.SelectedIndexChanged += new System.EventHandler(this.comboZone_SelectedIndexChanged);
            // 
            // numOuterPitch
            // 
            this.numOuterPitch.Font = new System.Drawing.Font("MS UI Gothic", 12F, System.Drawing.FontStyle.Regular, System.Drawing.GraphicsUnit.Point, ((byte)(128)));
            this.numOuterPitch.Location = new System.Drawing.Point(300, 145);
            this.numOuterPitch.Maximum = new decimal(new int[] {
            5000,
            0,
            0,
            0});
            this.numOuterPitch.Minimum = new decimal(new int[] {
            1,
            0,
            0,
            0});
            this.numOuterPitch.Name = "numOuterPitch";
            this.numOuterPitch.Size = new System.Drawing.Size(109, 27);
            this.numOuterPitch.TabIndex = 3;
            this.numOuterPitch.TextAlign = System.Windows.Forms.HorizontalAlignment.Center;
            this.numOuterPitch.Value = new decimal(new int[] {
            90,
            0,
            0,
            0});
            this.numOuterPitch.ValueChanged += new System.EventHandler(this.numOuterPitch_ValueChanged);
            // 
            // numInnerCoverage
            // 
            this.numInnerCoverage.Font = new System.Drawing.Font("MS UI Gothic", 12F, System.Drawing.FontStyle.Regular, System.Drawing.GraphicsUnit.Point, ((byte)(128)));
            this.numInnerCoverage.Location = new System.Drawing.Point(300, 180);
            this.numInnerCoverage.Maximum = new decimal(new int[] {
            5000,
            0,
            0,
            0});
            this.numInnerCoverage.Minimum = new decimal(new int[] {
            1,
            0,
            0,
            0});
            this.numInnerCoverage.Name = "numInnerCoverage";
            this.numInnerCoverage.Size = new System.Drawing.Size(109, 27);
            this.numInnerCoverage.TabIndex = 4;
            this.numInnerCoverage.TextAlign = System.Windows.Forms.HorizontalAlignment.Center;
            this.numInnerCoverage.Value = new decimal(new int[] {
            190,
            0,
            0,
            0});
            this.numInnerCoverage.ValueChanged += new System.EventHandler(this.numInnerCoverage_ValueChanged);
            // 
            // numToleranceM
            // 
            this.numToleranceM.Font = new System.Drawing.Font("MS UI Gothic", 12F, System.Drawing.FontStyle.Regular, System.Drawing.GraphicsUnit.Point, ((byte)(128)));
            this.numToleranceM.Location = new System.Drawing.Point(300, 109);
            this.numToleranceM.Maximum = new decimal(new int[] {
            1000,
            0,
            0,
            0});
            this.numToleranceM.Name = "numToleranceM";
            this.numToleranceM.Size = new System.Drawing.Size(109, 27);
            this.numToleranceM.TabIndex = 5;
            this.numToleranceM.TextAlign = System.Windows.Forms.HorizontalAlignment.Center;
            this.numToleranceM.Value = new decimal(new int[] {
            1,
            0,
            0,
            0});
            this.numToleranceM.ValueChanged += new System.EventHandler(this.numToleranceM_ValueChanged);
            // 
            // btnRun
            // 
            this.btnRun.Anchor = ((System.Windows.Forms.AnchorStyles)((System.Windows.Forms.AnchorStyles.Bottom | System.Windows.Forms.AnchorStyles.Right)));
            this.btnRun.Font = new System.Drawing.Font("MS UI Gothic", 12F, System.Drawing.FontStyle.Regular, System.Drawing.GraphicsUnit.Point, ((byte)(128)));
            this.btnRun.Location = new System.Drawing.Point(351, 250);
            this.btnRun.Name = "btnRun";
            this.btnRun.Size = new System.Drawing.Size(207, 45);
            this.btnRun.TabIndex = 6;
            this.btnRun.Text = "標識自動配置 ";
            this.btnRun.UseVisualStyleBackColor = true;
            this.btnRun.Click += new System.EventHandler(this.btnRun_Click);
            // 
            // label1
            // 
            this.label1.AutoSize = true;
            this.label1.Font = new System.Drawing.Font("MS UI Gothic", 12F, System.Drawing.FontStyle.Regular, System.Drawing.GraphicsUnit.Point, ((byte)(128)));
            this.label1.Location = new System.Drawing.Point(29, 79);
            this.label1.Name = "label1";
            this.label1.Size = new System.Drawing.Size(75, 20);
            this.label1.TabIndex = 7;
            this.label1.Text = "座標系 ";
            // 
            // label2
            // 
            this.label2.AutoSize = true;
            this.label2.Font = new System.Drawing.Font("MS UI Gothic", 12F, System.Drawing.FontStyle.Regular, System.Drawing.GraphicsUnit.Point, ((byte)(128)));
            this.label2.Location = new System.Drawing.Point(29, 152);
            this.label2.Name = "label2";
            this.label2.Size = new System.Drawing.Size(185, 20);
            this.label2.TabIndex = 8;
            this.label2.Text = "外部評定点ピッチ (m)";
            // 
            // label3
            // 
            this.label3.AutoSize = true;
            this.label3.Font = new System.Drawing.Font("MS UI Gothic", 12F, System.Drawing.FontStyle.Regular, System.Drawing.GraphicsUnit.Point, ((byte)(128)));
            this.label3.Location = new System.Drawing.Point(29, 187);
            this.label3.Name = "label3";
            this.label3.Size = new System.Drawing.Size(185, 20);
            this.label3.TabIndex = 9;
            this.label3.Text = "内部評定点ピッチ (m)";
            this.label3.Click += new System.EventHandler(this.label3_Click);
            // 
            // label4
            // 
            this.label4.AutoSize = true;
            this.label4.Font = new System.Drawing.Font("MS UI Gothic", 12F, System.Drawing.FontStyle.Regular, System.Drawing.GraphicsUnit.Point, ((byte)(128)));
            this.label4.Location = new System.Drawing.Point(29, 116);
            this.label4.Name = "label4";
            this.label4.Size = new System.Drawing.Size(249, 20);
            this.label4.TabIndex = 10;
            this.label4.Text = "頂点間引き許容離れ指定(m)";
            // 
            // Form1
            // 
            this.AutoScaleDimensions = new System.Drawing.SizeF(8F, 15F);
            this.AutoScaleMode = System.Windows.Forms.AutoScaleMode.Font;
            this.BackColor = System.Drawing.SystemColors.ActiveCaption;
            this.ClientSize = new System.Drawing.Size(582, 325);
            this.Controls.Add(this.label4);
            this.Controls.Add(this.label3);
            this.Controls.Add(this.label2);
            this.Controls.Add(this.label1);
            this.Controls.Add(this.btnRun);
            this.Controls.Add(this.numToleranceM);
            this.Controls.Add(this.numInnerCoverage);
            this.Controls.Add(this.numOuterPitch);
            this.Controls.Add(this.comboZone);
            this.Controls.Add(this.lblFilePath);
            this.Controls.Add(this.btnSelectFile);
            this.MaximumSize = new System.Drawing.Size(600, 372);
            this.MinimumSize = new System.Drawing.Size(600, 372);
            this.Name = "Form1";
            this.Text = "いきなり 評定点";
            ((System.ComponentModel.ISupportInitialize)(this.numOuterPitch)).EndInit();
            ((System.ComponentModel.ISupportInitialize)(this.numInnerCoverage)).EndInit();
            ((System.ComponentModel.ISupportInitialize)(this.numToleranceM)).EndInit();
            this.ResumeLayout(false);
            this.PerformLayout();

        }

        #endregion

        private System.Windows.Forms.Button btnSelectFile;
        private System.Windows.Forms.Label lblFilePath;
        private System.Windows.Forms.ComboBox comboZone;
        private System.Windows.Forms.NumericUpDown numOuterPitch;
        private System.Windows.Forms.NumericUpDown numInnerCoverage;
        private System.Windows.Forms.NumericUpDown numToleranceM;
        private System.Windows.Forms.Button btnRun;
        private System.Windows.Forms.Label label1;
        private System.Windows.Forms.Label label2;
        private System.Windows.Forms.Label label3;
        private System.Windows.Forms.Label label4;
    }
}

