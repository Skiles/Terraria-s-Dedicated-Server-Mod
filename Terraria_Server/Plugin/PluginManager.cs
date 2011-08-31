﻿using System;
using System.IO;
using System.Linq;
using System.Reflection;
using System.Collections.Generic;
using Terraria_Server.Events;
using System.Diagnostics;
using System.Reflection.Emit;

using Terraria_Server.Logging;

namespace Terraria_Server.Plugin
{
    /// <summary>
    /// PluginManager class.  Handles all input/output, loading, enabling, disabling, and hook processing for plugins
    /// [TODO] Reload plugin assembly, Not enable/disable.
    /// </summary>
    public class PluginManager
    {
        private String pluginPath = String.Empty;
        private String libraryPath = String.Empty;
        private Dictionary<String, Plugin> plugins;
        private Dictionary<String, Object> libraries;
        private Server server;

        /// <summary>
        /// Server's plugin list
        /// </summary>
        public Dictionary<String, Plugin> Plugins
        {
            get { return plugins; }
        }
        /// <summary>
        /// Server's plugin list
        /// </summary>
        public Dictionary<String, Object> Libraries
        {
            get { return libraries; }
        }
        
        /// <summary>
        /// PluginManager class constructor
        /// </summary>
        /// <param name="pluginPath">Path to plugin directory</param>
        /// <param name="libraryPath">Path to library directory</param>
        /// <param name="server">Current Server instance</param>
        public PluginManager(String pluginPath, String libraryPath, Server server)
        {
            this.pluginPath = pluginPath;
            this.libraryPath = libraryPath;
            this.server = server;

            plugins = new Dictionary<String, Plugin>();
            libraries = new Dictionary<String, Object>();
        }

        /// <summary>
        /// Initializes Plugin (Loads) and Checks for Out of Date Plugins.
        /// </summary>
        public void LoadAllPlugins()
        {
            LoadLibraries();
            LoadPlugins();

            CheckPlugins();

            ServerPluginsLoaded tdsmEvent = new ServerPluginsLoaded();
            tdsmEvent.PluginManager = this;
            processHook(Hooks.PLUGINS_LOADED, tdsmEvent);
        }

        public void CheckPlugins()
        {
            foreach (Plugin plugin in plugins.Values)
            {
                if (plugin.TDSMBuild != Statics.BUILD)
                {
                    ProgramLog.Admin.Log("[WARNING] Plugin Build Incorrect: " + plugin.Name); //Admin's responsibility.
                }
            }
        }

        public Object LoadLib(String Path, Type type)
        {
            try
            {
                Assembly assembly = null;
                using (FileStream fs = File.Open(Path, FileMode.Open))
                {

                    using (MemoryStream ms = new MemoryStream())
                    {

                        byte[] buffer = new byte[1024];

                        int read = 0;

                        while ((read = fs.Read(buffer, 0, 1024)) > 0)
                            ms.Write(buffer, 0, read);

                        assembly = Assembly.Load(ms.ToArray());

                    }

                }

                //some libs crash at GetTypes, Gotta fix (Should we load assembly or Initialize?)

                foreach (Type messageType in assembly.GetTypes()
                .Where(x => type.IsAssignableFrom(x) && x != type))
                {
                    if (!messageType.IsAbstract)
                    {
                        Object lib = (Object)Activator.CreateInstance(messageType);
                        if (lib == null)
                        {
                            throw new Exception("Could not Instantiate Library");
                        }
                        else
                        {
                            return lib;
                        }
                    }
                }                
                
            }
            catch (Exception exception)
            {
                ProgramLog.Log("Error Loading Library '" + Path + "'.");
                ProgramLog.Log("Library Load Exception '" + Path + "' : "
                    + exception.ToString());
            }

            return null;
        }
                
        /// <summary>
        /// Load the plugin located at the specified path.
        /// This only loads one plugin.
        /// </summary>
        /// <param name="pluginPath">Path to plugin</param>
        /// <returns>Instance of the successfully loaded plugin, otherwise null</returns>
        public Plugin loadPlugin(String pluginPath)
        {
            try
            {
                Plugin plugin = (Plugin)LoadLib(pluginPath, typeof(Plugin));
                if (plugin == null)
                {
                    throw new Exception("Could not Instantiate");
                }
                else
                {
                    plugin.Server = server;
                    plugin.Load();
                    return plugin;
                }
            }
            catch (Exception exception)
            {
                //Plugin Errors aren't our issue really, so .Error.Log Shouldn't be needed.
                ProgramLog.Log("Error Loading Plugin '" + pluginPath + "'. Is it up to Date?");
                ProgramLog.Log("Plugin Load Exception '" + pluginPath + "' : "
                    + exception.ToString());
            }

            return null;
        }

        public void LoadPlugins()
        {
            foreach (String file in Directory.GetFiles(pluginPath))
            {
                FileInfo fileInfo = new FileInfo(file);
                if (fileInfo.Extension.ToLower().Equals(".dll"))
                {
                    Plugin plugin = loadPlugin(file);
                    if (plugin != null)
                    {
                        plugins.Add(plugin.Name.ToLower().Trim(), plugin);
                    }
                }
            }
        }

        public void LoadLibraries()
        {
            foreach (String file in Directory.GetFiles(libraryPath))
            {
                FileInfo fileInfo = new FileInfo(file);
                if (fileInfo.Extension.ToLower().Equals(".dll"))
                {
                    Object library = (Object)LoadLib(file, typeof(Object));
                    if (library != null)
                    {
                        libraries.Add(fileInfo.Name, library);
                    }
                }
            }
        }

        /// <summary>
        /// Reloads all plugins currently running on the server
        /// </summary>
        public void ReloadPlugins()
        {
            DisablePlugins();

            LoadAllPlugins();
        }

        /// <summary>
        /// Enables all plugins available to the server
        /// </summary>
        public void EnablePlugins()
        {
            foreach (Plugin plugin in plugins.Values)
            {
                plugin.Enabled = true;
                plugin.Enable();
            }
        }

        /// <summary>
        /// Disables all plugins currently running on the server
        /// </summary>
        public void DisablePlugins()
        {
            foreach (Plugin plugin in plugins.Values)
            {
                plugin.Enabled = false;
                try
                {
                    plugin.Disable();
                }
                catch (Exception exception)
                {
                    ProgramLog.Error.Log("Plugin Disable Exception '" + pluginPath + "' : "
                    + exception.ToString());
                }
            }

            plugins.Clear();
            libraries.Clear();
        }
        
        /// <summary>
        /// Enables a plugin by name. Currently unused in core
        /// </summary>
        /// <param name="name">Plugin name</param>
        /// <returns>Returns true on plugin successfully Enabling</returns>
        public bool EnablePlugin(String name)
        {
        	String cleanedName = name.ToLower().Trim();
        	if(plugins.ContainsKey(cleanedName))
        	{
	        	Plugin plugin = plugins[cleanedName];
	            plugin.Enabled = true;
	            plugin.Enable();
	            return true;
        	}
            return false;
        }

        /// <summary>
        /// Disables a plugin by name.  Currently unused in core
        /// </summary>
        /// <param name="name">Name of plugin</param>
        /// <returns>Returns true on plugin successfully Disabling</returns>
        public bool DisablePlugin(String name)
        {
        	String cleanedName = name.ToLower().Trim();
        	if(plugins.ContainsKey(cleanedName))
        	{
	        	Plugin plugin = plugins[cleanedName];
	            plugin.Enabled = false;
	            plugin.Disable();
                plugin.pluginHooks.Clear();
                plugin.commands.Clear();
	            return true;
        	}
            return false;
        }

        /// <summary>
        /// Gets plugin instance by name.
        /// </summary>
        /// <param name="name">Plugin name</param>
        /// <returns>Returns found plugin if successful, otherwise returns null</returns>
        public Plugin GetPlugin(String name)
        {
        	String cleanedName = name.ToLower().Trim();
        	if(plugins.ContainsKey(cleanedName))
        	{
        		return plugins[cleanedName];
        	}
        	return null;
        }

        /// <summary>
        /// Gets the PluginManagers Loaded Plugins.
        /// </summary>
        /// <returns>Returns the current Dictionary of Plugins</returns>
        public Dictionary<String, Plugin> PluginList
        {
            get 
            {
                return plugins;
            }
        }
        
        /// <summary>
        /// Determines whether a plugins has registered a hook, then fires the appropriate method if so
        /// </summary>
        /// <param name="hook">Hook to process</param>
        /// <param name="hookEvent">Event instance to pass to any hooked methods</param>
        public void processHook(Hooks hook, Event hookEvent)
		{
            foreach (Plugin plugin in plugins.Values)
			{
				try
				{
                    if (plugin.containsHook(hook))
					{
                        switch (hook)
                        {
                            case Hooks.CONSOLE_COMMAND:
                                plugin.onConsoleCommand((ConsoleCommandEvent)hookEvent);
                                break;

                            case Hooks.PLAYER_COMMAND:
                                plugin.onPlayerCommand((PlayerCommandEvent)hookEvent);
                                break;

                            case Hooks.PLAYER_CHAT:
                                plugin.onPlayerChat((MessageEvent)hookEvent);
                                break;

                            case Hooks.PLAYER_PRELOGIN:
                                plugin.onPlayerPreLogin((PlayerLoginEvent)hookEvent);
                                break;

                            case Hooks.PLAYER_LOGIN:
                                plugin.onPlayerJoin((PlayerLoginEvent)hookEvent);
                                break;

                            case Hooks.PLAYER_LOGOUT:
                                plugin.onPlayerLogout((PlayerLogoutEvent)hookEvent);
                                break;

                            case Hooks.PLAYER_PARTYCHANGE:
                                plugin.onPlayerPartyChange((PartyChangeEvent)hookEvent);
                                break;

                            case Hooks.PLAYER_TILECHANGE:
                                plugin.onPlayerTileChange((PlayerTileChangeEvent)hookEvent);
                                break;

                            case Hooks.PLAYER_HURT:
                                plugin.onPlayerHurt((PlayerHurtEvent)hookEvent);
                                break;

                            case Hooks.PLAYER_CHEST:
                                plugin.onPlayerOpenChest((PlayerChestOpenEvent)hookEvent);
                                break;

                            case Hooks.PLAYER_DEATH:
                                plugin.onPlayerDeath((PlayerDeathEvent)hookEvent);
                                break;

                            case Hooks.DOOR_STATECHANGE:
                                plugin.onDoorStateChange((DoorStateChangeEvent)hookEvent);
                                break;

                            case Hooks.PLAYER_EDITSIGN:
                                plugin.onPlayerEditSign((PlayerEditSignEvent)hookEvent);
                                break;

                            case Hooks.PLAYER_PROJECTILE:
                                plugin.onPlayerProjectileUse((PlayerProjectileEvent)hookEvent);
                                break;

                            case Hooks.NPC_DEATH:
                                plugin.onNPCDeath((NPCDeathEvent)hookEvent);
                                break;

                            case Hooks.NPC_SPAWN:
                                plugin.onNPCSpawn((NPCSpawnEvent)hookEvent);
                                break;

                            case Hooks.PLAYER_TELEPORT:
                                plugin.onPlayerTeleport((PlayerTeleportEvent)hookEvent);
                                break;

                            case Hooks.PLAYER_MOVE:
                                plugin.onPlayerMove((PlayerMoveEvent)hookEvent);
                                break;

                            case Hooks.PLAYER_KEYPRESS:
                                plugin.onPlayerKeyPress((PlayerKeyPressEvent)hookEvent);
                                break;

                            case Hooks.PLAYER_PVPCHANGE:
                                plugin.onPlayerPvPChange((PlayerPvPChangeEvent)hookEvent);
                                break;

                            case Hooks.PLAYER_AUTH_QUERY:
                                plugin.onPlayerAuthQuery ((PlayerLoginEvent) hookEvent);
                                break;
                            
                            case Hooks.PLAYER_AUTH_REPLY:
                                plugin.onPlayerAuthReply ((PlayerLoginEvent) hookEvent);
                                break;

                            case Hooks.NPC_BOSSDEATH:
                                plugin.onNPCBossDeath((NPCBossDeathEvent)hookEvent);
                                break;

                            case Hooks.NPC_BOSSSUMMON:
                                plugin.onNPCBossSummon((NPCBossSummonEvent)hookEvent);
                                break;

                            case Hooks.PLAYER_FLOWLIQUID:
                                plugin.onPlayerFlowLiquid ((PlayerFlowLiquidEvent)hookEvent);
                                break;

                            case Hooks.PLAYER_CHESTBREAK:
                                plugin.onPlayerChestBreak((PlayerChestBreakEvent)hookEvent);
                                break;

                            case Hooks.TIME_CHANGED:
                                plugin.onTimeChange((TimeChangedEvent)hookEvent);
                                break;

                            case Hooks.PLUGINS_LOADED:
                                plugin.onServerPluginsLoaded((ServerPluginsLoaded)hookEvent);
                                break;
                        }
                    }
                }
                catch (Exception exception)
                {
                    ProgramLog.Log (exception, "Error passing event " + hook.ToString() + " to " + plugin.Name);
                }
            }
        }
    }
}
