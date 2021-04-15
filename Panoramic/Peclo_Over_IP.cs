
using System;
using System.Collections.Generic;
using System.Linq;
using System.Net;
using System.Net.NetworkInformation;
using System.Net.Sockets;
using System.Text;
using System.Threading.Tasks;
using System.Windows.Forms;
using System.Net.NetworkInformation;
using System.Timers;
using System.Threading;
using System.Collections;
using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Data;
using System.Drawing;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows.Forms;
using System.Net.Http;
using System.Net;
using System.IO;
using System.Xml;
using System.Net.NetworkInformation;
using System.Threading;
using System.Diagnostics;


namespace Panoramic
{
  class Peclo_Over_IP
  {
    public static IPAddress serverAddr = null;
    public static Socket sock = new Socket(AddressFamily.Unspecified, SocketType.Stream, ProtocolType.Tcp);
    public static IPEndPoint endPoint = new IPEndPoint(0, 0);



    public static async Task Connect(string ipAdr, string port)
    {
      if (sock.Connected)
      {
        CloseSock();
      }

      int.TryParse(port, out int checkedPort);
      //if (!PingAdr(ipAdr, checkedPort).Result)
      //{
      //    return;
      //}

      try
      {
        serverAddr = IPAddress.Parse(ipAdr);
        sock = new Socket(serverAddr.AddressFamily, SocketType.Stream, ProtocolType.Tcp);
        endPoint = new IPEndPoint(serverAddr, checkedPort);
        sock.Connect(endPoint);

      }
      catch (Exception e)
      {

      }
    }
    public static async Task<string> GetResponseManual(byte[] code)
    {


      if (!sock.Connected)
      {
        Console.WriteLine("Not connected manual");
        return null;
      }

      try
      {
        byte[] buffer = new byte[30];
        Receive(sock, buffer, 0, buffer.Length, 200);
        SendToSocket(code, true);

        string msg = "";
        for (int i = 0; i < buffer.Length; i++)
        {
          msg += MathStuff.ByteToHex(buffer[i]) + " ";
        }

        if (msg == "")
        {
          msg = "Couldn't get a response";
        }

        return msg;
      }
      catch (Exception e)
      {
        // MessageBox.Show(e.ToString());
        return null;
      }
    }
    public static string ByteToHex(byte msg)
    {
      string hex = msg.ToString("X");
      if (hex.ToArray().Length == 1)
      {
        hex = "0" + hex;
      }
      return hex;
    }

    //public static async Task<string> GetResponse(Com c)
    //{
    //    bool b = SendToSocket(c.sendCommand, true).Result;


    //    if (!sock.Connected)
    //    {
    //        Console.WriteLine("Not connected");
    //        return null;
    //    }

    //    try
    //    {
    //        byte[] buffer = new byte[c.length];
    //        Receive(sock, buffer, 0, buffer.Length, 1000);
    //        string msg = "";
    //        for (int i = 0; i < buffer.Length; i++)
    //        {
    //            msg += ByteToHex(buffer[i]) + " ";
    //        }

    //        if (msg == "")
    //        {
    //            msg = "Couldn't get a response";
    //        }
    //        Console.WriteLine(msg);


    //        return msg;
    //    }
    //    catch (Exception e)
    //    {
    //        MessageBox.Show(e.ToString());
    //        return null;
    //    }
    //}

    public static async Task<bool> SendToSocket(byte[] code, bool noUpdate = false)
    {
      if (!sock.Connected)
      {

        return false;
      }

      if (code != null)
      {
        sock.SendTo(code, endPoint);
        if (!noUpdate)
        {
        }
        return true;
      }
      return false;
    }
    public static void CloseSock()
    {
      if (sock != null && sock.Connected)
      {
        sock.Shutdown(SocketShutdown.Both);
        sock.Close();
      }
    }
    private static async Task Receive(Socket socket, byte[] buffer, int offset, int size, int timeout)
    {
      int startTickCount = Environment.TickCount;
      int received = 0;  // how many bytes is already received

      while (received < size)
      {
        if (Environment.TickCount > startTickCount + timeout)
          throw new Exception("Timeout.");
        try
        {
          sock.ReceiveTimeout = timeout;
          received += socket.Receive(buffer, offset + received, size - received, SocketFlags.None);
        }
        catch (SocketException ex)
        {
          if (ex.SocketErrorCode == SocketError.WouldBlock ||
              ex.SocketErrorCode == SocketError.IOPending ||
              ex.SocketErrorCode == SocketError.NoBufferSpaceAvailable)
          {
            await Task.Delay(30);// socket buffer is probably empty, wait and try again
          }
          else
            throw ex;  // any serious error occurr
        }
      }



    }

    public static string Pelco_Query(string querytype, string IP, string Port)
    {
      byte[] code = new byte[6];

      if (querytype == "POST")
      {
        code = new byte[] { 0xFF, 0x01, 0x07, 0x6B, 0x00, 0x00, 0x73 };
      }
      Peclo_Over_IP.Connect(IP, Port);

      bool validreply = false;
      string response = "";


      int count = 0;

      while (validreply == false && count < 400)
      {


        response = Peclo_Over_IP.GetResponseManual(code).Result;
        if (response != null)
        {

          string[] responsearray = response.Split(' ');


          for (int i = 0; i < responsearray.Length - 6; i++)
          {
            if (responsearray[i] == "FF" && responsearray[i + 1] == "01" && responsearray[i + 2] == "07" && responsearray[i + 3] == "6D")
            {
              try
              {
                byte address = Convert.ToByte(Convert.ToInt32(responsearray[i + 1], 16));
                byte command1 = Convert.ToByte(Convert.ToInt32(responsearray[i + 2], 16));
                byte command2 = Convert.ToByte(Convert.ToInt32(responsearray[i + 3], 16));
                byte data1 = Convert.ToByte(Convert.ToInt32(responsearray[i + 4], 16));
                byte data2 = Convert.ToByte(Convert.ToInt32(responsearray[i + 5], 16));
                byte returnedchecksum = Convert.ToByte(Convert.ToInt32(responsearray[i + 6], 16));

                byte CalculatedCheckSum = (byte)((address + command1 + command2 + data1 + data2) % 256);

                if (returnedchecksum == CalculatedCheckSum && response.StartsWith("00 00") == false)
                {
                  response = "FF " +
                      responsearray[i + 1] + " " +
                      responsearray[i + 2] + " " +
                      responsearray[i + 3] + " " +
                      responsearray[i + 4] + " " +
                      responsearray[i + 5] + " " +
                      responsearray[i + 6];

                  validreply = true;
                  break;
                }
              }
              catch (Exception g)
              {

              }

            }
          }



        }


        Thread.Sleep(1000);
        count++;
      }
      return response;

    }
    public static int Pan_Query(string querytype, string IP, string Port)
    {
      byte[] code = new byte[6];

      if (querytype == "Pan")
      {
        code = new byte[] { 0xFF, 0x01, 0x00, 0x51, 0x00, 0x00, 0x52 };
      }

      Peclo_Over_IP.Connect(IP, Port);
      bool validreply = false;
      string response = "";
      int panreply = 0;

      int count = 0;

      while (validreply == false && count < 200)
      {
        response = Peclo_Over_IP.GetResponseManual(code).Result;
        if (response != null)
        {

          string[] responsearray = response.Split(' ');


          for (int i = 0; i < responsearray.Length - 6; i++)
          {
            if (responsearray[i] == "FF" && responsearray[i + 1] == "01" && responsearray[i + 2] == "00" && responsearray[i + 3] == "59")
            {
              try
              {
                byte address = Convert.ToByte(Convert.ToInt32(responsearray[i + 1], 16));
                byte command1 = Convert.ToByte(Convert.ToInt32(responsearray[i + 2], 16));
                byte command2 = Convert.ToByte(Convert.ToInt32(responsearray[i + 3], 16));
                byte data1 = Convert.ToByte(Convert.ToInt32(responsearray[i + 4], 16));
                byte data2 = Convert.ToByte(Convert.ToInt32(responsearray[i + 5], 16));
                byte returnedchecksum = Convert.ToByte(Convert.ToInt32(responsearray[i + 6], 16));

                byte CalculatedCheckSum = (byte)((address + command1 + command2 + data1 + data2) % 256);

                if (returnedchecksum == CalculatedCheckSum && response.StartsWith("00 00") == false)
                {
                  response = "FF " +
                      responsearray[i + 1] + " " +
                      responsearray[i + 2] + " " +
                      responsearray[i + 3] + " " +
                      responsearray[i + 4] + " " +
                      responsearray[i + 5] + " " +
                      responsearray[i + 6];

                  string data = responsearray[i + 4] + responsearray[i + 5];
                  panreply = Convert.ToInt32(data, 16);
                  validreply = true;
                  break;
                }
              }
              catch (Exception g)
              {

              }

            }
          }



        }


        Thread.Sleep(200);
        count++;
      }
      return panreply;

    }
    public static int Tilt_Query(string querytype, string IP, string Port)
    {
      byte[] code = new byte[6];

      if (querytype == "Tilt")
      {
        code = new byte[] { 0xFF, 0x01, 0x00, 0x53, 0x00, 0x00, 0x54 };
      }

      Peclo_Over_IP.Connect(IP, Port);
      bool validreply = false;
      string response = "";
      int tiltreply = 0;

      int count = 0;

      while (validreply == false && count < 200)
      {
        response = Peclo_Over_IP.GetResponseManual(code).Result;
        if (response != null)
        {

          string[] responsearray = response.Split(' ');


          for (int i = 0; i < responsearray.Length - 6; i++)
          {
            if (responsearray[i] == "FF" && responsearray[i + 1] == "01" && responsearray[i + 2] == "00" && responsearray[i + 3] == "5B")
            {
              try
              {
                byte address = Convert.ToByte(Convert.ToInt32(responsearray[i + 1], 16));
                byte command1 = Convert.ToByte(Convert.ToInt32(responsearray[i + 2], 16));
                byte command2 = Convert.ToByte(Convert.ToInt32(responsearray[i + 3], 16));
                byte data1 = Convert.ToByte(Convert.ToInt32(responsearray[i + 4], 16));
                byte data2 = Convert.ToByte(Convert.ToInt32(responsearray[i + 5], 16));
                byte returnedchecksum = Convert.ToByte(Convert.ToInt32(responsearray[i + 6], 16));

                byte CalculatedCheckSum = (byte)((address + command1 + command2 + data1 + data2) % 256);

                if (returnedchecksum == CalculatedCheckSum && response.StartsWith("00 00") == false)
                {
                  response = "FF " +
                      responsearray[i + 1] + " " +
                      responsearray[i + 2] + " " +
                      responsearray[i + 3] + " " +
                      responsearray[i + 4] + " " +
                      responsearray[i + 5] + " " +
                      responsearray[i + 6];

                  string data = responsearray[i + 4] + responsearray[i + 5];
                  tiltreply = Convert.ToInt32(data, 16);
                  validreply = true;
                  break;
                }
              }
              catch (Exception g)
              {

              }


            }
          }



        }


        Thread.Sleep(200);
        count++;
      }
      return tiltreply;

    }

    public static int Zoom_Query(uint address, string IP, string Port)
    {
      byte[] code = new byte[6];
      byte pelcoaddress = 0x01;

      if (address == 1)
      {
        pelcoaddress = 0x01;
      }
      else if (address == 2)
      {
        pelcoaddress = 0x02;
      }
      else if (address == 3)
      {
        pelcoaddress = 0x03;
      }


      byte command1 = 0x00;
      byte command2 = 0x55;
      byte data1 = 0x00;
      byte data2 = 0x00;
      byte CalculatedCheckSum = (byte)((address + command1 + command2 + data1 + data2) % 256);

      code = new byte[] { 0xFF, pelcoaddress, command1, command2, data1, data2, CalculatedCheckSum };




      Peclo_Over_IP.Connect(IP, Port);
      bool validreply = false;
      string response = "";
      int zoomreply = 0;

      int count = 0;

      while (validreply == false && count < 200)
      {
        response = Peclo_Over_IP.GetResponseManual(code).Result;
        if (response != null)
        {

          string[] responsearray = response.Split(' ');


          for (int i = 0; i < responsearray.Length - 6; i++)
          {
            if (responsearray[i] == "FF" && responsearray[i + 2] == "00" && responsearray[i + 3] == "5D")
            {
              try
              {
                address = Convert.ToByte(Convert.ToInt32(responsearray[i + 1], 16));
                command1 = Convert.ToByte(Convert.ToInt32(responsearray[i + 2], 16));
                command2 = Convert.ToByte(Convert.ToInt32(responsearray[i + 3], 16));
                data1 = Convert.ToByte(Convert.ToInt32(responsearray[i + 4], 16));
                data2 = Convert.ToByte(Convert.ToInt32(responsearray[i + 5], 16));
                byte returnedchecksum = Convert.ToByte(Convert.ToInt32(responsearray[i + 6], 16));

                CalculatedCheckSum = (byte)((address + command1 + command2 + data1 + data2) % 256);

                if (returnedchecksum == CalculatedCheckSum && response.StartsWith("00 00") == false)
                {
                  response = "FF " +
                      responsearray[i + 1] + " " +
                      responsearray[i + 2] + " " +
                      responsearray[i + 3] + " " +
                      responsearray[i + 4] + " " +
                      responsearray[i + 5] + " " +
                      responsearray[i + 6];

                  string data = responsearray[i + 4] + responsearray[i + 5];
                  zoomreply = Convert.ToInt32(data, 16);
                  validreply = true;
                  break;
                }
              }
              catch (Exception g)
              {

              }

            }
          }



        }


        Thread.Sleep(200);
        count++;
      }
      return zoomreply;

    }
    public static int Focus_Query(uint address, string IP, string Port)
    {
      byte[] code = new byte[6];
      byte pelcoaddress = 0x01;

      if (address == 1)
      {
        pelcoaddress = 0x01;
      }
      else if (address == 2)
      {
        pelcoaddress = 0x02;
      }
      else if (address == 3)
      {
        pelcoaddress = 0x03;
      }


      byte command1 = 0x01;
      byte command2 = 0x55;
      byte data1 = 0x00;
      byte data2 = 0x00;
      byte CalculatedCheckSum = (byte)((address + command1 + command2 + data1 + data2) % 256);

      code = new byte[] { 0xFF, pelcoaddress, command1, command2, data1, data2, CalculatedCheckSum };




      Peclo_Over_IP.Connect(IP, Port);
      bool validreply = false;
      string response = "";
      int zoomreply = 0;

      int count = 0;

      while (validreply == false && count < 200)
      {
        response = Peclo_Over_IP.GetResponseManual(code).Result;
        if (response != null)
        {

          string[] responsearray = response.Split(' ');


          for (int i = 0; i < responsearray.Length - 6; i++)
          {
            if (responsearray[i] == "FF" && responsearray[i + 2] == "01" && responsearray[i + 3] == "5D")
            {
              try
              {
                address = Convert.ToByte(Convert.ToInt32(responsearray[i + 1], 16));
                command1 = Convert.ToByte(Convert.ToInt32(responsearray[i + 2], 16));
                command2 = Convert.ToByte(Convert.ToInt32(responsearray[i + 3], 16));
                data1 = Convert.ToByte(Convert.ToInt32(responsearray[i + 4], 16));
                data2 = Convert.ToByte(Convert.ToInt32(responsearray[i + 5], 16));
                byte returnedchecksum = Convert.ToByte(Convert.ToInt32(responsearray[i + 6], 16));

                CalculatedCheckSum = (byte)((address + command1 + command2 + data1 + data2) % 256);

                if (returnedchecksum == CalculatedCheckSum && response.StartsWith("00 00") == false)
                {
                  response = "FF " +
                      responsearray[i + 1] + " " +
                      responsearray[i + 2] + " " +
                      responsearray[i + 3] + " " +
                      responsearray[i + 4] + " " +
                      responsearray[i + 5] + " " +
                      responsearray[i + 6];

                  string data = responsearray[i + 4] + responsearray[i + 5];
                  zoomreply = Convert.ToInt32(data, 16);
                  validreply = true;
                  break;
                }
              }
              catch (Exception g)
              {

              }

            }
          }



        }


        Thread.Sleep(200);
        count++;
      }
      return zoomreply;

    }
    public static string Pelco_Mode_Query(string querytype, string IP, string Port)
    {
      byte[] code = new byte[6];

      if (querytype == "Pelco Mode")
      {
        code = new byte[] { 0xFF, 0x01, 0x03, 0x6B, 0x00, 0x00, 0x6F };
      }

      Peclo_Over_IP.Connect(IP, Port);
      bool validreply = false;
      string response = "";


      int count = 0;

      while (validreply == false && count < 200)
      {
        response = Peclo_Over_IP.GetResponseManual(code).Result;
        if (response != null)
        {

          string[] responsearray = response.Split(' ');


          for (int i = 0; i < responsearray.Length - 6; i++)
          {
            if (responsearray[i] == "FF" && responsearray[i + 1] == "01" && responsearray[i + 2] == "03" && responsearray[i + 3] == "6D")
            {
              try
              {
                byte address = Convert.ToByte(Convert.ToInt32(responsearray[i + 1], 16));
                byte command1 = Convert.ToByte(Convert.ToInt32(responsearray[i + 2], 16));
                byte command2 = Convert.ToByte(Convert.ToInt32(responsearray[i + 3], 16));
                byte data1 = Convert.ToByte(Convert.ToInt32(responsearray[i + 4], 16));
                byte data2 = Convert.ToByte(Convert.ToInt32(responsearray[i + 5], 16));
                byte returnedchecksum = Convert.ToByte(Convert.ToInt32(responsearray[i + 6], 16));

                byte CalculatedCheckSum = (byte)((address + command1 + command2 + data1 + data2) % 256);

                if (returnedchecksum == CalculatedCheckSum && response.StartsWith("00 00") == false)
                {

                  string modedata = responsearray[i + 4] + responsearray[i + 5];
                  int modedata_int = Convert.ToInt32(modedata, 16);

                  BitArray b = new BitArray(new int[] { modedata_int });

                  string binary = Convert.ToString(modedata_int, 2);

                  if (b[10] == false && b[11] == false)
                  {
                    response = "Traditional";
                    return response;
                    break;

                  }
                  else if (b[10] == true && b[11] == false)
                  {
                    response = "Strict";
                    return response;
                    break;

                  }
                  else if (b[10] == false && b[11] == true)
                  {
                    response = "RevTilt";
                    return response;
                    break;

                  }



                  response = "";
                  validreply = true;
                  break;
                }

              }
              catch (Exception g)
              {

              }

            }
          }



        }


        Thread.Sleep(200);
        count++;
      }
      return response;

    }
    public static void SendCommand(byte[] command, string IP, string Port)
    {

      Peclo_Over_IP.Connect(IP, Port);
      SendToSocket(command, true);


    }

    public static void Soft_Reset(string querytype, string IP, string Port)
    {
      byte[] code = new byte[6];

      if (querytype == "Reset")
      {
        code = new byte[] { 0xFF, 0x01, 0x00, 0x0F, 0x00, 0x00, 0x10 };
      }

      Peclo_Over_IP.Connect(IP, Port);
      SendCommand(code, IP, Port);
      Thread.Sleep(200);




    }


    public static async Task<List<string>> Stress_Test(byte[] code, string IP, string Port, int cycles, int waittime)
    {


      List<byte[]> commandlist = new List<byte[]>();


      Stopwatch stopwatch = new Stopwatch();

      //Peclo_Over_IP.Connect(IP, Port);
      AsyncCamCom.Connect(IP, Port);
      bool validreply = false;
      string response = "";
      int tiltreply = 0;
      string conditioned_response = "";
      List<string> results = new List<string>();

      int count = 0;

      while (validreply == false && count < 5)
      {
        stopwatch.Reset();
        stopwatch.Start();
        //response = Peclo_Over_IP.GetResponseManual(code).Result;
        response = await AsyncCamCom.QueryNewCommand(code).ConfigureAwait(false);

        stopwatch.Stop();

        if (response == null)
        {
          response = "null";
        }


        string[] responsearray = response.Split(' ');

        if (responsearray[0] == "FF" && responsearray[1] == "01")
        {
          byte address = Convert.ToByte(Convert.ToInt32(responsearray[1], 16));
          byte command1 = Convert.ToByte(Convert.ToInt32(responsearray[2], 16));
          byte command2 = Convert.ToByte(Convert.ToInt32(responsearray[3], 16));
          byte data1 = Convert.ToByte(Convert.ToInt32(responsearray[4], 16));
          byte data2 = Convert.ToByte(Convert.ToInt32(responsearray[5], 16));
          byte returnedchecksum = Convert.ToByte(Convert.ToInt32(responsearray[6], 16));

          byte CalculatedCheckSum = (byte)((address + command1 + command2 + data1 + data2) % 256);


          if (returnedchecksum == CalculatedCheckSum)
          {
            conditioned_response = "FF " +
                 responsearray[1] + " " +
                 responsearray[2] + " " +
                 responsearray[3] + " " +
                 responsearray[4] + " " +
                 responsearray[5] + " " +
                 responsearray[6];

            string data = responsearray[4] + responsearray[5];
            tiltreply = Convert.ToInt32(data, 16);
            validreply = true;

            results.Add(BitConverter.ToString(code).Replace('-', ' '));
            results.Add(conditioned_response);
            results.Add(response);
            results.Add(count.ToString());
            results.Add((stopwatch.Elapsed.TotalMilliseconds).ToString());

            break;
          }


        }
        else
        {


          results.Add(BitConverter.ToString(code).Replace('-', ' '));
          results.Add(conditioned_response);
          results.Add(response);
          results.Add(count.ToString());
          results.Add((stopwatch.Elapsed.TotalMilliseconds).ToString());
          count++;
        }





          // Thread.Sleep(500);
      
    }

      

      
      return results;
    }


  }
}
