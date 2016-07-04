//*********************************************************
//
// Copyright (c) Microsoft. All rights reserved.
// This code is licensed under the MIT License (MIT).
// THIS CODE IS PROVIDED *AS IS* WITHOUT WARRANTY OF
// ANY KIND, EITHER EXPRESS OR IMPLIED, INCLUDING ANY
// IMPLIED WARRANTIES OF FITNESS FOR A PARTICULAR
// PURPOSE, MERCHANTABILITY, OR NON-INFRINGEMENT.
//
//*********************************************************

using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace BackgroundAudioShared
{
    /// <summary>
    /// Collection of string constants used in the entire solution. This file is shared for all projects
    /// </summary>
    public static class ApplicationSettingsConstants
    {
        // Data keys
        public static string VideoId { get; private set; } = "videoid";
        public static string Position { get; private set; } = "position";
        public static string BackgroundTaskState { get; private set; } = "backgroundtaskstate"; // Started, Running, Cancelled
        public static string AppState { get; private set; } = "appstate"; // Suspended, Resumed
        public static string AppSuspendedTimestamp { get; private set; } = "appsuspendedtimestamp";
        public static string AppResumedTimestamp { get; private set; } = "appresumedtimestamp";
    }
}
