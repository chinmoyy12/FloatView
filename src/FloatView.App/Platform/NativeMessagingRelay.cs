using System;
using System.IO;
using System.IO.Pipes;
using System.Text;
using System.Threading;
using FloatView.App.Media;

namespace FloatView.App.Platform;

public static class NativeMessagingRelay
{
    public const string ArgFlag = "--native-messaging-host";

    public static bool IsNativeMessagingInvocation(string[] args)
    {
        if (args == null || args.Length == 0) return false;
        foreach (var arg in args)
        {
            if (arg.Equals(ArgFlag, StringComparison.OrdinalIgnoreCase) ||
                arg.StartsWith("chrome-extension://", StringComparison.OrdinalIgnoreCase) ||
                arg.StartsWith("edge-extension://", StringComparison.OrdinalIgnoreCase))
            {
                return true;
            }
        }
        return false;
    }

    public static void Run(string[] args)
    {
        try
        {
            using var pipeClient = new NamedPipeClientStream(".", BrowserBridgeService.PipeName, PipeDirection.InOut);
            // Wait up to 3 seconds for the main FloatView app named pipe
            pipeClient.Connect(3000);

            using var stdin = Console.OpenStandardInput();
            using var stdout = Console.OpenStandardOutput();
            using var pipeReader = new StreamReader(pipeClient, Encoding.UTF8);
            using var pipeWriter = new StreamWriter(pipeClient, Encoding.UTF8) { AutoFlush = true };

            while (true)
            {
                string? messageJson = ReadFramedMessage(stdin);
                if (messageJson == null) break;

                // Send to main FloatView instance
                pipeWriter.WriteLine(messageJson);

                // Read response from main instance
                string? responseJson = pipeReader.ReadLine();
                if (responseJson != null)
                {
                    WriteFramedMessage(stdout, responseJson);
                }
            }
        }
        catch
        {
            // Exit quietly when pipe or stdio closes
        }
    }

    public static string? ReadFramedMessage(Stream stream)
    {
        byte[] lenBytes = new byte[4];
        int bytesRead = 0;
        while (bytesRead < 4)
        {
            int read = stream.Read(lenBytes, bytesRead, 4 - bytesRead);
            if (read <= 0) return null;
            bytesRead += read;
        }

        int length = BitConverter.ToInt32(lenBytes, 0);
        if (length <= 0 || length > 1024 * 1024) return null; // 1MB Chrome native messaging limit

        byte[] buffer = new byte[length];
        bytesRead = 0;
        while (bytesRead < length)
        {
            int read = stream.Read(buffer, bytesRead, length - bytesRead);
            if (read <= 0) return null;
            bytesRead += read;
        }

        return Encoding.UTF8.GetString(buffer);
    }

    public static void WriteFramedMessage(Stream stream, string json)
    {
        byte[] bytes = Encoding.UTF8.GetBytes(json);
        byte[] lenBytes = BitConverter.GetBytes(bytes.Length);

        stream.Write(lenBytes, 0, 4);
        stream.Write(bytes, 0, bytes.Length);
        stream.Flush();
    }
}
