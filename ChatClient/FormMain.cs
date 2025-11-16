using System;
using System.Drawing;
using System.IO;
using System.Threading.Tasks;
using System.Windows.Forms;

namespace ChatClient
{
    public class FormMain : Form
    {
        private RichTextBox rtbChat;
        private TextBox tbMessage, tbIP, tbPort, tbName;
        private Button btnSend, btnAttach, btnConnect;
        private ListBox lbUsers;
        private Label lblStatus;

        private Client _client;

        public FormMain()
        {
            InitializeComponent();
            _client = new Client();
            _client.OnMessageReceived += Client_OnMessageReceived;
            _client.OnUserListUpdated += Client_OnUserListUpdated;
        }

        private void InitializeComponent()
        {
            this.Text = "ChatClient";
            this.StartPosition = FormStartPosition.CenterScreen;
            this.ClientSize = new Size(800, 560);

            rtbChat = new RichTextBox { ReadOnly = true, Location = new Point(10, 10), Size = new Size(560, 420) };
            tbMessage = new TextBox { Location = new Point(10, 440), Size = new Size(460, 24) };
            btnSend = new Button { Text = "Отправить", Location = new Point(480, 438), Size = new Size(90, 28) };
            btnAttach = new Button { Text = "Прикрепить", Location = new Point(580, 438), Size = new Size(90, 28) };
            tbIP = new TextBox { Location = new Point(580, 10), Size = new Size(200, 24), Text = "127.0.0.1" };
            tbPort = new TextBox { Location = new Point(580, 40), Size = new Size(200, 24), Text = "9000" };
            tbName = new TextBox { Location = new Point(580, 70), Size = new Size(200, 24), Text = "User" };
            btnConnect = new Button { Text = "Подключиться", Location = new Point(580, 100), Size = new Size(200, 30) };
            lbUsers = new ListBox { Location = new Point(580, 140), Size = new Size(200, 200) };
            lblStatus = new Label { Location = new Point(10, 480), Size = new Size(760, 24), Text = "Не подключено" };

            btnSend.Click += async (s, e) => await BtnSend_Click();
            btnAttach.Click += BtnAttach_Click;
            btnConnect.Click += async (s, e) => await BtnConnect_Click();

            tbMessage.KeyDown += async (s, e) =>
            {
                if (e.KeyCode == Keys.Enter)
                {
                    e.SuppressKeyPress = true;
                    await BtnSend_Click();
                }
            };

            this.Controls.AddRange(new Control[] { rtbChat, tbMessage, btnSend, btnAttach, tbIP, tbPort, tbName, btnConnect, lbUsers, lblStatus });
        }

        private async Task BtnConnect_Click()
        {
            int port = 9000;
            int.TryParse(tbPort.Text, out port);
            var ok = await _client.ConnectAsync(tbIP.Text, port, tbName.Text);
            lblStatus.Text = ok ? $"Connected as {tbName.Text}" : "Connection failed";
        }

        private async Task BtnSend_Click()
        {
            if (string.IsNullOrWhiteSpace(tbMessage.Text)) return;
            string recipient = lbUsers.SelectedItem?.ToString();
            await _client.SendTextMessageAsync(tbMessage.Text, recipient);

            var msg = new ChatMessage { Type = "message", Sender = _client.UserName, Recipient = recipient, Text = tbMessage.Text, Timestamp = DateTime.UtcNow };
            AppendMessageToChat(msg);
            tbMessage.Clear();
        }

        private async void BtnAttach_Click(object sender, EventArgs e)
        {
            using (var dlg = new OpenFileDialog())
            {
                if (dlg.ShowDialog() != DialogResult.OK) return;
                string recipient = lbUsers.SelectedItem?.ToString();
                await _client.SendFileAsync(dlg.FileName, recipient);

                var msg = new ChatMessage { Type = "file", Sender = _client.UserName, Recipient = recipient, FileName = Path.GetFileName(dlg.FileName), FileSize = (int)new FileInfo(dlg.FileName).Length, Timestamp = DateTime.UtcNow, Text = $"[Файл отправлен: {Path.GetFileName(dlg.FileName)}]" };
                AppendMessageToChat(msg);
            }
        }

        private void Client_OnUserListUpdated(string[] users)
        {
            if (InvokeRequired)
            {
                BeginInvoke(new Action<string[]>(Client_OnUserListUpdated), users);
                return;
            }
            lbUsers.Items.Clear();
            foreach (var u in users) lbUsers.Items.Add(u);
        }

        private void Client_OnMessageReceived(ChatMessage msg)
        {
            if (InvokeRequired)
            {
                BeginInvoke(new Action<ChatMessage>(Client_OnMessageReceived), msg);
                return;
            }
            AppendMessageToChat(msg);
        }

        private void AppendMessageToChat(ChatMessage msg)
        {
            string time = msg.Timestamp.ToLocalTime().ToString("HH:mm:ss");
            string text = msg.Text ?? "";
            rtbChat.AppendText($"[{time}] {msg.Sender}: {text}{Environment.NewLine}");
            rtbChat.ScrollToCaret();
        }

        protected override void OnFormClosing(FormClosingEventArgs e)
        {
            base.OnFormClosing(e);
            try
            {
                var disc = new ChatMessage { Type = "disconnect", Sender = _client.UserName, Timestamp = DateTime.UtcNow };
                // best effort - fire and forget
                _ = _client?.SendTextMessageAsync("disconnect", null);
                _client?.Disconnect();
            }
            catch { }
        }
    }
}
