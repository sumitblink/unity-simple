﻿// Copyright 2019 Google LLC
// All Rights Reserved.
//
// Licensed under the Apache License, Version 2.0 (the "License");
// you may not use this file except in compliance with the License.
// You may obtain a copy of the License at
//
//     http://www.apache.org/licenses/LICENSE-2.0
//
// Unless required by applicable law or agreed to in writing, software
// distributed under the License is distributed on an "AS IS" BASIS,
// WITHOUT WARRANTIES OR CONDITIONS OF ANY KIND, either express or implied.
// See the License for the specific language governing permissions and
// limitations under the License.

using Agones;
using System.Net;
using System.Net.Sockets;
using System.Text;
using UnityEngine;
using System.Collections;
namespace AgonesExample
{
    [RequireComponent(typeof(AgonesSdk))]
    public class UdpEchoServer : MonoBehaviour
    {
        private int Port { get; set; } = 7777;
        private UdpClient client = null;
        private AgonesSdk agones = null;

         void Start()
        {
            client = new UdpClient(Port);
            StartCoroutine(StartAgonesCheck());
        }

        IEnumerator StartAgonesCheck(){
            yield return new WaitForSeconds(3.0f);
            Debug.Log("Checking");
            AgonesReady(); 
        }

        async void AgonesReady(){

            agones = GetComponent<AgonesSdk>();
            bool ok = await agones.Ready();
            if (ok)
            {
                Debug.Log($"Server - Ready");
            }
            else
            {
                Debug.Log($"Server - Ready failed");
                Application.Quit();
            }
        }

        async void Update()
        {
            if (client.Available > 0)
            {
                IPEndPoint remote = null;
                byte[] recvBytes = client.Receive(ref remote);
                string recvText = Encoding.UTF8.GetString(recvBytes);

                string[] recvTexts = recvText.Split(' ');
                byte[] echoBytes = null;
                bool ok = false;
                switch (recvTexts[0])
                {
                    case "Shutdown":
                        ok = await agones.Shutdown();
                        Debug.Log($"Server - Shutdown {ok}");

                        echoBytes = Encoding.UTF8.GetBytes($"Shutdown {ok}");
                        client.Send(echoBytes, echoBytes.Length, remote);
                        Application.Quit();
                        return;

                    case "Allocate":
                        ok = await agones.Allocate();
                        Debug.Log($"Server - Allocate {ok}");

                        echoBytes = Encoding.UTF8.GetBytes($"Allocate {ok}");
                        break;

                    case "Label":
                        if (recvTexts.Length == 3)
                        {
                            (string key, string value) = (recvTexts[1], recvTexts[2]);
                            ok = await agones.SetLabel(key, value);
                            Debug.Log($"Server - SetLabel({recvTexts[1]}, {recvTexts[2]}) {ok}");

                            echoBytes = Encoding.UTF8.GetBytes($"SetLabel({recvTexts[1]}, {recvTexts[2]}) {ok}");
                        }
                        else
                        {
                            echoBytes = Encoding.UTF8.GetBytes($"ERROR: Invalid Label command, must use 2 arguments");
                        }
                        break;

                    case "Annotation":
                        if (recvTexts.Length == 3)
                        {
                            (string key, string value) = (recvTexts[1], recvTexts[2]);
                            ok = await agones.SetAnnotation(key, value);
                            Debug.Log($"Server - SetAnnotation({recvTexts[1]}, {recvTexts[2]}) {ok}");

                            echoBytes = Encoding.UTF8.GetBytes($"SetAnnotation({recvTexts[1]}, {recvTexts[2]}) {ok}");
                        }
                        else
                        {
                            echoBytes = Encoding.UTF8.GetBytes($"ERROR: Invalid Annotation command, must use 2 arguments");
                        }
                        break;
                    default:
                        echoBytes = Encoding.UTF8.GetBytes($"Echo : {recvText}");
                        break;
                }

                client.Send(echoBytes, echoBytes.Length, remote);

                Debug.Log($"Server - Receive[{remote.ToString()}] : {recvText}");
            }
        }

        void OnDestroy()
        {
            client.Close();
            Debug.Log("Server - Close");
        }
    }
}