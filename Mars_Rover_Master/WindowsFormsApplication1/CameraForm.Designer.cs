namespace WindowsFormsApplication1
{
    partial class CameraForm
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
            System.ComponentModel.ComponentResourceManager resources = new System.ComponentModel.ComponentResourceManager(typeof(CameraForm));
            this.AMC = new AxAXISMEDIACONTROLLib.AxAxisMediaControl();
            this.button1 = new System.Windows.Forms.Button();
            ((System.ComponentModel.ISupportInitialize)(this.AMC)).BeginInit();
            this.SuspendLayout();
            // 
            // AMC
            // 
            this.AMC.Enabled = true;
            this.AMC.Location = new System.Drawing.Point(12, 12);
            this.AMC.Name = "AMC";
            this.AMC.OcxState = ((System.Windows.Forms.AxHost.State)(resources.GetObject("AMC.OcxState")));
            this.AMC.Size = new System.Drawing.Size(430, 313);
            this.AMC.TabIndex = 0;
            // 
            // button1
            // 
            this.button1.Location = new System.Drawing.Point(26, 344);
            this.button1.Name = "button1";
            this.button1.Size = new System.Drawing.Size(75, 23);
            this.button1.TabIndex = 1;
            this.button1.Text = "button1";
            this.button1.UseVisualStyleBackColor = true;
            // 
            // CameraForm
            // 
            this.AutoScaleDimensions = new System.Drawing.SizeF(6F, 13F);
            this.AutoScaleMode = System.Windows.Forms.AutoScaleMode.Font;
            this.ClientSize = new System.Drawing.Size(454, 379);
            this.Controls.Add(this.button1);
            this.Controls.Add(this.AMC);
            this.Name = "CameraForm";
            this.Text = "Form1";
            this.Load += new System.EventHandler(this.CameraForm_Load);
            ((System.ComponentModel.ISupportInitialize)(this.AMC)).EndInit();
            this.ResumeLayout(false);

        }

        #endregion

        private AxAXISMEDIACONTROLLib.AxAxisMediaControl AMC;
        private System.Windows.Forms.Button button1;
    }
}

