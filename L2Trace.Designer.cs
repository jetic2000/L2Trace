namespace L2Trace
{
    partial class L2Trace
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
            this.backgroundWorker1 = new System.ComponentModel.BackgroundWorker();
            this.netAdapterComboBox = new System.Windows.Forms.ComboBox();
            this.toolTip1 = new System.Windows.Forms.ToolTip(this.components);
            this.buttonRun = new System.Windows.Forms.Button();
            this.logTextBox = new System.Windows.Forms.RichTextBox();
            this.tabControlSession = new System.Windows.Forms.TabControl();
            this.menuStrip1 = new System.Windows.Forms.MenuStrip();
            this.buttonBlockList = new System.Windows.Forms.Button();
            this.SuspendLayout();
            // 
            // netAdapterComboBox
            // 
            this.netAdapterComboBox.FormattingEnabled = true;
            this.netAdapterComboBox.Location = new System.Drawing.Point(587, 12);
            this.netAdapterComboBox.Name = "netAdapterComboBox";
            this.netAdapterComboBox.Size = new System.Drawing.Size(279, 20);
            this.netAdapterComboBox.TabIndex = 1;
            this.netAdapterComboBox.SelectedIndexChanged += new System.EventHandler(this.netAdapterComboBox_SelectedIndexChanged);
            // 
            // buttonRun
            // 
            this.buttonRun.Location = new System.Drawing.Point(587, 51);
            this.buttonRun.Name = "buttonRun";
            this.buttonRun.Size = new System.Drawing.Size(68, 25);
            this.buttonRun.TabIndex = 2;
            this.buttonRun.Text = "点击运行";
            this.buttonRun.UseVisualStyleBackColor = true;
            this.buttonRun.Click += new System.EventHandler(this.buttonRun_Click);
            // 
            // logTextBox
            // 
            this.logTextBox.Location = new System.Drawing.Point(587, 97);
            this.logTextBox.Name = "logTextBox";
            this.logTextBox.ReadOnly = true;
            this.logTextBox.ScrollBars = System.Windows.Forms.RichTextBoxScrollBars.Vertical;
            this.logTextBox.Size = new System.Drawing.Size(279, 376);
            this.logTextBox.TabIndex = 3;
            this.logTextBox.Text = "";
            this.logTextBox.TextChanged += new System.EventHandler(this.logTextBox_TextChanged);
            // 
            // tabControlSession
            // 
            this.tabControlSession.Location = new System.Drawing.Point(12, 12);
            this.tabControlSession.Name = "tabControlSession";
            this.tabControlSession.SelectedIndex = 0;
            this.tabControlSession.Size = new System.Drawing.Size(552, 461);
            this.tabControlSession.TabIndex = 5;
            // 
            // menuStrip1
            // 
            this.menuStrip1.Location = new System.Drawing.Point(0, 0);
            this.menuStrip1.Name = "menuStrip1";
            this.menuStrip1.Size = new System.Drawing.Size(878, 24);
            this.menuStrip1.TabIndex = 6;
            this.menuStrip1.Text = "menuStrip1";
            // 
            // buttonBlockList
            // 
            this.buttonBlockList.Location = new System.Drawing.Point(685, 51);
            this.buttonBlockList.Name = "buttonBlockList";
            this.buttonBlockList.Size = new System.Drawing.Size(108, 25);
            this.buttonBlockList.TabIndex = 7;
            this.buttonBlockList.Text = "点击刷新黑名单";
            this.buttonBlockList.UseVisualStyleBackColor = true;
            this.buttonBlockList.Click += new System.EventHandler(this.buttonBlockList_Click);
            // 
            // L2Trace
            // 
            this.AutoScaleDimensions = new System.Drawing.SizeF(6F, 12F);
            this.AutoScaleMode = System.Windows.Forms.AutoScaleMode.Font;
            this.ClientSize = new System.Drawing.Size(878, 485);
            this.Controls.Add(this.buttonBlockList);
            this.Controls.Add(this.tabControlSession);
            this.Controls.Add(this.logTextBox);
            this.Controls.Add(this.buttonRun);
            this.Controls.Add(this.netAdapterComboBox);
            this.Controls.Add(this.menuStrip1);
            this.MainMenuStrip = this.menuStrip1;
            this.Name = "L2Trace";
            this.TopMost = true;
            this.ResumeLayout(false);
            this.PerformLayout();

        }

        #endregion
        private System.ComponentModel.BackgroundWorker backgroundWorker1;
        private System.Windows.Forms.ComboBox netAdapterComboBox;
        private System.Windows.Forms.ToolTip toolTip1;
        private System.Windows.Forms.Button buttonRun;
        private System.Windows.Forms.RichTextBox logTextBox;
        private System.Windows.Forms.TabControl tabControlSession;
        private System.Windows.Forms.MenuStrip menuStrip1;
        private System.Windows.Forms.Button buttonBlockList;
    }
}

