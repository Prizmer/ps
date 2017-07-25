using System;
using System.Collections.Generic;
using System.Text;
using System.Collections;
using System.Threading;

using System.IO;
using System.IO.Ports;
using System.Net;
using System.Net.Sockets;

//при переносе в сервер опрос не забыть поменять namespace
namespace elfextendedapp
{
    public delegate int FindPacketSignature(Queue<byte> queue);

    public interface VirtualPort
    {
        int WriteReadData(FindPacketSignature func, byte[] out_buffer, ref byte[] in_buffer, int out_length, int target_in_length, uint pos_count_data_size = 0, uint size_data = 0, uint header_size = 0);
        string GetName();
        bool isOpened();
        SerialPort getSerialPortObject();

        bool OpenPort();
        void ClosePort();
    }

    public class TcpipPort : VirtualPort
    {
        private IPEndPoint remoteIPEndPoint;
        private int m_write_timeout = 600;
        private int m_read_timeout = 600;

        private Socket socket;

        public TcpipPort(string address, int port, int write_timeout=600, int read_timeout=600, int delay = 0)
        {
            IPAddress remoteIP = IPAddress.Parse(address);
            remoteIPEndPoint = new IPEndPoint(remoteIP, port);

            socket = new Socket(AddressFamily.InterNetwork, SocketType.Stream, ProtocolType.Tcp);
            socket.LingerState.Enabled = false;
            socket.LingerState.LingerTime = 0;

     
            m_write_timeout = write_timeout;
            m_read_timeout = read_timeout;
        }

        ~TcpipPort()
        {
            if (socket != null && socket.Connected)
                socket.Disconnect(true);
        }


        public int WriteReadData(FindPacketSignature func, byte[] out_buffer, ref byte[] in_buffer, int out_length, int target_in_length, uint pos_count_data_size = 0, uint size_data = 0, uint header_size = 0)
        {
            if (target_in_length != -1) return 0;
            if (!OpenPort()) return 0;

            socket.Send(out_buffer);
      
            socket.ReceiveTimeout = 1000;
            int elapsedTime = 0;
            List<byte> receivedBytes = new List<byte>();
            while (elapsedTime < m_read_timeout)
            {
                if (socket.Available > 0) { 
                in_buffer = new byte[socket.Available];
                socket.Receive(in_buffer);

                receivedBytes.AddRange(in_buffer);
                in_buffer = receivedBytes.ToArray();


                Queue<byte> tmpQ = new Queue<byte>();
                for (int j = receivedBytes.Count - 1; j >= 0; j--)
                    tmpQ.Enqueue(receivedBytes[j]);

                int packageSign = func(tmpQ);
                if (packageSign == 1) break;

                }
                Thread.Sleep(100);
                elapsedTime += 100;
            }


            return in_buffer.Length;
        }

        public string GetName()
        {
            return "TCP";
        }

        public bool isOpened()
        {
            return socket != null ? socket.Connected : false ;
        }

        public SerialPort getSerialPortObject()
        {
            throw new NotImplementedException();
        }

        public bool OpenPort()
        {
            if (!socket.Connected)
                socket.Connect(remoteIPEndPoint);
            else
                return true;

            return socket.Connected;
        }

        public void ClosePort()
        {
          //  if (socket.Connected) socket.Disconnect(true);
          //  else return;
            Thread.Sleep(5);

            return;
        }
    }

    public class ComPort : VirtualPort 
    {
        SerialPort serialPort;
        int readTimeout = 600;
        int writeTimeout = 600;

        public ComPort(byte number, int baudrate, byte data_bits, byte parity, byte stop_bits, ushort write_timeout, ushort read_timeout, byte attemts)
        {
            serialPort = new SerialPort("COM" + number);


            serialPort.BaudRate = baudrate;
            serialPort.Parity = (Parity)parity;

            serialPort.DataBits = data_bits;
            serialPort.StopBits = (StopBits)stop_bits;

            readTimeout = read_timeout;
            writeTimeout = write_timeout;
        }

        public ComPort(SerialPort sp, byte attempts, ushort read_timeout, ushort write_timeout)
        {
            serialPort = new SerialPort(sp.PortName, sp.BaudRate, sp.Parity, sp.DataBits, sp.StopBits);

            readTimeout = read_timeout;
            writeTimeout = write_timeout;

            serialPort.ReadTimeout = 5000;
            serialPort.WriteTimeout = 1000;
            serialPort.DtrEnable = true;
            serialPort.RtsEnable = true;

        }

        ~ComPort()
        {
            ClosePort();
        }

        public int  WriteReadData(FindPacketSignature func, byte[] out_buffer, ref byte[] in_buffer, int out_length, int target_in_length, uint pos_count_data_size = 0, uint size_data = 0, uint header_size = 0)
        {
            if (!OpenPort()) return 0;

           // Thread.Sleep(10);
            serialPort.Write(out_buffer, 0, out_buffer.Length);

            Thread.Sleep(10);
            if (serialPort.BytesToRead == 0)
                Thread.Sleep(1200);

            int elapsedTime = 0;

            List<byte> inList = new List<byte>();

            while (elapsedTime < readTimeout)
            {
                if (serialPort.BytesToRead > 0)
                {
                    in_buffer = new byte[serialPort.BytesToRead];
                    serialPort.Read(in_buffer, 0, in_buffer.Length);
                    if (in_buffer.Length > 0)
                        inList.AddRange(in_buffer);

                    Queue<byte> tmpQ = new Queue<byte>();

                    for (int j = inList.Count - 1; j >= 0; j--)
                        tmpQ.Enqueue(inList[j]);

                    int packageSign = func(tmpQ);
                    if (packageSign == 1) break;
                }

                in_buffer = inList.ToArray();

                if (in_buffer.Length > 2 && in_buffer[in_buffer.Length - 1] == 0x0a && in_buffer[in_buffer.Length - 2] == 0x0a)
                    break;


                Thread.Sleep(100);
                elapsedTime += 100;
            }


            
            return in_buffer == null ? 0 : in_buffer.Length;
        }

        public string  GetName()
        {
 	       if (serialPort != null)
               return serialPort.PortName;
           else
               return "";
        }
        public bool  isOpened()
        {
 	        if (serialPort != null && serialPort.IsOpen)
                return true;
            else
                return false;
        }
        public SerialPort  getSerialPortObject()
        {
 	        return serialPort;
        }

        public bool  OpenPort()
        {
 	        if (serialPort != null)
            {

                if (!serialPort.IsOpen)
                {
                    serialPort.Open();
                    serialPort.DiscardOutBuffer();
                    serialPort.DiscardInBuffer();

                    if (serialPort.IsOpen)
                    {
                        return true;
                    }
                    else
                    {
                        return false;
                    }
                }
                else
                {
                    serialPort.DiscardOutBuffer();
                    serialPort.DiscardInBuffer();
                    return true;
                }
            }

            return false;
        }
        public void  ClosePort()
        {
 	        if (serialPort != null && serialPort.IsOpen)
            {
                serialPort.Close();
            }
        }
    }
}