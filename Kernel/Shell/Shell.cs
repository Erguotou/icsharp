﻿

using System.Collections.Generic;
using Common.Serializer;
using iCSharp.Messages;

namespace iCSharp.Kernel.Shell
{
    using iCSharp.Kernel.IOPub;
    using NetMQ;
    using NetMQ.Sockets;
    using System.Threading;
    using Common.Logging;

    public class Shell : IServer
    {
        private ILog logger;
        private string address;
        private IOPub ioPub;

        private NetMQContext context;
        private RouterSocket server;

        private ManualResetEventSlim stopEvent;

        private Thread thread;
        private bool disposed;

        public Shell(ILog logger,string address, IOPub ioPub, NetMQContext context)
        {
            this.logger = logger;
            this.address = address;
            this.ioPub = ioPub;

            this.context = context;
            this.server = this.context.CreateRouterSocket();
            this.stopEvent = new ManualResetEventSlim();
        }

        public void Start()
        {
            this.thread = new Thread(this.StartServerLoop);
            this.thread.Start();

            this.logger.Info("Shell Started");
            //ThreadPool.QueueUserWorkItem(new WaitCallback(StartServerLoop));
        }

        private void StartServerLoop(object state)
        {
            this.server.Bind(this.address);

            this.logger.Info(string.Format("Binded the Shell server to address {0}", this.address));

            while (!this.stopEvent.Wait(0))
            {
                Message message = this.GetMessage();

                this.logger.Info(JsonSerializer.Serizlize(message));
            }
        }

        private Message GetMessage()
        {
            Message message = new Message();

            // Getting UUID
            message.UUID = this.server.ReceiveString();
            this.logger.Info(message.UUID);

            // Getting Delimeter "<IDS|MSG>"
            this.server.ReceiveString();

            // Getting Hmac
            message.HMac = this.server.ReceiveString();
            this.logger.Info(message.HMac);

            // Getting Header
            string header = this.server.ReceiveString();
            this.logger.Info(header);

            message.Header = JsonSerializer.Deserialize<Header>(header);

            // Getting parent header
            string parentHeader = this.server.ReceiveString();
            this.logger.Info(parentHeader);

            message.ParentHeader = JsonSerializer.Deserialize<Header>(parentHeader);

            // Getting metadata
            string metadata = this.server.ReceiveString();
            this.logger.Info(metadata);

            message.MetaData = JsonSerializer.Deserialize<Dictionary<string, object>>(metadata);

            // Getting content
            string content = this.server.ReceiveString();
            this.logger.Info(content);

            message.Content = JsonSerializer.Deserialize<Dictionary<string, object>>(content);

            return message;
        }


        public void Stop()
        {
            this.stopEvent.Set();
        }

        public ManualResetEventSlim GetWaitEvent()
        {
            return this.stopEvent;
        }

        public void Dispose()
        {
            this.Dispose(true);
        }

        protected void Dispose(bool dispose)
        {
            if(!this.disposed)
            {
                if(dispose)
                {
                    if(this.server != null)
                    {
                        this.server.Dispose();
                    }

                    this.disposed = true;
                }
            }
        }
    }
}