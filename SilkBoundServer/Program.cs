using System.Reflection;
using System.Runtime.Loader;
using SilkBound.Managers;
using SilkBound.Network.NetworkLayers.Impl;
using SilkBound.Types;
using SilkBound.Utils;

namespace SilkBoundServer;

internal static class Program
{
    private static bool _stopped;
    private static bool _depsLoaded;
    private static Config _config = null!;
    
    private static readonly ManualResetEvent ShutdownWaitHandle = new(false);
    public static Guid Guid;
    static async Task Main()
    {
        _depsLoaded = false;
        _stopped = false;
        AssemblyLoadContext.Default.Resolving += ResolveAssembly;
        if (AssemblyLoadContext.CurrentContextualReflectionContext != null)
            AssemblyLoadContext.CurrentContextualReflectionContext.Resolving += ResolveAssembly;
        /*
        if ((Directory.GetFiles(AppDomain.CurrentDomain.BaseDirectory).Length > 1
             || Directory.GetDirectories(AppDomain.CurrentDomain.BaseDirectory).Length > 1)
            && !File.Exists(Path.Combine(AppDomain.CurrentDomain.BaseDirectory, "libs/0Harmony.dll")))
        {
            Logger.Msg("Please place SilkBound's Standalone Server in it's own, empty folder!");
            Logger.Msg("Press any key to exit...");
            Console.ReadKey();
            _stopped = true;
        }


        if (_stopped)
            return;
        
    
        if (!File.Exists(Path.Combine(AppDomain.CurrentDomain.BaseDirectory, "libs") + "/0Harmony.dll"))
        {
            Logger.Msg("First launch detected! Downloading libraries...");
            var folder = Path.Combine(AppDomain.CurrentDomain.BaseDirectory, "libs");
            var filePath = Path.Combine(folder, "Libs.zip");

            Directory.CreateDirectory(folder);

            using HttpClient client = new HttpClient();

            await using (var stream = await client.GetStreamAsync("https://ambermeowmrrp.github.io/sblibs/Libs.zip"))
            await using (var file = new FileStream(filePath, FileMode.Create))
            {
                await stream.CopyToAsync(file);
            }

            ZipFile.ExtractToDirectory(filePath, folder);
            File.Delete(filePath);
            Logger.Msg("Finished.\n\n");
            _depsLoaded = true;
        }
        else
        */
        _depsLoaded = true;
        
        _config = ConfigurationManager.ReadFromFile("server");
        Console.CancelKeyPress += OnShutdown;
        AppDomain.CurrentDomain.ProcessExit += OnShutdown;
        Guid = Guid.NewGuid();
        var server = Server.Connect(new TCPServer("0.0.0.0", new StandaloneHandler(), _config.Port), "serverHost");
        server.Settings = _config.HostSettings;

        await Task.Run(Shutdown);

        ShutdownWaitHandle.WaitOne();
        Logger.Msg("Press any key to exit...");
        Console.ReadKey();
        
    }
    
    private static Task Shutdown()
    {
        while (true)
        {
            var input = Console.ReadLine();
            if (input == null) continue;
            
            if (!input.Trim().Equals("stop", StringComparison.OrdinalIgnoreCase)) continue;
            StopServer();
            Environment.Exit(0);
        }
    }


    private static bool _isShuttingDown;

    private static void OnShutdown(object? sender, EventArgs e)
    {
        if (_isShuttingDown)
            return;
        _isShuttingDown = true;
        StopServer();
        ShutdownWaitHandle.Set();
    }
    private static void StopServer()
    {
        if (_stopped) return;
        _stopped = true;
        _config.SaveToFile("server");
        Logger.Msg("Stopping server...");
    }

    private static Assembly? ResolveAssembly(AssemblyLoadContext context, AssemblyName name)
    {
        while (!_depsLoaded) ;
        var folder = Path.Combine(AppContext.BaseDirectory, "bin");
        var candidate = Path.Combine(folder, name.Name + ".dll");
        if (File.Exists(candidate))
        {
            return context.LoadFromAssemblyPath(candidate);
        }
        return null;
    }
}