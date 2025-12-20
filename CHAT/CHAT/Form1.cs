using System;
using System.Drawing;
using System.Drawing.Drawing2D;
using System.Threading.Tasks;
using System.Windows.Forms;
using System.Linq;

namespace CHAT
{
    public partial class Form1 : Form
    {
        private bool _humanLike = true;
        private string _adjectives = string.Empty;
        private OpenAIService? _aiService;
        private readonly List<ChatMessage> _conversation = new();
        private List<ChatMessage> _conversationHistory = new List<ChatMessage>();
        private string _openAiKey = "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";

        public Form1()
        {
            InitializeComponent();

            // Show settings on startup
            Shown += async (_, __) =>
            {
                var f2 = new Form2 { ChatFormReference = this };
                f2.ShowDialog();
            };

            _conversation.Add(new ChatMessage
            {
                Role = "system",
                Content = BuildSystemPrompt()
            });

            // FlowLayoutPanel setup
            flowLayoutPanelChat.WrapContents = false;
            flowLayoutPanelChat.FlowDirection = FlowDirection.TopDown;
            flowLayoutPanelChat.AutoScroll = true;
            flowLayoutPanelChat.HorizontalScroll.Enabled = false;
            flowLayoutPanelChat.HorizontalScroll.Visible = false;

            try
            {
                _aiService = new OpenAIService();
            }
            catch (Exception ex)
            {
                MessageBox.Show($"OpenAIService init failed: {ex.Message}", "Error", MessageBoxButtons.OK, MessageBoxIcon.Error);
                buttonSend.Enabled = false;

            }

            this.Resize += Form1_Resize;
        }

        public void ApplySettings(bool humanLike, string adjectives)
        {
            _humanLike = humanLike;
            _adjectives = adjectives ?? string.Empty;
            AddSystemMessage($"Settings applied — HumanLike={_humanLike}; Adjectives={_adjectives}");
        }

        private string BuildSystemPrompt()
        {
            string prompt = "You are a helpful chat assistant inside a Windows app. Reply in plain text and be concise.";

            prompt += _humanLike
                ? " Use a friendly, human-like tone and occasional small talk when appropriate."
                : " Use a neutral, concise assistant tone (no small talk).";

            if (!string.IsNullOrWhiteSpace(_adjectives))
                prompt += $" Emphasize these adjectives in responses: {_adjectives}.";

            return prompt;
        }


        private void ApplyRoundedCorners(Control c, int radius = 12)
        {
            var rect = new Rectangle(0, 0, c.Width, c.Height);
            var path = new GraphicsPath();

            path.AddArc(rect.X, rect.Y, radius, radius, 180, 90);
            path.AddArc(rect.Right - radius, rect.Y, radius, radius, 270, 90);
            path.AddArc(rect.Right - radius, rect.Bottom - radius, radius, radius, 0, 90);
            path.AddArc(rect.X, rect.Bottom - radius, radius, radius, 90, 90);
            path.CloseFigure();

            c.Region = new Region(path);
        }

        private void AddSystemMessage(string text) => AddBubble(text, Color.LightGray, Color.Black, true);
        private void AddUserMessage(string text) => AddBubble(text, Color.FromArgb(0, 120, 215), Color.White, false);
        private void AddBotMessage(string text) => AddBubble(text, Color.DarkGray, Color.White, true);

        private void AddBubble(string text, Color backColor, Color foreColor, bool leftSide)
        {
            int reservedWidth = SystemInformation.VerticalScrollBarWidth + 20;
            int maxBubbleWidth = (int)((flowLayoutPanelChat.ClientSize.Width - reservedWidth) * 0.5);

            var rtb = new RichTextBox
            {
                Text = text,
                BackColor = backColor,
                ForeColor = foreColor,
                BorderStyle = BorderStyle.None,
                Font = new Font("Arial", 10),
                Multiline = true,
                WordWrap = true,
                ReadOnly = true,
                ScrollBars = RichTextBoxScrollBars.None,
                Width = maxBubbleWidth
            };

            rtb.Height = TextRenderer.MeasureText(text + " ", rtb.Font,
                new Size(maxBubbleWidth, int.MaxValue),
                TextFormatFlags.WordBreak).Height + 16;

            var panel = new Panel
            {
                Width = flowLayoutPanelChat.ClientSize.Width,
                Height = rtb.Height + 10,
                Margin = Padding.Empty
            };

            rtb.Location = leftSide
                ? new Point(10, 5)
                : new Point(panel.Width - rtb.Width - 10, 5);

            panel.Controls.Add(rtb);
            flowLayoutPanelChat.Controls.Add(panel);

            ApplyRoundedCorners(rtb);

            // Auto-scroll to the new message
            flowLayoutPanelChat.ScrollControlIntoView(panel);
            flowLayoutPanelChat.VerticalScroll.Value = flowLayoutPanelChat.VerticalScroll.Maximum;
            flowLayoutPanelChat.PerformLayout();
        }

        private async void buttonSend_Click(object sender, EventArgs e) => await SendCurrentMessageAsync();

        private async Task SendCurrentMessageAsync()
        {
            var text = richTextBoxInput.Text.Trim();
            if (string.IsNullOrEmpty(text)) return;

            AddUserMessage(text);
            _conversationHistory.Add(new ChatMessage { Role = "user", Content = text });
            richTextBoxInput.Clear();

            if (_aiService == null)
            {
                AddBotMessage("(OpenAI service not initialized)");
                return;
            }

            buttonSend.Enabled = false;
            AddBotMessage("...thinking...");

            try
            {
                var fullPrompt = BuildFullPrompt();
                var response = await _aiService.SendMessageAsync(fullPrompt, text);

                if (flowLayoutPanelChat.Controls.Count > 0)
                {
                    var lastPanel = flowLayoutPanelChat.Controls[^1];
                    lastPanel?.Dispose();
                }

                AddBotMessage(response);
                _conversationHistory.Add(new ChatMessage { Role = "assistant", Content = response });
            }
            catch (Exception ex)
            {
                AddBotMessage($"[Error] {ex.Message}");
            }
            finally
            {
                buttonSend.Enabled = true;
            }
        }

        private string BuildFullPrompt()
        {
            string prompt = "You are a helpful chat assistant inside a Windows app. Reply in plain text and be concise.";

            prompt += _humanLike
                ? " Use a friendly, human-like tone and occasional small talk when appropriate."
                : " Use a neutral, concise assistant tone (no small talk).";

            if (!string.IsNullOrWhiteSpace(_adjectives))
                prompt += $" Emphasize these adjectives in responses: {_adjectives}.";

            foreach (var msg in _conversationHistory)
            {
                prompt += $"\n{(msg.Role == "user" ? "User" : "Assistant")}: {msg.Content}";
            }

            return prompt;
        }

        private void buttonSettings_Click(object sender, EventArgs e)
        {
            var f2 = new Form2 { ChatFormReference = this };
            f2.Show();
        }

        private async void richTextBoxInput_KeyDown(object sender, KeyEventArgs e)
        {
            if (e.KeyCode == Keys.Enter && !e.Shift)
            {
                e.SuppressKeyPress = true;
                await SendCurrentMessageAsync();
            }
        }

        private class ChatMessage
        {
            public string Role { get; set; } = ""; // "user" or "assistant"
            public string Content { get; set; } = "";
        }

        private void LayoutControls()
        {
            int bottomPadding = 20;

            // Header label
            label1.Left = (ClientSize.Width - label1.Width) / 2;
            label1.Top = 10;

            // Input box
            richTextBoxInput.Top = ClientSize.Height - richTextBoxInput.Height - bottomPadding;
            richTextBoxInput.Width = ClientSize.Width - buttonSend.Width - buttonSettings.Width - 40;

            buttonSettings.Top = richTextBoxInput.Top;
            buttonSettings.Left = 12;

            buttonSend.Top = richTextBoxInput.Top;
            buttonSend.Left = richTextBoxInput.Right + 5;

            // Chat panel
            flowLayoutPanelChat.Width = ClientSize.Width - 24;
            flowLayoutPanelChat.Height = richTextBoxInput.Top - label1.Bottom - 10;

            // Resize existing bubbles
            foreach (Panel row in flowLayoutPanelChat.Controls)
            {
                row.Width = flowLayoutPanelChat.ClientSize.Width;
                if (row.Controls.Count == 0) continue;
                if (row.Controls[0] is RichTextBox rtb)
                {
                    rtb.Width = (int)(row.Width * 0.5);
                    rtb.Height = TextRenderer.MeasureText(rtb.Text + " ", rtb.Font,
                        new Size(rtb.Width, int.MaxValue), TextFormatFlags.WordBreak).Height + 16;

                    rtb.Left = rtb.BackColor == Color.FromArgb(0, 120, 215)
                        ? row.Width - rtb.Width - 10
                        : 10;

                    row.Height = rtb.Height + 10;
                    ApplyRoundedCorners(rtb);
                }
            }

            flowLayoutPanelChat.HorizontalScroll.Value = 0;
        }

        private void Form1_Resize(object? sender, EventArgs e)
        {
            SuspendLayout();
            flowLayoutPanelChat.SuspendLayout();
            LayoutControls();
            flowLayoutPanelChat.ResumeLayout(true);
            ResumeLayout(true);
        }
    }
}
