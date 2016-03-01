using System;
using System.Collections.Generic;
using System.Linq;
using System.Net.Sockets;
using System.Text;
using System.Threading.Tasks;
using System.Threading;
using System.Text.RegularExpressions;
using System.Security.Cryptography;

namespace APSIM.MSPlugins.EventServer
{
    class SocketClient
    {
        private NetworkStream stream;
        private Thread th;

        public SocketClient(TcpClient _client)
        {
            stream = _client.GetStream();
            th = new Thread(() =>
                {
                    while (true)
                    {
                        // waiting for a data from socket client
                        try
                        {
                            while (!stream.DataAvailable) { 
                                Thread.Sleep(500);
                                if (!_client.Connected)
                                {
                                    SocketServer.DisconnectClient(this);
                                    return;
                                }
                            }
                        }
                        catch
                        {
                            th.Abort();
                            return;
                        }
                        Byte[] bytes = new Byte[_client.Available];
                        stream.Read(bytes, 0, bytes.Length);
                        //translate bytes of request to string
                        String data = Encoding.UTF8.GetString(bytes); // TODO change header parsing method because twice string translation

                        // check if header
                        if (new Regex("^GET").IsMatch(data))
                        {
                            // upgrade from http connection to websockets
                            Byte[] response = Encoding.UTF8.GetBytes("HTTP/1.1 101 Switching Protocols" + Environment.NewLine
                                + "Connection: Upgrade" + Environment.NewLine
                                + "Upgrade: websocket" + Environment.NewLine
                                + "Sec-WebSocket-Accept: " + Convert.ToBase64String(
                                    SHA1.Create().ComputeHash(
                                        Encoding.UTF8.GetBytes(
                                            new Regex("Sec-WebSocket-Key: (.*)").Match(data).Groups[1].Value.Trim() + "258EAFA5-E914-47DA-95CA-C5AB0DC85B11"
                                        )
                                    )
                                ) + Environment.NewLine
                                + Environment.NewLine);
                            stream.Write(response, 0, response.Length);
                        }
                        else
                        {
                            // in no header decode string
                            data = SocketClient.GetDecodedData(bytes, bytes.Length);
                            CommandQueue<Cmd>.Instance.Enqueue(new Cmd() { Name = "event", Args = data.Split(new char[0]), Stream = stream });
                            Send("OK");
                        }
                    }
                });
            th.Start();
        }

        public void Disconnect()
        {
            stream.Close();
 //           th.Join();
        }

        public void Send(string message)
        {
            Byte[] response = EncodeData(Encoding.UTF8.GetBytes(message));
            stream.Write(response, 0, response.Length);
            stream.Flush();
        }

        public void Send(byte[] message)
        {
            try
            {
                stream.Write(message, 0, message.Length);
                stream.Flush();
            }
            catch { }
        }
        public static string GetDecodedData(byte[] buffer, int length)
        {
            byte b = buffer[1];
            int dataLength = 0;
            int totalLength = 0;
            int keyIndex = 0;

            if (b - 128 <= 125)
            {
                dataLength = b - 128;
                keyIndex = 2;
                totalLength = dataLength + 6;
            }

            if (b - 128 == 126)
            {
                dataLength = BitConverter.ToInt16(new byte[] { buffer[3], buffer[2] }, 0);
                keyIndex = 4;
                totalLength = dataLength + 8;
            }

            if (b - 128 == 127)
            {
                dataLength = (int)BitConverter.ToInt64(new byte[] { buffer[9], buffer[8], buffer[7], buffer[6], buffer[5], buffer[4], buffer[3], buffer[2] }, 0);
                keyIndex = 10;
                totalLength = dataLength + 14;
            }

            if (totalLength > length)
                throw new Exception("The buffer length is small than the data length");

            byte[] key = new byte[] { buffer[keyIndex], buffer[keyIndex + 1], buffer[keyIndex + 2], buffer[keyIndex + 3] };

            int dataIndex = keyIndex + 4;
            int count = 0;
            for (int i = dataIndex; i < totalLength; i++)
            {
                buffer[i] = (byte)(buffer[i] ^ key[count % 4]);
                count++;
            }

            return Encoding.ASCII.GetString(buffer, dataIndex, dataLength);
        }

        public static byte[] EncodeData(byte[] buffer)
        {
            byte[] lb = new byte[buffer.Length+2];
            lb[0] = 0x81;
            lb[1] = Convert.ToByte(buffer.Length);
            System.Buffer.BlockCopy(buffer,0,lb,2,buffer.Length);
            return lb;
        }
 
    }
}
