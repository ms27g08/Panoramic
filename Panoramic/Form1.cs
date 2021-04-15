
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
using System.Drawing.Imaging;

namespace Panoramic
{
  public partial class Form1 : Form
  {
    public Form1()
    {
      InitializeComponent();
    }

    private void button1_Click(object sender, EventArgs e)
    {
      byte[] code = new byte[6];
      string IP = "192.168.1.183";
      string Port = "6791";
      string ipaddress = "192.168.1.183";
      string username = "admin";
      string password = "admin";
      string rtsp1 = "/videoinput_1:0/h264_1/onvif.stm";
      string imagefolder = @"C:\Users\Shell\Desktop\Images";
   

      int i = 0;

      //abs pan 0
      code = new byte[] { 0xFF, 0x01, 0x00, 0x4B, 0x00, 0x00, 0x4C };
      Peclo_Over_IP.SendCommand(code, IP, Port);

      Thread.Sleep(200);
      // tilt 0
      code = new byte[] { 0xFF, 0x01, 0x00, 0x4D, 0x00, 0x00, 0x4E };
      Peclo_Over_IP.SendCommand(code, IP, Port);  

      Thread.Sleep(3000);

     // code = new byte[] { 0xFF, 0x01, 0x00, 0x4F, 0xB7, 0x90, 0x97 };

      for (int j = 0; j < 6; j++)
      {
        byte[] command;
        if (6000 + 6000 * j == 36000)
        {
          command = new byte[] { 0xFF, 0x01, 0x00, 0x4B, 0x00, 0x00, 0x4C };
        }
        else
        {
          command = PanAbs(6000 + 6000 * j);
        }

        Peclo_Over_IP.SendCommand(command, IP, Port);
        Thread.Sleep(150);
        Peclo_Over_IP.SendCommand(command, IP, Port);
        Thread.Sleep(150);
        Peclo_Over_IP.SendCommand(command, IP, Port);
        Thread.Sleep(5000);
        string filename = imagefolder + "\\" + j.ToString() + ".bmp";

        ImageControl.savesnapshot(ipaddress, username, password, filename, imagefolder, rtsp1);
        Thread.Sleep(2000);

      }

      Thread.Sleep(3000);
      string[] fileNames = Directory.GetFiles(imagefolder, "*.bmp");

      List<Bitmap> images = new List<Bitmap>();

      for( int k = 0; k<fileNames.Length; k++)
      {

        Bitmap image = new Bitmap(fileNames[k]);
        images.Add(image);

      }

      Bitmap merged = MergeImages(images);

      merged.Save(imagefolder + "\\Merged.bmp", ImageFormat.Png);

   //   pictureBox1.Image = merged;




    }




    private static byte[] PanAbs(int panangle)
    {
      string hexValue = panangle.ToString("X");
      hexValue = hexValue.Insert(2, " ");

      string code = "FF 01 00 4B ";
      hexValue = code + hexValue;
      string[] split = hexValue.Split(' ');

      int data1 = int.Parse(split[4], System.Globalization.NumberStyles.HexNumber);
      int data2 = int.Parse(split[5], System.Globalization.NumberStyles.HexNumber);


      byte CalculatedCheckSum = (byte)((0x01 + 0x00 + 0x4B + data1 + data2) % 256);

      code = code + split[4] + " " + split[5] + " " + CalculatedCheckSum.ToString("X");



      string[] codearray = code.Split(' ');

      byte[] arr = new byte[codearray.Length];

      for (int i = 0; i < codearray.Length; ++i)
      {
        int num = Int32.Parse(codearray[i], System.Globalization.NumberStyles.HexNumber);

        arr[i] = Convert.ToByte(num);
      }




      return arr;
    }



    private Bitmap MergeImages(IEnumerable<Bitmap> images)
    {
      var enumerable = images as IList<Bitmap> ?? images.ToList();

      var width = 0;
      var height = 0;

      foreach (var image in enumerable)
      {
        width += image.Width;
        height = image.Height > height
            ? image.Height
            : height;
      }

      var bitmap = new Bitmap(width, height);
      using (var g = Graphics.FromImage(bitmap))
      {
        var localWidth = 0;
        foreach (var image in enumerable)
        {
          g.DrawImage(image, localWidth, 0);
          localWidth += image.Width;
        }
      }
      return bitmap;
    }

















    private void Form1_Load(object sender, EventArgs e)
    {

    }
  }
}
