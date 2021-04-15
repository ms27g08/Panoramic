using System;
using System.Collections;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows.Forms;

namespace Panoramic
{
  public class CommandQueue {


    public const int globalTime = 25;



    public static List<Command> queueList;
        public static List<Command> oldList; //need to add response log handling for this

        public static int total = 0;

        public static void Init() {
            queueList = new List<Command>();
            oldList = new List<Command>();

            StartTimer();
        }

        static Timer SendTimer;

        public static void StartTimer() {
  
            SendTimer = new Timer();
            SendTimer.Interval = globalTime * 2;
            SendTimer.Tick += new EventHandler(SendCurrentCommand);
            SendTimer.Start();
        }

        private static void SendCurrentCommand(object sender, EventArgs e) { 
            try {
                if (!AsyncCamCom.sock.Connected) {
                    return;
                }

                if (queueList.Count > 0) { 
                    Command com = queueList[0];

                    if (!com.sent && com != null) {
                        AsyncCamCom.currentCom = com;
                        WaitForCommandResponse(com).ConfigureAwait(false);
                    } else {
                        queueList.RemoveAt(0);
                    }
                }
            } catch (Exception err){
                MessageBox.Show("Error in queuelist.\n" + err.ToString());
            }
        }

        static async Task WaitForCommandResponse(Command com) {
            try {
                SendTimer.Stop();

                int i = 0;
                while (i < 5) {
                    bool repeated = false;
                    if (i > 0)
                        repeated = true;

                    AsyncCamCom.SendCurrent();
                    
                    await Task.Delay(globalTime); // decreasing this will reduce the delay between it checking if it's done but will send more commands
                    if (com.done) {
                        break;
                    } else {
                        await Task.Delay(globalTime);
                    }
                    i++;
                }

                //if(i > 1)
                //    MainForm.m.WriteToResponses("Sent command " + i + " times!", true, false);


                if (!com.done) {
                } else {
                    //MainForm.m.WriteToResponses(GetNameString() + "Received: " + com.myReturn.msg, false);
                }

                com.done = true;

                if (queueList.Contains(com))
                    queueList.Remove(com);
                
                
                SendTimer.Start();
            } catch (Exception e) {
                //MainForm.ShowPopup("Failed to process message return!\nShow more?", "Response Failed!", e.ToString());
            }
        }

    }

    public class ReturnCommand {
        public string msg;

        public Command myCommand;

        public void UpdateReturnMsg(string content) {
            msg = content;
            myCommand.done = true;
        }

        public static bool CheckInvalid(string message) {
            if (message == null) {
                return true;
            }
            if (
                message == "00 00 00 00 00 00 00" ||
                message.Length < 3) {
                return true;
            }
            return false;
        }

    }

    public class Command {

        public byte[] content;

        public bool sent;
        public bool done;

        public ReturnCommand myReturn;

        public Command(byte[] code) {
            content = code;

            myReturn = new ReturnCommand();
            myReturn.myCommand = this;

            CommandQueue.queueList.Add(this);
        }

        public void Finish() {
            sent = true;
        }

    }


}
