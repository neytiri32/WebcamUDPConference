using System;
using System.Drawing;
using System.Text;
using System.Windows.Forms;
using System.Threading.Tasks;

// UDP y Multicast.
using System.Net.Sockets;
using System.Net;
using System.IO;

namespace WebcamUDPMulticast
{
    public partial class Client : Form
    {
        private Bitmap image_bitmap = null;

        private UdpClient udpclient;
        private IPAddress multicastaddress;
        private IPEndPoint remoteep;
        private IPEndPoint remoteepChat;

        Socket uSocket;

        public Client()
        {
            InitializeComponent();

            udpclient = new UdpClient();
            multicastaddress = IPAddress.Parse("224.0.0.1");
            remoteep = new IPEndPoint(IPAddress.Any, 4672);
            remoteepChat = new IPEndPoint(multicastaddress, 8080);
            
            udpclient.JoinMulticastGroup(multicastaddress);

        }

        private void Form1_Load(object sender, EventArgs e)
        {

            uSocket = udpclient.Client;
            uSocket.SetSocketOption(SocketOptionLevel.Socket, SocketOptionName.ReuseAddress, true);
            try
            {
                uSocket.Bind(remoteep);
            }
            catch (Exception ex)
            {
                MessageBox.Show(ex.Message);
            }

            Task t1 = new Task(display_image);
            t1.Start();

        }

        //Chat
        private void sendButton_Click_1(object sender, EventArgs e)
        {
            try
            {
                Byte[] bufferChat = Encoding.ASCII.GetBytes(newMessageBox.Text);
                udpclient.Send(bufferChat, bufferChat.Length, remoteepChat);
                SetText("Me: " + string.Format(newMessageBox.Text));

            }
            catch (Exception ex)
            {
                MessageBox.Show(ex.Message);
            }

            newMessageBox.Clear();
        }
        private void SetText(string msg)
        {
            messagesHistory.Items.Add(msg);
        }

        //Video
        private void display_image()
        {
            try
            {
                while (remoteep != null)
                {
                    Byte[] buffer = udpclient.Receive(ref remoteep);
                    MemoryStream ms = new MemoryStream(buffer);
                    this.pictureBox1.Image = Image.FromStream(ms);
                }
            }
            catch (Exception ex)
            {
                MessageBox.Show(ex.Message);
            }
        }

        //Capture image
        private void btnSave_Click(object sender, EventArgs e)
        {
            if (this.pictureBox1.Image == null)
                return;

            Bitmap current = (Bitmap)this.pictureBox1.Image.Clone();
            using (SaveFileDialog sfd = new SaveFileDialog())
            {
                sfd.Filter = "*.jpeg|*.jpeg";
                if (sfd.ShowDialog() == DialogResult.OK)
                {
                    current.Save(sfd.FileName);
                }
            }
            current.Dispose();
        }

        //Open Image
        private Bitmap ResizeBitmap(Bitmap img)
        {
            int newWidth = 0;
            int newHeight = 0;
            double imgRatio;

            if (img.Width > img.Height)
            {
                imgRatio = ((double)img.Height / (double)img.Width) * 100;
                newWidth = pictureBox1.Width;
                newHeight = (int)(((double)newWidth / 100) * imgRatio);
            }
            else
            {
                imgRatio = ((double)img.Width / (double)img.Height) * 100;
                newHeight = pictureBox1.Height;
                newWidth = (int)(((double)newHeight / 100) * imgRatio);
            }

            Bitmap newImg = new Bitmap(newWidth, newHeight);

            using (Graphics g = Graphics.FromImage(newImg))
                g.DrawImage(img, 0, 0, newWidth, newHeight);

            return newImg;
        }

        private void btnOpen_Click_1(object sender, EventArgs e)
        {
            OpenFileDialog ofd = new OpenFileDialog();
            ofd.Title = "Open image...";
            ofd.Filter = "*.jpeg|*.jpeg";

            if (ofd.ShowDialog() == DialogResult.OK)
            {
                image_bitmap = new Bitmap(ofd.FileName, true);
                Bitmap resized = ResizeBitmap(image_bitmap);
                pictureBox2.Image = resized;
                label5.Text = "Image Frame: " + ofd.FileName;
            }
        }

    }
}
