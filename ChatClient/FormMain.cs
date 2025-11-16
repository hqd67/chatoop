using System;
using System.Drawing;
using System.Runtime.Remoting.Messaging;
using System.Windows.Forms;
using System.Xml.Linq;

public partial class FormMain : Form
{
    private Client client = new Client();

    public FormMain()
    {
        InitializeComponent();

        client.OnMessageReceived += OnMsg;
    }

    private async void btnConnect_Click(object sender, EventArgs e)
    {
        bool ok = await client.ConnectAsync(tbIP.Text, int.Parse(tbPort.Text), tbName.Text);
        if (ok) lblStatus.Text = "Подключено";
    }

    private async void btnSend_Click(object sender, EventArgs e)
    {
        await client.SendTextAsync(tbMessage.Text);
        tbMessage.Clear();
    }

    private void OnMsg(Message msg)
    {
        rtbChat.AppendText($"[{msg.Timestamp:HH:mm}] {msg.Sender}: {msg.Text}\n");
    }
}
