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
using System.Runtime.Serialization.Formatters.Binary;
using System.IO;
using System.Threading;

namespace Server
{
    public partial class Server : Form
    {
        public Server()
        {
            InitializeComponent();
            CheckForIllegalCrossThreadCalls = false;
            Connect();
        }
        #region .Parameters

        int[,] _Matrix = new int[9, 9];
        public static Random _Random = new Random();

        IPEndPoint _IP;
        Socket _Server;
        List<Socket> _ClientList;
        
        #endregion

        #region .Methods.Network
        void Connect()
        {
            _ClientList = new List<Socket>();
            // IP: địa chỉ của server
            _IP = new IPEndPoint(IPAddress.Any, 9999);
            _Server = new Socket(AddressFamily.InterNetwork, SocketType.Stream, ProtocolType.IP);

            _Server.Bind(_IP);

            Thread Listen = new Thread(() =>
            {

                try
                {
                    while (true)
                    {
                        _Server.Listen(100);
                        Socket client = _Server.Accept();
                        _ClientList.Add(client);

                        Thread receive = new Thread(Recieve);
                        receive.IsBackground = true;
                        receive.Start(client);
                    }
                }
                catch
                {
                    _IP = new IPEndPoint(IPAddress.Any, 9999);
                    _Server = new Socket(AddressFamily.InterNetwork, SocketType.Stream, ProtocolType.IP);
                }

            });
            Listen.IsBackground = true;
            Listen.Start();
        }

        void Recieve(object obj)
        {
            Socket _Client = obj as Socket;
            try
            {
                while (true)
                {
                    byte[] data = new byte[1024 * 5000];
                    _Client.Receive(data);

                    string message = (string)Deserialize(data);
                    if (message[0].ToString() == "1")
                        SendNewMatrix(_Client, message);
                    if (message[0].ToString() == "2")
                        SendCheckResult(_Client, message);
                    if (message[0].ToString() == "3")
                        SendSolution(_Client, message);
                   

                }
            }
            catch
            {
                _ClientList.Remove(_Client);
                _Client.Close();
            }
        }

        #endregion

        #region .Methods.Check

        void DetermineRowEdge(int row,ref int min,ref int max)
        {
            int k = row / 3;
            min = k * 3;
            max = min + 2;
        }

        void DetermineColumnEdge(int column,ref int min,ref int max)
        {
            int k = column / 3;
            min = k * 3;
            max = min + 2;
        }

        int CheckColumn(int _Row, int _Column, int _Value)
        {
            for (int i = 1; i < 9; i++)
            {
                if (_Matrix[i, _Column] == _Value && i != _Row)
                {
                    return -1;
                }
            }
            return 1;
        }

        int CheckRow(int _Row, int _Column, int _Value)
        {
            for (int i = 1; i < 9; i++)
            {
                if (_Matrix[i,_Row] == _Value && i != _Column)
                {
                    return -1;
                }
            }
            return 1;
        }

        int CheckBlock(int _Row, int _Column, int _Value) 
        {
            int rMin = -1, rMax = -1, cMin = -1, cMax = -1;

            DetermineColumnEdge(_Column, ref cMin, ref cMax);
            DetermineRowEdge(_Row, ref rMin, ref rMax);

            for (int i = rMin; i < rMax; i++)
            {
                for (int j = cMin; j < cMax; j++)
                {
                    if (_Matrix[i, j] == _Value && (i != _Row || j != _Column)) 
                        return -1; 
                }
            }

            return 1;
        }

        int CheckOverall(int _Row, int _Column, int _Value)
        {
            int tmp = CheckRow(_Row, _Column, _Value);
            if (tmp == -1)
                return -1;
            tmp = CheckColumn(_Row, _Column, _Value);
            if (tmp == -1)
                return -1;
            tmp = CheckBlock(_Row, _Column, _Value);
            if (tmp == -1)
                return -1;
            return 1;
        }

        void SendCheckResult(Socket client, string str)
        {
            string temp;
            temp = str.Substring(1);
            int t = 0;
            int check = 0;
            string result = "";

            for (int i = 0; i < 9; i++)
            {
                for (int j = 0; j < 9; j++)
                {
                    int k = int.Parse(temp[t].ToString());

                    if (CheckOverall(i, j, k) != 1)
                    {
                        check++;
                        result += "0";
                    }
                    else
                        result += "1";
                    t++;
                }
            }

            if (check != 0)
                client.Send(Serialize("21" + result));
            else
                client.Send(Serialize("22"));
        }
        #endregion

        #region .Methods.En-Decode
        //Convert the original object to sequentially string for send, recieve
        byte[] Serialize(object obj)
        {
            MemoryStream _Stream = new MemoryStream();
            BinaryFormatter _Formatter = new BinaryFormatter();

            _Formatter.Serialize(_Stream, obj);
            return _Stream.ToArray();
        }

        //Inverse of Serialize
        object Deserialize(byte[] data)
        {
            MemoryStream _Stream = new MemoryStream();
            BinaryFormatter _Formatter = new BinaryFormatter();

            return _Formatter.Deserialize(_Stream);
        }

        #endregion

        #region .Methods.Solution

        bool FindEmptyCell(int [,] _matrix, ref int _Row, ref int _Column)
        {
            for (_Row = 0; _Row < 9; _Row++)
                for (_Column = 0; _Column < 9; _Column++)
                    if (_matrix[_Row, _Column] == 0)
                        return true;
            return false;
        }

        //Backtrack Recursion
        bool SolveSudoku(int[,] _matrix)
        {
            int row = 0;
            int column = 0;

            if (!FindEmptyCell(_matrix, ref row, ref column))
                return true;

            for (int i = 1; i <= 9; i++) //i = value
            {
                if (CheckOverall(row, column, i) == 1)
                {
                    _matrix[row, column] = i;
                    if (SolveSudoku(_matrix))
                        return true;
                    _matrix[row, column] = 0;
                }
            }
            return false;
        }

        void ReLoadMatrix(string str)
        { 
            int t = 0;
            string temp;
            temp = str.Substring(1);

            for (int i = 0; i < 9; i++)
            {
                for (int j = 0; j < 9; j++)
                {
                    _Matrix[i, j] = int.Parse(temp[t].ToString());
                    t++;
                }
            }
        }

        private void SendSolution(Socket client, string str)
        {
            ReLoadMatrix(str);
            if (SolveSudoku(_Matrix))
            {
                string temp = "r";
                for (int i = 0; i < 9; i++)
                {
                    for (int j = 0; j < 9; j++)
                    {
                        temp += _Matrix[i, j].ToString();
                    }
                }
                client.Send(Serialize(temp));
            }
        }

        #endregion

        #region .Status
        void Status(string str)
        {

            if (str[0].ToString() == "1")
            {
                if (str[1].ToString() == "3")
                    tbStatus.Text += "New Game - Easy" + Environment.NewLine;
                if (str[1].ToString() == "5")
                    tbStatus.Text += "New Game - Medium" + Environment.NewLine;
                if (str[1].ToString() == "7")
                    tbStatus.Text += "New Game - Hard" + Environment.NewLine;
            }
            if (str[0].ToString() == "2")
                tbStatus.Text += "Checking: " + Environment.NewLine + str.Substring(1) + Environment.NewLine;
            if (str[0].ToString() == "3")
                tbStatus.Text += "Resolve: " + Environment.NewLine + str.Substring(1) + Environment.NewLine;
        }
        #endregion
        #region .Methods.CreateMatrix

        void InitRandomCell()
        {
            for (int i = 0; i < 9; i++)
            {
                for (int j = 0; j < 9; j++)
                {
                    int temp = _Random.Next(10);
                    if (temp > 6)
                    {
                        int ramdom = _Random.Next(1, 10);
                        if (CheckOverall(i, j, ramdom) == 1)
                            _Matrix[i, j] = ramdom;
                        else
                            _Matrix[i, j] = 0;
                    }
                    else
                        _Matrix[i, j] = 0;
                }
            }
        }

        void InitMatrix(string message) // use "message" from Client to determine mode + game level 
        {
            InitRandomCell(); 

            if (SolveSudoku(_Matrix))
            {
                for (int i = 0; i < 9; i++)
                {
                    for (int j = 0; j < 9; j++)
                    {
                        int rnd = _Random.Next(10);

                        if (rnd < int.Parse(message[1].ToString()))
                            _Matrix[i, j] = 0;
                    }
                }
            }
            else
                InitMatrix(message);
        }

        // Send matrix that initiated to client when have require
        void SendNewMatrix(Socket client, string message)
        {
            InitMatrix(message);

            string str = "";

            for (int i = 0; i < 9; i++)
            {
                for (int j = 0; j < 9; j++)
                {
                    str += _Matrix[i, j].ToString();
                }
            }
            client.Send(Serialize(str));
        }


        #endregion

        private void Start_Click(object sender, EventArgs e)
        {
            Connect();
        }
    }
}
