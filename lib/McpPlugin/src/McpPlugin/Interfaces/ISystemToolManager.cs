/*
┌────────────────────────────────────────────────────────────────────────┐
│  Author: Ivan Murzak (https://github.com/IvanMurzak)                   │
│  Repository: GitHub (https://github.com/IvanMurzak/MCP-Plugin-dotnet)  │
│  Copyright (c) 2025 Ivan Murzak                                        │
│  Licensed under the Apache License, Version 2.0.                       │
│  See the LICENSE file in the project root for more information.        │
└────────────────────────────────────────────────────────────────────────┘
*/

using System.Collections.Generic;
using com.RedPand.MCP.Plugin.Common.Hub.Client;
using com.RedPand.MCP.Plugin.Common.Model;

namespace com.RedPand.MCP.Plugin
{
    public interface ISystemToolManager : IClientSystemToolHub
    {
        int TotalToolsCount { get; }
        IEnumerable<IRunTool> GetAllTools();
        bool HasTool(string name);
    }
}
