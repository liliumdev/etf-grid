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

namespace grid_Server
{
    public partial class Form1 : Form
    {
        private int nextId = 0;

        private List<Worker> Workers = new List<Worker>();
        private List<Worker> AliveWorkers = new List<Worker>();
        private TcpListener server = null;
        private List<int> AddedWorkers = new List<int>();

        private bool startWork = false;
        
        public Form1()
        {
            InitializeComponent();

            server = new TcpListener(IPAddress.Any, 5643);
            server.Start();

            Task t = new Task(new Action(() =>
            {
                AcceptConnections();
            }));
            t.Start();
        }
        
        public void AcceptConnections()
        {
            while(true)
            {
                Workers.Add(new Worker(server.AcceptTcpClient(), nextId));

                Task t = new Task(new Action(() =>
                {
                    CommunicateWith(nextId);
                }));
                t.Start();

                Log(String.Format("Accepted client ID#{0}!", nextId));
                nextId++;
            }
        }

        public void CommunicateWith(int id)
        {
            Worker worker = Workers.Where(x => x.ID == id).First();
            TcpClient client = worker.Client;
            if (client == null) return;

            Byte[] bytes = new Byte[256];
            int num = 0;
            String data = null;

            try
            {
                NetworkStream stream = client.GetStream();

                int i;
                
                while ((i = stream.Read(bytes, num, bytes.Length - num)) != 0)
                {
                    num += i;
                    
                    if (bytes[num - 1] == '\n')
                    {
                        data = System.Text.Encoding.ASCII.GetString(bytes, 0, num).Trim();
                        HandleCommand(worker, data);
                        Log(String.Format("Received from ID#{0}: {1}", id, data));
                        num = 0;
                    }
                }
            }
            catch (SocketException e)
            {
                Log(String.Format("SocketException: {0}", e));
            }
            catch (System.IO.IOException e)
            {
                //Log(String.Format("IOException: {0}", e));
                Log(String.Format("Client ID#{0} disconnected", id));
                int index = AliveWorkers.FindIndex(x => x.ID == id);
                AliveWorkers.Find(x => x.ID == id).Client.Close();
                AliveWorkers.RemoveAt(index);
                listView1.Items.RemoveAt(index);
                Workers.RemoveAt(index);
            }
            catch (Exception e)
            {
                Log(String.Format("Exception: {0}", e));
            }
            finally
            {
                worker.Client.Close();
                Workers.Remove(worker);
            }
        }

        private void HandleCommand(Worker w, string command)
        {
            Worker worker = Workers.Find(x => x.ID == w.ID);
            if (command == "hello")
            {
                SendResponse(w, "hi");
                this.Invoke((MethodInvoker)delegate {
                    listView1.Items.Add(new ListViewItem(new string[] { "#" + w.ID.ToString(), "Waiting for command", "-1,-1" }));
                });

                AddedWorkers.Add(w.ID);
                AliveWorkers.Add(w);
                worker.State = WorkerState.WAITING_FOR_COMMAND;
            }
            else if(command == "ping")
            {
                if (!startWork)
                {
                    SendResponse(w, "pong");
                    worker.State = WorkerState.WAITING_FOR_COMMAND;
                }
                else
                {
                    // sad racunaj
                    foreach(Worker aw in AliveWorkers)
                    {
                        if(aw.ID == worker.ID)
                            SendResponse(worker, "work," + aw.WorkQueue.First().ToString() + "=>" + aw.WorkQueue.Last().ToString());
                    }
                }
            }
            else if (command == "ping_calculating")
            {
                SendResponse(w, "pong_calculating");
                worker.State = WorkerState.WORKING;

                this.Invoke((MethodInvoker) delegate {
                    listView1.Items[AddedWorkers.FindIndex(x => x == w.ID)].SubItems[1].Text = "Working ... ";
                });
            }
            else if(command.Contains("gotresult"))
            {
                worker.State = WorkerState.FINISHED;

                string[] parameters = command.Split(',');

                this.Invoke((MethodInvoker) delegate {
                    listView1.Items[AddedWorkers.FindIndex(x => x == w.ID)].SubItems[1].Text = "Finished ... ";
                    listView1.Items[AddedWorkers.FindIndex(x => x == w.ID)].SubItems[2].Text = parameters[1] + "," + parameters[2] + ", tile: " + parameters[3];
                });

                SendResponse(w, "quit");
            }
            else if(command == "quit")
            {
                worker.Client.Close();
                Workers.Remove(worker);
            }
        }

        private void SendResponse(Worker worker, string response)
        {
            byte[] resp = System.Text.Encoding.ASCII.GetBytes(response + "\r\n");
            worker.Client.GetStream().Write(resp, 0, resp.Length);
        }
        
        private void Log(string what)
        {
            this.Invoke((MethodInvoker)delegate {
                logger.Text += what + "\r\n";
            });
        }

        private void button1_Click(object sender, EventArgs e)
        {
            if (AliveWorkers.Count >= 12)
                MessageBox.Show("This simple grid demonstration does not allow more than 12 computers on the grid. It's easy to modify, however.");

            int howMuchEveryone = 12 / AliveWorkers.Count;
            int k = 0;

            for(int i = 0; i < AliveWorkers.Count; i++)
            {
                for (int j = 0; j < howMuchEveryone; j++)
                {
                    AliveWorkers[i].WorkQueue.Add(k);
                    k++;
                }

                if(i == AliveWorkers.Count - 1)
                {
                    // Dodaj ostatak
                    for (int j = 0; j < 12-howMuchEveryone * AliveWorkers.Count; j++)
                    {
                        AliveWorkers[i].WorkQueue.Add(k);
                        k++;
                    }
                }
            }

            startWork = true;

            Log("Okay!");

        }
    }

    public class Worker
    {
        public int ID;
        public TcpClient Client;
        public int Usage;
        public WorkerState State;
        public List<int> WorkQueue;

        public Worker(TcpClient c, int id)
        {
            Client = c;
            this.ID = id;
            State = WorkerState.DEAD;
            WorkQueue = new List<int>();
        }
    }


    public enum WorkerState
    {
        DEAD,
        WAITING_FOR_COMMAND,
        WORKING,
        CPU_NOT_FREE,
        FINISHED
    }
}
