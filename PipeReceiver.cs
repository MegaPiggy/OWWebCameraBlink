using OWML.Common;
using System;
using System.Collections;
using System.Diagnostics;
using System.Globalization;
using System.IO;
using System.Threading;
using UnityEngine;

namespace WebCameraBlink
{
    public class PipeReceiver : MonoBehaviour
    {
        private Process pythonProcess;
        private StreamReader reader;
        private string path;
        private Thread dataThread;
        private bool isRunning = true;
        public static float blinkValue = -2;
        public static float cameraID = 0;

        private void Start()
        {
            StartProgram();
        }

        private void OnDestroy()
        {
            blinkValue = -2;
            EndProgram();
        }

        private void OnDisable()
        {
            blinkValue = -2;
            EndProgram();
        }

        public void SelfDestruct()
        {
            EndProgram();
            GameObject.Destroy(gameObject);
        }

        private void StartProgram()
        {
            blinkValue = -2;
            StartPythonProcess();
            dataThread = new Thread(new ThreadStart(ReceiveData));
            dataThread.Start();
        }

        private void StartPythonProcess()
        {
            path = WebCameraBlink.Instance.ModHelper.Manifest.ModFolderPath + "PBP";
            string arguments = "--cameraId " + cameraID.ToString();
            WebCameraBlink.Instance.ModHelper.Console.WriteLine("Setting up process...");
            pythonProcess = new Process();
            pythonProcess.StartInfo.WorkingDirectory = path;
            pythonProcess.StartInfo.FileName = path + "\\PBP.exe";
            pythonProcess.StartInfo.Arguments = arguments;
            pythonProcess.StartInfo.UseShellExecute = false;
            pythonProcess.StartInfo.CreateNoWindow = true;
            pythonProcess.StartInfo.RedirectStandardOutput = true;
            pythonProcess.Start();
            WebCameraBlink.Instance.ModHelper.Console.WriteLine("PBP started!");
            WebCameraBlink.Instance.ModHelper.Console.WriteLine("Attaching PBP reader...");
            reader = pythonProcess.StandardOutput;
            WebCameraBlink.Instance.ModHelper.Console.WriteLine("PBP reader attached!");
        }

        private void EndProgram()
        {
            isRunning = false;
            StreamReader streamReader = reader;
            if (streamReader != null)
            {
                streamReader.Close();
            }
            if (pythonProcess != null && !pythonProcess.HasExited)
            {
                WebCameraBlink.Instance.ModHelper.Console.WriteLine("Killing PBP...");
                new Process
                {
                    StartInfo =
                {
                    CreateNoWindow = true,
                    UseShellExecute = false,
                    Arguments = "/F /IM PBP.exe",
                    FileName = "taskkill"
                }
                }.Start();
                pythonProcess.Close();
                WebCameraBlink.Instance.ModHelper.Console.WriteLine("PBP killed.");
            }
            if (dataThread != null && dataThread.IsAlive)
            {
                dataThread.Abort();
                WebCameraBlink.Instance.ModHelper.Console.WriteLine("Data thread aborted.");
            }
            blinkValue = -2;
        }

        public void RestartProgram()
        {
            StartCoroutine(RestartProtcol());
        }

        private IEnumerator RestartProtcol()
        {
            EndProgram();
            yield return new WaitForSeconds(1);
            StartProgram();
            yield break;
        }

        private void ReceiveData()
        {
            while (isRunning)
            {
                string text = reader.ReadLine();
                if (text != null)
                {
                    float num;
                    if (float.TryParse(text, NumberStyles.Float, CultureInfo.InvariantCulture, out num))
                    {
                        blinkValue = num;
                    }
                    else
                    {
                        WebCameraBlink.Instance.ModHelper.Console.WriteLine("Failed to parse received data as float: " + text, MessageType.Error);
                    }
                }
                Thread.Sleep(1);
            }
        }
    }

}
