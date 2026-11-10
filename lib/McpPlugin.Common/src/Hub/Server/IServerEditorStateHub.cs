// RedPand MCP. Licensed under the Apache License, Version 2.0.
using System.Threading.Tasks;
using com.RedPand.MCP.Plugin.Common.Model;

namespace com.RedPand.MCP.Plugin.Common.Hub.Server
{
    /// <summary>
    /// The plugin tells the server what its editor is doing (McpPlugin 8.20.0+). Its own interface, not part of
    /// <see cref="IServerMcpManager"/>: a server built before it does not have the method, and the plugin then stops sending.
    /// </summary>
    public interface IServerEditorStateHub
    {
        Task<ResponseData> NotifyEditorState(RequestEditorState request);
    }
}
