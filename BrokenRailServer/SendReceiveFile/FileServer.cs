﻿using BrokenRailMonitorViaWiFi;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Net;
using System.Net.Sockets;
using System.Text;
using System.Threading;
using System.Threading.Tasks;

namespace BrokenRailServer.SendReceiveFile
{
    public class FileServer
    {
        private TcpClient client;
        private NetworkStream streamToClient;
        private const int BufferSize = 1024;
        private byte[] buffer;
        private ProtocolHandler handler;
        public delegate void ShowMessageEventHandler(string str, DataLevel level);
        public event ShowMessageEventHandler ShowMessage;
        public delegate void FreshDevicesEventHandler();
        public event FreshDevicesEventHandler FreshDevices;
        public delegate void BroadcastEventHandler(byte dataType, byte[] dataContent);
        public event BroadcastEventHandler Broadcast;
        public TcpListener Listener { get; set; }


        public FileServer(TcpClient client, ShowMessageEventHandler showMsg)
        {
            this.client = client;

            //打印连接到的客户端信息  
            ShowMessage += showMsg;
            ShowMessage?.Invoke(string.Format("ClientConnected! Local :{0}<--Client: {1}", client.Client.LocalEndPoint, client.Client.RemoteEndPoint), DataLevel.Normal);

            //获得流  
            streamToClient = client.GetStream();
            buffer = new byte[BufferSize];

            handler = new ProtocolHandler();

        }

        //开始进行读取  
        public void BeginRead()
        {
            AsyncCallback callBack = new AsyncCallback(OnReadComplete);
            streamToClient.BeginRead(buffer, 0, BufferSize, callBack, null);
        }
        int totalBytes = 0;
        //读取完成时进行回调  
        private void OnReadComplete(IAsyncResult ar)
        {
            int bytesRead = 0;
            try
            {
                bytesRead = streamToClient.EndRead(ar);
                System.Windows.Application.Current.Dispatcher.Invoke(new Action(() =>
                {
                    if (bytesRead != 0)
                    {
                        ShowMessage?.Invoke(string.Format("Reading data,{0} bytes...", bytesRead), DataLevel.Default);
                    }
                }));
                //连接已断开
                if (bytesRead == 0)
                {
                    System.Windows.Application.Current.Dispatcher.Invoke(new Action(() =>
                    {
                        ShowMessage?.Invoke("Client offline.", DataLevel.Error);
                        FreshDevices?.Invoke();
                        long configFileSize = GetFileSize(AppDomain.CurrentDomain.BaseDirectory + "\\config.xml");
                        ShowMessage?.Invoke("配置文件大小为" + configFileSize, DataLevel.Default);
                        broadcastConfigFileSize(configFileSize);
                    }));
                    client.Close();
                    if (Listener != null)
                    {
                        Listener.Stop();
                    }
                    totalBytes = 0;
                    return;
                }

                string path = Environment.CurrentDirectory + "//config.xml";
                if (totalBytes == 0)
                {
                    if (File.Exists(path))
                    {
                        File.Delete(path);
                    }
                }

                string msg = Encoding.UTF8.GetString(buffer, 0, bytesRead);

                //从缓存Buffer中读入到文件流中  
                FileStream fs = null;
                if (totalBytes == 0)
                {
                    fs = new FileStream(path, FileMode.CreateNew, FileAccess.Write);
                }
                else
                {
                    fs = new FileStream(path, FileMode.Append, FileAccess.Write);
                }
                //do
                //{
                //bytesRead = streamToCLient.Read(buffer, 0, BufferSize);

                fs.Write(buffer, 0, bytesRead);
                totalBytes += bytesRead;
                System.Windows.Application.Current.Dispatcher.Invoke(new Action(() =>
                {
                    ShowMessage?.Invoke(string.Format("Receiving {0}", msg), DataLevel.Default);
                    ShowMessage?.Invoke(string.Format("Receiving {0} bytes ...", totalBytes), DataLevel.Default);
                }));
                //} while (bytesRead > 0);

                Array.Clear(buffer, 0, buffer.Length);//清空缓存,避免脏读  
                fs.Dispose();

                //获取protocol数组  
                //string[] protocolArray = handler.GetProtocol(msg);
                //foreach (string pro in protocolArray)
                //{
                //这里异步调用,不然这里会比较耗时  
                //ThreadStart start = new ThreadStart(receiveFile);
                //start.BeginInvoke(null, null);
                //}
                //再次调用BeginRead(),完成时调用自身,形成无限循环  
                AsyncCallback callBack = new AsyncCallback(OnReadComplete);
                streamToClient.BeginRead(buffer, 0, BufferSize, callBack, null);
            }
            catch (Exception ex)
            {
                if (streamToClient != null)
                {
                    streamToClient.Dispose();
                }
                client.Close();
                System.Windows.Application.Current.Dispatcher.Invoke(new Action(() =>
                {
                    ShowMessage?.Invoke(ex.Message, DataLevel.Error);
                }));
            }
        }
        //处理protocol  
        private void handleProtocol(object obj)
        {
            string pro = obj as string;
            ProtocolHelper helper = new ProtocolHelper(pro);
            FileProtocol protocol = helper.GetProtocol();

            if (protocol.Mode == FileRequestMode.Upload)
            {
                //客户端发送文件,对服务端来说则是接收文件  
                receiveFile();
            }
            //else if (protocol.Mode == FileRequestMode.Download)
            //{
            //客户端接收文件,对服务端来说是发送文件  
            //sendFile(protocol);  
            //}
        }
        //接收文件  
        private void receiveFile()
        {
            //获取远程客户端的位置  
            //IPEndPoint endPoint = client.Client.RemoteEndPoint as IPEndPoint;
            //IPAddress ip = endPoint.Address;

            //使用新端口,获得远程用于接收文件的端口  
            //endPoint = new IPEndPoint(ip, protocol.Port);

            //连接到远程客户端  
            //TcpClient localClient;
            try
            {
                //获取发送文件的流  
                NetworkStream streamToCLient = client.GetStream();

                //随机生成一个在当前目录下的文件名称  
                string path = Environment.CurrentDirectory + "//config.xml";
                if (File.Exists(path))
                {
                    File.Delete(path);
                }

                //byte[] fileBuffer = new byte[1024];//每次收1KB  
                FileStream fs = new FileStream(path, FileMode.CreateNew, FileAccess.Write);

                //从缓存Buffer中读入到文件流中  
                int bytesRead;
                int totalBytes = 0;
                do
                {
                    bytesRead = streamToCLient.Read(buffer, 0, BufferSize);

                    fs.Write(buffer, 0, bytesRead);
                    totalBytes += bytesRead;

                    System.Windows.Application.Current.Dispatcher.Invoke(new Action(() =>
                    {
                        ShowMessage?.Invoke(string.Format("Receiving {0} bytes ...", totalBytes), DataLevel.Default);
                    }));
                } while (bytesRead > 0);

                System.Windows.Application.Current.Dispatcher.Invoke(new Action(() =>
                {
                    ShowMessage?.Invoke(string.Format("Total {0} bytes received,Done! ", totalBytes), DataLevel.Default);
                }));

                streamToClient.Dispose();
                fs.Dispose();
                client.Close();
            }
            catch (Exception ex)
            {
                System.Windows.Application.Current.Dispatcher.Invoke(new Action(() =>
                {
                    ShowMessage?.Invoke(ex.Message, DataLevel.Error);
                }));
            }


        }
        //随机获取一个图片名称  
        private string generateFileName(string fileName)
        {
            DateTime now = DateTime.Now;
            return string.Format("{0}_{1}_{2}_{3}", now.Minute, now.Second, now.Millisecond, fileName);
        }

        //发送文件--异步方法  
        public void BeginSendFile(string filePath)
        {
            ParameterizedThreadStart start = new ParameterizedThreadStart(BeginSendFile);
            start.BeginInvoke(filePath, null, null);
        }

        private void BeginSendFile(object obj)
        {
            string filePath = obj as string;
            SendFile(filePath);
        }
        //发送文件  
        public void SendFile(string filePath)
        {
            //获取本地监听的端口号  
            IPEndPoint endPoint = client.Client.LocalEndPoint as IPEndPoint;
            int listeningPort = endPoint.Port;

            //获取发送的协议字符串
            //string fileName = Path.GetFileName(filePath);
            //FileProtocol protocol = new FileProtocol(FileRequestMode.Download, listeningPort, fileName);
            //string pro = protocol.ToString();

            //SendMessage(pro);//发送协议到客户端。

            ShowMessage?.Invoke("Start sending file...", DataLevel.Default);
            NetworkStream stream = client.GetStream();

            //创建文件流  
            FileStream fs = new FileStream(filePath, FileMode.Open, FileAccess.Read);

            byte[] fileBuffer = new byte[1024];
            //每次传输1KB  

            int bytesRead;
            int totalBytes = 0;

            //将文件流转写入网络流  
            try
            {
                do
                {
                    //Thread.Sleep(10);//模拟远程传输视觉效果,暂停10秒  
                    bytesRead = fs.Read(fileBuffer, 0, fileBuffer.Length);
                    stream.Write(fileBuffer, 0, bytesRead);
                    totalBytes += bytesRead;
                } while (bytesRead > 0);
                ShowMessage?.Invoke(string.Format("Total {0} bytes sent ,Done!", totalBytes), DataLevel.Error);
            }
            catch (Exception ex)
            {
                ShowMessage?.Invoke(ex.Message, DataLevel.Error);
            }
            finally
            {
                stream.Dispose();
                fs.Dispose();
                client.Close();
                if (Listener != null)
                {
                    Listener.Stop();
                }
                //listener.Stop();
            }
        }

        //发送消息到服务器  
        public void SendMessage(string msg)
        {
            byte[] temp = Encoding.UTF8.GetBytes(msg);//获得缓存  
            try
            {
                streamToClient.Write(temp, 0, temp.Length);
            }
            catch (Exception ex)
            {
                ShowMessage?.Invoke(ex.Message, DataLevel.Error);
            }
        }

        /// <summary>
        /// 获取文件大小
        /// </summary>
        /// <param name="sFullName"></param>
        /// <returns></returns>
        public static long GetFileSize(string sFullName)
        {
            long lSize = 0;
            if (File.Exists(sFullName))
                lSize = new FileInfo(sFullName).Length;
            return lSize;
        }

        private void broadcastConfigFileSize(long size)
        {
            byte[] bytesSize = new byte[3];
            bytesSize[0] = (byte)((size & 0xff0000) >> 16);
            bytesSize[1] = (byte)((size & 0xff00) >> 8);
            bytesSize[2] = (byte)(size & 0xff);
            Broadcast?.Invoke((byte)CommandType.BroadcastConfigFileSize, bytesSize);
        }
    }
}
