﻿// Copyright (c) Microsoft Open Technologies, Inc. All rights reserved.
// Licensed under the Apache License, Version 2.0. See License.txt in the project root for license information.

using Microsoft.AspNet.Server.Kestrel.Networking;
using System;
using System.Runtime.InteropServices;

namespace Microsoft.AspNet.Server.Kestrel.Http
{
    /// <summary>
    ///   Operations performed for buffered socket output
    /// </summary>
    public interface ISocketOutput
    {
        void Write(ArraySegment<byte> buffer, Action<Exception, object> callback, object state);
    }

    public class SocketOutput : ISocketOutput
    {
        private readonly KestrelThread _thread;
        private readonly UvStreamHandle _socket;

        public SocketOutput(KestrelThread thread, UvStreamHandle socket)
        {
            _thread = thread;
            _socket = socket;
        }

        public void Write(ArraySegment<byte> buffer, Action<Exception, object> callback, object state)
        {
            //TODO: need buffering that works
            var copy = new byte[buffer.Count];
            Array.Copy(buffer.Array, buffer.Offset, copy, 0, buffer.Count);

            KestrelTrace.Log.ConnectionWrite(0, buffer.Count);
            var req = new ThisWriteReq();
            req.Init(_thread.Loop);
            req.Contextualize(this, _socket, copy, callback, state);
            _thread.Post(x =>
            {
                ((ThisWriteReq)x).Write();
            }, req);
        }

        public class ThisWriteReq : UvWriteReq
        {
            private static readonly Action<UvWriteReq, int, Exception, object> _writeCallback = WriteCallback;
            private static void WriteCallback(UvWriteReq req, int status, Exception error, object state)
            {
                ((ThisWriteReq)state).OnWrite(req, status, error);
            }

            SocketOutput _self;
            byte[] _buffer;
            UvStreamHandle _socket;
            Action<Exception, object> _callback;
            object _state;

            internal void Contextualize(
                SocketOutput socketOutput,
                UvStreamHandle socket,
                byte[] buffer,
                Action<Exception, object> callback,
                object state)
            {
                _self = socketOutput;
                _socket = socket;
                _buffer = buffer;
                _callback = callback;
                _state = state;
            }

            public void Write()
            {
                Write(
                    _socket,
                    _buffer,
                    _writeCallback,
                    this);
            }

            private void OnWrite(UvWriteReq req, int status, Exception error)
            {
                KestrelTrace.Log.ConnectionWriteCallback(0, status);
                //NOTE: pool this?

                var callback = _callback;
                _callback = null;
                var state = _state;
                _state = null;

                Dispose();
                callback(error, state);
            }
        }


        public bool Flush(Action drained)
        {
            return false;
        }

    }
}
