﻿// Copyright (c) Microsoft Corporation. All rights reserved.
// Example09_SemanticKernel.cs

using System.ComponentModel;
using AutoGen.SemanticKernel.Extension;
using AutoGen.SemanticKernel.Middleware;
using FluentAssertions;
using Microsoft.SemanticKernel;
using Microsoft.SemanticKernel.ChatCompletion;
using Microsoft.SemanticKernel.Connectors.OpenAI;
namespace AutoGen.BasicSample;

public class LightPlugin
{
    public bool IsOn { get; set; } = false;

    [KernelFunction]
    [Description("Gets the state of the light.")]
    public string GetState() => this.IsOn ? "on" : "off";

    [KernelFunction]
    [Description("Changes the state of the light.'")]
    public string ChangeState(bool newState)
    {
        this.IsOn = newState;
        var state = this.GetState();

        // Print the state to the console
        Console.ForegroundColor = ConsoleColor.DarkBlue;
        Console.WriteLine($"[Light is now {state}]");
        Console.ResetColor();

        return state;
    }
}

public class Example10_SemanticKernel
{
    public static async Task RunAsync()
    {
        var openAIKey = Environment.GetEnvironmentVariable("OPENAI_API_KEY") ?? throw new Exception("Please set OPENAI_API_KEY environment variable.");
        var modelId = "gpt-3.5-turbo";
        var builder = Kernel.CreateBuilder()
            .AddOpenAIChatCompletion(modelId: modelId, apiKey: openAIKey);
        var kernel = builder.Build();
        var settings = new OpenAIPromptExecutionSettings
        {
            ToolCallBehavior = ToolCallBehavior.AutoInvokeKernelFunctions,
        };

        kernel.Plugins.AddFromObject(new LightPlugin());
        var skAgent = kernel
            .ToSemanticKernelAgent(name: "assistant", systemMessage: "You control the light", settings);

        // Send a message to the skAgent, the skAgent supports the following message types:
        // - IMessage<ChatMessageContent>
        // - (streaming) IMessage<StreamingChatMessageContent>
        // You can create an IMessage<ChatMessageContent> using MessageEnvelope.Create
        var chatMessageContent = MessageEnvelope.Create(new ChatMessageContent(AuthorRole.User, "Toggle the light"));
        var reply = await skAgent.SendAsync(chatMessageContent);
        reply.Should().BeOfType<MessageEnvelope<ChatMessageContent>>();
        Console.WriteLine((reply as IMessage<ChatMessageContent>).Content.Items[0].As<TextContent>().Text);

        // To support more AutoGen bulit-in IMessage, register skAgent with ChatMessageContentConnector
        var connector = new ChatMessageContentConnector();
        var skAgentWithMiddlewares = skAgent
            .RegisterMiddleware(connector)
            .RegisterPrintFormatMessageHook();

        // Now the skAgentWithMiddlewares supports more IMessage types like TextMessage, ImageMessage or MultiModalMessage
        // It also register a print format message hook to print the message in a human readable format to the console
        await skAgent.SendAsync(chatMessageContent);
        await skAgentWithMiddlewares.SendAsync(new TextMessage(Role.User, "Toggle the light"));

        // The more message type an agent support, the more flexible it is to be used in different scenarios
        // For example, since the TextMessage is supported, the skAgentWithMiddlewares can be used with user proxy.
        var userProxy = new UserProxyAgent("user");

        await skAgentWithMiddlewares.InitiateChatAsync(userProxy, "how can I help you today");
    }

}