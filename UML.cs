using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Reflection;
using System.Runtime.InteropServices;
using System.Security.Cryptography;
using Microsoft.Win32.SafeHandles;
using UML;
using UnityEngine;

class _UML
{
	private static void PreloadMod(string path, List<Mod> mods)
    {
		Log("UML", "Loading file " + Path.GetFileName(path));
		Assembly assembly = Assembly.LoadFrom(path);
		Mod mod3 = null;
		int num = 0;
		try
		{
			foreach (Type type2 in assembly.GetTypes())
			{
				ModInfo customAttribute = type2.GetCustomAttribute<ModInfo>();
				if (customAttribute == null && num != 2)
					num = 1;
				else
				{
					mod3 = new Mod(assembly, type2, customAttribute.name, customAttribute.author, customAttribute.version, customAttribute.guid, customAttribute.dependencies, customAttribute.modType, SHA256CheckSum(path));
					mods.Add(mod3);
				}
			}
		}
		catch (ReflectionTypeLoadException ex)
		{
			Log("UML", ex.ToString());
		}
		if (mod3 == null)
			Log("UML", " Failed to load mod (No Type has an attribute of ModInfo set)");
	}

	public static void _Start()
	{
		if (HooksMB.instance != null)
			return;
		GameObject gameObject = new GameObject();
		gameObject.AddComponent<HooksMB>();
		UnityEngine.Object.DontDestroyOnLoad(gameObject);
	}

	public static string SHA256CheckSum(string filePath)
	{
		string result;
		using (SHA256 sha = SHA256.Create())
		using (FileStream fileStream = File.OpenRead(filePath))
			result = BitConverter.ToString(sha.ComputeHash(fileStream)).Replace("-", "").ToLowerInvariant();
		return result;
	}

	public static void Log(string app, string message, bool onlyInGame = false)
	{
		while (app.Length < 9)
			app += " ";
		LogString += $"[{app}] {message}\n";
		while (LogString.Split('\n').Length > 67)
			LogString = LogString.Substring(LogString.IndexOf('\n') + 1);
		if (onlyInGame)
			return;
		Console.WriteLine("[" + app + "] " + message);
		if (Config.config.logfile)
			File.AppendAllText(Path.Combine(HooksMB.modloader, "log"), $"[{app}] {message}\n");
	}

	public static string LogString { get; private set; }

	public class HooksMB : MonoBehaviour
	{
		private void Start()
		{
			instance = this;
			if (!Directory.Exists(modloader))
				Directory.CreateDirectory(modloader);
			if (!Directory.Exists(modss))
				Directory.CreateDirectory(modss);
			if (!Directory.Exists(Path.Combine(modloader, "deps")))
				Directory.CreateDirectory(Path.Combine(modloader, "deps"));
			if (!File.Exists(Path.Combine(modloader, "config")))
				File.WriteAllText(Path.Combine(modloader, "config"), "console=true\nunitylog=true\nlogfile=true");
			new Config(File.ReadAllLines(Path.Combine(modloader, "config")));
			if (Config.config.console)
			{
				WinConsole.Initialize(true);
				Log("UML", "Initialized console");
			}
			if (Config.config.unitylog)
				Application.logMessageReceivedThreaded += delegate (string condition, string stackTrace, LogType type)
				{
					Log("Unity", condition + " " + stackTrace);
				};
			if (Config.config.logfile)
				File.WriteAllText(Path.Combine(modloader, "log"), "");
			AppDomain.CurrentDomain.AssemblyResolve += ResolveAssembly;
			Log("UML", "Preloading mods..");
			foreach (string file in from x in Directory.GetFiles(modss)
									where x.EndsWith(".dll")
									select x)
				PreloadMod(file, mods);
			if (File.Exists(Path.Combine(modloader, "autoload.dll")))
				PreloadMod(Path.Combine(modloader, "autoload.dll"), mods);
			Log("UML", "Loading mods..");
			int lastRemMods = 0;
			remainingMods = mods.Count;
			while (remainingMods > 0)
            {
				foreach (var mod in from x in mods
									where !x.isLoaded && x.dependecies.Count == 0
									select x)
				{
					Log("UML", $"Loading mod {mod.name} {mod.version} ({mod.version}) - {mod.modType}");
					object obj = Activator.CreateInstance(mod.type, null);
					if (!(obj is IMod mod1))
						Log("UML", " Failed to load mod (The Type with an attribute of ModInfo is not inhereting from IMod)");
					else
					{
						mod.StartInstance(mod1);
						foreach (Mod mod2 in from x in mods
											 where !x.isLoaded && x.dependecies.Contains(mod.guid)
											 select x)
							mod2.dependecies.Remove(mod.guid);
						remainingMods--;
					}
				}
				if(lastRemMods == remainingMods)
                {
					Log("UML", "Some mods could not be loaded, because they are missing a dependency:");
					foreach(var mod in from x in mods
									   where !x.isLoaded && x.dependecies.Count != 0
									   select x)
                    {
						Log("UML", $" Missing deps in mod {mod.name}:");
						foreach (var dep in mod.dependecies)
							Log("UML", $"  " + dep);
                    }
					break;
                }
				lastRemMods = remainingMods;
			}
			Log("UML", "Loaded all mods");
		}

		private void Update()
		{
			foreach(var mod in mods)
				if (mod.isLoaded)
					mod.mod.Update(Time.deltaTime);
			fadeSeconds += Time.deltaTime;
			if (Input.GetKeyDown(KeyCode.BackQuote))
				debugGuiOpen = !debugGuiOpen;
		}

		private void FixedUpdate()
		{
			foreach(var mod in mods)
				if (mod.isLoaded)
					mod.mod.FixedUpdate(Time.fixedDeltaTime);
		}

		private void OnGUI()
		{
			foreach (var mod in mods)
				if (mod.isLoaded)
					mod.mod.OnGUI();
			if (fadeSeconds < 6f)
			{
				GUIStyle middleright = new GUIStyle();
				middleright.normal.background = null;
				if (fadeSeconds < 5f)
					middleright.normal.textColor = Color.white;
				else
					middleright.normal.textColor = new Color(255f, 255f, 255f, (6f - fadeSeconds) / 1f);
				middleright.alignment = TextAnchor.MiddleRight;
				middleright.fontStyle = FontStyle.Bold;
				middleright.fontSize = 20;
				string loadedMods = "Loaded mods:";
				foreach(var mod in mods)
					loadedMods += $"\n{mod.name} ({mod.version}) by {mod.author}";
				GUI.Label(new Rect(0f, 0f, Screen.width, Screen.height), loadedMods, middleright);
			}
			GUIStyle bottomright = new GUIStyle();
			bottomright.normal.background = null;
			bottomright.normal.textColor = Color.white;
			bottomright.alignment = TextAnchor.LowerLeft;
			bottomright.fontStyle = FontStyle.Bold;
			bottomright.fontSize = 17;
			GUI.Label(new Rect(0f, 0f, Screen.width, Screen.height), $"UML by devilExE | Loaded {mods.Count} mods", bottomright);
			if (debugGuiOpen)
			{
				GUIStyle center = new GUIStyle();
				center.normal.textColor = Color.white;
				center.alignment = TextAnchor.UpperCenter;
				GUI.Box(new Rect(-5f, -5f, Screen.width + 10f, Screen.height + 10f), "");
				GUI.Label(new Rect(0f, 10f, Screen.width, Screen.height), "<size=25>[Universal Mod Loader]</size>", center);
				GUI.Label(new Rect(5f, 40f, Screen.width - 5f, Screen.height - 60f), LogString);
				debugCommand = GUI.TextArea(new Rect(0f, Screen.height - 20f, Screen.width, 20f), debugCommand);
				if (debugCommand.Contains("\n"))
				{
					debugCommand = debugCommand.Replace("\n", "");
					HandleCommand(debugCommand);
					debugCommand = "";
				}
			}
		}

		private void OnApplicationQuit()
		{
			foreach(var mod in mods)
				if (mod.isLoaded)
					mod.mod.OnApplicationQuit();
		}

		public HooksMB()
		{
			mods = new List<Mod>();
			debugGuiOpen = false;
			debugCommand = "";
		}

		public static Mod[] GetMods()
		{
			return instance.mods.ToArray();
		}

		private Assembly ResolveAssembly(object sender, ResolveEventArgs args)
		{
			Log("UML", " Resolving: " + args.Name);
			return Assembly.LoadFrom(Path.Combine(modloader, "deps", args.Name.Split(',')[0] + ".dll"));
		}

		private void HandleCommand(string command)
		{
			string[] args = command.Split(' ');
			Log("UML", "Command: " + command, true);
		}

		public static HooksMB instance;
		public static readonly string root = Application.dataPath + "/../";
		public static readonly string modloader = Path.Combine(root, "UML");
		public static readonly string modss = Path.Combine(modloader, "mods");
		private readonly List<Mod> mods;
		private float fadeSeconds;
		private int remainingMods;
		private bool debugGuiOpen;
		private string debugCommand;
	}

	public class Mod
	{
		public void StartInstance(IMod _mod)
		{
			mod = _mod;
			mod.Start();
			isLoaded = true;
		}

		public IMod mod { get; private set; }
		public bool isLoaded { get; private set; }

		public Mod(Assembly _asm, Type _type, string _name, string _author, string _version, string _guid, string[] _dependencies, ModType _modType, string _sha256)
		{
			asm = _asm;
			type = _type;
			name = _name;
			author = _author;
			version = _version;
			guid = _guid;
			dependecies = _dependencies.ToList<string>();
			modType = _modType;
			sha256 = _sha256;
		}

		public readonly Assembly asm;
		public readonly string name;
		public readonly string author;
		public readonly string version;
		public readonly string guid;
		public List<string> dependecies;
		public readonly Type type;
		public readonly ModType modType;
		public readonly string sha256;
	}

	private static class WinConsole
	{
		public static void Initialize(bool alwaysCreateNewConsole = true)
		{
			bool consoleAttached = true;
			if (alwaysCreateNewConsole
				|| (AttachConsole(ATTACH_PARRENT) == 0
				&& Marshal.GetLastWin32Error() != ERROR_ACCESS_DENIED))
				consoleAttached = AllocConsole() != 0;

			if (consoleAttached)
			{
				InitializeOutStream();
				InitializeInStream();
				SetConsoleTitle("[UML] Universal Mod Loader");
			}
		}

		private static void InitializeOutStream()
		{
			var fs = CreateFileStream("CONOUT$", GENERIC_WRITE, FILE_SHARE_WRITE, FileAccess.Write);
			if (fs == null)
				return;
			var writer = new StreamWriter(fs) { AutoFlush = true };
			Console.SetOut(writer);
			Console.SetError(writer);
		}

		private static void InitializeInStream()
		{
			var fs = CreateFileStream("CONIN$", GENERIC_READ, FILE_SHARE_READ, FileAccess.Read);
			if (fs != null)
				Console.SetIn(new StreamReader(fs));
		}

		private static FileStream CreateFileStream(string name, uint win32DesiredAccess, uint win32ShareMode,
								FileAccess dotNetFileAccess)
		{
			var file = new SafeFileHandle(CreateFileW(name, win32DesiredAccess, win32ShareMode, IntPtr.Zero, OPEN_EXISTING, FILE_ATTRIBUTE_NORMAL, IntPtr.Zero), true);
			if (!file.IsInvalid)
			{
				var fs = new FileStream(file, dotNetFileAccess);
				return fs;
			}
			return null;
		}

		[DllImport("kernel32.dll",
			EntryPoint = "AllocConsole",
			SetLastError = true,
			CharSet = CharSet.Auto,
			CallingConvention = CallingConvention.StdCall)]
		private static extern int AllocConsole();

		[DllImport("kernel32.dll",
			EntryPoint = "AttachConsole",
			SetLastError = true,
			CharSet = CharSet.Auto,
			CallingConvention = CallingConvention.StdCall)]
		private static extern uint AttachConsole(uint dwProcessId);

		[DllImport("kernel32.dll",
			EntryPoint = "CreateFileW",
			SetLastError = true,
			CharSet = CharSet.Auto,
			CallingConvention = CallingConvention.StdCall)]
		private static extern IntPtr CreateFileW(
			  string lpFileName,
			  uint dwDesiredAccess,
			  uint dwShareMode,
			  IntPtr lpSecurityAttributes,
			  uint dwCreationDisposition,
			  uint dwFlagsAndAttributes,
			  IntPtr hTemplateFile
			);

		[DllImport("kernel32.dll",
			EntryPoint = "SetConsoleTitle",
			SetLastError = true,
			CharSet = CharSet.Auto,
			CallingConvention = CallingConvention.StdCall)]
		private static extern bool SetConsoleTitle(string lpConsoleTitle);

		private const uint GENERIC_WRITE = 0x40000000;
		private const uint GENERIC_READ = 0x80000000;
		private const uint FILE_SHARE_READ = 0x00000001;
		private const uint FILE_SHARE_WRITE = 0x00000002;
		private const uint OPEN_EXISTING = 0x00000003;
		private const uint FILE_ATTRIBUTE_NORMAL = 0x80;
		private const uint ERROR_ACCESS_DENIED = 5;
		private const uint ATTACH_PARRENT = 0xFFFFFFFF;
	}

	public class Config
	{
		public Config(string[] lines)
		{
			foreach (string text in lines)
			{
				string label = text.Split('=')[0];
				string value = text.Split('=')[1];
				switch (label)
                {
					case "console":
						console = (value == "true");
						break;
					case "unitylog":
						unitylog = (value == "true");
						break;
					case "logfile":
						logfile = (value == "true");
						break;
					default:
						Log("Config", "[ERR] Unknown label: " + label);
						break;
                }
			}
			config = this;
		}

		public static Config config;
		public bool console = true;
		public bool unitylog = true;
		public bool logfile = true;
	}
}
