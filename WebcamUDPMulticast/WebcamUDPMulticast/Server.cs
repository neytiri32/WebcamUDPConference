using System;
using System.Drawing;
using System.Text;
using System.Windows.Forms;
using System.Threading;

// UDP y Multicast.
using System.Net.Sockets;
using System.Net;
using System.IO;

// IMAGE
using System.Drawing.Imaging;
using Touchless.Vision.Camera;

namespace WebcamUDPMulticast
{
    public partial class Server : Form
    {
        private IPAddress multicastaddress;

        const int UDP_PORT = 8080;
        private CameraFrameSource _frameSource;
        private static Bitmap _latestFrame;

        UdpClient udpserver;
        IPEndPoint remote;
        private IPEndPoint remoteChat = null;

        private Bitmap image_bitmap = null;


        public Server()
        {
            InitializeComponent();

            udpserver = new UdpClient(UDP_PORT);
            multicastaddress = IPAddress.Parse("224.0.0.1");
            udpserver.JoinMulticastGroup(multicastaddress);
            remote = new IPEndPoint(multicastaddress, UDP_PORT);

            Thread thr = new Thread(new ThreadStart(ServerStart));
            thr.Start();
            newClientButton.Enabled = true;
        }
        private void Server_Load(object sender, EventArgs e)
        {
            comboBoxCameras.Items.Clear();
            foreach (Camera cam in CameraService.AvailableCameras)
            {
                comboBoxCameras.Items.Add(cam);
            }

            CheckForIllegalCrossThreadCalls = false;
            remoteChat = null;
        }
        private void ServerStart()
        {
            while (true)
            {
                byte[] buffer = udpserver.Receive(ref remoteChat);
                string msg = Encoding.ASCII.GetString(buffer, 0, buffer.Length);
                SetText(string.Format(msg));
            }
        }
        private void SetText(string msg)
        {
            messagesHistory.Items.Add(msg);
        }

        //Video
        private void closeCam()
        {
            if (_frameSource != null)
            {
                _frameSource.NewFrame -= OnImageCaptured;
                _frameSource.Camera.Dispose();
                setFrameSource(null);
                pictureBox1.Paint -= new PaintEventHandler(drawLatestImage);
            }
        }
        private void startCapturing()
        {
            try
            {
                Camera c = (Camera)comboBoxCameras.SelectedItem;
                setFrameSource(new CameraFrameSource(c));
                _frameSource.Camera.CaptureWidth = 320;
                _frameSource.Camera.CaptureHeight = 240;
                _frameSource.Camera.Fps = 20;
                _frameSource.NewFrame += OnImageCaptured;

                pictureBox1.Paint += new PaintEventHandler(drawLatestImage);
                _frameSource.StartFrameCapture();

                button1.Enabled = false;
            }
            catch (Exception ex)
            {
                comboBoxCameras.Text = "Select a Camera.";
                MessageBox.Show(ex.Message);
            }
        }
        private void setFrameSource(CameraFrameSource frame)
        {
            if (_frameSource == frame)
                return;

            _frameSource = frame;
        }
        private void button1_Click(object sender, EventArgs e)
        {
            closeCam();
            startCapturing();
        }
        public void OnImageCaptured(Touchless.Vision.Contracts.IFrameSource frameSource, Touchless.Vision.Contracts.Frame frame, double fps)
        {
            _latestFrame = frame.Image;
            pictureBox1.Invalidate();
        }
        private void drawLatestImage(object sender, PaintEventArgs e)
        {
            remote = new IPEndPoint(multicastaddress, 4672);
            if (_latestFrame != null)
            {   
                e.Graphics.DrawImage(_latestFrame, pictureBox1.Width, 0, -pictureBox1.Width,
                pictureBox1.Height);
                _latestFrame = new Bitmap(_latestFrame, new Size(320, 240));

                using (MemoryStream ms = new MemoryStream())
                {
                    _latestFrame.Save(ms, ImageFormat.Jpeg);
                    Byte[] buffer = ms.ToArray();
                    udpserver.Send(buffer, buffer.Length, remote);
                }
            }
        }

        //Capture image
        private void btnSave_Click(object sender, EventArgs e)
        {
            if (_frameSource == null)
                return;

            Bitmap current = (Bitmap)_latestFrame.Clone();
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

        //Open image
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
        private void btnOpen_Click(object sender, EventArgs e)
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

        private void button2_Click(object sender, EventArgs e)
        {
            closeCam();
            button1.Enabled = true;
        }

        //Other
        private void newClientButton_Click(object sender, EventArgs e)
        {
            Client client = new Client();
            client.Show();
        }
        private void Server_FormClosing(object sender, FormClosingEventArgs e)
        {
            closeCam();
            udpserver.Close();
            Application.Exit();
            Environment.Exit(1);
        }
    }
}
