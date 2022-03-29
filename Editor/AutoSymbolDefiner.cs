using System;
using System.Collections;
using System.Collections.Generic;
using UnityEditor;
using UnityEditor.PackageManager;
using UnityEditor.PackageManager.Requests;
using UnityEditor.PackageManager.UI;
using UnityEngine;
using UnityEngine.UIElements;

namespace Arikan.Editor
{
    [InitializeOnLoad]
    public class AutoSymbolDefiner : IPackageManagerExtension
    {
        static ListRequest listRequest;

        [InitializeOnLoadMethod]
        [MenuItem("Tools/AutoSymbolDefiner DefineScripts")]
        public static void AutoSymbolDefiner_DefineScripts()
        {
            Debug.Log("AutoSymbolDefiner2 Initialized");
            Events.registeringPackages += OnRegisteredPackages;
            var list = UnityEditor.PackageManager.Client.List(true, true);
            EditorApplication.update += AutoSymbolDefinerProgress;
        }
        static void AutoSymbolDefinerProgress()
        {
            if (listRequest != null && listRequest.IsCompleted)
            {
                EditorApplication.update -= AutoSymbolDefinerProgress;
                if (listRequest.Status >= StatusCode.Failure)
                {
                    Debug.LogError(listRequest.Error.message);
                    listRequest = null;
                    return;
                }

                var list = listRequest.Result;
                foreach (var pck in list)
                {
                    var direct = pck.isDirectDependency && pck.source
                            != PackageSource.BuiltIn && pck.source
                            != PackageSource.Embedded;
                    if (direct)
                    {
                        Debug.Log("Package Adding: " + pck.name);
                        AddSymbol(pck);
                    }
                    else
                    {
                        Debug.Log("Package Skipping: " + pck.name);
                    }
                }

                Debug.Log("AutoSymbolDefiner2 Completed");
            }
            listRequest = null;
        }

        // [UnityEditor.Callbacks.DidReloadScripts]
        [InitializeOnLoadMethod]
        private static void OnScriptsReloaded()
        {
            Debug.Log("AutoSymbolDefiner Initialized 1");
            Events.registeringPackages += OnRegisteredPackages;
            Debug.Log("AutoSymbolDefiner Initialized 2");
            PackageManagerExtensions.RegisterExtension(new AutoSymbolDefiner());

        }

        private static void OnRegisteredPackages(PackageRegistrationEventArgs obj)
        {
            Debug.Log("Registered packages changed : " + obj.added.Count + " added, " + obj.removed.Count + " removed");
            foreach (var item in obj.added)
            {
                AddSymbol(item);
            }
            foreach (var item in obj.removed)
            {
                RemoveSymbol(item);
            }
        }

        public static void AddSymbol(UnityEditor.PackageManager.PackageInfo package)
        {
            var symbol = package.packageId.Replace(".", "_");

            var symbols = PlayerSettings.GetScriptingDefineSymbolsForGroup(EditorUserBuildSettings.selectedBuildTargetGroup);
            if (!symbols.Contains(symbol))
            {
                symbols += ";" + symbol;
                PlayerSettings.SetScriptingDefineSymbolsForGroup(EditorUserBuildSettings.selectedBuildTargetGroup, symbols);
            }
        }

        public static void RemoveSymbol(UnityEditor.PackageManager.PackageInfo package)
        {
            var symbol = package.packageId.Replace(".", "_");

            var symbols = PlayerSettings.GetScriptingDefineSymbolsForGroup(EditorUserBuildSettings.selectedBuildTargetGroup);
            if (symbols.Contains(symbol))
            {
                symbols = symbols.Replace(symbol, "");
                PlayerSettings.SetScriptingDefineSymbolsForGroup(EditorUserBuildSettings.selectedBuildTargetGroup, symbols);
            }
        }

        public VisualElement CreateExtensionUI()
        {
            Debug.Log("CreateExtensionUI");
            return null;
        }

        public void OnPackageSelectionChange(UnityEditor.PackageManager.PackageInfo packageInfo)
        {
            Debug.Log("Package selected : " + packageInfo.name);
        }

        public void OnPackageAddedOrUpdated(UnityEditor.PackageManager.PackageInfo packageInfo)
        {
            Debug.Log("Package added or updated : " + packageInfo.name);
            AddSymbol(packageInfo);
        }

        public void OnPackageRemoved(UnityEditor.PackageManager.PackageInfo packageInfo)
        {
            Debug.Log("Package removed : " + packageInfo.name);
            RemoveSymbol(packageInfo);
        }
    }

}

