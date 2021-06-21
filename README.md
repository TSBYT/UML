# UML
 Universal Mod Loader (can be added to any Mono-compiled Unity Game).
 It's also multiplayer friendly, as it supports client-only and multiplayer (requiered to join a server/lobby) mods.
# API
 ## UML namespace
  UML contains a namespace: UML, which is the base for loading mods (they inherit and assign an attribute from here)
  ### UML.ModInfo
   This attribute class allows UML to know different things about your mod. ONLY one class needs this attribute.
   There are four ways to attribute a class with UML.ModInfo:
   ```cs
// Basic:
ModInfo("Mod Name", "Mod Author", "Mod Version");
   
// With GUID:
ModInfo("me.myname.mymod", "Mod Name", "Mod Author", "Mod Version");
// NOTE: it is recommended to keep the GUID (first parameter) lowercase, and in the following format:
//  me.myname.mymod   - it is for a general person, who doesn't own a domain. Example: me.devilexe.test-mod
//  tld.domain.mymod  - it is for a person or team, who own a domain. Example: go-ro.redline2.test-mod. if you own a subdomain, make the tld as domain-tld, like in the example
   
// With dependencies:
ModInfo("me.myname.mymod", "Mod Name", "Mod Author", "Mod Version", new string[] { "me.author.dep1", "me.another-author.dep2" });
// NOTE: you can supply needed mods guid, and UML will load the dependency before the mod itself
   
// With client/server side:
ModInfo("me.myname.mymod", "Mod Name", "Mod Author", "Mod Version", new string[] { "me.author.dep1", "me.another-author.dep2" }, UML.ModType.ClientOnly);
// NOTE: the example given marks a mod as client-only. You can use UML.ModType.Multiplayer to mark it needed for multiplayer. Feel free to add more to the enum
   ```
  
  ### UML.IMod
   This gives you acces to usefull functions. ONLY one class needs to inherit from this (the same that has an attribute of UML.ModInfo)
   Functions icluded in UML.IMod:
   ```cs
void IMod.Start();
void IMod.Update(float deltaTime);
void IMod.FixedUpdate(float fixedDeltaTime);
void IMod.OnGUI();
void IMod.OnApplicationQuit();
// NOTE: your class needs to implement all methods listed here
   ```
 
 ## Dependency loading
  UML allows for dependency loading.
  Basically, a needed dependency will load before loading the mod that is dependent on it. It also applies to dependencies itself.
  UML will load all the mods with no dependencies, and then remove the loaded mod guid from all unloaded mods dependency list, and then it will repeat the process.
  If no more mods load after one such process, UML will stop loading mods, and print missing dependencies to the console.
 
 ## Project External References
  UML allows any mod to use external dependencies (the ones that you add in the References category in VS).
  It will resolve all needed dependencies by taking their base name (for example 0Harmony), adding a .dll to its end (0Harmony.dll) and loading it from UML/deps
 
 ## Ingame console
  UML gives you a debug console that shows all the logs and allows for executing commands.
 
 ## Custom commands
  TBA (To Be Added).
  Currently you can only add commands to the UML itself.
 
 ## Logger
  UML allows mods to log to the console.
  ```cs
Log("My Mod", "Hello, world!");
  ```
  You can log only to the ingame console like this
  ```cs
Log("My Mod", "Hello, world!", true);
// NOTE: the third argument specifies logging only to the ingame console (true) or to the console and/or logfile (false)
  ```
  
 ## Config (UML/config)
  UML allows users to toggle different features.
  For boolean values, they are true if the text is EXACTLY `true`. Else, it will be treated as false (don't use `True`)
  Features:
  ### console
  Type: boolean
  Usage: Specifies rather to show an external console or not.
  ### unitylog
  Type: boolean
  Usage: Specifies rather to log Unity messages to the console or not.
  ### logfile
  Type: boolean
  Usage: Specifies rather to copy the output of the external console to a file (UML/log)
 
 # Creating a mod
 How to create a mod (really basic):
```cs
using UML;
using UnityEngine;
[ModInfo("My Mod", "devilExE", "0.0.0")
public class MyMod : IMod
{
    public void IMod.Start()
    {
        Log("My Mod", "Hello, world!");
    }
    public void IMod.Update(float deltaTime)
    {
         // execute input related tasks here
    }
    public void IMod.FixedUpdate(float fixedDeltaTime)
    {
         // execute physics related tasks here
    }
    public void IMod.OnGUI()
    {
        GUI.Label(new Rect(0f, 50f, 100f, 100f), "Hello, screen!");
    }
    public void IMod.OnApplicationQuit()
    {
        Log("My Mod", "Goodbye, world!");
    }
}
```
Make sure to include from Game_Data/Managed the following:
- Assembly-CSharp.dll
- System.\*.dll (except System.Core.dll)
- UnityEngine.\*.dll

# Importing UML into a game
1. Open Assembly-CSharp.dll (located at Game_Data/Managed) in DNSpy (or something similar)
2. Open up its treeview by clicking on the arrow to the right of it
3. Right click on the `-` namespace and select `Add Class (C#)`
4. Paste the contents of UML-namespace.cs into it and compile
5. Compile the dll by pressing Ctrl+Shift+S (or by clicking on the two diskets up at the top)
6. Right click on the `-` namespace and select `Add Class (C#)`
7. Paste the contents of UML.cs into it and compile
8. Compile the dll by pressing Ctrl+Shift+S (or by clicking on the two diskets up at the top)
9. Enjoy :)
NOTE: make sure to launch the game at least once before you start modding as it creates all the needed files
