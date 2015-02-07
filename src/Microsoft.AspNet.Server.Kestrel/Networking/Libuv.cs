// Copyright (c) .NET Foundation. All rights reserved.
// Licensed under the Apache License, Version 2.0. See License.txt in the project root for license information.

using System;
using System.Runtime.InteropServices;

namespace Microsoft.AspNet.Server.Kestrel.Networking
{
    public static class Libuv
    {
        public static bool IsWindows
        {
            get
            {
#if DNXCORE50
                // Until Environment.OSVersion.Platform is exposed on .NET Core, we
                // try to call uname and if that fails we assume we are on Windows.
                return GetUname() == string.Empty;
#else
                var p = (int)Environment.OSVersion.Platform;
                return (p != 4) && (p != 6) && (p != 128);
#endif
            }
        }

        public static void Check(int statusCode)
        {
            Exception error;
            Check(statusCode, out error);
            if (error != null)
            {
                throw error;
            }
        }

        public static void Check(int statusCode, out Exception error)
        {
            error = null;

            if (statusCode < 0)
            {
                var errorName = Marshal.PtrToStringAnsi(UnsafeNativeMethods.uv_err_name(statusCode));
                var errorDescription = Marshal.PtrToStringAnsi(UnsafeNativeMethods.uv_strerror(statusCode));
                error = new Exception("Error " + statusCode + " " + errorName + " " + errorDescription);
            }
        }
    }
}