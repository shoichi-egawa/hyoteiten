namespace hyoteiten
{
    partial class PointEditForm
    {
        private System.ComponentModel.IContainer components = null;

        protected override void Dispose(bool disposing)
        {
            if (disposing && (components != null))
            {
                components.Dispose();
            }
            base.Dispose(disposing);
        }

        #region Windows Form Designer generated code

        private void InitializeComponent()
        {
            this.canvasPanel = new System.Windows.Forms.Panel();
            this.radMove = new System.Windows.Forms.RadioButton();
            this.radAddNH = new System.Windows.Forms.RadioButton();
            this.radAddKS = new System.Windows.Forms.RadioButton();
            this.btnDelete = new System.Windows.Forms.Button();
            this.btnOk = new System.Windows.Forms.Button();
            this.btnCancel = new System.Windows.Forms.Button();
            this.SuspendLayout();
            // 
            // canvasPanel
            // 
            this.canvasPanel.Anchor = ((System.Windows.Forms.AnchorStyles)((((System.Windows.Forms.AnchorStyles.Top | System.Windows.Forms.AnchorStyles.Bottom) 
            | System.Windows.Forms.AnchorStyles.Left) 
            | System.Windows.Forms.AnchorStyles.Right)));
            this.canvasPanel.BackColor = System.Drawing.Color.FromArgb(((int)(((byte)(30)))), ((int)(((byte)(30)))), ((int)(((byte)(30)))));
            this.canvasPanel.Location = new System.Drawing.Point(15, 105);
            this.canvasPanel.Name = "canvasPanel";
            this.canvasPanel.Size = new System.Drawing.Size(621, 402);
            this.canvasPanel.TabIndex = 0;
            // 
            // radMove
            // 
            this.radMove.AutoSize = true;
            this.radMove.Checked = true;
            this.radMove.Location = new System.Drawing.Point(12, 12);
            this.radMove.Name = "radMove";
            this.radMove.Size = new System.Drawing.Size(110, 16);
            this.radMove.TabIndex = 1;
            this.radMove.TabStop = true;
            this.radMove.Text = "選択・ドラッグ移動";
            this.radMove.UseVisualStyleBackColor = true;
            // 
            // radAddNH
            // 
            this.radAddNH.AutoSize = true;
            this.radAddNH.Location = new System.Drawing.Point(12, 34);
            this.radAddNH.Name = "radAddNH";
            this.radAddNH.Size = new System.Drawing.Size(107, 16);
            this.radAddNH.TabIndex = 2;
            this.radAddNH.Text = "内部点(NH)追加";
            this.radAddNH.UseVisualStyleBackColor = true;
            // 
            // radAddKS
            // 
            this.radAddKS.AutoSize = true;
            this.radAddKS.Location = new System.Drawing.Point(12, 56);
            this.radAddKS.Name = "radAddKS";
            this.radAddKS.Size = new System.Drawing.Size(105, 16);
            this.radAddKS.TabIndex = 3;
            this.radAddKS.Text = "検証点(KS)追加";
            this.radAddKS.UseVisualStyleBackColor = true;
            // 
            // btnDelete
            // 
            this.btnDelete.Anchor = ((System.Windows.Forms.AnchorStyles)((System.Windows.Forms.AnchorStyles.Top | System.Windows.Forms.AnchorStyles.Right)));
            this.btnDelete.Location = new System.Drawing.Point(456, 9);
            this.btnDelete.Name = "btnDelete";
            this.btnDelete.Size = new System.Drawing.Size(180, 23);
            this.btnDelete.TabIndex = 4;
            this.btnDelete.Text = "選択した点を削除";
            this.btnDelete.UseVisualStyleBackColor = true;
            // 
            // btnOk
            // 
            this.btnOk.Anchor = ((System.Windows.Forms.AnchorStyles)((System.Windows.Forms.AnchorStyles.Top | System.Windows.Forms.AnchorStyles.Right)));
            this.btnOk.Location = new System.Drawing.Point(456, 38);
            this.btnOk.Name = "btnOk";
            this.btnOk.Size = new System.Drawing.Size(180, 23);
            this.btnOk.TabIndex = 5;
            this.btnOk.Text = "編集完了(Google Earth確認)";
            this.btnOk.UseVisualStyleBackColor = true;
            // 
            // btnCancel
            // 
            this.btnCancel.Anchor = ((System.Windows.Forms.AnchorStyles)((System.Windows.Forms.AnchorStyles.Top | System.Windows.Forms.AnchorStyles.Right)));
            this.btnCancel.Location = new System.Drawing.Point(456, 67);
            this.btnCancel.Name = "btnCancel";
            this.btnCancel.Size = new System.Drawing.Size(180, 23);
            this.btnCancel.TabIndex = 6;
            this.btnCancel.Text = "キャンセル";
            this.btnCancel.UseVisualStyleBackColor = true;
            // 
            // PointEditForm
            // 
            this.AutoScaleDimensions = new System.Drawing.SizeF(6F, 12F);
            this.AutoScaleMode = System.Windows.Forms.AutoScaleMode.Font;
            this.BackColor = System.Drawing.SystemColors.ActiveCaption;
            this.ClientSize = new System.Drawing.Size(648, 524);
            this.Controls.Add(this.canvasPanel);
            this.Controls.Add(this.btnCancel);
            this.Controls.Add(this.btnOk);
            this.Controls.Add(this.btnDelete);
            this.Controls.Add(this.radAddKS);
            this.Controls.Add(this.radAddNH);
            this.Controls.Add(this.radMove);
            this.MinimumSize = new System.Drawing.Size(443, 398);
            this.Name = "PointEditForm";
            this.StartPosition = System.Windows.Forms.FormStartPosition.CenterParent;
            this.Text = "標識位置 簡易グラフィック編集 (ドラッグ移動・追加・削除)";
            this.ResumeLayout(false);
            this.PerformLayout();

        }

        #endregion

        private System.Windows.Forms.Panel canvasPanel;
        private System.Windows.Forms.RadioButton radMove;
        private System.Windows.Forms.RadioButton radAddNH;
        private System.Windows.Forms.RadioButton radAddKS;
        private System.Windows.Forms.Button btnDelete;
        private System.Windows.Forms.Button btnOk;
        private System.Windows.Forms.Button btnCancel;
    }
}