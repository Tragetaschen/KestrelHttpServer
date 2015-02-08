﻿// Copyright (c) Microsoft Open Technologies, Inc. All rights reserved.
// Licensed under the Apache License, Version 2.0. See License.txt in the project root for license information.

using System;
using System.Threading;

namespace Microsoft.AspNet.Server.Kestrel.Networking
{
    public abstract class UvHandle : UvMemory
    {
        private readonly uv_close_cb _destroyMemory;
        private readonly Action<Action<IntPtr>, IntPtr> _queueCloseHandle;

        public UvHandle(
            int threadId,
            int size,
            Action<Action<IntPtr>, IntPtr> queueCloseHandle)
            : base(threadId, size)
        {
            _destroyMemory = DestroyMemory;
            _queueCloseHandle = queueCloseHandle;
        }

        protected override bool ReleaseHandle()
        {
            var memory = handle;
            if (memory != IntPtr.Zero)
            {
                handle = IntPtr.Zero;

                if (Thread.CurrentThread.ManagedThreadId == ThreadId)
                {
                    UnsafeNativeMethods.uv_close(memory, _destroyMemory);
                }
                else
                {
                    _queueCloseHandle(
                        memory2 => UnsafeNativeMethods.uv_close(memory2, _destroyMemory),
                        memory);
                }
            }
            return true;
        }

        public void Reference()
        {
            Validate();
            UnsafeNativeMethods.uv_ref(this);
        }

        public void Unreference()
        {
            Validate();
            UnsafeNativeMethods.uv_unref(this);
        }
    }
}
