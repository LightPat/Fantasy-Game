using System.Collections;
using System.Collections.Generic;
using UnityEngine;

namespace LightPat.Networking
{
    public class ConsoleToGUI : MonoBehaviour
    {
        static string myLog = "";
        private string output;
        private string stack;

        void OnEnable()
        {
            Application.logMessageReceived += Log;
        }

        void OnDisable()
        {
            Application.logMessageReceived -= Log;
        }

        public void Log(string logString, string stackTrace, LogType type)
        {
            output = logString;
            stack = stackTrace;
            myLog = output + "\n" + myLog;
            if (myLog.Length > 1000)
            {
                myLog = myLog.Substring(0, 1000);
            }
        }

        void OnGUI()
        {
            //if (!Application.isEditor) //Do not display in editor ( or you can use the UNITY_EDITOR macro to also disable the rest)
            {
                myLog = GUI.TextArea(new Rect(10, 10, Screen.width/4 - 10, Screen.height/4 - 10), myLog);
            }
        }
    }
}
