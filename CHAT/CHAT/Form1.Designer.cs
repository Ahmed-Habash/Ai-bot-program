namespace CHAT
{
    partial class Form1
    {
        private System.ComponentModel.IContainer components = null;
        private System.Windows.Forms.Label label1;
        private System.Windows.Forms.FlowLayoutPanel flowLayoutPanelChat;
        private System.Windows.Forms.RichTextBox richTextBoxInput;
        private System.Windows.Forms.Button buttonSend;
        private System.Windows.Forms.Button buttonSettings;

        protected override void Dispose(bool disposing)
        {
            if (disposing && (components != null))
            {
                components.Dispose();
            }
            base.Dispose(disposing);
        }

        private void InitializeComponent()
        {
            label1 = new Label();
            flowLayoutPanelChat = new FlowLayoutPanel();
            richTextBoxInput = new RichTextBox();
            buttonSend = new Button();
            buttonSettings = new Button();
            SuspendLayout();
            // 
            // label1
            // 
            label1.AutoSize = true;
            label1.Font = new Font("Arial", 18F, FontStyle.Bold);
            label1.ForeColor = SystemColors.ButtonHighlight;
            label1.Location = new Point(0, 0);
            label1.Name = "label1";
            label1.Size = new Size(219, 29);
            label1.TabIndex = 0;
            label1.Text = "CHAT WITH BOTS";
            label1.TextAlign = ContentAlignment.MiddleCenter;
            // 
            // flowLayoutPanelChat
            // 
            flowLayoutPanelChat.AutoScroll = true;
            flowLayoutPanelChat.FlowDirection = FlowDirection.TopDown;
            flowLayoutPanelChat.Location = new Point(12, 50);
            flowLayoutPanelChat.Name = "flowLayoutPanelChat";
            flowLayoutPanelChat.Size = new Size(560, 240);
            flowLayoutPanelChat.TabIndex = 1;
            flowLayoutPanelChat.WrapContents = false;
            // 
            // richTextBoxInput
            // 
            richTextBoxInput.Location = new Point(82, 305);
            richTextBoxInput.Name = "richTextBoxInput";
            richTextBoxInput.Size = new Size(420, 31);
            richTextBoxInput.TabIndex = 2;
            richTextBoxInput.Text = "";
            richTextBoxInput.KeyDown += richTextBoxInput_KeyDown;
            // 
            // buttonSend
            // 
            buttonSend.Location = new Point(510, 305);
            buttonSend.Name = "buttonSend";
            buttonSend.Size = new Size(62, 31);
            buttonSend.TabIndex = 3;
            buttonSend.Text = "Send";
            buttonSend.UseVisualStyleBackColor = true;
            buttonSend.Click += buttonSend_Click;
            // 
            // buttonSettings
            // 
            buttonSettings.Location = new Point(12, 305);
            buttonSettings.Name = "buttonSettings";
            buttonSettings.Size = new Size(64, 31);
            buttonSettings.TabIndex = 4;
            buttonSettings.Text = "Settings";
            buttonSettings.UseVisualStyleBackColor = true;
            buttonSettings.Click += buttonSettings_Click;
            // 
            // Form1
            // 
            AutoScaleDimensions = new SizeF(7F, 15F);
            AutoScaleMode = AutoScaleMode.Font;
            BackColor = Color.FromArgb(0, 64, 64);
            ClientSize = new Size(584, 361);
            Controls.Add(label1);
            Controls.Add(flowLayoutPanelChat);
            Controls.Add(richTextBoxInput);
            Controls.Add(buttonSend);
            Controls.Add(buttonSettings);
            Name = "Form1";
            Text = "Chat";
            Resize += Form1_Resize;
            ResumeLayout(false);
            PerformLayout();
        }
    }
}
