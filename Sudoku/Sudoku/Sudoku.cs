using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Data;
using System.Drawing;
using System.Linq;
using System.Net;
using System.Net.Sockets;
using System.Text;
using System.Threading.Tasks;
using System.Windows.Forms;
using System.Runtime.Serialization.Formatters.Binary;
using System.IO;
using System.Threading;

namespace Sudoku
{
    public partial class Sudoku : Form
    {
        public Sudoku()
        {
            InitializeComponent();
            CheckForIllegalCrossThreadCalls = false;
            Connect();
        }


        #region .Variables
        int[,] _Matrix = new int[9,9];

        IPEndPoint _IP;
        Socket _Socket;

        #endregion


        #region .Methods Network

        //Connect to Server
        void Connect()
        {
            _IP = new IPEndPoint(IPAddress.Parse("127.0.0.1"), 9999);
            _Socket = new Socket(AddressFamily.InterNetwork, SocketType.Stream, ProtocolType.IP);

            try
            {
                _Socket.Connect(_IP);
            }
            catch
            {
                MessageBox.Show("Can't connect to Server", "Error", MessageBoxButtons.OK, MessageBoxIcon.Error);
                return;
            }

            Thread listen = new Thread(Recieve);
            listen.IsBackground = true;
            listen.Start();
        }


        void Recieve()
        {
            try
            {
                while (true)
                {
                    byte[] data = new byte[1024 * 5000];
                    _Socket.Receive(data);

                    string message = (string)Deserialize(data);
                    if (message[0].ToString() == "2")
                        Checking(message);
                    else if (message[0].ToString() == "3")
                        ShowSolution(message.Substring(1));
                    else
                        LoadGame(message);
                }
            }
            catch
            {
                _Socket.Close();
            }
        }
        #endregion

        int GetLevel()
        {
            int lv = 3;

            if (cbbLevel.Text == "Easy")
                lv = 3;
            if (cbbLevel.Text == "Medium")
                lv = 5;
            if (cbbLevel.Text == "Hard") 
                lv = 7;

            return lv;
        }

        #region .Methods.En-Decode
        byte[] Serialize(object obj)
        {
            MemoryStream stream = new MemoryStream();
            BinaryFormatter formatter = new BinaryFormatter();

            formatter.Serialize(stream,obj);

            return stream.ToArray();
        }

        object Deserialize(byte[] data)
        {
            MemoryStream stream = new MemoryStream(data);
            BinaryFormatter formatter = new BinaryFormatter();

            return formatter.Deserialize(stream);
        }

        #endregion

        #region .Methods.StartGame
        private void btnStart_Click(object sender, EventArgs e)
        {
            foreach (Control control in tlpPlayZone.Controls)
            {
                TextBox tbBox = control as TextBox;
                tbBox.ForeColor = Color.Empty;
                tbBox.BackColor = Color.Empty;
                tbBox.Text = string.Empty;
            }

            _Socket.Send(Serialize("1" + GetLevel()));

        }

        void LoadGame(string str)
        {
            int t = 0;

            foreach (Control control in tlpPlayZone.Controls)
            {
                TextBox tbBox = control as TextBox;

                _Matrix[tlpPlayZone.GetRow(tbBox), tlpPlayZone.GetColumn(tbBox)] = int.Parse(str[t].ToString());

                if (_Matrix[tlpPlayZone.GetRow(tbBox), tlpPlayZone.GetColumn(tbBox)] != 0)
                {
                    tbBox.Text = _Matrix[tlpPlayZone.GetRow(tbBox), tlpPlayZone.GetColumn(tbBox)].ToString();
                    tbBox.ForeColor = Color.Green;
                    tbBox.BackColor = BackColor;
                }
                else
                    tbBox.Text = string.Empty;

                t++;
            }
        }
        #endregion

        #region .Methods.Checking

        //Get all variables of Playzone add to string and send to Server
        void PreChecking()
        {
            string str = "2";
            int tmp = 0;

            foreach (Control control in tlpPlayZone.Controls)
            {
                TextBox tbBox = control as TextBox;
                if (tbBox.Text == string.Empty)
                    tmp++;
                else
                    str += tbBox.Text;
            }

            if (tmp != 0)
                MessageBox.Show("Please fill all of blank cells !!", "Warning");
            else
                _Socket.Send(Serialize(str));
        }

        //Load result from Server to check 
        void Checking(string str)
        {
            if (str[1].ToString() == "1")
            {
                string tmp = str.Substring(2);
                int t = 0;

                foreach (Control control in tlpPlayZone.Controls)
                {
                    TextBox tbBox = control as TextBox;

                    if (tmp[t].ToString() == "0")
                        tbBox.ForeColor = Color.Red;
                    if (tmp[t].ToString() == "1" && tbBox.ForeColor == Color.Red)
                        tbBox.ForeColor = Color.Black;

                    t++;
                }
                MessageBox.Show("Wrong !!Please try again!", "Warning", MessageBoxButtons.OK, MessageBoxIcon.Error);
            }
            if (str[1].ToString() == "2")
            {
                foreach (Control control in tlpPlayZone.Controls)
                {
                    TextBox tbBox = control as TextBox;
                    if (tbBox.ForeColor == Color.Red)
                        tbBox.ForeColor = Color.Black;
                }
                MessageBox.Show("Congratulations, You Win!", "Message", MessageBoxButtons.OK, MessageBoxIcon.Information);
            }

        }

        #endregion

        #region .Methods.ShowSolution
        private void btnShowSolution_Click(object sender, EventArgs e)
        {
            RequireSolution();
        }
        
        //send the require solution to Server
        void RequireSolution()
        {
            string str = "3";
            foreach (Control control in tlpPlayZone.Controls)
            {
                TextBox tbBox = control as TextBox;
                str += _Matrix[tlpPlayZone.GetRow(tbBox), tlpPlayZone.GetColumn(tbBox)].ToString();
            }
            _Socket.Send(Serialize(str));
        }

        void ShowSolution(string str)
        {
            int i = 0;

            foreach (Control control in tlpPlayZone.Controls)
            {
                TextBox tbBox = control as TextBox;
                if (tbBox.ForeColor == Color.Red)
                    tbBox.ForeColor = Color.Black;
                _Matrix[tlpPlayZone.GetRow(tbBox), tlpPlayZone.GetColumn(tbBox)] = int.Parse(str[i].ToString());
                tbBox.Text = str[i].ToString();
                i++;
            }
        }

        #endregion

      

        private void btnCheck_Click(object sender, EventArgs e)
        {
            PreChecking();
        }

       
        private void textBox1_KeyPress(object sender, KeyPressEventArgs e)
        {
            TextBox tbBox = sender as TextBox;

            if (tbBox.BackColor == BackColor)
            {
                e.Handled = true;
            }
            else
            {
                if ((e.KeyChar >= (char)49 && e.KeyChar <= (char)57) || e.KeyChar == (char)08)
                    e.Handled = false;
                else
                    e.Handled = true;
            }
        }

        private void tlpPlayZone_Paint_1(object sender, PaintEventArgs e)
        {
            e.Graphics.DrawLine(new Pen(Color.Black, 2), 120, 0, 120, 400);
            e.Graphics.DrawLine(new Pen(Color.Black, 2), 245, 0, 245, 400);
            e.Graphics.DrawLine(new Pen(Color.Black, 2), 0, 127, 400, 127);
            e.Graphics.DrawLine(new Pen(Color.Black, 2), 0, 250, 400, 250);
        }
    }
}
