﻿using System;
using System.Collections.Generic;
using System.Linq;
using System.Net.Sockets;
using System.Threading;
using SimpleNET;

namespace MyApp // Note: actual namespace depends on the project name.
{
    internal class Program
    {
        static void Main(string[] args)
        {
            Console.WriteLine("to try server enter s. to try client enter c.");
            var input = Console.ReadLine();

            if (input == "s")
            {
                ServerExample();
            }
            else if (input == "c")
            {
                ClientExample();
            }
            else
            {
                Console.WriteLine("Invalid input");
            }
        }

        static void ServerExample()
        {
            var server = new Server(22003);

            var clients = new List<Socket>();
            server.StartServer();

            bool running = true;

            while (running)
            {
                Thread.Sleep(16);

                if (server.PollForConnection(out var newClient))
                {
                    Console.WriteLine("New Client Connected: " + Utils.GetSocketAddress(newClient));
                    clients.Add(newClient);
                }

                foreach (var sock in clients.ToList())
                {
                    var result = server.CheckConnectionAlive(sock);
                    if (!result)
                    {
                        Console.WriteLine("Lost connection with client port:" + Utils.GetSocketPort(sock));
                        clients.Remove(sock);
                    }
                }

                foreach (var sock in clients)
                {
                    var buffer = new byte[512];
                    int packageSize = IO.ReadNextPackage(sock, ref buffer);

                    if (packageSize == 0 || packageSize == -1)
                        continue;

                    int readOffset = 0;
                    var msgStr = Serializer.ReadString(buffer, ref readOffset);

                    Console.WriteLine("Client says: " + msgStr + " size: " + packageSize);
                }
            }

            server.ShutdownServer();
            clients.Clear();
        }

        private static void ClientExample()
        {
            var client = new Client();

            client.Connect("127.0.0.1", 22003);

            var buffer = new byte[512];
            while (true)
            {
                Thread.Sleep(16);
                int writeOffset = 0;

                var isAlive = client.CheckConnectionAlive();
                if (!isAlive)
                {
                    Console.WriteLine("Connection lost");
                    return;
                }

                var msg = Console.ReadLine();

                Serializer.WriteString(msg, ref buffer, ref writeOffset);

                IO.SendPackage(client.Sock, buffer, writeOffset);
            }
        }
    }
}
