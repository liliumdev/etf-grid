using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Data;
using System.Drawing;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows.Forms;
using System.Net;
using System.Net.Sockets;
using System.Threading;
using System.Diagnostics;

namespace grid_Client
{
    public partial class Form1 : Form
    {
        private TcpClient client;
        private Object aliveObj = new Object();
        private Object finishObj = new Object();
        private bool alive = false;

        private int result_x = -1;
        private int result_y = -1;
        private int result_tile = -1;

        private bool working = false;
        private bool finished = false;

        private PerformanceCounter cpuCounter;

        public Form1()
        {
            InitializeComponent();


            cpuCounter = new PerformanceCounter();
            cpuCounter.CategoryName = "Processor";
            cpuCounter.CounterName = "% Processor Time";
            cpuCounter.InstanceName = "_Total";
            usageLabel.Text = "CPU Usage: " + cpuCounter.NextValue() + "%";

            pictureBox2.BackColor = System.Drawing.Color.Transparent;
            pictureBox2.Image = Image.FromFile("crosshair.png");
            pictureBox2.Parent = pictureBox1;
            pictureBox2.Visible = false;
        }

        void HandleResponse(string response)
        {
            if (response == "hi")
            {
                lock (aliveObj)
                {
                    alive = true;
                }
                SendCommandAndGetResponse("ping");
                this.Invoke((MethodInvoker)delegate
                {
                    statusLabel.Text = "Connected";
                });
            }
            else if (response == "pong")
            {
                Thread.Sleep(2000);
                SendCommandAndGetResponse("ping");
            }
            else if (response == "pong_calculating")
            {
                this.Invoke((MethodInvoker)delegate
                {
                    statusLabel.Text = "Working";
                });
                lock (finishObj)
                {
                    if (working && !finished)
                    {
                        Thread.Sleep(2000);
                        SendCommandAndGetResponse("ping_calculating");
                    }
                    else if (working && finished)
                    {
                        this.Invoke((MethodInvoker)delegate
                        {
                            statusLabel.Text = "Finished";
                        });
                        SendCommandAndGetResponse("gotresult," + result_x.ToString() + "," + result_y.ToString() + "," + result_tile);
                    }
                }
            }
            else if (response == "quit")
            {
                this.Invoke((MethodInvoker)delegate
                {
                    statusLabel.Text = "Ordered to quit";
                });
            }
            else if (response.Contains("work"))
            {
                string[] parameters = response.Split(',');
                string[] range = parameters[1].Split(new string[] { "=>" }, StringSplitOptions.RemoveEmptyEntries);
                int from = int.Parse(range[0]);
                int to = int.Parse(range[1]);

                working = true;

                Task t = new Task(new Action(() =>
                {
                    int maxWidth = (to-from+1) * 2048;
                    int j = 0;
                    for (int i = from; i <= to; i++)
                    {
                        this.Invoke((MethodInvoker) delegate 
                        {
                            pictureBox1.Image = Image.FromFile("sarajevo/tile" + i.ToString() + ".png");
                        });

                        Bitmap bmp = (Bitmap)Image.FromFile("sarajevo/tile" + i.ToString() + ".png");
                        for (int x = 0; x < bmp.Width; x++)
                        {
                            for (int y = 0; y < bmp.Height; y++)
                            {
                                Color pix = bmp.GetPixel(x, y);

                                // fe06fa piksel koji se nalazi u tile10
                                if (pix.R == 0xFE && pix.G == 0x06 && pix.B == 0xFA)
                                {
                                    finished = true;

                                    result_x = x; result_y = y; result_tile = i;

                                    this.Invoke((MethodInvoker) delegate 
                                    {
                                        pictureBox2.Location = new Point(x - 75, y - 75);
                                        pictureBox2.Visible = true;
                                    });
                                }
                            }
                            
                                if (finished) break;

                            this.Invoke((MethodInvoker)delegate {
                                progressLabel.Text = (x + j * 2048).ToString() + " / " + maxWidth.ToString();
                            });
                        }

                        j++;
                        usageLabel.Text = "CPU Usage: " + Math.Round(cpuCounter.NextValue(), 2) + "%";
                    }
                    
                    finished = true;
                }));
                t.Start();

                SendCommandAndGetResponse("ping_calculating");
            }
        }

        string SendCommandAndGetResponse(string command)
        {
            try
            {
                Byte[] data = System.Text.Encoding.ASCII.GetBytes(command + "\r\n");

                NetworkStream stream = client.GetStream();
                stream.Write(data, 0, data.Length);
                
                data = new Byte[256];
                String responseData = String.Empty;
                Int32 bytes = stream.Read(data, 0, data.Length);
                responseData = System.Text.Encoding.ASCII.GetString(data, 0, bytes);
                Log(String.Format("Received: {0}", responseData));

                string response = responseData.Trim();
                HandleResponse(response);

                return response;
            }
            catch (ArgumentNullException e)
            {
                Log(String.Format("ArgumentNullException: {0}", e));
            }
            catch (SocketException e)
            {
                Log(String.Format("SocketException: {0}", e));
            }

            return "";
        }
        
        private void Log(string what)
        {
            this.Invoke((MethodInvoker)delegate
            {
                logger.Text += what + "\r\n";
            });
        }

        private void button1_Click(object sender, EventArgs e)
        {
            Task t = new Task(new Action(() =>
            {
                try
                {
                    client = new TcpClient();
                    //client.Connect(IPAddress.Parse("REMOTE.IP.IDE.OVDJE"), 5643);
                    client.Connect(IPAddress.Parse("127.0.0.1"), 5643);
                }
                catch (Exception ex)
                {
                    MessageBox.Show(ex.Message.ToString());
                }
                Log("Connecting, sending hello ...");
                while (!client.Connected) Thread.Sleep(500);
                while (true)
                {
                    lock (aliveObj)
                    {
                        if (alive) break;
                    }
                    SendCommandAndGetResponse("hello");
                    Thread.Sleep(500);
                }
            }));
            t.Start();
        }
    }
}
