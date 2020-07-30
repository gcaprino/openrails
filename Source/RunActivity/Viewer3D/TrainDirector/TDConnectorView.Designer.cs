namespace Orts.Viewer3D.TrainDirector
{
    partial class TDConnectorView
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
            this.LayoutBrowser = new System.Windows.Forms.WebBrowser();
            this.label1 = new System.Windows.Forms.Label();
            this.TDUrl = new System.Windows.Forms.TextBox();
            this.ConnectTDbutton = new System.Windows.Forms.Button();
            this.SuspendLayout();
            // 
            // LayoutBrowser
            // 
            this.LayoutBrowser.Anchor = ((System.Windows.Forms.AnchorStyles)(((System.Windows.Forms.AnchorStyles.Bottom | System.Windows.Forms.AnchorStyles.Left) 
            | System.Windows.Forms.AnchorStyles.Right)));
            this.LayoutBrowser.Location = new System.Drawing.Point(13, 71);
            this.LayoutBrowser.MinimumSize = new System.Drawing.Size(20, 20);
            this.LayoutBrowser.Name = "LayoutBrowser";
            this.LayoutBrowser.Size = new System.Drawing.Size(1012, 486);
            this.LayoutBrowser.TabIndex = 0;
            // 
            // label1
            // 
            this.label1.AutoSize = true;
            this.label1.Location = new System.Drawing.Point(13, 31);
            this.label1.Name = "label1";
            this.label1.Size = new System.Drawing.Size(130, 13);
            this.label1.TabIndex = 1;
            this.label1.Text = "Train Director Server URL";
            // 
            // TDUrl
            // 
            this.TDUrl.Location = new System.Drawing.Point(198, 23);
            this.TDUrl.Name = "TDUrl";
            this.TDUrl.Size = new System.Drawing.Size(422, 20);
            this.TDUrl.TabIndex = 2;
            this.TDUrl.TextChanged += new System.EventHandler(this.TDUrl_TextChanged);
            // 
            // ConnectTDbutton
            // 
            this.ConnectTDbutton.Location = new System.Drawing.Point(676, 19);
            this.ConnectTDbutton.Name = "ConnectTDbutton";
            this.ConnectTDbutton.Size = new System.Drawing.Size(75, 23);
            this.ConnectTDbutton.TabIndex = 3;
            this.ConnectTDbutton.Text = "Connect...";
            this.ConnectTDbutton.UseVisualStyleBackColor = true;
            this.ConnectTDbutton.Click += new System.EventHandler(this.ConnectTDbutton_Click);
            // 
            // TDConnectorView
            // 
            this.AutoScaleDimensions = new System.Drawing.SizeF(6F, 13F);
            this.AutoScaleMode = System.Windows.Forms.AutoScaleMode.Font;
            this.ClientSize = new System.Drawing.Size(1028, 569);
            this.Controls.Add(this.ConnectTDbutton);
            this.Controls.Add(this.TDUrl);
            this.Controls.Add(this.label1);
            this.Controls.Add(this.LayoutBrowser);
            this.Name = "TDConnectorView";
            this.Text = "TDConnectorView";
            this.Load += new System.EventHandler(this.TDConnectorView_Load);
            this.ResumeLayout(false);
            this.PerformLayout();

        }

        #endregion

        private System.Windows.Forms.WebBrowser LayoutBrowser;
        private System.Windows.Forms.Label label1;
        private System.Windows.Forms.TextBox TDUrl;
        private System.Windows.Forms.Button ConnectTDbutton;
    }
}