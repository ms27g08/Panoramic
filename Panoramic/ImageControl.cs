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
    class ImageControl
    {

        public static void savesnapshot(string IP, string username, string password, string filename, string filelocation, string rtsp)
        {

            string ffmpeglocation = @"C:\ffmpeg\ffmpeg-20200415-51db0a4-win64-static\bin\ffmpeg.exe";


            //contrsuct rtps stream
            rtsp = "rtsp://" + username + ":" + password + "@" + IP + ":554" + rtsp;
            //define location for ffmpeg
            string exePath = ffmpeglocation;
            //arguments to save snapshot from 
            string arguements = " -i " + rtsp + " -f image2 -vframes 1 -pix_fmt yuvj420p " + filename;


            using (Process p = new Process())
            {
                p.StartInfo.UseShellExecute = false;
                p.StartInfo.CreateNoWindow = true;
                p.StartInfo.RedirectStandardOutput = true;
                p.StartInfo.FileName = exePath;
                p.StartInfo.Arguments = arguements;
                p.Start();
            }

        }


        public static void recording(string IP, string username, string password, string filename, string filelocation, string rtsp, int duration)
        {

            string ffmpeglocation = @"C:\ffmpeg\ffmpeg-20200415-51db0a4-win64-static\bin\ffmpeg.exe";

            //contrsuct rtps stream
            rtsp = "rtsp://" + username + ":" + password + "@" + IP + ":554" + rtsp;
            //define location for ffmpeg
            string exePath = ffmpeglocation;
            //arguments to save snapshot from 
            string arguements = " -i " + rtsp + " -t " + duration + " -vcodec copy " + filename;


            using (Process p = new Process())
            {
                p.StartInfo.UseShellExecute = false;
                p.StartInfo.CreateNoWindow = true;
                p.StartInfo.RedirectStandardOutput = true;
                p.StartInfo.FileName = exePath;
                p.StartInfo.Arguments = arguements;
                p.Start();
            }

        }





    }
}
