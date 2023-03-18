using BeatSaberMarkupLanguage.GameplaySetup;
using HarmonyLib;
using IPA;
using IPA.Config.Stores;
using IPA.Loader;
using System;
using System.Reflection;
using UnityEngine.SceneManagement;
using IPALogger = IPA.Logging.Logger;

namespace NoteMovementFix

{
	[Plugin(RuntimeOptions.DynamicInit)]
	public class Plugin
	{
		internal static Plugin Instance;
		internal static IPALogger Log;
		internal static Harmony harmony;
		internal static bool InReplay = false;
		internal static bool Submission = true;

		static class BsmlWrapper
		{
			static readonly bool hasBsml = IPA.Loader.PluginManager.GetPluginFromId("BeatSaberMarkupLanguage") != null;

			public static void EnableUI()
			{
				try
				{
					void wrap() => GameplaySetup.instance.AddTab("NoteMovementFix", "NoteMovementFix.Views.settings.bsml", Config.Instance, MenuType.All);

					if (hasBsml)
					{
						wrap();
					}
				}
				catch (Exception e)
				{
					Log.Warn(e.Message);
				}

			}
			public static void DisableUI()
			{
				void wrap() => GameplaySetup.instance.RemoveTab("NoteMovementFix");

				if (hasBsml)
				{
					wrap();
				}
			}
		}

		[Init]
		public Plugin(IPALogger logger, IPA.Config.Config conf)
		{
			Instance = this;
			Log = logger;
			Config.Instance = conf.Generated<Config>();
			harmony = new Harmony("Loloppe.BeatSaber.NoteMovementFix");
		}

		[OnEnable]
		public void OnEnable()
		{
			SceneManager.activeSceneChanged += SceneManager_activeSceneChanged;
			harmony.PatchAll(Assembly.GetExecutingAssembly());
			BsmlWrapper.EnableUI();
		}

		private void SceneManager_activeSceneChanged(Scene arg0, Scene arg1)
		{
			if (arg1.name == "GameCore")
			{
				var scoresaber = PluginManager.GetPluginFromId("ScoreSaber");
				if(scoresaber != null)
                {
					MethodBase ScoreSaber_playbackEnabled = AccessTools.Method("ScoreSaber.Core.ReplaySystem.HarmonyPatches.PatchHandleHMDUnmounted:Prefix");
					if (ScoreSaber_playbackEnabled != null && (bool)ScoreSaber_playbackEnabled.Invoke(null, null) == false)
					{
						InReplay = true;
						return;
					}
				}
				
				var beatleader = PluginManager.GetPluginFromId("BeatLeader");
				if (beatleader != null)
				{
					var _replayStarted = beatleader?.Assembly.GetType("BeatLeader.Replayer.ReplayerLauncher")?
					.GetProperty("IsStartedAsReplay", BindingFlags.Static | BindingFlags.Public);
					if (_replayStarted != null && (bool)_replayStarted.GetValue(null, null))
					{
						InReplay = true;
						return;
					}
				}
			}

			InReplay = false;
		}

		[OnDisable]
		public void OnDisable()
		{
			SceneManager.activeSceneChanged -= SceneManager_activeSceneChanged;
			harmony.UnpatchSelf();
			BsmlWrapper.DisableUI();
		}
	}
}
