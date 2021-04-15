using System;
using System.Collections.Generic;
using System.Drawing;
using System.Linq;
using System.Net;
using System.Net.NetworkInformation;
using System.Net.Sockets;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using System.Timers;
using System.Windows.Forms;

namespace Panoramic
{
  public class AsyncCamCom
  {

    public static Socket sock = new Socket(AddressFamily.InterNetwork, SocketType.Stream, ProtocolType.Tcp);
    static byte[] receiveBuffer;

    public static Command SendNewCommand(byte[] code)
    {
      Command com = new Command(code);
      return com;
    }

    public async static Task<string> QueryNewCommand(byte[] send)
    {
      Command com = SendNewCommand(send);
      string result = await CheckCommandResult(com).ConfigureAwait(false);
      return result;
    }

    private static async Task<string> CheckCommandResult(Command oldCom)
    {
      for (int i = 0; i < 600; i++)
      {
        if (oldCom.done)
          break;
        await Task.Delay(CommandQueue.globalTime * 2).ConfigureAwait(false);
      }

      if (oldCom != null)
      {
        return oldCom.myReturn.msg;

      }

      return "";
    }

    public static void Connect(string ip, string port)
    {
      try
      {
        int.TryParse(port, out int checkedPort);


        IPEndPoint end = new IPEndPoint(IPAddress.Parse(ip),
                                checkedPort);
        if (sock == null || (sock.Connected && end == sock.RemoteEndPoint as IPEndPoint))
        {
          return;
        }
        else
        {
          Disconnect();
        }

        sock = new Socket(AddressFamily.InterNetwork, SocketType.Stream, ProtocolType.Tcp);

        sock.BeginConnect(end, ConnectCallback, null);
      }
      catch (Exception e)
      {
        MessageBox.Show("An error occured whilst connecting to camera!\n" + e.ToString());
      }
    }

    private static void ConnectCallback(IAsyncResult AR)
    {
      try
      {
        sock.EndConnect(AR);
        if (!sock.Connected)
          return;
        receiveBuffer = new byte[50];

        sock.BeginReceive(receiveBuffer, 0, receiveBuffer.Length, SocketFlags.None, ReceiveCallback, null);
      }
      catch (Exception e)
      {
      }
    }

    public static void Disconnect()
    {
      try
      {
        if (sock == null)
          return;
        if (!sock.Connected)
          return;
        sock.Shutdown(SocketShutdown.Both);
        sock.Close();
      }
      catch (Exception e)
      {
      }
    }

    public static Command currentCom;

    public static void SendCurrent()
    {
      try
      {
        sock.BeginSend(currentCom.content, 0, currentCom.content.Length, SocketFlags.None, SendCallback, null);
      }
      catch (Exception e)
      {
      }
    }

    private static void SendCallback(IAsyncResult AR)
    {
      try
      {
        sock.EndSend(AR);
      }
      catch (Exception e)
      {
      }
    }

    private static async void ReceiveCallback(IAsyncResult AR)
    { //why is this inconsistent?
      try
      {
        if (receiveBuffer.Length < 50)
        {
          return;
        }

        int received = sock.EndReceive(AR);
        if (received > 0)
        {
          await SaveResponse();
        }

        sock.BeginReceive(receiveBuffer, 0, receiveBuffer.Length, SocketFlags.None, ReceiveCallback, null);
      }
      catch (Exception e)
      {
      }
    }

    static async Task SaveResponse()
    {
      try
      {
        if (currentCom == null)
        {
          return;
        }

        string msg = "";
        int comCount = 0;
        bool startedCom = false;

        for (int i = 0; i < receiveBuffer.Length; i++)
        {
          string hex = receiveBuffer[i].ToString("X").ToUpper();

          if (hex != "0" && !startedCom)
          {
            comCount = 7;
            startedCom = true;
          }

          if (comCount > 0)
          {
            if (hex.Length == 1)
            {
              hex = "0" + hex;
            }
            msg += hex + " ";
            comCount--;
          }
          else
          {
            break;
          }
        }

        msg = msg.Trim();

        if (msg.Length > 0 && msg.StartsWith("F"))
        {

          CommandQueue.oldList.Add(currentCom);

          if (currentCom.myReturn != null)
          {
            currentCom.myReturn.UpdateReturnMsg(msg);
            currentCom.Finish();
          }
        }
      }
      catch (Exception e)
      {
      };
    }
  }
}
