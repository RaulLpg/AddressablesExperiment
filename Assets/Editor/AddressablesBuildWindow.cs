using System;
using System.Collections.Generic;
using System.IO;
using UnityEditor;
using UnityEditor.AddressableAssets;
using UnityEditor.AddressableAssets.Build;
using UnityEditor.AddressableAssets.Settings;
using UnityEditor.AddressableAssets.Settings.GroupSchemas;
using UnityEngine;
using UnityEngine.AddressableAssets;

public class AddressablesBuildWindow : EditorWindow
{
    public static ContentTypeGroupSchema.ContentType BuildContentType;

    
    [MenuItem("Window/Addressables Build")]
    public static void ShowWindow()
    {
        //Show existing window instance. If one doesn't exist, make one.
        EditorWindow.GetWindow(typeof(AddressablesBuildWindow));
    }

    static AddressableAssetSettings GetSettingsObject(string settingsAsset)
    {
        // This step is optional, you can also use the default settings:
        //settings = AddressableAssetSettingsDefaultObject.Settings;

        AddressableAssetSettings settings
            = AssetDatabase.LoadAssetAtPath<ScriptableObject>(settingsAsset)
                as AddressableAssetSettings;

        if (settings == null)
            Debug.LogError($"{settingsAsset} couldn't be found or isn't " +
                           $"a settings object.");

        return settings;
    }


    void OnGUI()
    {
        GUILayout.Label("Current settings: " + AddressableAssetSettingsDefaultObject.Settings.name);

        if (GUILayout.Button("Build Main"))
        {
            BuildMainContent();
        }
        
        if (GUILayout.Button("Build Secondary"))
        {
            BuildSecondaryContent();
        }
    }

    private void BuildMainContent()
    {
        //string customSettingsPath = "Assets/ContentSettings/MainContentData/MainContentDataSettings.asset";
        ContentTypeGroupSchema.ContentType contentType = ContentTypeGroupSchema.ContentType.Main;

        //var customSettings = GetSettingsObject(customSettingsPath);
        var settings = GetSettingsObject("Assets/AddressableAssetsData/AddressableAssetSettings.asset");
        BuildContentType = contentType;

        SetProfile(settings, "Content");
        BuildContentWithSettings(settings, contentType);
    }

    private void BuildSecondaryContent()
    {
        //string customSettingsPath = "Assets/ContentSettings/MainContentData/MainContentDataSettings.asset";
        ContentTypeGroupSchema.ContentType contentType = ContentTypeGroupSchema.ContentType.Secondary;

        //var customSettings = GetSettingsObject(customSettingsPath);
        var settings = GetSettingsObject("Assets/AddressableAssetsData/AddressableAssetSettings.asset");

        BuildContentType = contentType;

        SetProfile(settings, "Content");
        BuildContentWithSettings(settings, contentType);
    }

    static Dictionary<AddressableAssetGroup, bool> includedInBuild = new Dictionary<AddressableAssetGroup, bool>();

    private static void BuildContentWithSettings(AddressableAssetSettings settings, ContentTypeGroupSchema.ContentType contentType)
    {
        foreach (var group in settings.groups)
        {
            var contentTypeSch = group.GetSchema<ContentTypeGroupSchema>();
            var Sch = group.GetSchema<BundledAssetGroupSchema>();
            includedInBuild[group] = Sch != null && Sch.IncludeInBuild;
            if (Sch != null)
            {
                if (contentTypeSch != null && contentTypeSch.contentType == contentType)
                {
                    Sch.IncludeInBuild = true;
                }
                else
                {
                    Sch.IncludeInBuild = false;
                }
            }
        }

        var buildContext = new AddressablesDataBuilderInput(settings);
        settings.ActivePlayerDataBuilder.BuildData<AddressablesPlayerBuildResult>(buildContext);

        foreach (var file in buildContext.Registry.GetFilePaths())
        {
            Debug.Log("Generated file: " + file);
        }


        foreach (var group in settings.groups)
        {
            var Sch = group.GetSchema<BundledAssetGroupSchema>();
            if (includedInBuild.ContainsKey(group) && Sch != null)
            {
                Sch.IncludeInBuild = includedInBuild[group];
            }
        }


        // Copy the AddressableAssetsData/[platform]/content_state.bin to the directory of our custom settings (could be anywhere)
        var contentStatePath = ContentUpdateScript.GetContentStateDataPath(false);
        var contentStateCustomPath = GetContentStateDataPathForContentType(contentType, false);
        File.Copy(contentStatePath, contentStateCustomPath, true);
    }

    static void SetProfile(AddressableAssetSettings settings, string profileName)
    {
        string profileId = settings.profileSettings.GetProfileId(profileName);
        if (string.IsNullOrEmpty(profileId))
            Debug.LogWarning($"Couldn't find a profile named, {profileName}, " +
                             $"using current profile instead.");
        else
            settings.activeProfileId = profileId;
    }

    public static string GetContentStateDataPathForContentType(ContentTypeGroupSchema.ContentType contentType, bool browse)
    {
        //var assetPath = settings.ConfigFolder;
        var assetPath = Path.Combine($"Assets/State/{contentType}", PlatformMappingService.GetPlatform().ToString());

        if (browse)
        {
            if (string.IsNullOrEmpty(assetPath))
                assetPath = Application.dataPath;

            assetPath = EditorUtility.OpenFilePanel("Build Data File", Path.GetDirectoryName(assetPath), "bin");

            if (string.IsNullOrEmpty(assetPath))
                return null;

            return assetPath;
        }

        Directory.CreateDirectory(assetPath);
        var path = Path.Combine(assetPath, "addressables_content_state.bin");
        return path;
    }
}