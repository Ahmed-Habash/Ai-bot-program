using System;
using System.Drawing;
using System.Drawing.Drawing2D;
using System.Net;
using System.Net.Mail;
using System.Threading.Tasks;
using System.Windows.Forms;
using System.Linq;
using static System.Windows.Forms.VisualStyles.VisualStyleElement.ListView;


namespace CHAT
{
    public partial class Form1 : Form
    {
        private bool _humanLike = true;
        private string _adjectives = string.Empty;
        private OpenAIService? _aiService;
        // Stores all chat messages for memory (user + bot)
        private List<ChatMessage> _chatMemory = new List<ChatMessage>();

        // Still keep the pending email memory
        private (string recipient, string subject, string body)? _pendingEmail = null;


        public Form1()
        {
            InitializeComponent();

            // Show settings on startup
            Shown += async (_, __) =>
            {
                var f2 = new Form2 { ChatFormReference = this };
                f2.ShowDialog();
            };

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
            _chatMemory.Add(new ChatMessage { Role = "user", Content = text }); // store user message
            richTextBoxInput.Clear();

            if (_aiService == null)
            {
                AddBotMessage("(OpenAI service not initialized)");
                return;
            }

            buttonSend.Enabled = false;

            // Check if user is confirming a pending email
            if (_pendingEmail.HasValue && text.ToLower().Contains("yes"))
            {
                var email = _pendingEmail.Value;
                bool success = await SendEmailAsync(email.recipient, email.subject, email.body);
                AddBotMessage(success
                    ? $"Email sent to {email.recipient}!"
                    : $"Failed to send email to {email.recipient}.");
                _pendingEmail = null;
                _chatMemory.Add(new ChatMessage { Role = "bot", Content = success ? $"Email sent to {email.recipient}" : $"Failed to send email to {email.recipient}" });
                buttonSend.Enabled = true;
                return;
            }

            AddBotMessage("...thinking...");

            try
            {
                // Build the system prompt including memory
                string sysPrompt = BuildSystemPrompt();
                string memoryPrompt = string.Join("\n", _chatMemory.Select(m => $"{m.Role}: {m.Content}"));
                string finalPrompt = sysPrompt + "\n\nPrevious conversation:\n" + memoryPrompt + "\nUser: " + text;

                var response = await _aiService.SendMessageAsync(finalPrompt, text);

                // Remove "...thinking..."
                if (flowLayoutPanelChat.Controls.Count > 0)
                {
                    var lastPanel = flowLayoutPanelChat.Controls[flowLayoutPanelChat.Controls.Count - 1];
                    lastPanel?.Dispose();
                }

                // Parse email command if present
                if (response.StartsWith("[COMMAND: email]"))
                {
                    try
                    {
                        var lines = response.Split(new[] { '\n' }, StringSplitOptions.RemoveEmptyEntries);
                        string recipient = lines.FirstOrDefault(l => l.StartsWith("Recipient: "))?.Replace("Recipient: ", "").Trim() ?? "";
                        string subject = lines.FirstOrDefault(l => l.StartsWith("Subject: "))?.Replace("Subject: ", "").Trim() ?? "";
                        string body = string.Join("\n", lines.SkipWhile(l => !l.StartsWith("Body: ")).Skip(1));

                        if (!string.IsNullOrEmpty(recipient) && !string.IsNullOrEmpty(subject) && !string.IsNullOrEmpty(body))
                        {
                            _pendingEmail = (recipient, subject, body);
                            AddBotMessage($"Got it! I’ve prepared the email to {recipient}. Reply 'yes' to send it now.");
                            _chatMemory.Add(new ChatMessage { Role = "bot", Content = $"Prepared email to {recipient}" });
                        }
                        else
                        {
                            AddBotMessage("Email command detected, but some fields are missing.");
                            _chatMemory.Add(new ChatMessage { Role = "bot", Content = "Email command detected, but some fields are missing." });
                        }
                    }
                    catch (Exception ex)
                    {
                        AddBotMessage($"[Email Parsing Error] {ex.Message}");
                        _chatMemory.Add(new ChatMessage { Role = "bot", Content = $"[Email Parsing Error] {ex.Message}" });
                    }
                }
                else
                {
                    AddBotMessage(response);
                    _chatMemory.Add(new ChatMessage { Role = "bot", Content = response });
                }
            }
            catch (Exception ex)
            {
                AddBotMessage($"[Error] {ex.Message}");
                _chatMemory.Add(new ChatMessage { Role = "bot", Content = $"[Error] {ex.Message}" });
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

            prompt += " When the user asks you to write or send an email, respond ONLY in the following format:";
            prompt += "\n[COMMAND: email]";
            prompt += "\nRecipient: recipient-email@example.com";
            prompt += "\nSubject: subject of the email";
            prompt += "\nBody: the email content goes here";
            prompt += "\nDo NOT ask for the user's email password or secrets.";

            // Append conversation history for memory
            foreach (var msg in _conversationHistory)
            {
                if (msg.Role == "user") prompt += $"\nUser: {msg.Content}";
                else if (msg.Role == "assistant") prompt += $"\nAssistant: {msg.Content}";
            }

            return prompt;
        }


        private string BuildSystemPrompt()
        {
            var prompt = "You are a helpful chat assistant inside a Windows app. Reply in plain text and be concise.";

            // Human-like tone
            prompt += _humanLike
                ? " Use a friendly, human-like tone and occasional small talk when appropriate."
                : " Use a neutral, concise assistant tone (no small talk).";

            // Emphasize adjectives if provided
            if (!string.IsNullOrWhiteSpace(_adjectives))
                prompt += $" Emphasize these adjectives in responses: {_adjectives}.";

            // Add email command instructions
            prompt += " When the user asks you to write or send an email, respond ONLY in the following format:";
            prompt += "\n[COMMAND: email]";
            prompt += "\nRecipient: recipient-email@example.com";
            prompt += "\nSubject: subject of the email";
            prompt += "\nBody: the email content goes here";

            prompt += "\nDo NOT ask for the user's email password or secrets, and do not write anything outside this format.";

            return prompt;
        }


        private async Task HandleEmailCommand(string userMessage)
        {
            
            try
            {
                // Simple format: send email to [recipient] subject [subject] body [body]
                int toIndex = 14;
                int subjectIndex = userMessage.ToLower().IndexOf(" subject ", toIndex);
                int bodyIndex = userMessage.ToLower().IndexOf(" body ", toIndex);

                if (subjectIndex == -1 || bodyIndex == -1)
                {
                    AddBotMessage("Email command format: send email to [recipient] subject [subject] body [body]");
                    return;
                }

                string recipient = userMessage.Substring(toIndex, subjectIndex - toIndex).Trim();
                string subject = userMessage.Substring(subjectIndex + 9, bodyIndex - (subjectIndex + 9)).Trim();
                string body = userMessage.Substring(bodyIndex + 6).Trim();

                bool success = await SendEmailAsync(recipient, subject, body);
                AddBotMessage(success ? $"Email sent to {recipient}" : $"Failed to send email to {recipient}");
            }
            catch
            {
                AddBotMessage("Error parsing email command.");
            }
        }

        private async Task<bool> SendEmailAsync(string recipient, string subject, string body)
        {
            try
            {
                string senderEmail = Environment.GetEnvironmentVariable("EMAIL_ADDRESS");
                string senderPassword = Environment.GetEnvironmentVariable("EMAIL_PASSWORD");

                if (string.IsNullOrEmpty(senderEmail) || string.IsNullOrEmpty(senderPassword))
                {
                    AddBotMessage("[Email Error] Email credentials not set in environment variables.");
                    return false;
                }

                using var client = new SmtpClient("smtp.gmail.com", 587)
                {
                    Credentials = new NetworkCredential(senderEmail, senderPassword),
                    EnableSsl = true,
                    Timeout = 10000 // 10 seconds timeout
                };

                using var mail = new MailMessage(senderEmail, recipient, subject, body);

                await client.SendMailAsync(mail);
                return true;
            }
            catch (SmtpException smtpEx)
            {
                AddBotMessage($"[SMTP Error] StatusCode: {smtpEx.StatusCode}, Message: {smtpEx.Message}");
                if (smtpEx.InnerException != null)
                    AddBotMessage($"[SMTP Inner Exception] {smtpEx.InnerException.Message}");
                return false;
            }
            catch (Exception ex)
            {
                AddBotMessage($"[Email Error] {ex.Message}");
                return false;
            }
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

        private List<ChatMessage> _conversationHistory = new List<ChatMessage>();

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

    public class ChatMessage
    {
        public string Role { get; set; } = ""; // "user" or "bot"
        public string Content { get; set; } = "";
    }
}
